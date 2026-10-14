namespace Trackify.Domain.Tracks;

/// <summary>
/// A connection the plan claims but the geometry refuses: the walk reached the same part by two
/// different routes and they disagree. In practice this is a loop the user drew that does not
/// actually close with real parts — the most useful thing the planner can tell them, and the reason
/// the walker reports mismatches instead of letting the last write win.
/// </summary>
public sealed record TrackJoinMismatch
{
    /// <summary>The connection that could not be honoured.</summary>
    public required TrackPlanLink Link { get; init; }

    /// <summary>Where the already-placed part puts the joint.</summary>
    public required StudPose Expected { get; init; }

    /// <summary>Where the other side's own placement puts it.</summary>
    public required StudPose Actual { get; init; }

    /// <summary>How far apart the two are, in studs.</summary>
    public double GapStuds => Expected.DistanceTo(Actual);

    /// <summary>
    /// How many 22.5 degree steps the two facings differ by, as the shortest signed turn (-8..7).
    /// Signed and shortest because the useful message is "your loop is one curve short", which a
    /// plain 0..15 difference would report as 15.
    /// </summary>
    public int HeadingStepsOff
    {
        get
        {
            var difference = TrackMetrics.NormalizeHeading(Expected.Heading - Actual.Heading);

            return difference > TrackMetrics.HeadingCount / 2 ? difference - TrackMetrics.HeadingCount : difference;
        }
    }
}
