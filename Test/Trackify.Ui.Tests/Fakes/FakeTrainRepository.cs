using System.Linq.Expressions;
using Trackify.Application.Trains;
using Trackify.Domain.Trains;

namespace Trackify.Ui.Tests.Fakes;

/// <summary>
/// In-memory train store. Needed only because <c>RemoteTrainSync</c> takes one in its constructor —
/// with no server URL configured it is never actually read.
/// </summary>
public sealed class FakeTrainRepository(params Train[] trains) : ITrainRepository
{
    private readonly List<Train> _trains = [.. trains];

    public Task<Train?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_trains.FirstOrDefault(train => train.Id == id));

    public Task<IReadOnlyList<Train>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Train>>(_trains);

    public Task<IReadOnlyList<Train>> FindAsync(
        Expression<Func<Train, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Train>>([.. _trains.AsQueryable().Where(predicate)]);

    public Task AddAsync(Train entity, CancellationToken cancellationToken = default)
    {
        _trains.Add(entity);

        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Train> entities, CancellationToken cancellationToken = default)
    {
        _trains.AddRange(entities);

        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(Train entity, CancellationToken cancellationToken = default)
    {
        var index = _trains.FindIndex(train => train.Id == entity.Id);
        if (index < 0) return Task.FromResult(false);

        _trains[index] = entity;

        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_trains.RemoveAll(train => train.Id == id) > 0);
}
