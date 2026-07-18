using Microsoft.EntityFrameworkCore;

namespace Trackify.Infrastructure.Persistence;

/// <summary>
/// EF Core + SQLite repository for trains — the default CRUD comes from
/// <see cref="BaseRepository{TContext, T}"/>. The Uno app and the CLI share the schema, so a
/// <c>trackify.db</c> written by one is readable by the other.
/// </summary>
public sealed class SqliteTrainRepository(IDbContextFactory<TrackifyDbContext> dbContextFactory)
    : BaseRepository<TrackifyDbContext, Train>(dbContextFactory), ITrainRepository
{
    /// <summary>Default SQLite location: <c>&lt;ApplicationData&gt;/Trackify/trackify.db</c> (on Linux: <c>~/.config</c>).</summary>
    public static string DefaultDatabasePath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Trackify", "trackify.db");
}
