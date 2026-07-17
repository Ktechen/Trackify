namespace Trackify.Application.Trains;

/// <summary>Finds a saved train by its id or (case-insensitive) name — shared lookup for every front-end.</summary>
public sealed class TrainResolver(ITrainStore store)
{
    public async Task<TrainConfig?> FindAsync(string nameOrId, CancellationToken ct = default)
    {
        var trains = await store.LoadAsync(ct);
        return trains.FirstOrDefault(t =>
            string.Equals(t.Id, nameOrId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.Name, nameOrId, StringComparison.OrdinalIgnoreCase));
    }
}
