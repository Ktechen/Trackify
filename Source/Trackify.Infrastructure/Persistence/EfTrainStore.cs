using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trackify.Infrastructure.Logging;

namespace Trackify.Infrastructure.Persistence;

/// <summary>
/// EF Core + SQLite implementation of the train store. The Uno app and the CLI share the schema, so a
/// <c>trackify.db</c> written by one is readable by the other. A context is created per operation via
/// <see cref="IDbContextFactory{TContext}"/> (thread-safe); the database is created on first use.
/// </summary>
public sealed class EfTrainStore : ITrainStore
{
    private readonly IDbContextFactory<TrackifyDbContext> _contextFactory;
    private readonly ILogger _log;

    public EfTrainStore(IDbContextFactory<TrackifyDbContext> contextFactory, ILogger<EfTrainStore> logger)
    {
        _contextFactory = contextFactory;
        _log = logger;

        using var db = _contextFactory.CreateDbContext();
        db.Database.EnsureCreated();
    }

    /// <summary>Default SQLite location: <c>&lt;ApplicationData&gt;/Trackify/trackify.db</c> (on Linux: <c>~/.config</c>).</summary>
    public static string DefaultDbPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Trackify", "trackify.db");

    public async Task<IReadOnlyList<TrainConfig>> LoadAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var trains = await db.Trains.AsNoTracking().ToListAsync(ct);
        Log.StoreLoaded(_log, trains.Count, Source(db));
        return trains;
    }

    public async Task SaveAsync(IReadOnlyList<TrainConfig> trains, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        // Replace-the-set semantics, matching the previous store contract.
        await db.Trains.ExecuteDeleteAsync(ct);
        db.Trains.AddRange(trains);
        await db.SaveChangesAsync(ct);

        Log.StoreSaved(_log, trains.Count, Source(db));
    }

    private static string Source(TrackifyDbContext db) => db.Database.GetDbConnection().DataSource ?? "sqlite";
}
