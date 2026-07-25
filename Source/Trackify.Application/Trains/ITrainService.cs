namespace Trackify.Application.Trains;

/// <summary>
/// Read-side facade over the train repository: returns <see cref="TrainDto"/>s at the boundary so
/// front-ends never see the Domain entity. Front-ends depend on this abstraction.
/// </summary>
public interface ITrainService
{
    /// <summary>All saved trains as DTOs.</summary>
    Task<IReadOnlyList<TrainDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Finds a saved train by its id or (case-insensitive) name; null if none matches.</summary>
    Task<TrainDto?> FindAsync(string nameOrId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a discovered hub as a train, de-duplicated by hub identity (HubId, then MAC): if a train
    /// already refers to that hub it is returned unchanged. Returns the saved (or existing) train.
    /// </summary>
    Task<TrainDto> SaveDiscoveredAsync(DiscoveredHubDto hub, CancellationToken cancellationToken = default);
}
