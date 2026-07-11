using Trackify.Models.Trains.Enums;

namespace Trackify.Models.Trains;

public partial class TrackSegment : ObservableObject
{
    [ObservableProperty] private string id = "";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private SegmentType type = SegmentType.Straight;
    [ObservableProperty] private int maxSpeed = 70;
    [ObservableProperty] private TrackDirection direction = TrackDirection.Forward;
    [ObservableProperty] private SpeedFunctionType accelFn = SpeedFunctionType.EaseOut;
    [ObservableProperty] private SpeedFunctionType brakeFn = SpeedFunctionType.EaseIn;
    [ObservableProperty] private SensorType sensor = SensorType.None;
    [ObservableProperty] private SensorActionType action = SensorActionType.Notify;
    [ObservableProperty] private int slowTarget = 30;

    /// <summary>Static SVG-style path data for the track centerline, in the shared 900x600 canvas viewBox.</summary>
    public string PathData { get; set; } = "";

    /// <summary>Wider, untrimmed path data used for pointer hit-testing and the selection glow.</summary>
    public string HitPathData { get; set; } = "";

    /// <summary>Segment midpoint, used as the direction-arrow's translation anchor.</summary>
    public double MidX { get; set; }
    public double MidY { get; set; }

    /// <summary>Top-left of a fixed 40x20 box centered on the speed-label position (Canvas.Left/Top friendly).</summary>
    public double LabelLeft { get; set; }
    public double LabelTop { get; set; }

    /// <summary>Top-left of the fixed 30x30 sensor-marker box (Canvas.Left/Top friendly).</summary>
    public double SensorLeft { get; set; }
    public double SensorTop { get; set; }

    public double SensorLineX1 { get; set; }
    public double SensorLineY1 { get; set; }
    public double SensorLineX2 { get; set; }
    public double SensorLineY2 { get; set; }

    /// <summary>Raw track-tangent direction at the segment midpoint (forward travel), used to orient the direction arrow.</summary>
    public double TanX { get; set; }
    public double TanY { get; set; }

    public double ArrowRotationDeg => Math.Atan2(TanY, TanX) * 180 / Math.PI + (Direction == TrackDirection.Reverse ? 180 : 0);

    public string TypeLabel => Type switch
    {
        SegmentType.Straight => "Gerade",
        SegmentType.Curve => "Kurve",
        _ => "Bahnhof",
    };

    public string SpeedColor => MaxSpeed switch
    {
        <= 0 => "#E5484D",
        < 35 => "#E5701C",
        < 60 => "#F5A623",
        < 85 => "#2FAE4A",
        _ => "#16A34A",
    };

    public bool HasSensor => Sensor != SensorType.None;

    public string SensorColor => Sensor == SensorType.Color ? "#0A54C9" : "#E8730C";

    public string SensorGlyph => Sensor == SensorType.Color ? "F" : "A";

    public string SensorToken => "DeviceType." + (Sensor == SensorType.None ? "NONE" : "COLOR_DISTANCE_SENSOR");

    public bool ShowSlowTarget => Action == SensorActionType.Slower;

    public string AccelFormulaDisplay => LegoinoCatalog.SpeedFunction(AccelFn).Formula;

    public string BrakeFormulaDisplay => LegoinoCatalog.SpeedFunction(BrakeFn).Formula;

    public SpeedProfileGraph Graph => SpeedCurve.BuildGraph(
        MaxSpeed / 100.0,
        x => SpeedCurve.Evaluate(AccelFn, x),
        x => SpeedCurve.Evaluate(BrakeFn, x));

    partial void OnMaxSpeedChanged(int value)
    {
        OnPropertyChanged(nameof(SpeedColor));
        OnPropertyChanged(nameof(Graph));
    }

    partial void OnAccelFnChanged(SpeedFunctionType value)
    {
        OnPropertyChanged(nameof(AccelFormulaDisplay));
        OnPropertyChanged(nameof(Graph));
    }

    partial void OnBrakeFnChanged(SpeedFunctionType value)
    {
        OnPropertyChanged(nameof(BrakeFormulaDisplay));
        OnPropertyChanged(nameof(Graph));
    }

    partial void OnSensorChanged(SensorType value)
    {
        OnPropertyChanged(nameof(HasSensor));
        OnPropertyChanged(nameof(SensorColor));
        OnPropertyChanged(nameof(SensorGlyph));
        OnPropertyChanged(nameof(SensorToken));
    }

    partial void OnActionChanged(SensorActionType value) => OnPropertyChanged(nameof(ShowSlowTarget));

    partial void OnDirectionChanged(TrackDirection value) => OnPropertyChanged(nameof(ArrowRotationDeg));
}
