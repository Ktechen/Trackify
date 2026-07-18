using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Trackify.Application.Common;
using Trackify.Domain.Common;

namespace Trackify.Infrastructure.Persistence;

/// <summary>
/// Default EF Core CRUD for any <see cref="BaseEntity"/>, so a concrete repository is usually just
/// <c>: BaseRepository&lt;TContext, TEntity&gt;, ITEntityRepository</c>. Generic over the
/// <typeparamref name="TContext"/> too, so the base type never depends on a concrete
/// <see cref="DbContext"/>. A context is created per operation via <see cref="IDbContextFactory{TContext}"/>
/// (thread-safe); the database is created on first use. Writes run inside a database transaction
/// (committed on success, rolled back on error) whenever the provider is relational (SQLite here);
/// a non-relational provider skips it.
/// </summary>
public abstract class BaseRepository<TContext, T> : IBaseRepository<T>
    where TContext : DbContext
    where T : BaseEntity
{
    protected readonly IDbContextFactory<TContext> DbContextFactory;

    protected BaseRepository(IDbContextFactory<TContext> dbContextFactory)
    {
        DbContextFactory = dbContextFactory;

        using var database = DbContextFactory.CreateDbContext();
        database.Database.EnsureCreated();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var database = await DbContextFactory.CreateDbContextAsync(cancellationToken);
        return await database.Set<T>().AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var database = await DbContextFactory.CreateDbContextAsync(cancellationToken);
        return await database.Set<T>().AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await using var database = await DbContextFactory.CreateDbContextAsync(cancellationToken);
        return await database.Set<T>().AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
    }

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => ExecuteInTransactionAsync(async (database, token) =>
        {
            database.Set<T>().Add(entity);
            return await database.SaveChangesAsync(token);
        }, cancellationToken);

    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        => ExecuteInTransactionAsync(async (database, token) =>
        {
            database.Set<T>().AddRange(entities);
            return await database.SaveChangesAsync(token);
        }, cancellationToken);

    public Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.DateUpdated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return ExecuteInTransactionAsync(async (database, token) =>
        {
            database.Set<T>().Update(entity);
            return await database.SaveChangesAsync(token) > 0;
        }, cancellationToken);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => ExecuteInTransactionAsync(async (database, token) =>
        {
            var entity = await database.Set<T>().FirstOrDefaultAsync(candidate => candidate.Id == id, token);
            if (entity is null) return false;

            database.Set<T>().Remove(entity);
            return await database.SaveChangesAsync(token) > 0;
        }, cancellationToken);

    // Runs a write inside a transaction on relational providers (SQLite), committing on success and
    // rolling back on error; non-relational providers just run the operation.
    private async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<TContext, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
    {
        await using var database = await DbContextFactory.CreateDbContextAsync(cancellationToken);

        if (!database.Database.IsRelational())
        {
            return await operation(database, cancellationToken);
        }

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(database, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
