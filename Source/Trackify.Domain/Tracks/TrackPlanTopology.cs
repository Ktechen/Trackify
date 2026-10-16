namespace Trackify.Domain.Tracks;

/// <summary>
/// The structure of a track plan: which parts are laid and which of their ports are plugged into
/// each other. Immutable — every edit returns a new topology, which is what makes undo/redo in the
/// editor a plain stack.
///
/// There are no coordinates here on purpose. See <see cref="TrackPlanWalker"/> for why.
/// </summary>
public sealed record TrackPlanTopology
{
    /// <summary>An empty plan.</summary>
    public static TrackPlanTopology Empty { get; } = new() { Parts = [], Links = [] };

    /// <summary>The laid parts.</summary>
    public required IReadOnlyList<TrackPlanPart> Parts { get; init; }

    /// <summary>How they are plugged together.</summary>
    public required IReadOnlyList<TrackPlanLink> Links { get; init; }

    /// <summary>Whether <paramref name="partId"/>'s <paramref name="portName"/> already has something in it.</summary>
    public bool IsPortUsed(Guid partId, string portName)
    {
        foreach (var link in Links)
        {
            if (link.Uses(partId, portName)) return true;
        }

        return false;
    }

    /// <summary>The part with <paramref name="partId"/>, or false when the plan has no such part.</summary>
    public bool TryGetPart(Guid partId, out TrackPlanPart part)
    {
        foreach (var candidate in Parts)
        {
            if (candidate.Id != partId) continue;

            part = candidate;
            return true;
        }

        part = default;
        return false;
    }

    /// <summary>Adds a part, leaving it unconnected.</summary>
    public TrackPlanTopology Add(TrackPlanPart part) => this with { Parts = [.. Parts, part] };

    /// <summary>Plugs two ports together.</summary>
    public TrackPlanTopology Connect(TrackPlanLink link) => this with { Links = [.. Links, link] };

    /// <summary>Removes a part and every connection that touched it.</summary>
    public TrackPlanTopology Remove(Guid partId) => this with
    {
        Parts = [.. Parts.Where(part => part.Id != partId)],
        Links = [.. Links.Where(link => link.FromPartId != partId && link.ToPartId != partId)],
    };

    /// <summary>Unplugs one connection, leaving both parts in place.</summary>
    public TrackPlanTopology Disconnect(Guid partId, string portName) => this with
    {
        Links = [.. Links.Where(link => !link.Uses(partId, portName))],
    };
}
