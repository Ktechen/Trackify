namespace Trackify.Domain.Tracks;

/// <summary>
/// One rule for throwing a point automatically: a train arriving at <see cref="FromPort"/> — and,
/// when set, heading for <see cref="TowardPartId"/> — means the point must be set to
/// <see cref="SetRoute"/> before it gets there.
///
/// The data exists now; acting on it is a later job. Without it the plan could record how a point
/// happens to be standing but not what it should do, which is the thing that actually matters once
/// a train is running.
/// </summary>
public sealed class SwitchEntryRule
{
    /// <summary>Which of the point's ports the train comes in by (see <see cref="TrackPortNames"/>).</summary>
    public string FromPort { get; set; } = TrackPortNames.Toe;

    /// <summary>Optionally, the part the train is headed for — so one point can route by destination.</summary>
    public Guid? TowardPartId { get; set; }

    /// <summary>How the point must be set for that train.</summary>
    public SwitchRoute SetRoute { get; set; } = SwitchRoute.Main;
}
