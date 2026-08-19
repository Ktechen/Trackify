namespace Trackify.Helpers;

/// <summary>
/// Lays out an arbitrary, user-built track (straights/curves/stations/switches, branching at
/// switches into their own strands) on the shared 900x600 canvas — turtle graphics: walk each
/// strand's pieces in order from an anchor pose (position + heading), each piece advancing the pose.
/// A branch strand's anchor is its switch's end pose, rotated by the branch's peel-off direction, so
/// switches never need geometry of their own beyond a short stub.
/// Two passes, because a plan grows wherever the user builds and how far it reaches is only known
/// once every strand is walked: the walk collects each piece's shape in unbounded world coordinates
/// from a (0,0) anchor, then the emit pass bakes in the scale/offset that centers the finished plan
/// on the canvas — so nothing can walk off the visible area, and everything the canvas needs is a
/// plain, ready-to-draw coordinate (no data-bound render transforms, which don't inherit a
/// DataContext on every platform).
/// Replaces the previous closed-form fixed 8-segment stadium loop now that the Streckenplaner has
/// real CRUD instead of one hard-coded layout.
/// </summary>
public static class TrackGeometry
{
    private const double StraightLength = 90;
    private const double StationLength = 90;
    private const double SwitchLength = 55;
    private const double CurveRadius = 70;
    private const double Trim = 6;
    private const double CanvasWidth = 900;
    private const double CanvasHeight = 600;

    /// <summary>Kept free on every side of the fitted track, so the ballast stroke and the label/sensor
    /// markers hanging off the centerline stay on the canvas — see <c>TrackSegment.ApplyGeometry</c>
    /// for the outward offsets this has to cover.</summary>
    private const double Margin = 70;

    /// <summary>Vertical spacing between strands parked below the plan by the orphan pass.</summary>
    private const double OrphanLaneGap = 170;

    /// <summary>Points sampled along an arc to bound it — enough that the fit never clips a curve's
    /// bulge (a quarter of a 70-radius circle deviates well under a pixel between samples).</summary>
    private const int ArcSamples = 8;

    private static readonly double QuarterTurn = Math.PI / 2;
    private static readonly double BranchPeel = Math.PI / 6;

    public static TrackLayout Build(IReadOnlyList<TrackGeometryInput> segments)
    {
        var pieces = Walk(segments);
        var view = Fit(pieces);
        var result = new Dictionary<Guid, SegmentGeometry>(pieces.Count);

        foreach (var piece in pieces) result[piece.Id] = Emit(piece, view);

        return new TrackLayout(
            result,
            // The ballast layer behind the colored track — every piece's own path, as separate subpaths
            // (each starting with its own "M") within one Data string, drawn with a thick stroke.
            string.Join(" ", result.Values.Select(g => g.PathData)));
    }

    private static List<Piece> Walk(IReadOnlyList<TrackGeometryInput> segments)
    {
        var byStrand = segments.GroupBy(s => s.StrandId).ToDictionary(g => g.Key, g => g.OrderBy(s => s.Order).ToList());
        var pieces = new List<Piece>(segments.Count);
        var anchors = new Dictionary<Guid, Pose> { [Guid.Empty] = new(0, 0, 0) };
        var pending = new Queue<Guid>([Guid.Empty]);
        var visited = new HashSet<Guid>();
        var lane = 0;

        while (true)
        {
            while (pending.Count > 0)
            {
                var strandId = pending.Dequeue();
                if (!visited.Add(strandId) || !byStrand.TryGetValue(strandId, out var strand)) continue;

                var pose = anchors[strandId];
                foreach (var segment in strand)
                {
                    var piece = segment.Type == SegmentType.Curve
                        ? ArcPiece(segment.Id, pose, segment.Curve == CurveDirection.Left ? -1.0 : 1.0)
                        : LinePiece(segment.Id, pose, LengthOf(segment.Type));

                    pieces.Add(piece);
                    pose = piece.End;

                    if (segment.Type == SegmentType.Switch && segment.BranchStrandId is { } branchId)
                    {
                        var peelSign = segment.BranchDirection == CurveDirection.Left ? -1.0 : 1.0;
                        anchors[branchId] = pose with { Heading = pose.Heading + (peelSign * BranchPeel) };
                        pending.Enqueue(branchId);
                    }
                }
            }

            // Orphan pass — a strand nothing branches into (a branch whose Weiche is missing, e.g. from a
            // plan saved mid-write) is unreachable from the main anchor. Without this it would get no
            // geometry at all and keep rendering wherever the previous layout happened to leave it, so it
            // is parked on its own lane below the plan: visible, and therefore deletable.
            // Guid.Empty is always visited by the first inner pass, so it doubles as "nothing left".
            var orphanStrandId = byStrand.Keys.FirstOrDefault(id => !visited.Contains(id));
            if (orphanStrandId == Guid.Empty) return pieces;

            anchors[orphanStrandId] = new Pose(0, ++lane * OrphanLaneGap, 0);
            pending.Enqueue(orphanStrandId);
        }
    }

    /// <summary>Centers the plan's bounding box on the canvas, shrinking it (never enlarging) when it
    /// would otherwise reach into the margin reserved for the labels and sensor markers.</summary>
    private static View Fit(List<Piece> pieces)
    {
        if (pieces.Count == 0) return new View(1, CanvasWidth / 2, CanvasHeight / 2);

        var extent = pieces.Select(p => p.Extent).Aggregate((a, b) => a.Union(b));
        var scale = Math.Min(1, Math.Min(
            (CanvasWidth - (2 * Margin)) / Math.Max(extent.MaxX - extent.MinX, 1),
            (CanvasHeight - (2 * Margin)) / Math.Max(extent.MaxY - extent.MinY, 1)));

        return new View(
            scale,
            (CanvasWidth / 2) - (scale * (extent.MinX + extent.MaxX) / 2),
            (CanvasHeight / 2) - (scale * (extent.MinY + extent.MaxY) / 2));
    }

    private static SegmentGeometry Emit(Piece piece, View view)
    {
        var tanX = Math.Cos(piece.MidHeading);
        var tanY = Math.Sin(piece.MidHeading);
        var (pathData, hitPathData) = piece.Arc is { } arc ? ArcPaths(arc, view) : LinePaths(piece.Line, view);

        return new SegmentGeometry(
            piece.Id, pathData, hitPathData,
            MidX: view.X(piece.MidX), MidY: view.Y(piece.MidY),
            // The left-hand normal of the travel direction — for straights *and* curves, so labels and
            // sensor markers stay on the same side of the track throughout. (Deriving a curve's normal
            // from "away from the arc center" instead flips it between left and right curves.)
            OutwardX: tanY, OutwardY: -tanX,
            TanX: tanX, TanY: tanY);
    }

    private static double LengthOf(SegmentType type) => type switch
    {
        SegmentType.Station => StationLength,
        SegmentType.Switch => SwitchLength,
        _ => StraightLength,
    };

    private static Piece LinePiece(Guid id, Pose start, double length)
    {
        var end = new Pose(
            start.X + (Math.Cos(start.Heading) * length),
            start.Y + (Math.Sin(start.Heading) * length),
            start.Heading);

        return new Piece(
            id, end,
            MidX: (start.X + end.X) / 2, MidY: (start.Y + end.Y) / 2, MidHeading: start.Heading,
            Extent: Extent.Of([(start.X, start.Y), (end.X, end.Y)]),
            Line: new LineShape(start.X, start.Y, end.X, end.Y),
            Arc: null);
    }

    private static Piece ArcPiece(Guid id, Pose start, double sign)
    {
        var cx = start.X + (CurveRadius * Math.Cos(start.Heading + (sign * QuarterTurn)));
        var cy = start.Y + (CurveRadius * Math.Sin(start.Heading + (sign * QuarterTurn)));
        var startAngle = Math.Atan2(start.Y - cy, start.X - cx);
        var endAngle = startAngle + (sign * QuarterTurn);
        var midAngle = startAngle + (sign * QuarterTurn / 2);
        var end = new Pose(
            cx + (CurveRadius * Math.Cos(endAngle)),
            cy + (CurveRadius * Math.Sin(endAngle)),
            start.Heading + (sign * QuarterTurn));

        return new Piece(
            id, end,
            MidX: cx + (CurveRadius * Math.Cos(midAngle)), MidY: cy + (CurveRadius * Math.Sin(midAngle)),
            MidHeading: start.Heading + (sign * QuarterTurn / 2),
            Extent: ArcExtent(cx, cy, startAngle, endAngle),
            Line: null,
            Arc: new ArcShape(cx, cy, startAngle, endAngle, sign));
    }

    // Drawn path and (untrimmed, wider-stroked) hit path. The trim gap that separates neighboring
    // pieces is applied after fitting, so it stays the same few pixels at any scale.
    private static (string PathData, string HitPathData) LinePaths(LineShape? shape, View view)
    {
        var line = shape!.Value;
        var (x0, y0) = (view.X(line.X0), view.Y(line.Y0));
        var (x1, y1) = (view.X(line.X1), view.Y(line.Y1));

        return (LinePath(x0, y0, x1, y1, Trim), LinePath(x0, y0, x1, y1, 0));
    }

    private static (string PathData, string HitPathData) ArcPaths(ArcShape arc, View view)
    {
        var (cx, cy) = (view.X(arc.Cx), view.Y(arc.Cy));
        var radius = CurveRadius * view.Scale;
        var trimAngle = Trim / radius;

        return (
            ArcPath(cx, cy, radius, arc.StartAngle + (arc.Sign * trimAngle), arc.EndAngle - (arc.Sign * trimAngle), arc.Sign),
            ArcPath(cx, cy, radius, arc.StartAngle, arc.EndAngle, arc.Sign));
    }

    private static string LinePath(double x0, double y0, double x1, double y1, double trim)
    {
        if (trim > 0)
        {
            var len = Math.Sqrt(((x1 - x0) * (x1 - x0)) + ((y1 - y0) * (y1 - y0)));
            if (len > trim * 2)
            {
                var t = trim / len;
                (x0, y0, x1, y1) = (x0 + ((x1 - x0) * t), y0 + ((y1 - y0) * t), x1 - ((x1 - x0) * t), y1 - ((y1 - y0) * t));
            }
        }
        return FormattableString.Invariant($"M {x0:0.0} {y0:0.0} L {x1:0.0} {y1:0.0}");
    }

    private static string ArcPath(double cx, double cy, double radius, double startAngle, double endAngle, double sign)
    {
        var sx = cx + (radius * Math.Cos(startAngle));
        var sy = cy + (radius * Math.Sin(startAngle));
        var ex = cx + (radius * Math.Cos(endAngle));
        var ey = cy + (radius * Math.Sin(endAngle));
        var sweep = sign > 0 ? 1 : 0;
        return FormattableString.Invariant(
            $"M {sx:0.0} {sy:0.0} A {radius:0.0},{radius:0.0} 0 0,{sweep} {ex:0.0} {ey:0.0}");
    }

    private static Extent ArcExtent(double cx, double cy, double startAngle, double endAngle)
    {
        var points = new (double X, double Y)[ArcSamples + 1];
        for (var i = 0; i <= ArcSamples; i++)
        {
            var angle = startAngle + ((endAngle - startAngle) * i / ArcSamples);
            points[i] = (cx + (CurveRadius * Math.Cos(angle)), cy + (CurveRadius * Math.Sin(angle)));
        }

        return Extent.Of(points);
    }

    private readonly record struct Pose(double X, double Y, double Heading);

    /// <summary>One walked piece in world coordinates: where it leaves the pose, where its markers go,
    /// what it covers, and the shape to emit — exactly one of <paramref name="Line"/>/<paramref name="Arc"/>.</summary>
    [ImplicitKeys(IsEnabled = false)]
    private readonly record struct Piece(
        Guid Id,
        Pose End,
        double MidX,
        double MidY,
        double MidHeading,
        Extent Extent,
        LineShape? Line,
        ArcShape? Arc);

    private readonly record struct LineShape(double X0, double Y0, double X1, double Y1);

    private readonly record struct ArcShape(double Cx, double Cy, double StartAngle, double EndAngle, double Sign);

    /// <summary>World-to-canvas transform from the fit pass.</summary>
    private readonly record struct View(double Scale, double OffsetX, double OffsetY)
    {
        public double X(double worldX) => (worldX * Scale) + OffsetX;

        public double Y(double worldY) => (worldY * Scale) + OffsetY;
    }

    private readonly record struct Extent(double MinX, double MinY, double MaxX, double MaxY)
    {
        public static Extent Of(IReadOnlyList<(double X, double Y)> points) => new(
            points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X), points.Max(p => p.Y));

        public Extent Union(Extent other) => new(
            Math.Min(MinX, other.MinX), Math.Min(MinY, other.MinY),
            Math.Max(MaxX, other.MaxX), Math.Max(MaxY, other.MaxY));
    }
}

/// <summary>The subset of a track segment's fields <see cref="TrackGeometry"/> needs to lay it out —
/// decoupled from the presentation model so the geometry engine has no upward dependency.</summary>
[ImplicitKeys(IsEnabled = false)]
public readonly record struct TrackGeometryInput(
    Guid Id,
    SegmentType Type,
    CurveDirection? Curve,
    Guid StrandId,
    int Order,
    Guid? BranchStrandId,
    CurveDirection? BranchDirection);

[ImplicitKeys(IsEnabled = false)]
public readonly record struct SegmentGeometry(
    Guid Id,
    string PathData,
    string HitPathData,
    double MidX,
    double MidY,
    double OutwardX,
    double OutwardY,
    double TanX,
    double TanY);
