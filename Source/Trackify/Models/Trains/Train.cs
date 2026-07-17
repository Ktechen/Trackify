
namespace Trackify.Models.Trains;

public partial class Train : ObservableObject
{
    [ObservableProperty] private string id = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private HubType hub = HubType.PoweredUpHub;
    [ObservableProperty] private string bleAddress = "";
    // Platform BLE device id captured during discovery (Android MAC / iOS UUID); the key used to connect.
    [ObservableProperty] private string hubId = "";
    [ObservableProperty] private LedColorType color = LedColorType.Green;
    [ObservableProperty] private DeviceType portA = DeviceType.TrainMotor;
    [ObservableProperty] private DeviceType portB = DeviceType.None;
    [ObservableProperty] private int speed = 70;
    [ObservableProperty] private SpeedFunctionType accelFn = SpeedFunctionType.EaseOut;
    [ObservableProperty] private string accelExpression = "1-(1-x)^2";
    [ObservableProperty] private SpeedFunctionType brakeFn = SpeedFunctionType.EaseIn;
    [ObservableProperty] private string brakeExpression = "1-(1-x)^2";
    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private bool isHardwareConnected;
    [ObservableProperty] private string connectionStatus = "Nicht verbunden";

    public string ColorHex => LegoinoCatalog.Color(Color).Hex;

    public string HubShort => LegoinoCatalog.Hub(Hub).Short;

    public string PortSummary => $"{HubShort} · {LegoinoCatalog.Device(PortA).ListLabel}/{LegoinoCatalog.Device(PortB).ListLabel}";

    public string StatusLabel => IsActive ? "Aktiv" : "Inaktiv";

    public string SpeedLabel => (Speed > 0 ? "+" : "") + Speed;

    public string SpeedDirectionLabel => Speed < 0 ? "rückwärts" : Speed > 0 ? "vorwärts" : "Stopp";

    public string HubToken => "HubType." + Hub;

    public string PortAToken => "DeviceType." + PortA;

    public string PortBToken => "DeviceType." + PortB;

    public string ColorToken => "Color." + Color;

    public bool IsAccelCustom => AccelFn == SpeedFunctionType.Custom;

    public bool IsBrakeCustom => BrakeFn == SpeedFunctionType.Custom;

    public bool IsAccelFormulaValid => AccelFn != SpeedFunctionType.Custom || SpeedFunction.TryCompile(AccelExpression, out _);

    public bool IsBrakeFormulaValid => BrakeFn != SpeedFunctionType.Custom || SpeedFunction.TryCompile(BrakeExpression, out _);

    public string AccelFormulaDisplay => AccelFn == SpeedFunctionType.Custom
        ? (IsAccelFormulaValid ? $"f(x) = {AccelExpression}" : "⚠ ungültige Formel")
        : LegoinoCatalog.SpeedFunction(AccelFn).Formula;

    public string BrakeFormulaDisplay => BrakeFn == SpeedFunctionType.Custom
        ? (IsBrakeFormulaValid ? $"f(x) = {BrakeExpression}" : "⚠ ungültige Formel")
        : LegoinoCatalog.SpeedFunction(BrakeFn).Formula;

    public SpeedProfileGraph Graph => SpeedCurve.BuildGraph(
        Math.Abs((int)Speed) / 100.0,
        SpeedFunction.ResolvePhaseFunction(AccelFn, AccelExpression),
        SpeedFunction.ResolvePhaseFunction(BrakeFn, BrakeExpression));

    partial void OnColorChanged(LedColorType value)
    {
        OnPropertyChanged(nameof(ColorHex));
        OnPropertyChanged(nameof(ColorToken));
    }

    partial void OnHubChanged(HubType value)
    {
        OnPropertyChanged(nameof(HubShort));
        OnPropertyChanged(nameof(PortSummary));
        OnPropertyChanged(nameof(HubToken));
    }

    partial void OnPortAChanged(DeviceType value)
    {
        OnPropertyChanged(nameof(PortSummary));
        OnPropertyChanged(nameof(PortAToken));
    }

    partial void OnPortBChanged(DeviceType value)
    {
        OnPropertyChanged(nameof(PortSummary));
        OnPropertyChanged(nameof(PortBToken));
    }

    partial void OnIsActiveChanged(bool value) => OnPropertyChanged(nameof(StatusLabel));

    partial void OnSpeedChanged(int value)
    {
        OnPropertyChanged(nameof(SpeedLabel));
        OnPropertyChanged(nameof(SpeedDirectionLabel));
        OnPropertyChanged(nameof(Graph));
    }

    partial void OnAccelFnChanged(SpeedFunctionType value)
    {
        OnPropertyChanged(nameof(IsAccelCustom));
        OnPropertyChanged(nameof(IsAccelFormulaValid));
        OnPropertyChanged(nameof(AccelFormulaDisplay));
        OnPropertyChanged(nameof(Graph));
    }

    partial void OnAccelExpressionChanged(string value)
    {
        OnPropertyChanged(nameof(IsAccelFormulaValid));
        OnPropertyChanged(nameof(AccelFormulaDisplay));
        OnPropertyChanged(nameof(Graph));
    }

    partial void OnBrakeFnChanged(SpeedFunctionType value)
    {
        OnPropertyChanged(nameof(IsBrakeCustom));
        OnPropertyChanged(nameof(IsBrakeFormulaValid));
        OnPropertyChanged(nameof(BrakeFormulaDisplay));
        OnPropertyChanged(nameof(Graph));
    }

    partial void OnBrakeExpressionChanged(string value)
    {
        OnPropertyChanged(nameof(IsBrakeFormulaValid));
        OnPropertyChanged(nameof(BrakeFormulaDisplay));
        OnPropertyChanged(nameof(Graph));
    }

    public Train Clone(string newId) => new()
    {
        Id = newId,
        Name = Name + " (Kopie)",
        Hub = Hub,
        BleAddress = "",
        Color = Color,
        PortA = PortA,
        PortB = PortB,
        Speed = Speed,
        AccelFn = AccelFn,
        AccelExpression = AccelExpression,
        BrakeFn = BrakeFn,
        BrakeExpression = BrakeExpression,
        IsActive = IsActive,
    };
}
