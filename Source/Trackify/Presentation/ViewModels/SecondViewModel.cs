using System.Collections.ObjectModel;
using Trackify.Models.Trains;
using Windows.Foundation;

namespace Trackify.Presentation.ViewModels;

/// <summary>
/// The Streckenplaner: you draw, it lays real LEGO track.
///
/// The plan's truth is a <see cref="TrackPlanTopology"/> — parts plus port connections — and nothing
/// here ever writes a coordinate. A gesture becomes a stroke in studs, the stroke becomes a run of
/// catalogue parts (<see cref="TrackSnapper"/>), and the drawing comes back out of walking the graph.
/// That is what makes everything on screen buildable out of real track by construction.
///
/// Building needs no properties at all; speed, direction and sensor are edited afterwards, by
/// tapping a piece.
/// </summary>
public partial class SecondViewModel : ObservableObject
{
    /// <summary>How near a press must land to an open end, in canvas pixels, to grab it.</summary>
    private const double AnchorGrabPixels = 44;

    /// <summary>Minimum movement, in studs, before a drag adds another stroke point.</summary>
    private const double StrokeStepStuds = 2;

    private const double MinZoom = 0.4;
    private const double MaxZoom = 3;
    private const double ZoomStep = 0.2;

    private readonly INavigator _navigator;
    private readonly ITrackPlanRepository _repository;

    private readonly Dictionary<Guid, TrackPart> _parts = [];
    private readonly Stack<PlanState> _undoStack = [];
    private readonly Stack<PlanState> _redoStack = [];

    private TrackPlanTopology _topology = TrackPlanTopology.Empty;
    private FittedPlan _fitted = FittedPlan.Empty;

    // In-flight gesture: where it started, the stroke so far, and the run it currently proposes.
    private TrackOpenEnd? _strokeFrom;
    private readonly List<StudPoint> _stroke = [];
    private TrackSnapResult _preview = TrackSnapResult.None;

    [ObservableProperty] private TrackPiece? selectedPiece;
    [ObservableProperty] private string bedPathData = "";
    [ObservableProperty] private string previewPathData = "";
    [ObservableProperty] private int pieceCount;
    [ObservableProperty] private int sensorCount;
    [ObservableProperty] private int openEndCount;
    [ObservableProperty] private bool isBuildable = true;
    [ObservableProperty] private string problem = "";
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private double zoomFactor = 1;

    public SecondViewModel(INavigator navigator, ITrackPlanRepository repository)
    {
        _navigator = navigator;
        _repository = repository;

        SelectPieceCommand = new RelayCommand<TrackPiece>(piece => SelectedPiece = piece);
        SelectAnchorCommand = new RelayCommand<TrackAnchor>(SelectAnchor);
        SetDirectionCommand = new RelayCommand<string>(SetDirection);
        SetSensorCommand = new RelayCommand<string>(SetSensor);
        AddPartCommand = new RelayCommand<PaletteItem>(AddPart);
        GoBackCommand = new AsyncRelayCommand(async () => await _navigator.NavigateBackAsync(this));

        _ = LoadAsync();
    }

    /// <summary>Every laid part, plus the ghost preview while a drag is in flight.</summary>
    public ObservableCollection<TrackPiece> Pieces { get; } = [];

    /// <summary>The open ends, drawn as the big tappable dots a drag starts from.</summary>
    public ObservableCollection<TrackAnchor> Anchors { get; } = [];

    /// <summary>The canvas's displayed size at the current zoom, bound onto a Viewbox — zoom stays a
    /// plain bound number rather than an imperative ScrollViewer call.</summary>
    public double CanvasWidth => TrackPlanFitter.CanvasWidth * ZoomFactor;

    /// <inheritdoc cref="CanvasWidth" />
    public double CanvasHeight => TrackPlanFitter.CanvasHeight * ZoomFactor;

    public IReadOnlyList<SpeedFunctionOption> SpeedFunctionOptions { get; } =
        [.. LegoinoCatalog.SpeedFunctions.Where(option => option.Value != SpeedFunctionType.Custom)];

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

    /// <summary>
    /// The parts a deliberate tap appends, in the order they are offered: the ones a gesture can also
    /// draw first, then the three it must never guess. Bound rather than hand-written in XAML so the
    /// palette can wrap — seven fixed buttons in one row are unreachable on a phone.
    /// </summary>
    public IReadOnlyList<PaletteItem> Palette { get; } =
    [
        new("Gerade", TrackPartCatalog.StraightKey, TrackPortNames.Start),
        new("Kurve links", TrackPartCatalog.CurveLeftKey, TrackPortNames.Start),
        new("Kurve rechts", TrackPartCatalog.CurveRightKey, TrackPortNames.Start),
        new("Bahnhof", TrackPartCatalog.StationKey, TrackPortNames.Start),
        new("Weiche links", TrackPartCatalog.SwitchLeftKey, TrackPortNames.Toe),
        new("Weiche rechts", TrackPartCatalog.SwitchRightKey, TrackPortNames.Toe),
        new("Kreuzung", TrackPartCatalog.CrossingKey, TrackPortNames.CrossA1),
    ];

    public IRelayCommand<TrackPiece> SelectPieceCommand { get; }

    public IRelayCommand<TrackAnchor> SelectAnchorCommand { get; }

    public IRelayCommand<string> SetDirectionCommand { get; }

    public IRelayCommand<string> SetSensorCommand { get; }

    /// <summary>Appends one <see cref="PaletteItem"/>; the palette's seven buttons all come through here.</summary>
    public IRelayCommand<PaletteItem> AddPartCommand { get; }

    public IAsyncRelayCommand GoBackCommand { get; }

    /// <summary>
    /// Whether the window is too narrow to show everything at once — a phone, or a desktop window
    /// dragged narrow. Set by the page's responsive code-behind, which is the one place that knows the
    /// actual width. It lives here rather than in the view so the compact rules are bindable *and*
    /// unit-testable, which matters because the rendered UI cannot be verified in this environment.
    /// </summary>
    [ObservableProperty] private bool isCompact;

    /// <summary>
    /// The inverse of <see cref="IsCompact"/>, for the parts that only a wide layout has room for
    /// (the header counts, the subtitle, the speed legend). A property rather than an inverting
    /// visibility converter, matching how <see cref="HasProblem"/> avoids one.
    /// </summary>
    public bool IsRoomy => !IsCompact;

    /// <summary>
    /// Whether the parts list is open. Only consulted in the compact layout, where the list is a sheet
    /// rather than a permanent pane: stacked under the canvas it takes 40% of a phone screen whose
    /// whole point is the drawing area. Closed by default — you build first and inspect afterwards.
    /// </summary>
    [ObservableProperty] private bool isPieceListOpen;

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>Whether two open ends are near enough that closing the loop is worth offering.</summary>
    public bool CanCloseLoop => FindClosableEnds() is not null;

    /// <summary>Whether a piece is selected, so "Teil löschen" and the inspector have something to act on.</summary>
    public bool HasSelection => SelectedPiece is not null;

    /// <summary>Whether a joint does not line up. Bound directly, so no inverting converter is needed.</summary>
    public bool HasProblem => !IsBuildable;

    /// <summary>
    /// Whether the plan is still small enough that the hint on how to draw is worth the space. Two
    /// parts is "the seeded straight plus one" — by then the gesture has been understood.
    /// </summary>
    public bool IsPlanTiny => PieceCount <= 2;

    /// <summary>The open end a deliberate part is appended to: the one tapped, else the first free one.</summary>
    private TrackAnchor? ActiveAnchor => Anchors.FirstOrDefault(anchor => anchor.IsActive) ?? Anchors.FirstOrDefault();

    // ---- drawing -----------------------------------------------------------------------------

    /// <summary>
    /// A press: if it landed on an open end, that is where the new track will grow from. A press on
    /// empty canvas with nothing laid yet seeds the first straight, so a child can just start
    /// drawing instead of hunting for a button.
    /// </summary>
    [RelayCommand]
    private void DrawStart(Point point)
    {
        ResetStroke();

        if (_topology.Parts.Count == 0) SeedFirstPiece();

        if (NearestAnchor(point) is not { } anchor) return;

        SelectAnchor(anchor);

        _strokeFrom = TrackPlanWalker.Walk(_topology).OpenEnds
            .Where(end => end.PartId == anchor.PartId)
            .Where(end => string.Equals(end.PortName, anchor.PortName, StringComparison.Ordinal))
            .Select(end => (TrackOpenEnd?)end)
            .FirstOrDefault();

        if (_strokeFrom is { } start) _stroke.Add(new StudPoint(start.WorldPose.X, start.WorldPose.Y));
    }

    /// <summary>A drag: extends the stroke and re-fits the ghost preview to it.</summary>
    [RelayCommand]
    private void DrawMove(Point point)
    {
        if (_strokeFrom is not { } from) return;

        var studs = _fitted.ToStuds(point.X, point.Y);

        // A finger reports points far closer together than a part is long; re-snapping on every one
        // would be wasted work and would make the preview jitter.
        if (_stroke.Count > 0 && _stroke[^1].DistanceTo(studs) < StrokeStepStuds) return;

        _stroke.Add(studs);
        _preview = TrackSnapper.Snap(from, _stroke);

        Refresh();
    }

    /// <summary>
    /// Release: the proposed run becomes real. If the drag finished near another open end, the loop
    /// is closed exactly — by searching for the parts that actually reach it, not by bending
    /// anything to fit.
    /// </summary>
    [RelayCommand]
    private void DrawEnd(Point point)
    {
        if (_strokeFrom is null || _preview.IsEmpty)
        {
            ResetStroke();
            Refresh();
            return;
        }

        PushUndo();

        var run = _preview;
        var inherited = _parts.GetValueOrDefault(_strokeFrom.Value.PartId);

        ResetStroke();
        Apply(run, inherited);
        CloseOntoNearbyEnd(run, point);
        Refresh();

        SelectedPiece = Pieces.FirstOrDefault(piece => piece.Id == run.Parts[^1].Id);
    }

    /// <summary>Joins the two nearest open ends with the fewest parts that genuinely reach.</summary>
    [RelayCommand]
    private void CloseLoop()
    {
        if (FindClosableEnds() is not { } pair) return;

        var closure = TrackLoopCloser.Close(pair.From, pair.To);
        if (closure.Links.Count == 0) return;

        PushUndo();
        Apply(closure, _parts.GetValueOrDefault(pair.From.PartId));
        Refresh();
    }

    // ---- deliberate parts --------------------------------------------------------------------

    [RelayCommand] private void AddSwitchLeft() => Append(TrackPartCatalog.SwitchLeftKey, TrackPortNames.Toe);

    [RelayCommand] private void AddSwitchRight() => Append(TrackPartCatalog.SwitchRightKey, TrackPortNames.Toe);

    [RelayCommand] private void AddCrossing() => Append(TrackPartCatalog.CrossingKey, TrackPortNames.CrossA1);

    [RelayCommand] private void AddStation() => Append(TrackPartCatalog.StationKey, TrackPortNames.Start);

    [RelayCommand] private void AddStraight() => Append(TrackPartCatalog.StraightKey, TrackPortNames.Start);

    [RelayCommand] private void AddCurveLeft() => Append(TrackPartCatalog.CurveLeftKey, TrackPortNames.Start);

    [RelayCommand] private void AddCurveRight() => Append(TrackPartCatalog.CurveRightKey, TrackPortNames.Start);

    /// <summary>
    /// The palette's way in. The named Add* commands above stay because they read better from a
    /// hand-written button and the tests name them; both end in the same <see cref="Append"/>.
    /// </summary>
    private void AddPart(PaletteItem? item)
    {
        if (item is null)
            return;

        Append(item.PartKey, item.EntryPort);
    }

    // A point or crossing is never guessed from a gesture — the snapper only lays straights and
    // curves — so these are placed on purpose, at the active open end.
    private void Append(string specKey, string entryPort)
    {
        if (ActiveAnchor is not { } anchor) return;

        PushUndo();

        var part = new TrackPlanPart(Guid.CreateVersion7(), specKey);

        _topology = _topology
            .Add(part)
            .Connect(new TrackPlanLink(anchor.PartId, anchor.PortName, part.Id, entryPort));
        _parts[part.Id] = NewPart(part, _parts.GetValueOrDefault(anchor.PartId));

        Refresh();
        SelectedPiece = Pieces.FirstOrDefault(piece => piece.Id == part.Id);
    }

    // ---- editing -----------------------------------------------------------------------------

    [RelayCommand]
    private void DeletePiece(TrackPiece? piece)
    {
        if ((piece ?? SelectedPiece) is not { } target) return;

        PushUndo();

        _topology = _topology.Remove(target.Id);
        _parts.Remove(target.Id);

        if (SelectedPiece?.Id == target.Id) SelectedPiece = null;

        Refresh();
    }

    [RelayCommand]
    private void Undo()
    {
        if (_undoStack.Count == 0) return;

        _redoStack.Push(Capture());
        Restore(_undoStack.Pop());
    }

    [RelayCommand]
    private void Redo()
    {
        if (_redoStack.Count == 0) return;

        _undoStack.Push(Capture());
        Restore(_redoStack.Pop());
    }

    [RelayCommand]
    private void Clear()
    {
        PushUndo();

        _topology = TrackPlanTopology.Empty;
        _parts.Clear();
        SelectedPiece = null;

        Refresh();
    }

    /// <summary>A closed oval out of real parts: two half turns of eight R40 curves, joined by straights.</summary>
    [RelayCommand]
    private void TemplateOval()
    {
        PushUndo();
        Build(
            [
                .. Enumerable.Repeat(TrackPartCatalog.CurveRightKey, 8), TrackPartCatalog.StationKey,
                .. Enumerable.Repeat(TrackPartCatalog.CurveRightKey, 8), TrackPartCatalog.StraightKey,
            ],
            close: true);
        Refresh();
    }

    /// <summary>A straight run between two stations.</summary>
    [RelayCommand]
    private void TemplateLine()
    {
        PushUndo();
        Build(
            [
                TrackPartCatalog.StationKey,
                TrackPartCatalog.StraightKey,
                TrackPartCatalog.StraightKey,
                TrackPartCatalog.StationKey,
            ],
            close: false);
        Refresh();
    }

    [RelayCommand] private void ZoomIn() => ZoomFactor = Math.Min(MaxZoom, ZoomFactor + ZoomStep);

    [RelayCommand] private void ZoomOut() => ZoomFactor = Math.Max(MinZoom, ZoomFactor - ZoomStep);

    [RelayCommand] private void ZoomFit() => ZoomFactor = 1;

    /// <summary>Opens or closes the parts-list sheet. Only reachable in the compact layout.</summary>
    [RelayCommand] private void TogglePieceList() => IsPieceListOpen = !IsPieceListOpen;

    [RelayCommand]
    private void SetAccelFn(string value)
    {
        if (SelectedPiece is { } piece) piece.AccelFn = Enum.Parse<SpeedFunctionType>(value);
    }

    [RelayCommand]
    private void SetBrakeFn(string value)
    {
        if (SelectedPiece is { } piece) piece.BrakeFn = Enum.Parse<SpeedFunctionType>(value);
    }

    [RelayCommand]
    private void SetAction(string value)
    {
        if (SelectedPiece is { } piece) piece.Action = Enum.Parse<SensorActionType>(value);
    }

    /// <summary>Sets how a point stands. Written straight through, since it is the stored plan's data.</summary>
    [RelayCommand]
    private void SetRoute(string value)
    {
        if (SelectedPiece is not { } piece) return;

        piece.DefaultRoute = Enum.Parse<SwitchRoute>(value);

        if (_parts.TryGetValue(piece.Id, out var part) && part.Switch is { } config)
        {
            config.DefaultRoute = piece.DefaultRoute;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsSaving = true;
        try
        {
            CopyEditsBackToParts();
            await _repository.ReplaceAsync(Snapshot());
        }
        catch (Exception exception)
        {
            // Saying "saved" when it was not is the worst outcome here, so a failure is shown rather
            // than left to the command's swallowed task.
            Problem = $"Speichern fehlgeschlagen: {exception.Message}";
            IsBuildable = false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    // ---- plumbing ----------------------------------------------------------------------------

    private async Task LoadAsync()
    {
        TrackPlanSnapshot snapshot;

        try
        {
            snapshot = await _repository.LoadAsync();
        }
        catch (Exception exception)
        {
            // The constructor starts this without awaiting it, so an unhandled failure here would
            // vanish as an unobserved task exception and leave an editor that silently has no plan.
            // Say so on the canvas instead, and still seed a piece so it can be drawn on. The
            // message is set after seeding, because Refresh() clears it.
            SeedFirstPiece();

            Problem = $"Streckenplan konnte nicht geladen werden: {exception.Message}";
            IsBuildable = false;

            return;
        }

        _parts.Clear();
        foreach (var part in snapshot.Parts) _parts[part.Id] = part;
        _topology = snapshot.ToTopology();

        // A brand-new planner gets one piece of track, so there are two glowing dots to pull from
        // rather than a blank sheet with no way in.
        if (_topology.Parts.Count == 0)
        {
            SeedFirstPiece();
            return;
        }

        Refresh();
    }

    private void SeedFirstPiece()
    {
        var part = new TrackPlanPart(Guid.CreateVersion7(), TrackPartCatalog.StraightKey);

        _topology = TrackPlanTopology.Empty.Add(part);
        _parts[part.Id] = NewPart(part, null);

        Refresh();
    }

    // Rebuilds everything the view binds to from the one source of truth: walk the graph, fit it to
    // the canvas, then reconcile the bound collections in place so selection survives.
    private void Refresh()
    {
        var previewIds = _preview.Parts.Select(part => part.Id).ToHashSet();
        var topology = _preview.IsEmpty ? _topology : _preview.ApplyTo(_topology);
        var layout = TrackPlanWalker.Walk(topology);

        _fitted = TrackPlanFitter.Fit(layout, previewIds);

        ReconcilePieces(previewIds);
        ReconcileAnchors();

        BedPathData = _fitted.BedPathData;
        PreviewPathData = _fitted.PreviewPathData;
        PieceCount = _topology.Parts.Count;
        SensorCount = _parts.Values.Count(part => part.Sensor != SensorType.None);
        OpenEndCount = layout.OpenEnds.Count(end => !previewIds.Contains(end.PartId));
        IsBuildable = layout.IsBuildable;
        Problem = layout.Mismatches.Count == 0
            ? ""
            : FormattableString.Invariant($"Hier passt es nicht zusammen: {layout.Mismatches[0].GapStuds:0.0} Stud Lücke.");

        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(CanCloseLoop));
        OnPropertyChanged(nameof(HasProblem));
        OnPropertyChanged(nameof(IsPlanTiny));
    }

    private void ReconcilePieces(IReadOnlySet<Guid> previewIds)
    {
        var existing = Pieces.ToDictionary(piece => piece.Id);
        var keep = new HashSet<Guid>();

        foreach (var fitted in _fitted.Pieces)
        {
            keep.Add(fitted.PartId);

            if (!existing.TryGetValue(fitted.PartId, out var piece))
            {
                piece = new TrackPiece { Id = fitted.PartId };
                Pieces.Add(piece);
            }

            if (_parts.TryGetValue(fitted.PartId, out var part))
            {
                CopyInto(piece, part);
            }
            else if (previewIds.Contains(fitted.PartId))
            {
                // A ghost part has no stored behaviour yet; it only needs to look like its kind.
                piece.SpecKey = _preview.Parts.First(candidate => candidate.Id == fitted.PartId).SpecKey;
            }

            piece.ApplyGeometry(fitted);
        }

        foreach (var stale in Pieces.Where(piece => !keep.Contains(piece.Id)).ToList()) Pieces.Remove(stale);
    }

    private void ReconcileAnchors()
    {
        var active = Anchors.FirstOrDefault(anchor => anchor.IsActive);

        Anchors.Clear();

        foreach (var fitted in _fitted.Anchors)
        {
            Anchors.Add(new TrackAnchor
            {
                PartId = fitted.PartId,
                PortName = fitted.PortName,
                X = fitted.X,
                Y = fitted.Y,
                IsActive = active is not null && active.Matches(fitted.PartId, fitted.PortName),
            });
        }
    }

    private void SelectAnchor(TrackAnchor? anchor)
    {
        foreach (var candidate in Anchors)
        {
            candidate.IsActive = anchor is not null && candidate.Matches(anchor.PartId, anchor.PortName);
        }
    }

    private TrackAnchor? NearestAnchor(Point point)
    {
        TrackAnchor? nearest = null;
        var bestDistance = AnchorGrabPixels;

        foreach (var anchor in Anchors)
        {
            var dx = anchor.X - point.X;
            var dy = anchor.Y - point.Y;
            var distance = Math.Sqrt((dx * dx) + (dy * dy));

            if (distance > bestDistance) continue;

            bestDistance = distance;
            nearest = anchor;
        }

        return nearest;
    }

    // After a run lands, if it finished near some other open end, close onto it exactly.
    private void CloseOntoNearbyEnd(TrackSnapResult run, Point releasedAt)
    {
        var ends = TrackPlanWalker.Walk(_topology).OpenEnds;
        var lastId = run.Parts[^1].Id;

        // TrackOpenEnd is a struct, so "not found" comes back as default rather than null.
        var frontier = ends.FirstOrDefault(end => end.PartId == lastId);
        if (frontier.PartId == Guid.Empty) return;

        var releasePoint = _fitted.ToStuds(releasedAt.X, releasedAt.Y);
        var reach = AnchorGrabPixels * _fitted.StudsPerPixel;

        var target = ends
            .Where(end => end.PartId != frontier.PartId)
            .Select(end => (End: end, Gap: Distance(end.WorldPose, releasePoint)))
            .Where(candidate => candidate.Gap <= reach)
            .OrderBy(candidate => candidate.Gap)
            .Select(candidate => candidate.End)
            .FirstOrDefault();

        if (target.PartId == Guid.Empty) return;

        var closure = TrackLoopCloser.Close(frontier, target);
        if (closure.Links.Count == 0) return;

        Apply(closure, _parts.GetValueOrDefault(frontier.PartId));
    }

    private static double Distance(in StudPose pose, in StudPoint point)
    {
        var dx = pose.X - point.X;
        var dy = pose.Y - point.Y;

        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private void Apply(TrackSnapResult result, TrackPart? inherited)
    {
        _topology = result.ApplyTo(_topology);
        foreach (var part in result.Parts) _parts[part.Id] = NewPart(part, inherited);
    }

    private (TrackOpenEnd From, TrackOpenEnd To)? FindClosableEnds()
    {
        var ends = TrackPlanWalker.Walk(_topology).OpenEnds;
        if (ends.Count < 2) return null;

        (TrackOpenEnd From, TrackOpenEnd To)? best = null;
        var bestDistance = double.MaxValue;

        for (var i = 0; i < ends.Count; i++)
        {
            for (var j = i + 1; j < ends.Count; j++)
            {
                var distance = ends[i].WorldPose.DistanceTo(ends[j].WorldPose);

                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = (ends[i], ends[j]);
            }
        }

        // Ten parts is the closer's budget and no part reaches further than a straight, so a wider
        // gap is not one the planner can bridge — better to not offer than to offer and fail.
        return bestDistance <= TrackLoopCloser.DefaultMaxParts * TrackMetrics.StraightStuds ? best : null;
    }

    private TrackPart NewPart(TrackPlanPart part, TrackPart? inherited) => new()
    {
        Id = part.Id,
        SpecKey = part.SpecKey,
        Name = NextName(part.SpecKey),
        PlanOrder = _parts.Count,
        MaxSpeed = inherited?.MaxSpeed ?? 70,
        Direction = inherited?.Direction ?? TrackDirection.Forward,
        AccelFn = inherited?.AccelFn ?? SpeedFunctionType.EaseOut,
        BrakeFn = inherited?.BrakeFn ?? SpeedFunctionType.EaseIn,
        Switch = part.Spec.Kind == TrackPartKind.Switch ? new SwitchConfig() : null,
    };

    private string NextName(string specKey)
        => $"{Label(specKey)} {_parts.Values.Count(part => part.SpecKey == specKey) + 1}";

    private static string Label(string specKey) => specKey switch
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

    private void Build(IReadOnlyList<string> specKeys, bool close)
    {
        _topology = TrackPlanTopology.Empty;
        _parts.Clear();
        SelectedPiece = null;
        ResetStroke();

        var ids = new List<Guid>(specKeys.Count);

        foreach (var specKey in specKeys)
        {
            var part = new TrackPlanPart(Guid.CreateVersion7(), specKey);

            _topology = _topology.Add(part);
            _parts[part.Id] = NewPart(part, null);

            if (ids.Count > 0)
            {
                _topology = _topology.Connect(
                    new TrackPlanLink(ids[^1], TrackPortNames.End, part.Id, TrackPortNames.Start));
            }

            ids.Add(part.Id);
        }

        if (close && ids.Count > 1)
        {
            _topology = _topology.Connect(
                new TrackPlanLink(ids[^1], TrackPortNames.End, ids[0], TrackPortNames.Start));
        }
    }

    private void ResetStroke()
    {
        _stroke.Clear();
        _preview = TrackSnapResult.None;
        _strokeFrom = null;
    }

    // Undo covers building — laying, deleting, templates. Property edits in the inspector are not on
    // the stack: they are reversible by setting them back, and mixing the two would make one tap on
    // "Zurück" undo something the user never thought of as a step.
    private void PushUndo()
    {
        _undoStack.Push(Capture());
        _redoStack.Clear();
    }

    private PlanState Capture() => new(_topology, [.. _parts.Values.Select(part => part with { })]);

    private void Restore(PlanState state)
    {
        _topology = state.Topology;
        _parts.Clear();
        foreach (var part in state.Parts) _parts[part.Id] = part;

        SelectedPiece = null;
        ResetStroke();
        Refresh();
    }

    private TrackPlanSnapshot Snapshot() => new()
    {
        Parts = [.. _parts.Values],
        Links =
        [
            .. _topology.Links.Select(link => new TrackLink
            {
                FromPartId = link.FromPartId,
                FromPort = link.FromPort,
                ToPartId = link.ToPartId,
                ToPort = link.ToPort,
            }),
        ],
    };

    // The inspector edits the bound TrackPiece; the stored part is the thing that gets saved.
    private void CopyEditsBackToParts()
    {
        foreach (var piece in Pieces)
        {
            if (!_parts.TryGetValue(piece.Id, out var part)) continue;

            part.Name = piece.Name;
            part.MaxSpeed = piece.MaxSpeed;
            part.Direction = piece.Direction;
            part.AccelFn = piece.AccelFn;
            part.BrakeFn = piece.BrakeFn;
            part.Sensor = piece.Sensor;
            part.Action = piece.Action;
            part.SlowTarget = piece.SlowTarget;
        }
    }

    private static void CopyInto(TrackPiece piece, TrackPart part)
    {
        piece.SpecKey = part.SpecKey;
        piece.Name = part.Name;
        piece.MaxSpeed = part.MaxSpeed;
        piece.Direction = part.Direction;
        piece.AccelFn = part.AccelFn;
        piece.BrakeFn = part.BrakeFn;
        piece.Sensor = part.Sensor;
        piece.Action = part.Action;
        piece.SlowTarget = part.SlowTarget;
        piece.DefaultRoute = part.Switch?.DefaultRoute ?? SwitchRoute.Main;
    }

    private void SetDirection(string? value)
    {
        if (SelectedPiece is { } piece && value is not null) piece.Direction = Enum.Parse<TrackDirection>(value);
    }

    private void SetSensor(string? value)
    {
        if (SelectedPiece is not { } piece || value is null) return;

        piece.Sensor = Enum.Parse<SensorType>(value);

        if (_parts.TryGetValue(piece.Id, out var part)) part.Sensor = piece.Sensor;

        SensorCount = _parts.Values.Count(candidate => candidate.Sensor != SensorType.None);
    }

    partial void OnZoomFactorChanged(double value)
    {
        OnPropertyChanged(nameof(CanvasWidth));
        OnPropertyChanged(nameof(CanvasHeight));
    }

    partial void OnSelectedPieceChanged(TrackPiece? value) => OnPropertyChanged(nameof(HasSelection));

    partial void OnIsCompactChanged(bool value) => OnPropertyChanged(nameof(IsRoomy));

    /// <summary>A point the undo stack can return to: the graph plus a copy of every part's settings.</summary>
    private readonly record struct PlanState(TrackPlanTopology Topology, IReadOnlyList<TrackPart> Parts);
}
