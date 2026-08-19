namespace Trackify.Application.Trains;

/// <inheritdoc cref="ITrackPlanService" />
public sealed class TrackPlanService : ITrackPlanService
{
    public IReadOnlyList<TrackSegmentDto> AppendPart(
        IReadOnlyList<TrackSegmentDto> segments, Guid strandId, SegmentType type, CurveDirection? curve = null)
    {
        var strand = StrandSorted(segments, strandId);
        var predecessor = strand.Count > 0 ? strand[^1] : FindAnchor(segments, strandId);

        var part = new TrackSegmentDto
        {
            Id = Guid.CreateVersion7(),
            Name = NextName(segments, type, curve),
            Type = type,
            Curve = type == SegmentType.Curve ? curve ?? CurveDirection.Right : null,
            MaxSpeed = predecessor?.MaxSpeed ?? 70,
            Direction = predecessor?.Direction ?? TrackDirection.Forward,
            AccelFn = predecessor?.AccelFn ?? SpeedFunctionType.EaseOut,
            BrakeFn = predecessor?.BrakeFn ?? SpeedFunctionType.EaseIn,
            StrandId = strandId,
            Order = strand.Count,
        };

        return [.. segments, part];
    }

    public SwitchInsertResult InsertSwitch(IReadOnlyList<TrackSegmentDto> segments, Guid strandId, CurveDirection branchDirection)
    {
        var appended = AppendPart(segments, strandId, SegmentType.Switch);
        var switchSegment = appended[^1];
        var branchStrandId = Guid.CreateVersion7();

        var updated = appended
            .Select(s => s.Id == switchSegment.Id ? s with { BranchStrandId = branchStrandId, BranchDirection = branchDirection } : s)
            .ToList();

        return new SwitchInsertResult(updated, branchStrandId);
    }

    public IReadOnlyList<TrackSegmentDto> ApplyTemplate(TemplateType template)
    {
        var parts = template switch
        {
            TemplateType.Oval => OvalParts,
            TemplateType.Acht => OvalParts.Concat(MirroredOvalParts),
            TemplateType.PunktZuPunkt => PunktZuPunktParts,
            _ => [],
        };

        var segments = new List<TrackSegmentDto>();
        var order = 0;
        foreach (var (type, curve) in parts)
        {
            segments.Add(new TrackSegmentDto
            {
                Id = Guid.CreateVersion7(),
                Name = NextName(segments, type, curve),
                Type = type,
                Curve = curve,
                MaxSpeed = 70,
                StrandId = Guid.Empty,
                Order = order++,
            });
        }

        return segments;
    }

    public IReadOnlyList<TrackSegmentDto> Duplicate(IReadOnlyList<TrackSegmentDto> segments, Guid segmentId)
    {
        var source = segments.First(s => s.Id == segmentId);
        var strand = StrandSorted(segments, source.StrandId);
        var index = strand.FindIndex(s => s.Id == segmentId);

        strand.Insert(index + 1, source with { Id = Guid.CreateVersion7(), Name = source.Name + " (Kopie)" });

        return ReplaceStrand(segments, source.StrandId, strand);
    }

    public IReadOnlyList<TrackSegmentDto> Reorder(IReadOnlyList<TrackSegmentDto> segments, Guid segmentId, bool up)
    {
        var source = segments.First(s => s.Id == segmentId);
        var strand = StrandSorted(segments, source.StrandId);
        var index = strand.FindIndex(s => s.Id == segmentId);
        var swapWith = up ? index - 1 : index + 1;

        if (swapWith < 0 || swapWith >= strand.Count) return segments;

        (strand[index], strand[swapWith]) = (strand[swapWith], strand[index]);

        return ReplaceStrand(segments, source.StrandId, strand);
    }

    public IReadOnlyList<TrackSegmentDto> Delete(IReadOnlyList<TrackSegmentDto> segments, Guid segmentId)
    {
        var source = segments.First(s => s.Id == segmentId);
        var remaining = segments.Where(s => s.Id != segmentId).ToList();

        if (source.Type == SegmentType.Switch && source.BranchStrandId is { } branchId)
        {
            remaining = RemoveStrandCascade(remaining, branchId);
        }

        var strand = StrandSorted(remaining, source.StrandId);
        return ReplaceStrand(remaining, source.StrandId, strand);
    }

    public IReadOnlyList<TrackSegmentDto> Clear() => [];

    // Every strand's open end to append to: its own last segment by Order, or — for a fresh, empty
    // branch strand — the Weiche that anchors it (so the branch's first part still inherits speed/
    // direction/f(x), matching "erbt ... vom Vorgänger" even before the branch has segments of its own).
    private static TrackSegmentDto? FindAnchor(IReadOnlyList<TrackSegmentDto> segments, Guid strandId)
        => strandId == Guid.Empty ? null : segments.FirstOrDefault(s => s.Type == SegmentType.Switch && s.BranchStrandId == strandId);

    private static List<TrackSegmentDto> StrandSorted(IReadOnlyList<TrackSegmentDto> segments, Guid strandId)
        => [.. segments.Where(s => s.StrandId == strandId).OrderBy(s => s.Order)];

    // Renumbers the given strand's Order to its list position and splices it back into the full set.
    private static IReadOnlyList<TrackSegmentDto> ReplaceStrand(IReadOnlyList<TrackSegmentDto> segments, Guid strandId, List<TrackSegmentDto> strand)
    {
        for (var i = 0; i < strand.Count; i++) strand[i] = strand[i] with { Order = i };
        return [.. segments.Where(s => s.StrandId != strandId), .. strand];
    }

    // Deleting a Weiche takes its whole branch with it, including any further Weichen nested inside.
    private static List<TrackSegmentDto> RemoveStrandCascade(List<TrackSegmentDto> segments, Guid strandId)
    {
        var branchSegments = segments.Where(s => s.StrandId == strandId).ToList();
        var result = segments.Where(s => s.StrandId != strandId).ToList();

        foreach (var segment in branchSegments)
        {
            if (segment.Type == SegmentType.Switch && segment.BranchStrandId is { } nestedBranchId)
            {
                result = RemoveStrandCascade(result, nestedBranchId);
            }
        }

        return result;
    }

    private static string NextName(IReadOnlyList<TrackSegmentDto> segments, SegmentType type, CurveDirection? curve)
        => $"{PartLabel(type, curve)} {segments.Count(s => s.Type == type && s.Curve == curve) + 1}";

    private static string PartLabel(SegmentType type, CurveDirection? curve) => type switch
    {
        SegmentType.Straight => "Gerade",
        SegmentType.Curve => curve == CurveDirection.Left ? "Kurve links" : "Kurve rechts",
        SegmentType.Switch => "Weiche",
        SegmentType.Station => "Bahnhof",
        _ => "Segment",
    };

    // A running oval: half straight, half station (the "top" straight split at its midpoint, matching
    // the original hard-coded seed), a 180° turn (two 90° curve pieces), the "bottom" straight (also
    // split in two), then the opposite 180° turn back to the start heading.
    private static readonly (SegmentType Type, CurveDirection? Curve)[] OvalParts =
    [
        (SegmentType.Straight, null), (SegmentType.Station, null),
        (SegmentType.Curve, CurveDirection.Right), (SegmentType.Curve, CurveDirection.Right),
        (SegmentType.Straight, null), (SegmentType.Straight, null),
        (SegmentType.Curve, CurveDirection.Right), (SegmentType.Curve, CurveDirection.Right),
    ];

    // The second lobe, turning the other way. OvalParts ends back at the start pose, so this one has to
    // *lead* with its 180° turn: leading with straights (like OvalParts does) would retrace the first
    // lobe's opening straight and station piece-for-piece — two segments stacked at identical
    // coordinates. Turning away first and closing with the straights instead puts the lobe on the far
    // side of the shared point, which is what makes the pair read as a figure-eight.
    private static readonly (SegmentType Type, CurveDirection? Curve)[] MirroredOvalParts =
    [
        (SegmentType.Curve, CurveDirection.Left), (SegmentType.Curve, CurveDirection.Left),
        (SegmentType.Straight, null), (SegmentType.Straight, null),
        (SegmentType.Curve, CurveDirection.Left), (SegmentType.Curve, CurveDirection.Left),
        (SegmentType.Straight, null), (SegmentType.Station, null),
    ];

    private static readonly (SegmentType Type, CurveDirection? Curve)[] PunktZuPunktParts =
    [
        (SegmentType.Station, null), (SegmentType.Straight, null), (SegmentType.Straight, null), (SegmentType.Station, null),
    ];
}
