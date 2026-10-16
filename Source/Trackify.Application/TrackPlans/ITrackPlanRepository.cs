namespace Trackify.Application.TrackPlans;

/// <summary>
/// Storage for the whole plan at once. Deliberately not a per-entity <c>IBaseRepository</c>: a part
/// without its connections is meaningless and a half-written graph would render as track that falls
/// apart, so the plan is loaded and replaced as one unit.
/// </summary>
public interface ITrackPlanRepository
{
    /// <summary>Loads the saved plan, or <see cref="TrackPlanSnapshot.Empty"/> when there is none.</summary>
    Task<TrackPlanSnapshot> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Replaces the saved plan entirely.</summary>
    Task ReplaceAsync(TrackPlanSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>Adds parts and connections to the saved plan, keeping what is already there.</summary>
    Task AppendAsync(TrackPlanSnapshot snapshot, CancellationToken cancellationToken = default);
}
