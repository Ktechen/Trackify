namespace Trackify.Domain.Tracks;

/// <summary>
/// Everything a point needs beyond its geometry: how it is standing, which motor throws it, where
/// the sensor that spots an approaching train is plugged in, and the rules that decide which way to
/// set it (<see cref="EntryRules"/>).
///
/// The hub and port fields are deliberately plain strings and nullable: a plan is built at the
/// kitchen table long before anything is wired up, and an unwired point is a perfectly valid plan.
/// </summary>
public sealed class SwitchConfig
{
    /// <summary>Which way the point sits when nothing has asked for anything else.</summary>
    public SwitchRoute DefaultRoute { get; set; } = SwitchRoute.Main;

    /// <summary>Hub address of the motor that throws this point; null while it is still hand-thrown.</summary>
    public string? MotorHubId { get; set; }

    /// <summary>Port on <see cref="MotorHubId"/> the motor is plugged into.</summary>
    public string? MotorPort { get; set; }

    /// <summary>What kind of sensor watches this point's approach.</summary>
    public SensorType SensorType { get; set; } = SensorType.None;

    /// <summary>Hub address the sensor is plugged into.</summary>
    public string? SensorHubId { get; set; }

    /// <summary>Port on <see cref="SensorHubId"/> the sensor is plugged into.</summary>
    public string? SensorPort { get; set; }

    /// <summary>For a colour sensor, which colour counts as the trigger.</summary>
    public LedColorType? SensorTrigger { get; set; }

    /// <summary>The automatic-routing rules, in the order they should be considered.</summary>
    public List<SwitchEntryRule> EntryRules { get; set; } = [];

    /// <summary>Whether a motor has been assigned, i.e. whether this point can be thrown at all.</summary>
    public bool IsMotorised => !string.IsNullOrWhiteSpace(MotorHubId) && !string.IsNullOrWhiteSpace(MotorPort);

    /// <summary>Whether a sensor has been assigned.</summary>
    public bool HasSensor => SensorType != SensorType.None
        && !string.IsNullOrWhiteSpace(SensorHubId)
        && !string.IsNullOrWhiteSpace(SensorPort);

    /// <summary>
    /// Which way the point must be set for a train arriving at <paramref name="fromPort"/> bound for
    /// <paramref name="towardPartId"/>. The first matching rule wins; destination-specific rules are
    /// considered before catch-all ones, so a general rule never shadows a specific one.
    /// </summary>
    public SwitchRoute ResolveRoute(string fromPort, Guid? towardPartId = null)
    {
        foreach (var rule in EntryRules)
        {
            if (rule.TowardPartId is null || towardPartId is null) continue;
            if (!string.Equals(rule.FromPort, fromPort, StringComparison.Ordinal)) continue;
            if (rule.TowardPartId == towardPartId) return rule.SetRoute;
        }

        foreach (var rule in EntryRules)
        {
            if (rule.TowardPartId is not null) continue;
            if (string.Equals(rule.FromPort, fromPort, StringComparison.Ordinal)) return rule.SetRoute;
        }

        return DefaultRoute;
    }
}
