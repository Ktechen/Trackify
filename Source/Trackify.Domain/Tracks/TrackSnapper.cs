namespace Trackify.Domain.Tracks;

/// <summary>
/// Turns a drawn stroke into real track: walking out from an open end, it repeatedly picks whichever
/// straight or R40 curve follows the stroke best, until the stroke runs out.
///
/// This is the piece that makes "just draw it" safe. The user's finger moves freely, but nothing
/// free-form is ever stored — only whole catalogue parts plugged port to port, so whatever appears
/// on screen can be built out of the box of track on the floor. Fitting greedily (rather than
/// searching for a globally best fit) is deliberate: it runs on every pointer move, and a preview
/// that follows the finger immediately is worth more than one that is a few millimetres closer.
///
/// Closing a loop is a different problem and belongs to <see cref="TrackLoopCloser"/>: greedy
/// following will come close to another open end but essentially never land on it exactly.
/// </summary>
public static class TrackSnapper
{
    /// <summary>How many parts one gesture may lay, so a wild drag cannot fill the plan.</summary>
    public const int DefaultMaxParts = 60;

    /// <summary>
    /// Studs of error per 22.5 degree step of heading disagreement with the stroke. Keeps the fit
    /// from zig-zagging between a left and a right curve when a straight tracks the stroke better.
    /// </summary>
    private const double HeadingPenaltyStudsPerStep = 2.5;

    /// <summary>
    /// Lays parts from <paramref name="from"/> along <paramref name="stroke"/>. Returns
    /// <see cref="TrackSnapResult.None"/> when the stroke is too short to justify a single part.
    /// </summary>
    public static TrackSnapResult Snap(
        TrackOpenEnd from,
        IReadOnlyList<StudPoint> stroke,
        int maxParts = DefaultMaxParts)
    {
        var path = DrawnPath.From(stroke);
        if (path.TotalLength <= 0) return TrackSnapResult.None;

        var parts = new List<TrackPlanPart>();
        var links = new List<TrackPlanLink>();
        var frontier = from.WorldPose;
        var frontierPart = from.PartId;
        var frontierPort = from.PortName;
        var travelled = 0.0;

        while (parts.Count < maxParts)
        {
            var best = BestPiece(frontier, path, travelled);
            if (best is not { } piece) break;

            var part = new TrackPlanPart(Guid.NewGuid(), piece.Spec.Key);

            parts.Add(part);
            links.Add(new TrackPlanLink(frontierPart, frontierPort, part.Id, TrackPortNames.Start));

            frontier = piece.End;
            frontierPart = part.Id;
            frontierPort = TrackPortNames.End;
            travelled += piece.Spec.TotalLengthStuds;
        }

        if (parts.Count == 0) return TrackSnapResult.None;

        return new TrackSnapResult
        {
            Parts = parts,
            Links = links,
            EndPose = frontier,
        };
    }

    /// <summary>
    /// Where a part's local frame has to sit for its <paramref name="portName"/> to plug into
    /// <paramref name="openEnd"/> — the one placement rule the snapper and the loop closer share.
    /// </summary>
    public static StudPose AnchorFor(TrackPartSpec spec, string portName, in StudPose openEnd)
        => openEnd.Reversed().Compose(spec.PortPose(portName).Inverse());

    /// <summary>The pose a part leaves behind when plugged into <paramref name="openEnd"/> by its start.</summary>
    internal static StudPose EndPoseOf(TrackPartSpec spec, in StudPose openEnd)
        => AnchorFor(spec, TrackPortNames.Start, openEnd).Compose(spec.PortPose(TrackPortNames.End));

    // Picks the catalogue piece whose far end lands closest to where the stroke will be once this
    // piece's worth of track has been laid. Returns null when too little stroke is left to bother.
    private static Candidate? BestPiece(in StudPose frontier, in DrawnPath path, double travelled)
    {
        Candidate? best = null;
        var bestScore = double.MaxValue;

        foreach (var spec in TrackPartCatalog.DrawablePieces)
        {
            var length = spec.TotalLengthStuds;

            // Stop once the stroke has less than half a part left to follow, so a gesture does not
            // overshoot its own end by a whole piece.
            if (travelled + (length / 2) > path.TotalLength) continue;

            var end = EndPoseOf(spec, frontier);
            var target = path.PointAt(travelled + length);
            var headingOff = Math.Abs(
                TrackMetrics.ShortestHeadingStep(end.Heading, path.HeadingAt(travelled + length)));
            var strayX = end.X - target.X;
            var strayY = end.Y - target.Y;
            var score = Math.Sqrt((strayX * strayX) + (strayY * strayY))
                + (headingOff * HeadingPenaltyStudsPerStep);

            if (score >= bestScore) continue;

            bestScore = score;
            best = new Candidate(spec, end);
        }

        return best;
    }

    /// <summary>One piece under consideration: which part, and where it would leave off.</summary>
    private readonly record struct Candidate(TrackPartSpec Spec, StudPose End);

    /// <summary>
    /// The user's stroke with cumulative lengths, so it can be sampled by distance along itself
    /// rather than by point index — a finger produces points at wildly uneven spacing.
    /// </summary>
    private readonly record struct DrawnPath(IReadOnlyList<StudPoint> Points, double[] Cumulative)
    {
        public double TotalLength => Cumulative.Length == 0 ? 0 : Cumulative[^1];

        public static DrawnPath From(IReadOnlyList<StudPoint> points)
        {
            if (points.Count == 0) return new DrawnPath(points, []);

            var cumulative = new double[points.Count];
            for (var i = 1; i < points.Count; i++)
            {
                cumulative[i] = cumulative[i - 1] + points[i].DistanceTo(points[i - 1]);
            }

            return new DrawnPath(points, cumulative);
        }

        /// <summary>The point <paramref name="distance"/> studs along the stroke, clamped to its ends.</summary>
        public StudPoint PointAt(double distance)
        {
            if (Points.Count == 0) return default;
            if (distance <= 0) return Points[0];
            if (distance >= TotalLength) return Points[^1];

            var index = Segment(distance);
            var spanLength = Cumulative[index + 1] - Cumulative[index];
            var t = spanLength <= 0 ? 0 : (distance - Cumulative[index]) / spanLength;

            return new StudPoint(
                Points[index].X + ((Points[index + 1].X - Points[index].X) * t),
                Points[index].Y + ((Points[index + 1].Y - Points[index].Y) * t));
        }

        /// <summary>
        /// The stroke's own direction at <paramref name="distance"/>, rounded to the nearest of the
        /// 16 headings track can actually point in.
        /// </summary>
        public int HeadingAt(double distance)
        {
            if (Points.Count < 2) return 0;

            var index = Segment(Math.Clamp(distance, 0, TotalLength));
            var dx = Points[index + 1].X - Points[index].X;
            var dy = Points[index + 1].Y - Points[index].Y;
            var steps = Math.Atan2(dy, dx) / TrackMetrics.RadiansPerHeadingStep;

            return TrackMetrics.NormalizeHeading((int)Math.Round(steps));
        }

        // Index of the stroke span containing `distance`.
        private int Segment(double distance)
        {
            for (var i = 0; i < Cumulative.Length - 1; i++)
            {
                if (distance <= Cumulative[i + 1]) return i;
            }

            return Math.Max(0, Cumulative.Length - 2);
        }
    }
}
