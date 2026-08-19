
namespace Trackify.Models.Trains;

public partial class TrackSegment : ObservableObject
{
    // Canvas marker boxes — TrackCanvas.xaml draws elements of exactly these sizes, and TrackGeometry's
    // fit padding keeps the largest outward offset on screen.
    private const double LabelWidth = 40;
    private const double LabelHeight = 21;
    private const double LabelOffset = 40;
    private const double SensorSize = 30;
    private const double SensorOffset = 50;
    private const double ArrowLength = 16;
    private const double ArrowHalfWidth = 6;

    /// <summary>Where a sensor's connecting line starts: just clear of the 30-thick ballast stroke.</summary>
    private const double TrackEdge = 18;

    [ObservableProperty] private Guid id = Guid.CreateVersion7();
    [ObservableProperty] private string name = "";
    [ObservableProperty] private SegmentType type = SegmentType.Straight;
    [ObservableProperty] private int maxSpeed = 70;
    [ObservableProperty] private TrackDirection direction = TrackDirection.Forward;
    [ObservableProperty] private SpeedFunctionType accelFn = SpeedFunctionType.EaseOut;
    [ObservableProperty] private SpeedFunctionType brakeFn = SpeedFunctionType.EaseIn;
    [ObservableProperty] private SensorType sensor = SensorType.None;
    [ObservableProperty] private SensorActionType action = SensorActionType.Notify;
    [ObservableProperty] private int slowTarget = 30;

    // Strand/branch graph — see Domain's TrackSegment for the full field-by-field rationale.
    [ObservableProperty] private Guid strandId = Guid.Empty;
    [ObservableProperty] private int order;
    [ObservableProperty] private CurveDirection? curve;
    [ObservableProperty] private Guid? branchStrandId;
    [ObservableProperty] private CurveDirection? branchDirection;
    [ObservableProperty] private SwitchRoute route = SwitchRoute.Main;
    [ObservableProperty] private Guid? branchLinkSegmentId;

    /// <summary>Static SVG-style path data for the track centerline, in the shared 900x600 canvas viewBox.</summary>
    public string PathData { get; private set; } = "";

    /// <summary>Wider, untrimmed path data used for pointer hit-testing and the selection glow.</summary>
    public string HitPathData { get; private set; } = "";

    /// <summary>Segment midpoint — every marker on the piece (label, sensor, arrow) hangs off it.</summary>
    public double MidX { get; private set; }
    public double MidY { get; private set; }

    /// <summary>Top-left of the speed label's box, centered on its position (Canvas.Left/Top friendly).</summary>
    public double LabelLeft { get; private set; }
    public double LabelTop { get; private set; }

    /// <summary>Top-left of the sensor-marker box (Canvas.Left/Top friendly).</summary>
    public double SensorLeft { get; private set; }
    public double SensorTop { get; private set; }

    /// <summary>The travel-direction arrow: a triangle at the segment midpoint, pointing along the
    /// track (flipped for <see cref="TrackDirection.Reverse"/>), as ready-to-draw path data. It is
    /// emitted rather than drawn once and rotated by a bound RenderTransform, because a
    /// <c>{Binding}</c> inside a transform has no DataContext to inherit — transforms aren't in the
    /// visual tree — so the angle silently never arrives.</summary>
    public string ArrowPathData
    {
        get
        {
            var sign = Direction == TrackDirection.Reverse ? -1 : 1;
            var (tanX, tanY) = (TanX * sign, TanY * sign);
            var (tipX, tipY) = (MidX + (tanX * ArrowLength / 2), MidY + (tanY * ArrowLength / 2));
            var (backX, backY) = (MidX - (tanX * ArrowLength / 2), MidY - (tanY * ArrowLength / 2));
            var (leftX, leftY) = (backX + (tanY * ArrowHalfWidth), backY - (tanX * ArrowHalfWidth));
            var (rightX, rightY) = (backX - (tanY * ArrowHalfWidth), backY + (tanX * ArrowHalfWidth));

            return FormattableString.Invariant($"M {leftX:0.0} {leftY:0.0} L {tipX:0.0} {tipY:0.0} L {rightX:0.0} {rightY:0.0} Z");
        }
    }

    public double SensorLineX1 { get; private set; }
    public double SensorLineY1 { get; private set; }
    public double SensorLineX2 { get; private set; }
    public double SensorLineY2 { get; private set; }

    /// <summary>Raw track-tangent direction at the segment midpoint (forward travel), used to orient the direction arrow.</summary>
    public double TanX { get; private set; }
    public double TanY { get; private set; }

    /// <summary>Copies a freshly-computed <see cref="TrackGeometry"/> result onto this segment and
    /// derives the label/sensor marker positions from it — called for every segment after any
    /// structural change to the plan (append/delete/reorder/…), since a change anywhere upstream in
    /// a strand shifts every piece after it.</summary>
    public void ApplyGeometry(SegmentGeometry geometry)
    {
        PathData = geometry.PathData;
        HitPathData = geometry.HitPathData;
        MidX = geometry.MidX;
        MidY = geometry.MidY;
        TanX = geometry.TanX;
        TanY = geometry.TanY;
        // Label and sensor sit on opposite sides of the track, both offset along the piece's outward
        // normal, then pulled back by half their box so the offset lands on the box's center.
        LabelLeft = MidX - (geometry.OutwardX * LabelOffset) - (LabelWidth / 2);
        LabelTop = MidY - (geometry.OutwardY * LabelOffset) - (LabelHeight / 2);
        SensorLeft = MidX + (geometry.OutwardX * SensorOffset) - (SensorSize / 2);
        SensorTop = MidY + (geometry.OutwardY * SensorOffset) - (SensorSize / 2);
        SensorLineX1 = MidX + (geometry.OutwardX * TrackEdge);
        SensorLineY1 = MidY + (geometry.OutwardY * TrackEdge);
        SensorLineX2 = MidX + (geometry.OutwardX * (SensorOffset - 2));
        SensorLineY2 = MidY + (geometry.OutwardY * (SensorOffset - 2));

        OnPropertyChanged(nameof(PathData));
        OnPropertyChanged(nameof(HitPathData));
        OnPropertyChanged(nameof(MidX));
        OnPropertyChanged(nameof(MidY));
        OnPropertyChanged(nameof(TanX));
        OnPropertyChanged(nameof(TanY));
        OnPropertyChanged(nameof(LabelLeft));
        OnPropertyChanged(nameof(LabelTop));
        OnPropertyChanged(nameof(SensorLeft));
        OnPropertyChanged(nameof(SensorTop));
        OnPropertyChanged(nameof(ArrowPathData));
        OnPropertyChanged(nameof(SensorLineX1));
        OnPropertyChanged(nameof(SensorLineY1));
        OnPropertyChanged(nameof(SensorLineX2));
        OnPropertyChanged(nameof(SensorLineY2));
    }

    public TrackGeometryInput GeometryInput => new(Id, Type, Curve, StrandId, Order, BranchStrandId, BranchDirection);

    public bool IsWeiche => Type == SegmentType.Switch;

    public string TypeLabel => Type switch
    {
        SegmentType.Straight => "Gerade",
        SegmentType.Curve => Curve == CurveDirection.Left ? "Kurve links" : "Kurve rechts",
        SegmentType.Switch => "Weiche",
        SegmentType.Station => "Bahnhof",
        _ => "Segment",
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
        x => SpeedFunction.Evaluate(AccelFn, x),
        x => SpeedFunction.Evaluate(BrakeFn, x));

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

    partial void OnDirectionChanged(TrackDirection value) => OnPropertyChanged(nameof(ArrowPathData));

    partial void OnTypeChanged(SegmentType value)
    {
        OnPropertyChanged(nameof(TypeLabel));
        OnPropertyChanged(nameof(IsWeiche));
    }

    partial void OnCurveChanged(CurveDirection? value) => OnPropertyChanged(nameof(TypeLabel));
}
