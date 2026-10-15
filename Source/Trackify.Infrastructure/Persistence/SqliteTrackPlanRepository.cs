using Microsoft.EntityFrameworkCore;

namespace Trackify.Infrastructure.Persistence;

/// <summary>
/// Stores the track plan in SQLite. Deliberately not a <see cref="BaseRepository{TContext,T}"/>: a
/// plan is an aggregate, so it is read and written whole, inside one transaction. Half a graph would
/// render as track that falls apart.
/// </summary>
public sealed class SqliteTrackPlanRepository : ITrackPlanRepository
{
    private readonly IDbContextFactory<TrackifyDbContext> _dbContextFactory;

    public SqliteTrackPlanRepository(IDbContextFactory<TrackifyDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;

        using var database = _dbContextFactory.CreateDbContext();
        TrackifySchema.BringUpToDate(database);
    }

    public async Task<TrackPlanSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var database = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var parts = await database.TrackParts
            .AsNoTracking()
            .OrderBy(part => part.PlanOrder)
            .ToListAsync(cancellationToken);

        if (parts.Count == 0) return TrackPlanSnapshot.Empty;

        var links = await database.TrackLinks.AsNoTracking().ToListAsync(cancellationToken);

        return new TrackPlanSnapshot { Parts = parts, Links = links };
    }

    public Task ReplaceAsync(TrackPlanSnapshot snapshot, CancellationToken cancellationToken = default)
        => WriteAsync(snapshot, replace: true, cancellationToken);

    public Task AppendAsync(TrackPlanSnapshot snapshot, CancellationToken cancellationToken = default)
        => WriteAsync(snapshot, replace: false, cancellationToken);

    private async Task WriteAsync(TrackPlanSnapshot snapshot, bool replace, CancellationToken cancellationToken)
    {
        await using var database = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (replace)
            {
                database.TrackLinks.RemoveRange(await database.TrackLinks.ToListAsync(cancellationToken));
                database.TrackParts.RemoveRange(await database.TrackParts.ToListAsync(cancellationToken));
                await database.SaveChangesAsync(cancellationToken);
            }

            // Appending has to continue the existing order, or the newcomers would all claim
            // positions that are already taken and the layout root would depend on load order.
            var offset = 0;
            if (!replace && await database.TrackParts.AnyAsync(cancellationToken))
            {
                offset = await database.TrackParts.MaxAsync(part => part.PlanOrder, cancellationToken) + 1;
            }

            foreach (var part in snapshot.Parts) part.PlanOrder += offset;

            database.TrackParts.AddRange(snapshot.Parts);
            database.TrackLinks.AddRange(snapshot.Links);

            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
