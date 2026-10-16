namespace Trackify.Domain.Tracks;

/// <summary>
/// A laid, saved part: which catalogue piece it is, how a train should behave over it, and — for a
/// point — how it should be thrown (<see cref="Switch"/>).
///
/// Distinct from <see cref="TrackPlanPart"/> on purpose: that one is the bare identity plus
/// catalogue key the geometry needs, keeping <see cref="TrackPlanWalker"/> and
/// <see cref="TrackSnapper"/> free of any dependency on drive behaviour. Convert with
/// <see cref="ToPlanPart"/>.
///
/// A record because <see cref="BaseEntity"/> is one; note that the generated equality therefore
/// compares <see cref="Switch"/> by reference.
/// </summary>
public sealed record TrackPart : BaseEntity
{
    /// <summary>Which catalogue part this is — see <see cref="TrackPartCatalog"/>.</summary>
    public string SpecKey { get; set; } = TrackPartCatalog.StraightKey;

    /// <summary>What the planner calls it ("Kurve rechts 3").</summary>
    public string Name { get; set; } = "";

    /// <summary>Stable display order, so lists and the layout root do not shuffle between loads.</summary>
    public int PlanOrder { get; set; }

    /// <summary>Top speed over this part.</summary>
    public int MaxSpeed { get; set; } = 70;

    /// <summary>Which way a train runs over it.</summary>
    public TrackDirection Direction { get; set; } = TrackDirection.Forward;

    /// <summary>Acceleration curve.</summary>
    public SpeedFunctionType AccelFn { get; set; } = SpeedFunctionType.EaseOut;

    /// <summary>Braking curve.</summary>
    public SpeedFunctionType BrakeFn { get; set; } = SpeedFunctionType.EaseIn;

    /// <summary>A sensor sitting on this part, if any.</summary>
    public SensorType Sensor { get; set; } = SensorType.None;

    /// <summary>What that sensor should cause.</summary>
    public SensorActionType Action { get; set; } = SensorActionType.Notify;

    /// <summary>Target speed when the sensor action is to slow down.</summary>
    public int SlowTarget { get; set; } = 30;

    /// <summary>Point configuration; null for every part that is not a point.</summary>
    public SwitchConfig? Switch { get; set; }

    /// <summary>This part as the geometry layer wants it.</summary>
    public TrackPlanPart ToPlanPart() => new(Id, SpecKey);
}
