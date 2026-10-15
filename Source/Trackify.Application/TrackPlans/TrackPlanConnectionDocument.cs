namespace Trackify.Application.TrackPlans;

/// <summary>
/// One joint, written as "p1:end" / "p2:start" — compact and readable, which matters because a
/// person is expected to be able to open this file and understand it.
/// </summary>
public sealed record TrackPlanConnectionDocument
{
    /// <summary>One side, as "&lt;part id&gt;:&lt;port&gt;".</summary>
    public string From { get; init; } = "";

    /// <summary>The other side, same form.</summary>
    public string To { get; init; } = "";
}
