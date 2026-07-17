
namespace Trackify.Domain.Trains;

/// <summary>
/// The persisted, transport-agnostic configuration of one track segment (drive behaviour + sensor).
/// Pure data: the canvas geometry/SVG and German labels that the planner renders live in the
/// presentation layer, not here.
/// </summary>
public sealed record TrackSegmentConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public SegmentType Type { get; set; } = SegmentType.Straight;
    public int MaxSpeed { get; set; } = 70;
    public TrackDirection Direction { get; set; } = TrackDirection.Forward;
    public SpeedFunctionType AccelFn { get; set; } = SpeedFunctionType.EaseOut;
    public SpeedFunctionType BrakeFn { get; set; } = SpeedFunctionType.EaseIn;
    public SensorType Sensor { get; set; } = SensorType.None;
    public SensorActionType Action { get; set; } = SensorActionType.Notify;
    public int SlowTarget { get; set; } = 30;
}
