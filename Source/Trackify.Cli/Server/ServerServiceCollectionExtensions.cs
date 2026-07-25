using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Trackify.Cli.Server;

/// <summary>
/// DI composition for the Trackify backend, mirroring the <c>AddTrackify…</c> layer pattern: SignalR,
/// the speed-state store, JSON (enums as names), and open CORS so the app (incl. the WASM head) can
/// reach it. The REST/hub handlers themselves resolve their use-cases from DI.
/// </summary>
public static class ServerServiceCollectionExtensions
{
    public static IServiceCollection AddTrackifyServer(this IServiceCollection services)
    {
        services.AddSingleton<TrainStateStore>();
        services.AddSignalR();

        // Enums as readable names on the wire (matches the store).
        services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // Allow any LAN origin to call the API/hub so the app (incl. the WASM head, which sends an
        // Origin) can reach it. Credentials are deliberately NOT enabled — the backend uses no cookies
        // or auth, so we avoid the unsafe any-origin + AllowCredentials combination (CWE-942).
        services.AddCors(o => o.AddDefaultPolicy(p =>
            p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod()));

        return services;
    }
}
