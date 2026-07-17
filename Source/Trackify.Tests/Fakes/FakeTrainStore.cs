using Trackify.Application.Trains;

namespace Trackify.Tests.Fakes;

/// <summary>An in-memory <see cref="ITrainStore"/> seeded with a fixed set of trains.</summary>
public sealed class FakeTrainStore(params TrainConfig[] trains) : ITrainStore
{
    private List<TrainConfig> _trains = [.. trains];

    public Task<IReadOnlyList<TrainConfig>> LoadAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TrainConfig>>(_trains);

    public Task SaveAsync(IReadOnlyList<TrainConfig> trainsToSave, CancellationToken ct = default)
    {
        _trains = [.. trainsToSave];
        return Task.CompletedTask;
    }
}
