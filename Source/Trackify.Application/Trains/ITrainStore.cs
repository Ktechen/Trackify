
namespace Trackify.Application.Trains;

/// <summary>
/// Persists the configured trains. The Uno app and the CLI share the schema; the concrete store
/// (JSON file, etc.) is an Infrastructure concern selected per front-end at composition time.
/// </summary>
public interface ITrainStore
{
    /// <summary>Loads all saved trains (empty if none have been saved yet).</summary>
    Task<IReadOnlyList<TrainConfig>> LoadAsync(CancellationToken ct = default);

    /// <summary>Replaces the saved set with <paramref name="trains"/>.</summary>
    Task SaveAsync(IReadOnlyList<TrainConfig> trains, CancellationToken ct = default);
}
