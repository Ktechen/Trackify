using Microsoft.AspNetCore.SignalR.Client;
using Trackify.Application.Remote;

namespace Trackify.Services.Remote;

/// <summary>
/// <see cref="ILegoService"/> backed by a remote Trackify backend: one-shot actions over REST (Refit)
/// and real-time speed/LED over SignalR. Selected instead of the device's own Bluetooth when the app
/// is in Server mode, so the whole UI works unchanged — only the transport moves to the Pi.
/// </summary>
public sealed class RemoteLegoService : ILegoService, IAsyncDisposable
{
    private readonly ITrackifyApi _api;
    private readonly HubConnection _hub;

    /// <summary>Raised when any client changes a hub's speed — lets the UI show the live current speed.</summary>
    public event Action<string, int>? SpeedChanged;

    public RemoteLegoService(ITrackifyApi api, RemoteServerOptions options)
    {
        _api = api;
        _hub = new HubConnectionBuilder()
            .WithUrl($"{options.BaseUrl.TrimEnd('/')}{ApiRoutes.TrainHub}")
            .WithAutomaticReconnect()
            .Build();

        _hub.On<string, int>(TrainHubMethods.SpeedChanged, (hubId, speed) => SpeedChanged?.Invoke(hubId, speed));
    }

    public bool IsSupported => true;

    public Task<IReadOnlyList<DiscoveredHubDto>> DiscoverAsync(CancellationToken ct = default)
        => _api.DiscoverAsync(ct);

    public async Task ConnectAsync(string hubId, HubType hubType, CancellationToken ct = default)
    {
        await EnsureHubAsync(ct);
        await _api.ConnectAsync(hubId, hubType, ct);
    }

    public Task DisconnectAsync(string hubId, CancellationToken ct = default)
        => _api.DisconnectAsync(hubId, ct);

    public async Task SetSpeedAsync(string hubId, byte port, sbyte power, CancellationToken ct = default)
    {
        await EnsureHubAsync(ct);
        await _hub.InvokeAsync(TrainHubMethods.SetSpeed, hubId, (int)port, (int)power, ct);
    }

    public async Task SetLedAsync(string hubId, byte red, byte green, byte blue, CancellationToken ct = default)
    {
        await EnsureHubAsync(ct);
        await _hub.InvokeAsync(TrainHubMethods.SetLed, hubId, (int)red, (int)green, (int)blue, ct);
    }

    private async Task EnsureHubAsync(CancellationToken ct)
    {
        if (_hub.State == HubConnectionState.Disconnected)
            await _hub.StartAsync(ct);
    }

    public async ValueTask DisposeAsync() => await _hub.DisposeAsync();
}
