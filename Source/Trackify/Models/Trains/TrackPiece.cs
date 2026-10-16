namespace Trackify.Models.Trains;

/// <summary>
/// One part as the Streckenplaner shows it: the drive behaviour the inspector edits, plus the
/// ready-to-draw coordinates <see cref="TrackPlanFitter"/> worked out.
///
/// The topology — which part is plugged into which — is deliberately <em>not</em> here. That lives in
/// the ViewModel's <c>TrackPlanTopology</c>, because it is the plan's structural truth, and a piece
/// that could edit its own connections would let the view invent geometry.
/// </summary>
public partial class TrackPiece : ObservableObject
{
    private const double LabelWidth = 40;
    private const double LabelHeight = 21;
    private const double LabelOffset = 34;
    private const double SensorSize = 28;
    private const double SensorOffset = 42;

    [ObservableProperty] private string specKey = TrackPartCatalog.StraightKey;
    [ObservableProperty] private string name = "";
    [ObservableProperty] private int maxSpeed = 70;
    [ObservableProperty] private TrackDirection direction = TrackDirection.Forward;
    [ObservableProperty] private SpeedFunctionType accelFn = SpeedFunctionType.EaseOut;
    [ObservableProperty] private SpeedFunctionType brakeFn = SpeedFunctionType.EaseIn;
    [ObservableProperty] private SensorType sensor = SensorType.None;
    [ObservableProperty] private SensorActionType action = SensorActionType.Notify;
    [ObservableProperty] private int slowTarget = 30;

    /// <summary>Which way a point is set. Meaningless on anything that is not a point.</summary>
    [ObservableProperty] private SwitchRoute defaultRoute = SwitchRoute.Main;

    [ObservableProperty] private string pathData = "";
    [ObservableProperty] private string hitPathData = "";
    [ObservableProperty] private double labelLeft;
    [ObservableProperty] private double labelTop;
    [ObservableProperty] private double sensorLeft;
    [ObservableProperty] private double sensorTop;
    [ObservableProperty] private bool isPreview;

    /// <summary>Identity, shared with the plan graph and the stored part.</summary>
    public required Guid Id { get; init; }

    /// <summary>German label for this kind of part, as the planner lists it.</summary>
    public string TypeLabel => SpecKey switch
    {
        TrackPartCatalog.StraightKey => "Gerade",
        TrackPartCatalog.StationKey => "Bahnhof",
        TrackPartCatalog.CurveLeftKey => "Kurve links",
        TrackPartCatalog.CurveRightKey => "Kurve rechts",
        TrackPartCatalog.SwitchLeftKey => "Weiche links",
        TrackPartCatalog.SwitchRightKey => "Weiche rechts",
        TrackPartCatalog.CrossingKey => "Kreuzung",
        _ => "Gleis",
    };

    /// <summary>Whether this part is a point, which is what unlocks the switch part of the inspector.</summary>
    public bool IsSwitch => SpecKey is TrackPartCatalog.SwitchLeftKey or TrackPartCatalog.SwitchRightKey;

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

    public bool ShowSlowTarget => Action == SensorActionType.Slower;

    public string AccelFormulaDisplay => LegoinoCatalog.SpeedFunction(AccelFn).Formula;

    public string BrakeFormulaDisplay => LegoinoCatalog.SpeedFunction(BrakeFn).Formula;

    /// <summary>The LWP device token the sensor section shows, matching the CLI's vocabulary.</summary>
    public string SensorToken => "DeviceType." + (Sensor == SensorType.None ? "NONE" : "COLOR_DISTANCE_SENSOR");

    /// <summary>The speed profile the widget draws — presentation only.</summary>
    public SpeedProfileGraph Graph => SpeedCurve.BuildGraph(
        MaxSpeed / 100.0,
        x => SpeedFunction.Evaluate(AccelFn, x),
        x => SpeedFunction.Evaluate(BrakeFn, x));

    /// <summary>Takes the coordinates the fitter worked out for this part.</summary>
    public void ApplyGeometry(in FittedPiece piece)
    {
        PathData = piece.PathData;
        HitPathData = piece.HitPathData;
        IsPreview = piece.IsPreview;

        // Label and sensor sit on opposite sides of the track, both offset along the piece's outward
        // normal, then pulled back by half their box so the offset lands on the box's centre.
        LabelLeft = piece.MidX - (piece.OutwardX * LabelOffset) - (LabelWidth / 2);
        LabelTop = piece.MidY - (piece.OutwardY * LabelOffset) - (LabelHeight / 2);
        SensorLeft = piece.MidX + (piece.OutwardX * SensorOffset) - (SensorSize / 2);
        SensorTop = piece.MidY + (piece.OutwardY * SensorOffset) - (SensorSize / 2);
    }

    partial void OnMaxSpeedChanged(int value)
    {
        OnPropertyChanged(nameof(SpeedColor));
        OnPropertyChanged(nameof(Graph));
    }

    partial void OnSpecKeyChanged(string value)
    {
        OnPropertyChanged(nameof(TypeLabel));
        OnPropertyChanged(nameof(IsSwitch));
    }

    partial void OnSensorChanged(SensorType value)
    {
        OnPropertyChanged(nameof(HasSensor));
        OnPropertyChanged(nameof(SensorColor));
        OnPropertyChanged(nameof(SensorGlyph));
        OnPropertyChanged(nameof(SensorToken));
    }

    partial void OnActionChanged(SensorActionType value) => OnPropertyChanged(nameof(ShowSlowTarget));

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
}
