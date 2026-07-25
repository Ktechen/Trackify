using Refit;
using Trackify.Application.Remote;

namespace Trackify.Services.Remote;

/// <summary>
/// Refit client for the backend's one-shot REST actions (list/discover/connect/disconnect + current
/// state). Real-time speed/LED go over SignalR instead — see <see cref="RemoteLegoService"/>. Routes
/// come from the shared <see cref="ApiRoutes"/> so client and server never drift.
/// </summary>
public interface ITrackifyApi
{
    [Get(ApiRoutes.Trains)]
    Task<IReadOnlyList<TrainDto>> GetTrainsAsync(CancellationToken ct = default);

    [Post(ApiRoutes.Discover)]
    Task<IReadOnlyList<DiscoveredHubDto>> DiscoverAsync(CancellationToken ct = default);

    [Post(ApiRoutes.Connect)]
    Task ConnectAsync(string hubId, HubType hubType, CancellationToken ct = default);

    [Post(ApiRoutes.Disconnect)]
    Task DisconnectAsync(string hubId, CancellationToken ct = default);

    [Get(ApiRoutes.State)]
    Task<IReadOnlyList<TrainSpeedState>> GetStateAsync(CancellationToken ct = default);
}
