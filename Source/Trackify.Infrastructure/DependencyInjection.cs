using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trackify.Infrastructure.Ble;
using Trackify.Infrastructure.Persistence;

namespace Trackify.Infrastructure;

/// <summary>Infrastructure layer composition (persistence + hub transport).</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Infrastructure implementations: the EF Core + SQLite train repository and the
    /// Linux/BlueZ hub transport (a no-op stand-in off-Linux). Pass <paramref name="databasePath"/> to
    /// override the default SQLite database location (e.g. from the CLI's TRACKIFY_STORE).
    /// </summary>
    public static IServiceCollection AddTrackifyInfrastructure(this IServiceCollection services, string? databasePath = null)
    {
        var resolvedPath = string.IsNullOrWhiteSpace(databasePath) ? SqliteTrainRepository.DefaultDatabasePath() : databasePath;
        var directory = Path.GetDirectoryName(resolvedPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        services.AddDbContextFactory<TrackifyDbContext>(options => options.UseSqlite($"Data Source={resolvedPath}"));
        services.AddSingleton<ITrainRepository, SqliteTrainRepository>();

        services.AddLinuxLego();
        return services;
    }
}
