namespace Trackify.Application.Trains;

/// <summary>
/// Data-transfer view of a saved track segment. Mirrors <see cref="TrainDto"/>'s boundary role: the
/// Domain entity never leaks past the use-case layer. Map with <see cref="TrackSegmentMapping"/>.
/// </summary>
public sealed record TrackSegmentDto
{
    public Guid Id { get; init; }
    public string Name { get; set; } = "";
    public SegmentType Type { get; set; } = SegmentType.Straight;
    public int MaxSpeed { get; set; } = 70;
    public TrackDirection Direction { get; set; } = TrackDirection.Forward;
    public SpeedFunctionType AccelFn { get; set; } = SpeedFunctionType.EaseOut;
    public SpeedFunctionType BrakeFn { get; set; } = SpeedFunctionType.EaseIn;
    public SensorType Sensor { get; set; } = SensorType.None;
    public SensorActionType Action { get; set; } = SensorActionType.Notify;
    public int SlowTarget { get; set; } = 30;
    public Guid StrandId { get; set; } = Guid.Empty;
    public int Order { get; set; }
    public CurveDirection? Curve { get; set; }
    public Guid? BranchStrandId { get; set; }
    public CurveDirection? BranchDirection { get; set; }
    public SwitchRoute Route { get; set; } = SwitchRoute.Main;
    public Guid? BranchLinkSegmentId { get; set; }
}
