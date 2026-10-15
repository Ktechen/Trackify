using System.Globalization;

namespace Trackify.Application.TrackPlans;

/// <inheritdoc cref="ITrackPlanExchangeService" />
public sealed class TrackPlanExchangeService(ITrackPlanRepository repository) : ITrackPlanExchangeService
{
    public TrackPlanImportResult Validate(string json)
        => TrackPlanSerializer.TryRead(json, out var document, out var error)
            ? Inspect(document, out _)
            : TrackPlanImportResult.Failed(error);

    public async Task<TrackPlanImportResult> ImportAsync(
        string json, bool replace, CancellationToken cancellationToken = default)
    {
        if (!TrackPlanSerializer.TryRead(json, out var document, out var error))
        {
            return TrackPlanImportResult.Failed(error);
        }

        var result = Inspect(document, out var snapshot);
        if (!result.Succeeded) return result;

        if (replace)
        {
            await repository.ReplaceAsync(snapshot, cancellationToken);
        }
        else
        {
            await repository.AppendAsync(snapshot, cancellationToken);
        }

        return result;
    }

    public async Task<string> ExportAsync(string planName = "", CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.LoadAsync(cancellationToken);
        var ordered = snapshot.Parts.OrderBy(part => part.PlanOrder).ToList();

        // Document-local ids, so the file reads as p1..pN instead of a wall of GUIDs.
        var localIds = new Dictionary<Guid, string>(ordered.Count);
        for (var index = 0; index < ordered.Count; index++)
        {
            localIds[ordered[index].Id] = $"p{index + 1}";
        }

        return TrackPlanSerializer.Write(new TrackPlanDocument
        {
            Name = planName,
            Parts = [.. ordered.Select(part => ToDocument(part, localIds))],
            Connections =
            [
                .. snapshot.Links
                    .Where(link => localIds.ContainsKey(link.FromPartId) && localIds.ContainsKey(link.ToPartId))
                    .Select(link => new TrackPlanConnectionDocument
                    {
                        From = $"{localIds[link.FromPartId]}:{link.FromPort}",
                        To = $"{localIds[link.ToPartId]}:{link.ToPort}",
                    }),
            ],
        });
    }

    public async Task<TrackPlanSummary> SummariseAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.LoadAsync(cancellationToken);
        if (snapshot.Parts.Count == 0) return TrackPlanSummary.Empty;

        var layout = snapshot.ToLayout();
        var switches = snapshot.Parts.Where(part => part.Switch is not null).ToList();

        return new TrackPlanSummary
        {
            PartCount = snapshot.Parts.Count,
            ConnectionCount = snapshot.Links.Count,
            OpenEndCount = layout.OpenEnds.Count,
            SwitchCount = switches.Count,
            MotorisedSwitchCount = switches.Count(part => part.Switch!.IsMotorised),
            SensorCount = snapshot.Parts.Count(part => part.Sensor != SensorType.None),
            TotalLengthStuds = snapshot.Parts
                .Where(part => TrackPartCatalog.TryGet(part.SpecKey, out _))
                .Sum(part => TrackPartCatalog.Get(part.SpecKey).TotalLengthStuds),
            IsBuildable = layout.IsBuildable,
            PartsByKey = snapshot.Parts
                .GroupBy(part => part.SpecKey, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal),
            Problems = [.. layout.Mismatches.Select(Describe)],
        };
    }

    // Validates a document and, when it holds together, builds the snapshot it describes.
    private static TrackPlanImportResult Inspect(TrackPlanDocument document, out TrackPlanSnapshot snapshot)
    {
        snapshot = TrackPlanSnapshot.Empty;

        if (!string.Equals(document.Format, TrackPlanDocument.FormatName, StringComparison.OrdinalIgnoreCase))
        {
            return TrackPlanImportResult.Failed(
                $"Not a track plan: expected format '{TrackPlanDocument.FormatName}', found '{document.Format}'.");
        }

        if (document.Version is < 1 or > TrackPlanDocument.CurrentVersion)
        {
            return TrackPlanImportResult.Failed(
                $"Plan version {document.Version} is not supported; this build reads version "
                + $"1..{TrackPlanDocument.CurrentVersion}.");
        }

        var errors = new List<string>();
        var warnings = new List<string>();
        var parts = ReadParts(document, errors);

        if (document.Parts.Count == 0) errors.Add("The plan has no parts.");

        var links = ReadConnections(document, parts, errors);

        if (errors.Count > 0)
        {
            return new TrackPlanImportResult { Succeeded = false, Errors = errors, PlanName = document.Name };
        }

        var built = new TrackPlanSnapshot
        {
            Parts = [.. parts.Values.Select(entry => entry.Part)],
            Links = links,
        };

        ResolveEntryRuleTargets(document, parts);

        var layout = built.ToLayout();
        foreach (var mismatch in layout.Mismatches) warnings.Add(Describe(mismatch));

        if (!string.Equals(document.Units, "stud", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"Unknown unit '{document.Units}'; lengths were read as studs.");
        }

        snapshot = built;

        return new TrackPlanImportResult
        {
            Succeeded = true,
            PlanName = document.Name,
            PartCount = built.Parts.Count,
            ConnectionCount = built.Links.Count,
            OpenEndCount = layout.OpenEnds.Count,
            Warnings = warnings,
        };
    }

    private static Dictionary<string, PartEntry> ReadParts(TrackPlanDocument document, List<string> errors)
    {
        var parts = new Dictionary<string, PartEntry>(document.Parts.Count, StringComparer.Ordinal);
        var order = 0;

        foreach (var documentPart in document.Parts)
        {
            if (string.IsNullOrWhiteSpace(documentPart.Id))
            {
                errors.Add("A part has no id.");
                continue;
            }

            if (documentPart.Id.Contains(':', StringComparison.Ordinal))
            {
                errors.Add($"Part id '{documentPart.Id}' must not contain ':'.");
                continue;
            }

            if (!TrackPartCatalog.TryGet(documentPart.Part, out var spec))
            {
                errors.Add($"Part '{documentPart.Id}' is an unknown track part '{documentPart.Part}'.");
                continue;
            }

            if (parts.ContainsKey(documentPart.Id))
            {
                errors.Add($"Duplicate part id '{documentPart.Id}'.");
                continue;
            }

            parts[documentPart.Id] = new PartEntry(ToEntity(documentPart, spec, order++), spec);
        }

        return parts;
    }

    private static List<TrackLink> ReadConnections(
        TrackPlanDocument document, Dictionary<string, PartEntry> parts, List<string> errors)
    {
        var links = new List<TrackLink>(document.Connections.Count);
        var occupied = new HashSet<string>(StringComparer.Ordinal);

        foreach (var connection in document.Connections)
        {
            if (!TryEndpoint(connection.From, parts, errors, out var from)) continue;
            if (!TryEndpoint(connection.To, parts, errors, out var to)) continue;

            // A port takes exactly one thing; two connections into one port is not a plan, it is a
            // contradiction, and silently keeping the first would hide it.
            if (!occupied.Add(connection.From)) errors.Add($"Port '{connection.From}' is connected more than once.");
            if (!occupied.Add(connection.To)) errors.Add($"Port '{connection.To}' is connected more than once.");

            links.Add(new TrackLink
            {
                FromPartId = from.PartId,
                FromPort = from.PortName,
                ToPartId = to.PartId,
                ToPort = to.PortName,
            });
        }

        return links;
    }

    private static bool TryEndpoint(
        string endpoint, Dictionary<string, PartEntry> parts, List<string> errors, out Endpoint resolved)
    {
        resolved = default;

        var separator = endpoint.IndexOf(':', StringComparison.Ordinal);
        if (separator <= 0 || separator == endpoint.Length - 1)
        {
            errors.Add($"Connection endpoint '{endpoint}' is not in the form 'part:port'.");
            return false;
        }

        var partId = endpoint[..separator];
        var portName = endpoint[(separator + 1)..];

        if (!parts.TryGetValue(partId, out var entry))
        {
            errors.Add($"Connection endpoint '{endpoint}' names a part that is not in the plan.");
            return false;
        }

        if (!entry.Spec.HasPort(portName))
        {
            errors.Add($"Connection endpoint '{endpoint}': a '{entry.Spec.Key}' has no port '{portName}'.");
            return false;
        }

        resolved = new Endpoint(entry.Part.Id, portName);
        return true;
    }

    // Entry rules point at a part by document-local id, so they can only be resolved once every
    // part has an identity.
    private static void ResolveEntryRuleTargets(TrackPlanDocument document, Dictionary<string, PartEntry> parts)
    {
        foreach (var documentPart in document.Parts)
        {
            if (documentPart.Switch is null) continue;
            if (!parts.TryGetValue(documentPart.Id, out var entry)) continue;
            if (entry.Part.Switch is not { } config) continue;

            var documentRules = documentPart.Switch.EntryRules;
            var shared = Math.Min(config.EntryRules.Count, documentRules.Count);

            for (var index = 0; index < shared; index++)
            {
                if (documentRules[index].TowardPart is { } towardId && parts.TryGetValue(towardId, out var target))
                {
                    config.EntryRules[index].TowardPartId = target.Part.Id;
                }
            }
        }
    }

    private static TrackPart ToEntity(TrackPlanPartDocument document, TrackPartSpec spec, int order)
    {
        var behaviour = document.Behaviour ?? new TrackPlanBehaviourDocument();

        return new TrackPart
        {
            Id = Guid.CreateVersion7(),
            SpecKey = spec.Key,
            Name = document.Name,
            PlanOrder = order,
            MaxSpeed = behaviour.MaxSpeed,
            Direction = behaviour.Direction,
            AccelFn = behaviour.AccelFn,
            BrakeFn = behaviour.BrakeFn,
            Sensor = behaviour.Sensor,
            Action = behaviour.Action,
            SlowTarget = behaviour.SlowTarget,
            Switch = document.Switch is null ? null : ToEntity(document.Switch),
        };
    }

    private static SwitchConfig ToEntity(TrackPlanSwitchDocument document) => new()
    {
        DefaultRoute = document.DefaultRoute,
        MotorHubId = document.MotorHubId,
        MotorPort = document.MotorPort,
        SensorType = document.SensorType,
        SensorHubId = document.SensorHubId,
        SensorPort = document.SensorPort,
        SensorTrigger = document.SensorTrigger,
        EntryRules =
        [
            .. document.EntryRules.Select(rule => new SwitchEntryRule
            {
                FromPort = rule.FromPort,
                SetRoute = rule.SetRoute,
            }),
        ],
    };

    private static TrackPlanPartDocument ToDocument(TrackPart part, Dictionary<Guid, string> localIds) => new()
    {
        Id = localIds[part.Id],
        Part = part.SpecKey,
        Name = part.Name,
        Behaviour = new TrackPlanBehaviourDocument
        {
            MaxSpeed = part.MaxSpeed,
            Direction = part.Direction,
            AccelFn = part.AccelFn,
            BrakeFn = part.BrakeFn,
            Sensor = part.Sensor,
            Action = part.Action,
            SlowTarget = part.SlowTarget,
        },
        Switch = part.Switch is null ? null : ToDocument(part.Switch, localIds),
    };

    private static TrackPlanSwitchDocument ToDocument(SwitchConfig config, Dictionary<Guid, string> localIds) => new()
    {
        DefaultRoute = config.DefaultRoute,
        MotorHubId = config.MotorHubId,
        MotorPort = config.MotorPort,
        SensorType = config.SensorType,
        SensorHubId = config.SensorHubId,
        SensorPort = config.SensorPort,
        SensorTrigger = config.SensorTrigger,
        EntryRules =
        [
            .. config.EntryRules.Select(rule => new TrackPlanEntryRuleDocument
            {
                FromPort = rule.FromPort,
                TowardPart = rule.TowardPartId is { } toward && localIds.TryGetValue(toward, out var local)
                    ? local
                    : null,
                SetRoute = rule.SetRoute,
            }),
        ],
    };

    private static string Describe(TrackJoinMismatch mismatch)
    {
        var gap = mismatch.GapStuds.ToString("0.0", CultureInfo.InvariantCulture);

        return mismatch.HeadingStepsOff == 0
            ? $"A joint does not line up: {gap} studs apart."
            : $"A joint does not line up: {gap} studs apart and "
                + $"{Math.Abs(mismatch.HeadingStepsOff)} curve(s) off. The loop does not really close.";
    }

    /// <summary>A part being read, paired with the catalogue geometry its ports are checked against.</summary>
    private readonly record struct PartEntry(TrackPart Part, TrackPartSpec Spec);

    /// <summary>A resolved "part:port" endpoint.</summary>
    private readonly record struct Endpoint(Guid PartId, string PortName);
}
