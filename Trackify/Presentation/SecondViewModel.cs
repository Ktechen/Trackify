using System.Collections.ObjectModel;
using System.ComponentModel;
using Trackify.Models.Trains;
using Trackify.Models.Trains.Enums;
using TrackSegment = Trackify.Models.Trains.TrackSegment;

namespace Trackify.Presentation;

public record LegendItem(string Color, string Label);

public partial class SecondViewModel : ObservableObject
{
    private readonly INavigator _navigator;

    [ObservableProperty] private TrackSegment? selectedSegment;
    [ObservableProperty] private int segmentCount;
    [ObservableProperty] private int sensorCount;
    [ObservableProperty] private int averageSpeed;

    public ObservableCollection<TrackSegment> Segments { get; } = [];

    public string TrackBedPathData { get; }

    public IReadOnlyList<SpeedFunctionOption> SpeedFunctionOptions { get; } =
        [.. LegoinoCatalog.SpeedFunctions.Where(f => f.Value != SpeedFunctionType.Custom)];

    public IReadOnlyList<DirectionOption> DirectionOptions => LegoinoCatalog.Directions;

    public IReadOnlyList<SensorTypeOption> SensorTypeOptions => LegoinoCatalog.SensorTypes;

    public IReadOnlyList<SensorActionOption> SensorActionOptions => LegoinoCatalog.SensorActions;

    public IReadOnlyList<LegendItem> Legend { get; } =
    [
        new("#E5484D", "Stop"),
        new("#F5A623", "langsam"),
        new("#2FAE4A", "mittel"),
        new("#16A34A", "schnell"),
    ];

    public IRelayCommand<TrackSegment> SelectSegmentCommand { get; }

    public IRelayCommand<string> SetDirectionCommand { get; }

    public IRelayCommand<string> SetSensorCommand { get; }

    public IAsyncRelayCommand GoBackCommand { get; }

    public SecondViewModel(INavigator navigator)
    {
        _navigator = navigator;

        SelectSegmentCommand = new RelayCommand<TrackSegment>(t => SelectedSegment = t);
        SetDirectionCommand = new RelayCommand<string>(v => { if (SelectedSegment is not null) SelectedSegment.Direction = Enum.Parse<TrackDirection>(v!); });
        SetSensorCommand = new RelayCommand<string>(v => { if (SelectedSegment is not null) SelectedSegment.Sensor = Enum.Parse<SensorType>(v!); });
        GoBackCommand = new AsyncRelayCommand(async () => await _navigator.NavigateBackAsync(this));

        TrackBedPathData = TrackGeometry.BuildTrackBed();

        Seed();
        SelectedSegment = Segments.FirstOrDefault(s => s.Id == "SEG-2");
        RefreshStats();
    }

    private void Seed()
    {
        var geometry = TrackGeometry.BuildSegments().ToDictionary(g => g.Id);

        TrackSegment Create(
            string id, int maxSpeed,
            SpeedFunctionType accel = SpeedFunctionType.EaseOut, SpeedFunctionType brake = SpeedFunctionType.EaseIn,
            SensorType sensor = SensorType.None, SensorActionType action = SensorActionType.Notify, int slow = 30)
        {
            var g = geometry[id];
            var segment = new TrackSegment
            {
                Id = id,
                Name = TrackGeometry.Names[id],
                Type = TrackGeometry.Types[id],
                MaxSpeed = maxSpeed,
                AccelFn = accel,
                BrakeFn = brake,
                Sensor = sensor,
                Action = action,
                SlowTarget = slow,
                PathData = g.PathData,
                HitPathData = g.HitPathData,
                MidX = g.MidX,
                MidY = g.MidY,
                TanX = g.TanX,
                TanY = g.TanY,
                LabelLeft = g.MidX - (g.OutwardX * 40) - 20,
                LabelTop = g.MidY - (g.OutwardY * 40) + 5 - 10,
                SensorLeft = g.MidX + (g.OutwardX * 50) - 15,
                SensorTop = g.MidY + (g.OutwardY * 50) - 15,
                SensorLineX1 = g.MidX + (g.OutwardX * 18),
                SensorLineY1 = g.MidY + (g.OutwardY * 18),
                SensorLineX2 = g.MidX + (g.OutwardX * 48),
                SensorLineY2 = g.MidY + (g.OutwardY * 48),
            };
            segment.PropertyChanged += SegmentOnPropertyChanged;
            return segment;
        }

        Segments.Add(Create("SEG-1", 80));
        Segments.Add(Create("SEG-2", 20, brake: SpeedFunctionType.SCurve, sensor: SensorType.Color, action: SensorActionType.Stop));
        Segments.Add(Create("SEG-3", 55));
        Segments.Add(Create("SEG-4", 50, sensor: SensorType.Distance, action: SensorActionType.Slower, slow: 30));
        Segments.Add(Create("SEG-5", 90));
        Segments.Add(Create("SEG-6", 85, sensor: SensorType.Color, action: SensorActionType.Notify));
        Segments.Add(Create("SEG-7", 50));
        Segments.Add(Create("SEG-8", 55));
    }

    private void SegmentOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TrackSegment.MaxSpeed) or nameof(TrackSegment.Sensor)) RefreshStats();
    }

    private void RefreshStats()
    {
        SegmentCount = Segments.Count;
        SensorCount = Segments.Count(s => s.HasSensor);
        AverageSpeed = Segments.Count > 0 ? (int)Math.Round(Segments.Average(s => s.MaxSpeed)) : 0;
    }
}
