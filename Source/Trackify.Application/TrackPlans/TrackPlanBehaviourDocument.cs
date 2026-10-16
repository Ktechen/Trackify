namespace Trackify.Application.TrackPlans;

/// <summary>
/// How a train should behave over one part. Kept separate from the part's identity because the two
/// are different kinds of fact: a photograph can tell you the topology but never the intended speed.
/// </summary>
public sealed record TrackPlanBehaviourDocument
{
    /// <summary>Top speed over the part.</summary>
    public int MaxSpeed { get; init; } = 70;

    /// <summary>Travel direction.</summary>
    public TrackDirection Direction { get; init; } = TrackDirection.Forward;

    /// <summary>Acceleration curve.</summary>
    public SpeedFunctionType AccelFn { get; init; } = SpeedFunctionType.EaseOut;

    /// <summary>Braking curve.</summary>
    public SpeedFunctionType BrakeFn { get; init; } = SpeedFunctionType.EaseIn;

    /// <summary>Sensor on the part, if any.</summary>
    public SensorType Sensor { get; init; } = SensorType.None;

    /// <summary>What the sensor triggers.</summary>
    public SensorActionType Action { get; init; } = SensorActionType.Notify;

    /// <summary>Target speed when the action is to slow down.</summary>
    public int SlowTarget { get; init; } = 30;
}
