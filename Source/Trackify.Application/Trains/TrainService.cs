namespace Trackify.Application.Trains;

/// <summary>
/// Read-side facade over the train repository for every front-end. It maps the Domain entity to
/// <see cref="TrainDto"/> at the boundary, so callers (CLI / Uno app) only ever see DTOs — the
/// entity stays a persistence detail behind the repository.
/// </summary>
public sealed class TrainService(ITrainRepository repository)
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
}
