namespace Trackify.Application.Trains;

/// <summary>
/// Data-transfer view of a saved train. This is what crosses the Application boundary to the
/// front-ends (CLI / Uno app), so the Domain entity <see cref="Train"/> never leaks past the
/// use-case layer. It carries the configuration fields a front-end reads or edits; persistence
/// audit fields (Id timestamps) stay on the Domain entity. Map with the extensions in
/// <see cref="TrainMapping"/> (<c>ToDto</c> / <c>ToEntity</c>).
/// </summary>
public sealed record TrainDto
{
    public Guid Id { get; init; }
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
