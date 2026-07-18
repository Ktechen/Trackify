using System.Linq.Expressions;
using Trackify.Domain.Common;

namespace Trackify.Application.Common;

/// <summary>
/// Default CRUD contract shared by every repository. Concrete implementations live in Infrastructure;
/// a per-entity repository interface (e.g. <c>ITrainRepository</c>) just extends this.
/// </summary>
public interface IBaseRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>Replaces the entity with the same id; returns whether a row changed.</summary>
    Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Deletes the entity by id; returns whether a row was removed.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
