namespace Trackify.Application.TrackPlans;

/// <summary>
/// A measured world pose in studs, with the heading as a 22.5 degree step 0..15.
///
/// Never required and never trusted over the connections. It is the hook for importing a plan that
/// was measured rather than drawn — a photograph of a real layout yields poses, and ports that land
/// on each other within tolerance can be paired up into connections from there.
/// </summary>
public sealed record TrackPlanPoseDocument
{
    /// <summary>Studs right of the plan origin.</summary>
    public double X { get; init; }

    /// <summary>Studs below the plan origin.</summary>
    public double Y { get; init; }

    /// <summary>Facing, as a 22.5 degree step 0..15.</summary>
    public int Heading { get; init; }
}
