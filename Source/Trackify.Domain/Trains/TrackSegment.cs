namespace Trackify.Domain.Trains;

/// <summary>
/// The persisted, transport-agnostic configuration of one track segment (drive behaviour + sensor).
/// Pure data: the canvas geometry/SVG and German labels that the planner renders live in the
/// presentation layer, not here.
///
/// Segments form strands: an ordered chain (<see cref="StrandId"/> + <see cref="Order"/>) that a
/// part is always appended to at its open (last) end. A <see cref="SegmentType.Switch"/> segment is
/// the trunk continuing straight through the switch point *and* the anchor a second, branch strand
/// (<see cref="BranchStrandId"/>) starts from — so a switch never needs its own geometry, only a
/// pointer to where its branch begins.
/// </summary>
public sealed record TrackSegment : BaseEntity
{
    public string Name { get; set; } = "";
    public SegmentType Type { get; set; } = SegmentType.Straight;
    public int MaxSpeed { get; set; } = 70;
    public TrackDirection Direction { get; set; } = TrackDirection.Forward;
    public SpeedFunctionType AccelFn { get; set; } = SpeedFunctionType.EaseOut;
    public SpeedFunctionType BrakeFn { get; set; } = SpeedFunctionType.EaseIn;
    public SensorType Sensor { get; set; } = SensorType.None;
    public SensorActionType Action { get; set; } = SensorActionType.Notify;
    public int SlowTarget { get; set; } = 30;

    /// <summary>Which strand this segment belongs to. The main strand uses <see cref="Guid.Empty"/>.</summary>
    public Guid StrandId { get; set; } = Guid.Empty;

    /// <summary>Position within <see cref="StrandId"/>, 0-based in travel order.</summary>
    public int Order { get; set; }

    /// <summary>Set when <see cref="Type"/> is <see cref="SegmentType.Curve"/>: which way it bends.</summary>
    public CurveDirection? Curve { get; set; }

    /// <summary>Switch-only: the strand id of the branch this switch creates.</summary>
    public Guid? BranchStrandId { get; set; }

    /// <summary>Switch-only: which way the branch peels off from the trunk.</summary>
    public CurveDirection? BranchDirection { get; set; }

    /// <summary>Switch-only: which route is currently thrown ("Gestellt auf" in the planner).</summary>
    public SwitchRoute Route { get; set; } = SwitchRoute.Main;

    /// <summary>Switch-only, optional: the segment in another strand the branch's open end connects
    /// back to, closing a loop ("Zweig-Ende anschließen an" in the planner).</summary>
    public Guid? BranchLinkSegmentId { get; set; }
}
