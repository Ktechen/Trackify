namespace Trackify.Application.Trains;

/// <summary>
/// Read-side facade over the train repository for every front-end. It maps the Domain entity to
/// <see cref="TrainDto"/> at the boundary, so callers (CLI / Uno app) only ever see DTOs — the
/// entity stays a persistence detail behind the repository.
/// </summary>
public sealed class TrainService(ITrainRepository repository) : ITrainService
{
    /// <summary>All saved trains as DTOs.</summary>
    public async Task<IReadOnlyList<TrainDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var trains = await repository.GetAllAsync(cancellationToken);
        return trains.Select(train => train.ToDto()).ToList();
    }

    /// <summary>Finds a saved train by its id or (case-insensitive) name; null if none matches.</summary>
    public async Task<TrainDto?> FindAsync(string nameOrId, CancellationToken cancellationToken = default)
    {
        var trains = await repository.GetAllAsync(cancellationToken);
        var match = trains.FirstOrDefault(train =>
            string.Equals(train.Id.ToString(), nameOrId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(train.Name, nameOrId, StringComparison.OrdinalIgnoreCase));
        return match?.ToDto();
    }

    /// <summary>
    /// Saves a discovered hub as a train, de-duplicated by hub identity (HubId, then MAC): if a train
    /// already refers to that hub it is returned unchanged. Returns the saved (or existing) train.
    /// </summary>
    public async Task<TrainDto> SaveDiscoveredAsync(DiscoveredHubDto hub, CancellationToken cancellationToken = default)
    {
        var trains = await repository.GetAllAsync(cancellationToken);
        var existing = trains.FirstOrDefault(train =>
            (!string.IsNullOrWhiteSpace(hub.Id) && string.Equals(train.HubId, hub.Id, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(hub.MacAddress) && string.Equals(train.BleAddress, hub.MacAddress, StringComparison.OrdinalIgnoreCase)));
        if (existing is not null)
            return existing.ToDto();

        var train = new Train
        {
            Name = string.IsNullOrWhiteSpace(hub.Name) ? (hub.MacAddress ?? hub.Id) : hub.Name,
            HubId = hub.Id,
            BleAddress = hub.MacAddress ?? string.Empty,
            Hub = hub.HubType ?? HubType.PoweredUpHub,
        };
        await repository.AddAsync(train, cancellationToken);
        return train.ToDto();
    }
}
