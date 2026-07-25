using System.Text.Json;
using System.Text.Json.Serialization;
using Refit;

namespace Trackify.Services.Remote;

/// <summary>
/// Wires the remote (Server-mode) transport: a Refit REST client plus the SignalR-backed
/// <see cref="RemoteLegoService"/>, registered as the app's <see cref="ILegoService"/>. Call this
/// instead of the local per-head transport when the user has entered a backend URL.
/// </summary>
public static class RemoteLegoServiceExtensions
{
    public static IServiceCollection AddTrackifyRemote(this IServiceCollection services, string baseUrl)
    {
        services.AddSingleton(new RemoteServerOptions { BaseUrl = baseUrl });

        // The backend serialises enums as names (matching the store); mirror that here.
        var settings = new RefitSettings(new SystemTextJsonContentSerializer(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        }));
        services.AddSingleton(RestService.For<ITrackifyApi>(baseUrl, settings));

        services.AddSingleton<ILegoService, RemoteLegoService>();
        return services;
    }
}
