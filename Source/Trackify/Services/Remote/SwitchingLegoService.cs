namespace Trackify.Services.Remote;

/// <summary>
/// Routes <see cref="ILegoService"/> calls to the local (on-device Bluetooth) or the remote (backend)
/// transport based on the live <see cref="ConnectionState"/> — so the HMI's mode switch takes effect
/// immediately, without restarting the app. The remote transport is (re)created when the URL changes.
/// </summary>
public sealed class SwitchingLegoService(ConnectionState state, ILegoService? local) : ILegoService
{
    private RemoteLegoService? _remote;
    private string? _remoteUrl;

    public bool IsSupported => state.UseServer || (local?.IsSupported ?? false);

    public Task<IReadOnlyList<DiscoveredHubDto>> DiscoverAsync(CancellationToken ct = default)
        => Active().DiscoverAsync(ct);

    public Task ConnectAsync(string hubId, HubType hubType, CancellationToken ct = default)
        => Active().ConnectAsync(hubId, hubType, ct);

    public Task DisconnectAsync(string hubId, CancellationToken ct = default)
        => Active().DisconnectAsync(hubId, ct);

    public Task SetSpeedAsync(string hubId, byte port, sbyte power, CancellationToken ct = default)
        => Active().SetSpeedAsync(hubId, port, power, ct);

    public Task SetLedAsync(string hubId, byte red, byte green, byte blue, CancellationToken ct = default)
        => Active().SetLedAsync(hubId, red, green, blue, ct);

    private ILegoService Active()
    {
        if (!state.UseServer)
            return local ?? throw new InvalidOperationException(
                "Direktmodus (Bluetooth) wird auf dieser Plattform nicht unterstützt — bitte Servermodus verwenden.");

        var url = state.ServerUrl;
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("Bitte zuerst eine Server-Adresse eingeben.");

        if (_remote is null || _remoteUrl != url)
        {
            var previous = _remote;
            _remote = RemoteLegoService.Create(url);
            _remoteUrl = url;
            if (previous is not null)
                _ = previous.DisposeAsync(); // best-effort: drop the old connection in the background
        }

        return _remote;
    }
}
