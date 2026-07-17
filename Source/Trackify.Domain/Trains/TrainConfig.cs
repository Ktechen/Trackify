
namespace Trackify.Domain.Trains;

/// <summary>
/// The persisted, transport-agnostic configuration of a single train. Pure data shared by every
/// front-end (Uno app + CLI); runtime connection state (connected? status text) is deliberately NOT
/// here — that belongs to the presentation/control layer.
/// </summary>
public sealed record TrainConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public HubType Hub { get; set; } = HubType.PoweredUpHub;

    /// <summary>User-facing MAC/address text; free-form.</summary>
    public string BleAddress { get; set; } = "";

    /// <summary>Platform BLE device id captured during discovery (Android MAC / iOS UUID); the key used to connect.</summary>
    public string HubId { get; set; } = "";

    public LedColorType Color { get; set; } = LedColorType.Green;
    public DeviceType PortA { get; set; } = DeviceType.TrainMotor;
    public DeviceType PortB { get; set; } = DeviceType.None;
    public int Speed { get; set; } = 70;
    public SpeedFunctionType AccelFn { get; set; } = SpeedFunctionType.EaseOut;
    public string AccelExpression { get; set; } = "1-(1-x)^2";
    public SpeedFunctionType BrakeFn { get; set; } = SpeedFunctionType.EaseIn;
    public string BrakeExpression { get; set; } = "1-(1-x)^2";
    public bool IsActive { get; set; } = true;
}
