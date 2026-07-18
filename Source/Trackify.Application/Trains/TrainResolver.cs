namespace Trackify.Application.Trains;

/// <summary>Finds a saved train by its id or (case-insensitive) name — shared lookup for every front-end.</summary>
public sealed class TrainResolver(ITrainRepository repository)
{
    public async Task<Train?> FindAsync(string nameOrId, CancellationToken cancellationToken = default)
    {
        var trains = await repository.GetAllAsync(cancellationToken);
        return trains.FirstOrDefault(train =>
            string.Equals(train.Id.ToString(), nameOrId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(train.Name, nameOrId, StringComparison.OrdinalIgnoreCase));
    }
}
