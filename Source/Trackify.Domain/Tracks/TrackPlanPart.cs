namespace Trackify.Domain.Tracks;

/// <summary>
/// One laid part in a plan: an identity plus which catalogue part it is. Deliberately carries no
/// position — where it sits is <em>derived</em> by walking the connections
/// (<see cref="TrackPlanWalker"/>), because a stored coordinate could describe a layout that cannot
/// physically be plugged together.
/// </summary>
public readonly record struct TrackPlanPart(Guid Id, string SpecKey)
{
    /// <summary>The catalogue geometry for this part.</summary>
    public TrackPartSpec Spec => TrackPartCatalog.Get(SpecKey);
}
