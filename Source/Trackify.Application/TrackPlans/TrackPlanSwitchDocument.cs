namespace Trackify.Application.TrackPlans;

/// <summary>A point's stored configuration: how it sits, what throws it, what watches it, how to route it.</summary>
public sealed record TrackPlanSwitchDocument
{
    /// <summary>Where the point sits when no rule applies.</summary>
    public SwitchRoute DefaultRoute { get; init; } = SwitchRoute.Main;

    /// <summary>Hub the throwing motor is on.</summary>
    public string? MotorHubId { get; init; }

    /// <summary>Port the throwing motor is plugged into.</summary>
    public string? MotorPort { get; init; }

    /// <summary>Kind of sensor watching the approach.</summary>
    public SensorType SensorType { get; init; } = SensorType.None;

    /// <summary>Hub the sensor is on.</summary>
    public string? SensorHubId { get; init; }

    /// <summary>Port the sensor is plugged into.</summary>
    public string? SensorPort { get; init; }

    /// <summary>Colour that counts as the trigger, for a colour sensor.</summary>
    public LedColorType? SensorTrigger { get; init; }

    /// <summary>Automatic-routing rules, in priority order.</summary>
    public IReadOnlyList<TrackPlanEntryRuleDocument> EntryRules { get; init; } = [];
}
