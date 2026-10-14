namespace Trackify.Domain.Tracks;

/// <summary>
/// Finds the shortest run of parts that joins two open ends exactly — the "Kreis schließen" step.
///
/// This exists because <see cref="TrackSnapper"/> cannot do it. Following a stroke greedily gets
/// near another open end but essentially never lands on it, and "near" is not a thing you can plug
/// together. Closing a loop is a search, not a fit: it has to end on the target pose exactly, which
/// is only decidable because headings are integers.
///
/// Iterative deepening, so the answer is always the fewest parts that work, with two admissible
/// bounds doing the pruning: no part advances more than a straight's length, and no part turns by
/// more than one 22.5 degree step. Those cut the 3^n tree down to something that returns instantly
/// at the depths a person would actually accept.
/// </summary>
public static class TrackLoopCloser
{
    /// <summary>
    /// How many parts the closer may add. Ten is well past what a user would accept as "close the
    /// loop for me" and still searches in well under a millisecond.
    /// </summary>
    public const int DefaultMaxParts = 10;

    /// <summary>
    /// The shortest run of parts joining <paramref name="from"/> to <paramref name="to"/>, or
    /// <see cref="TrackSnapResult.None"/> when no run of at most <paramref name="maxParts"/> does.
    /// </summary>
    public static TrackSnapResult Close(TrackOpenEnd from, TrackOpenEnd to, int maxParts = DefaultMaxParts)
    {
        if (from.PartId == to.PartId && string.Equals(from.PortName, to.PortName, StringComparison.Ordinal))
        {
            return TrackSnapResult.None;
        }

        // Already nose to nose: the loop just needs the connection, not more track.
        if (from.WorldPose.Joins(to.WorldPose))
        {
            return new TrackSnapResult
            {
                Parts = [],
                Links = [new TrackPlanLink(from.PartId, from.PortName, to.PartId, to.PortName)],
                EndPose = to.WorldPose,
                ClosesLoop = true,
            };
        }

        for (var depth = 1; depth <= maxParts; depth++)
        {
            var chosen = new List<TrackPartSpec>(depth);

            if (Search(from.WorldPose, to.WorldPose, depth, chosen)) return Build(from, to, chosen);
        }

        return TrackSnapResult.None;
    }

    // Depth-limited search for a run of exactly `remaining` parts ending on the target.
    private static bool Search(in StudPose frontier, in StudPose target, int remaining, List<TrackPartSpec> chosen)
    {
        if (remaining == 0) return frontier.Joins(target);
        if (!CanStillReach(frontier, target, remaining)) return false;

        foreach (var spec in TrackPartCatalog.DrawablePieces)
        {
            chosen.Add(spec);

            if (Search(TrackSnapper.EndPoseOf(spec, frontier), target, remaining - 1, chosen)) return true;

            chosen.RemoveAt(chosen.Count - 1);
        }

        return false;
    }

    // Both bounds are admissible, so pruning never discards a run that would have worked:
    // no part moves further than a straight, and none turns by more than one step.
    private static bool CanStillReach(in StudPose frontier, in StudPose target, int remaining)
    {
        var reach = (remaining * TrackMetrics.StraightStuds) + TrackMetrics.JoinToleranceStuds;
        if (frontier.DistanceTo(target) > reach) return false;

        // The run has to finish facing the target head on.
        var facing = TrackMetrics.NormalizeHeading(target.Heading + (TrackMetrics.HeadingCount / 2));

        return Math.Abs(TrackMetrics.ShortestHeadingStep(facing, frontier.Heading)) <= remaining;
    }

    private static TrackSnapResult Build(TrackOpenEnd from, TrackOpenEnd to, List<TrackPartSpec> chosen)
    {
        var parts = new List<TrackPlanPart>(chosen.Count);
        var links = new List<TrackPlanLink>(chosen.Count + 1);
        var frontier = from.WorldPose;
        var frontierPart = from.PartId;
        var frontierPort = from.PortName;

        foreach (var spec in chosen)
        {
            var part = new TrackPlanPart(Guid.NewGuid(), spec.Key);

            parts.Add(part);
            links.Add(new TrackPlanLink(frontierPart, frontierPort, part.Id, TrackPortNames.Start));

            frontier = TrackSnapper.EndPoseOf(spec, frontier);
            frontierPart = part.Id;
            frontierPort = TrackPortNames.End;
        }

        links.Add(new TrackPlanLink(frontierPart, frontierPort, to.PartId, to.PortName));

        return new TrackSnapResult
        {
            Parts = parts,
            Links = links,
            EndPose = frontier,
            ClosesLoop = true,
        };
    }
}
