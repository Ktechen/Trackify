using System.Text.Json;
using System.Text.Json.Serialization;
using Refit;

namespace Trackify.Services.Remote;

/// <summary>Builds a Refit client for a backend base URL, configured to match the server's JSON
/// (enums as names). Used for both the remote transport and the train sync.</summary>
internal static class TrackifyApiFactory
{
    public static ITrackifyApi Create(string baseUrl)
    {
        var settings = new RefitSettings(new SystemTextJsonContentSerializer(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        }));
        return RestService.For<ITrackifyApi>(baseUrl, settings);
    }
}
