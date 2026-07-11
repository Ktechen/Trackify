using Trackify.Models.Trains.Enums;

namespace Trackify.Models.Trains;

public record HubOption(HubType Value, string Label, string Short);

public record DeviceOption(DeviceType Value, string Label, string Short, string ListLabel);

public record ColorOption(LedColorType Value, string Name, string Hex);

public record SpeedFunctionOption(SpeedFunctionType Value, string Label, string Formula);

public record DirectionOption(TrackDirection Value, string Label);

public record SensorTypeOption(SensorType Value, string Label);

public record SensorActionOption(SensorActionType Value, string Label);

public static class LegoinoCatalog
{
    public static readonly IReadOnlyList<HubOption> Hubs =
    [
        new(HubType.PoweredUpHub, "City Hub · 88009", "City Hub"),
        new(HubType.ControlPlusHub, "Technic Hub · 88012", "Technic Hub"),
        new(HubType.BoostMoveHub, "Move Hub · 88006", "Move Hub"),
        new(HubType.DuploTrainHub, "Duplo Train Hub", "Duplo"),
        new(HubType.PoweredUpRemote, "Remote Control · 88010", "Remote"),
        new(HubType.WeDo2SmartHub, "WeDo 2.0 Hub", "WeDo 2.0"),
    ];

    public static readonly IReadOnlyList<DeviceOption> Devices =
    [
        new(DeviceType.None, "— leer", "—", "—"),
        new(DeviceType.TrainMotor, "Zugmotor", "MOT", "Motor"),
        new(DeviceType.MediumLinearMotor, "Medium-Motor", "MED", "Motor"),
        new(DeviceType.Light, "Licht", "LED", "Licht"),
        new(DeviceType.ColorDistanceSensor, "Farb-/Abstandssensor", "C&D", "Sensor"),
    ];

    public static readonly IReadOnlyList<ColorOption> Colors =
    [
        new(LedColorType.Red, "Rot", "#D3282F"),
        new(LedColorType.Orange, "Orange", "#E8730C"),
        new(LedColorType.Yellow, "Gelb", "#F5B716"),
        new(LedColorType.Green, "Grün", "#2FAE4A"),
        new(LedColorType.Cyan, "Cyan", "#17C3D6"),
        new(LedColorType.LightBlue, "Hellblau", "#38B6FF"),
        new(LedColorType.Blue, "Blau", "#0A54C9"),
        new(LedColorType.Purple, "Violett", "#8B3FF0"),
        new(LedColorType.Pink, "Pink", "#FF5FB0"),
        new(LedColorType.White, "Weiß", "#F2F2EE"),
        new(LedColorType.Black, "Schwarz", "#2A2D2A"),
        new(LedColorType.None, "Aus", "#C9CCC4"),
    ];

    public static readonly IReadOnlyList<SpeedFunctionOption> SpeedFunctions =
    [
        new(SpeedFunctionType.Linear, "Linear", "f(x) = x"),
        new(SpeedFunctionType.EaseIn, "Ease-In", "f(x) = x²"),
        new(SpeedFunctionType.EaseOut, "Ease-Out", "f(x) = 1−(1−x)²"),
        new(SpeedFunctionType.SCurve, "S-Kurve", "f(x) = 3x²−2x³"),
        new(SpeedFunctionType.Exponential, "Exponentiell", "f(x) = (e³ˣ−1)/(e³−1)"),
        new(SpeedFunctionType.Custom, "Eigene Formel…", ""),
    ];

    public static readonly IReadOnlyList<DirectionOption> Directions =
    [
        new(TrackDirection.Forward, "Vorwärts"),
        new(TrackDirection.Reverse, "Rückwärts"),
    ];

    public static readonly IReadOnlyList<SensorTypeOption> SensorTypes =
    [
        new(SensorType.None, "Kein"),
        new(SensorType.Color, "Farbe"),
        new(SensorType.Distance, "Abstand"),
    ];

    public static readonly IReadOnlyList<SensorActionOption> SensorActions =
    [
        new(SensorActionType.Stop, "Stopp"),
        new(SensorActionType.Slower, "Langsamer"),
        new(SensorActionType.Notify, "Melden"),
        new(SensorActionType.ReverseDirection, "Richtung wechseln"),
    ];

    public static HubOption Hub(HubType value) => Hubs.First(h => h.Value == value);
    public static DeviceOption Device(DeviceType value) => Devices.First(d => d.Value == value);
    public static ColorOption Color(LedColorType value) => Colors.First(c => c.Value == value);
    public static SpeedFunctionOption SpeedFunction(SpeedFunctionType value) => SpeedFunctions.First(f => f.Value == value);
}
