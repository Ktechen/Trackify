using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trackify.Infrastructure.Ble;
using Trackify.Infrastructure.Persistence;

namespace Trackify.Infrastructure;

/// <summary>Infrastructure layer composition (persistence + hub transport).</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Infrastructure implementations: the EF Core + SQLite train store and the
    /// Linux/BlueZ hub transport (a no-op stand-in off-Linux). Pass <paramref name="storePath"/> to
    /// override the default SQLite database location (e.g. from the CLI's TRACKIFY_STORE).
    /// </summary>
    public static IServiceCollection AddTrackifyInfrastructure(this IServiceCollection services, string? storePath = null)
    {
        var databasePath = string.IsNullOrWhiteSpace(storePath) ? EfTrainStore.DefaultDbPath() : storePath;
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        services.AddDbContextFactory<TrackifyDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
        services.AddSingleton<ITrainStore, EfTrainStore>();

        services.AddLinuxLego();
        return services;
    }
}
