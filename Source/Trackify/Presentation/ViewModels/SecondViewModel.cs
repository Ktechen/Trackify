using System.Collections.ObjectModel;
using System.ComponentModel;
using Trackify.Models.Trains;
using TrackSegment = Trackify.Models.Trains.TrackSegment;

namespace Trackify.Presentation.ViewModels;

public partial class SecondViewModel : ObservableObject
{
    private readonly INavigator _navigator;
    private readonly ITrackSegmentRepository _repository;
    private readonly ITrackPlanService _trackPlanService;

    [ObservableProperty] private TrackSegment? selectedSegment;
    [ObservableProperty] private int segmentCount;
    [ObservableProperty] private int sensorCount;
    [ObservableProperty] private int averageSpeed;
    [ObservableProperty] private string trackBedPathData = "";
    [ObservableProperty] private Guid activeStrandId = Guid.Empty;
    [ObservableProperty] private bool dlgOpen;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private double zoomFactor = 1;

    private const double MinZoom = 0.4;
    private const double MaxZoom = 3;
    private const double ZoomStep = 0.2;
    private const double CanvasBaseWidth = 900;
    private const double CanvasBaseHeight = 600;

    /// <summary>The canvas's displayed size at the current zoom — bound onto a Viewbox wrapping the
    /// fixed 900x600-coordinate canvas, so zoom is a plain bound number, not an imperative
    /// ScrollViewer API call (which isn't reliably supported across every Uno target).</summary>
    public double CanvasWidth => CanvasBaseWidth * ZoomFactor;

    public double CanvasHeight => CanvasBaseHeight * ZoomFactor;

    public ObservableCollection<TrackSegment> Segments { get; } = [];

    public ObservableCollection<StrandGroup> StrandGroups { get; } = [];

    public IReadOnlyList<TrackPartOption> TrackParts => LegoinoCatalog.TrackParts;

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

    public string ActiveStrandName => StrandGroups.FirstOrDefault(g => g.StrandId == ActiveStrandId)?.Name ?? "Hauptstrecke";

    /// <summary>The Weiche a "Weiche einfügen" dialog is currently being opened for — the strand the
    /// new switch is appended to (usually <see cref="ActiveStrandId"/>).</summary>
    public Guid PendingSwitchStrandId { get; private set; }

    /// <summary>The selected Weiche's branch strand, if any (drives the "Im Zweig bauen" section).</summary>
    public string SelectedBranchName => Branch(SelectedSegment)?.Name ?? "";

    public string SelectedBranchCount => Branch(SelectedSegment) is { } branch ? $"{branch.Count} Segmente" : "";

    /// <summary>Candidate segments a branch's open end could be logically linked to ("Zweig-Ende
    /// anschließen an") — stored as metadata for later routing use; this pass doesn't bend the
    /// branch's drawn geometry to visually meet the target.</summary>
    public IReadOnlyList<TrackSegment> BranchLinkOptions =>
        SelectedSegment is null ? [] : [.. Segments.Where(s => s.StrandId != SelectedSegment.BranchStrandId)];

    public IRelayCommand<TrackSegment> SelectSegmentCommand { get; }

    public IRelayCommand<string> SetDirectionCommand { get; }

    public IRelayCommand<string> SetSensorCommand { get; }

    public IAsyncRelayCommand GoBackCommand { get; }

    public SecondViewModel(INavigator navigator, ITrackSegmentRepository repository, ITrackPlanService trackPlanService)
    {
        _navigator = navigator;
        _repository = repository;
        _trackPlanService = trackPlanService;

        SelectSegmentCommand = new RelayCommand<TrackSegment>(t => SelectedSegment = t);
        SetDirectionCommand = new RelayCommand<string>(v => { if (SelectedSegment is not null) SelectedSegment.Direction = Enum.Parse<TrackDirection>(v!); });
        SetSensorCommand = new RelayCommand<string>(v => { if (SelectedSegment is not null) SelectedSegment.Sensor = Enum.Parse<SensorType>(v!); });
        GoBackCommand = new AsyncRelayCommand(async () => await _navigator.NavigateBackAsync(this));

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var saved = await _repository.GetAllAsync();
        var dtos = saved.Count > 0 ? [.. saved.Select(TrackSegmentMapping.ToDto)] : _trackPlanService.ApplyTemplate(TemplateType.Oval);

        ApplyPlan(dtos);
        SelectedSegment = Segments.OrderBy(s => s.StrandId == ActiveStrandId ? 0 : 1).ThenBy(s => s.Order).FirstOrDefault();
    }

    [RelayCommand]
    private void AddPart(TrackPartOption part)
    {
        if (part.Type == SegmentType.Switch)
        {
            PendingSwitchStrandId = ActiveStrandId;
            DlgOpen = true;
            return;
        }

        ApplyPlan(_trackPlanService.AppendPart(CurrentDtos(), ActiveStrandId, part.Type, part.Curve));
    }

    [RelayCommand] private void ZoomIn() => ZoomFactor = Math.Min(MaxZoom, ZoomFactor + ZoomStep);
    [RelayCommand] private void ZoomOut() => ZoomFactor = Math.Max(MinZoom, ZoomFactor - ZoomStep);
    [RelayCommand] private void ZoomFit() => ZoomFactor = 1;

    [RelayCommand] private void DlgLeftStay() => InsertSwitch(CurveDirection.Left, goToBranch: false);
    [RelayCommand] private void DlgRightStay() => InsertSwitch(CurveDirection.Right, goToBranch: false);
    [RelayCommand] private void DlgLeftGo() => InsertSwitch(CurveDirection.Left, goToBranch: true);
    [RelayCommand] private void DlgRightGo() => InsertSwitch(CurveDirection.Right, goToBranch: true);
    [RelayCommand] private void DlgCancel() => DlgOpen = false;

    private void InsertSwitch(CurveDirection direction, bool goToBranch)
    {
        var result = _trackPlanService.InsertSwitch(CurrentDtos(), PendingSwitchStrandId, direction);
        ApplyPlan(result.Segments);
        DlgOpen = false;
        if (goToBranch) ActiveStrandId = result.BranchStrandId;
    }

    [RelayCommand] private void TemplateOval() => ApplyTemplate(TemplateType.Oval);
    [RelayCommand] private void TemplateAcht() => ApplyTemplate(TemplateType.Acht);
    [RelayCommand] private void TemplatePunktZuPunkt() => ApplyTemplate(TemplateType.PunktZuPunkt);

    [RelayCommand]
    private void TemplateClear()
    {
        ActiveStrandId = Guid.Empty;
        ApplyPlan(_trackPlanService.Clear());
        SelectedSegment = null;
    }

    private void ApplyTemplate(TemplateType template)
    {
        ActiveStrandId = Guid.Empty;
        ApplyPlan(_trackPlanService.ApplyTemplate(template));
        SelectedSegment = Segments.OrderBy(s => s.Order).FirstOrDefault();
    }

    [RelayCommand]
    private void SelectStrand(Guid strandId) => ActiveStrandId = strandId;

    [RelayCommand]
    private void GotoBranch()
    {
        if (SelectedSegment?.BranchStrandId is { } branchId) ActiveStrandId = branchId;
    }

    [RelayCommand]
    private void SetBranchDirection(string direction)
    {
        if (SelectedSegment is null) return;
        SelectedSegment.BranchDirection = Enum.Parse<CurveDirection>(direction);
    }

    [RelayCommand]
    private void SetRoute(string route)
    {
        if (SelectedSegment is null) return;
        SelectedSegment.Route = Enum.Parse<SwitchRoute>(route);
    }

    [RelayCommand]
    private void DuplicateSegment(TrackSegment? segment)
    {
        if ((segment ?? SelectedSegment) is not { } target) return;
        ApplyPlan(_trackPlanService.Duplicate(CurrentDtos(), target.Id));
    }

    [RelayCommand] private void MoveUp(TrackSegment? segment) => Reorder(segment, up: true);

    [RelayCommand] private void MoveDown(TrackSegment? segment) => Reorder(segment, up: false);

    private void Reorder(TrackSegment? segment, bool up)
    {
        if ((segment ?? SelectedSegment) is not { } target) return;
        ApplyPlan(_trackPlanService.Reorder(CurrentDtos(), target.Id, up));
    }

    [RelayCommand]
    private void DeleteSegment(TrackSegment? segment)
    {
        if ((segment ?? SelectedSegment) is not { } target) return;
        var wasSelected = SelectedSegment?.Id == target.Id;

        ApplyPlan(_trackPlanService.Delete(CurrentDtos(), target.Id));

        if (wasSelected) SelectedSegment = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsSaving = true;
        try
        {
            var existing = await _repository.GetAllAsync();
            foreach (var stale in existing) await _repository.DeleteAsync(stale.Id);
            await _repository.AddRangeAsync(Segments.Select(s => ToDto(s).ToEntity()));
        }
        finally
        {
            IsSaving = false;
        }
    }

    private List<TrackSegmentDto> CurrentDtos() => [.. Segments.Select(ToDto)];

    // Reconciles Segments with a freshly-computed plan (upsert by Id, in place, so SelectedSegment
    // and any bound UI keep their identity), recomputes geometry for everything downstream of the
    // change, and rebuilds the strand groups + stats.
    private void ApplyPlan(IReadOnlyList<TrackSegmentDto> dtos)
    {
        var byId = Segments.ToDictionary(s => s.Id);
        var keepIds = new HashSet<Guid>();

        foreach (var dto in dtos)
        {
            keepIds.Add(dto.Id);
            if (byId.TryGetValue(dto.Id, out var existing))
            {
                CopyInto(existing, dto);
            }
            else
            {
                var segment = ToModel(dto);
                segment.PropertyChanged += SegmentOnPropertyChanged;
                Segments.Add(segment);
            }
        }

        foreach (var stale in Segments.Where(s => !keepIds.Contains(s.Id)).ToList())
        {
            stale.PropertyChanged -= SegmentOnPropertyChanged;
            Segments.Remove(stale);
        }

        var layout = TrackGeometry.Build([.. Segments.Select(s => s.GeometryInput)]);
        foreach (var segment in Segments)
        {
            if (layout.Segments.TryGetValue(segment.Id, out var geo)) segment.ApplyGeometry(geo);
        }

        TrackBedPathData = layout.BedPathData;

        RebuildStrandGroups();
        RefreshStats();

        if (SelectedSegment is not null && !keepIds.Contains(SelectedSegment.Id)) SelectedSegment = null;
    }

    private void RebuildStrandGroups()
    {
        var branchNumber = new Dictionary<Guid, int>();
        var next = 1;
        foreach (var owner in Segments.Where(s => s.Type == SegmentType.Switch && s.BranchStrandId is not null))
        {
            branchNumber[owner.BranchStrandId!.Value] = next++;
        }

        var strandIds = new List<Guid> { Guid.Empty };
        strandIds.AddRange(Segments.Select(s => s.StrandId).Distinct().Where(id => id != Guid.Empty)
            .OrderBy(id => branchNumber.GetValueOrDefault(id, int.MaxValue)));

        var existing = StrandGroups.ToDictionary(g => g.StrandId);
        var keep = new HashSet<Guid>();

        foreach (var strandId in strandIds)
        {
            keep.Add(strandId);
            var name = strandId == Guid.Empty ? "Hauptstrecke" : $"Zweig {branchNumber.GetValueOrDefault(strandId, 0)}";

            if (!existing.TryGetValue(strandId, out var group))
            {
                group = new StrandGroup { StrandId = strandId };
                StrandGroups.Add(group);
            }
            group.Name = name;
            group.IsActive = strandId == ActiveStrandId;

            group.Rows.Clear();
            foreach (var row in Segments.Where(s => s.StrandId == strandId).OrderBy(s => s.Order)) group.Rows.Add(row);
            group.Count = group.Rows.Count;
        }

        foreach (var goneStrand in StrandGroups.Where(g => !keep.Contains(g.StrandId)).ToList()) StrandGroups.Remove(goneStrand);

        OnPropertyChanged(nameof(ActiveStrandName));
        OnPropertyChanged(nameof(SelectedBranchName));
        OnPropertyChanged(nameof(SelectedBranchCount));
    }

    private StrandGroup? Branch(TrackSegment? segment)
        => segment?.BranchStrandId is { } id ? StrandGroups.FirstOrDefault(g => g.StrandId == id) : null;

    private static void CopyInto(TrackSegment segment, TrackSegmentDto dto)
    {
        segment.Name = dto.Name;
        segment.Type = dto.Type;
        segment.MaxSpeed = dto.MaxSpeed;
        segment.Direction = dto.Direction;
        segment.AccelFn = dto.AccelFn;
        segment.BrakeFn = dto.BrakeFn;
        segment.Sensor = dto.Sensor;
        segment.Action = dto.Action;
        segment.SlowTarget = dto.SlowTarget;
        segment.StrandId = dto.StrandId;
        segment.Order = dto.Order;
        segment.Curve = dto.Curve;
        segment.BranchStrandId = dto.BranchStrandId;
        segment.BranchDirection = dto.BranchDirection;
        segment.Route = dto.Route;
        segment.BranchLinkSegmentId = dto.BranchLinkSegmentId;
    }

    private static TrackSegment ToModel(TrackSegmentDto dto)
    {
        var segment = new TrackSegment { Id = dto.Id };
        CopyInto(segment, dto);
        return segment;
    }

    private static TrackSegmentDto ToDto(TrackSegment segment) => new()
    {
        Id = segment.Id,
        Name = segment.Name,
        Type = segment.Type,
        MaxSpeed = segment.MaxSpeed,
        Direction = segment.Direction,
        AccelFn = segment.AccelFn,
        BrakeFn = segment.BrakeFn,
        Sensor = segment.Sensor,
        Action = segment.Action,
        SlowTarget = segment.SlowTarget,
        StrandId = segment.StrandId,
        Order = segment.Order,
        Curve = segment.Curve,
        BranchStrandId = segment.BranchStrandId,
        BranchDirection = segment.BranchDirection,
        Route = segment.Route,
        BranchLinkSegmentId = segment.BranchLinkSegmentId,
    };

    partial void OnZoomFactorChanged(double value)
    {
        OnPropertyChanged(nameof(CanvasWidth));
        OnPropertyChanged(nameof(CanvasHeight));
    }

    partial void OnActiveStrandIdChanged(Guid value)
    {
        foreach (var group in StrandGroups) group.IsActive = group.StrandId == value;
        OnPropertyChanged(nameof(ActiveStrandName));
    }

    partial void OnSelectedSegmentChanged(TrackSegment? value)
    {
        OnPropertyChanged(nameof(SelectedBranchName));
        OnPropertyChanged(nameof(SelectedBranchCount));
        OnPropertyChanged(nameof(BranchLinkOptions));
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
