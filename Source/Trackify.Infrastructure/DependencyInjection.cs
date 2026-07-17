using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trackify.Infrastructure.Ble;
using Trackify.Infrastructure.Persistence;

namespace Trackify.Infrastructure;

/// <summary>Infrastructure layer composition (persistence + hub transport).</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Infrastructure implementations: the JSON train store and the Linux/BlueZ hub
    /// transport (a no-op stand-in off-Linux). Pass <paramref name="storePath"/> to override the
    /// default store location (e.g. from the CLI's TRACKIFY_STORE).
    /// </summary>
    public static IServiceCollection AddTrackifyInfrastructure(this IServiceCollection services, string? storePath = null)
    {
        services.AddSingleton<ITrainStore>(sp =>
        {
            var log = sp.GetRequiredService<ILogger<JsonTrainStore>>();
            return string.IsNullOrWhiteSpace(storePath) ? new JsonTrainStore(log) : new JsonTrainStore(storePath, log);
        });
        services.AddLinuxLego();
        return services;
    }
}
