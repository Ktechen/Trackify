using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Trackify.Infrastructure.Persistence;

/// <summary>
/// Brings the store up to the latest migration, and refuses to do so in a way that would look like a
/// bug.
///
/// Trackify used <c>EnsureCreated()</c> before migrations existed (arc42 ADR-06 → ADR-18). A database
/// from that era has the tables but no <c>__EFMigrationsHistory</c>, so plain <c>Migrate()</c> tries
/// to create <c>Trains</c> a second time and dies with "table already exists" — which says nothing
/// about what to do. That file holds the user's saved trains, so deleting it automatically is not
/// ours to decide; this says plainly what it is and what to do instead.
/// </summary>
internal static class TrackifySchema
{
    /// <summary>Migrates <paramref name="database"/>, or throws an actionable error for a pre-migration store.</summary>
    public static void BringUpToDate(DbContext database)
    {
        var store = database.Database;
        if (!store.IsRelational()) return;

        if (store.GetAppliedMigrations().Any())
        {
            store.Migrate();
            return;
        }

        // Nothing recorded: either a brand-new database, which migrates cleanly, or a pre-migration
        // one, which cannot.
        if (WasCreatedBeforeMigrations(store))
        {
            throw new InvalidOperationException(
                $"The Trackify store at '{store.GetDbConnection().DataSource}' predates EF Core "
                + "migrations: it has the tables but no __EFMigrationsHistory, so it cannot be "
                + "migrated in place. Move the file aside — it still holds your saved trains — and "
                + "start again; a fresh store is created automatically. See arc42 ADR-18.");
        }

        store.Migrate();
    }

    // SQLite-specific on purpose: it is the only provider this store runs on, and the check has to
    // look at the schema catalogue rather than the EF model.
    private static bool WasCreatedBeforeMigrations(DatabaseFacade store)
    {
        if (store.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true) return false;

        return store
            .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = 'Trains'")
            .AsEnumerable()
            .FirstOrDefault() > 0;
    }
}
