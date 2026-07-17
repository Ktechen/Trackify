#if WINDOWS
using SharpBrick.PoweredUp;
using SharpBrick.PoweredUp.Bluetooth;
using SharpBrick.PoweredUp.Protocol;
using HubType = Trackify.Domain.Enums.HubType;

namespace Trackify.Application.Services;

/// <summary>
/// Controls LEGO Powered Up hubs over Windows (WinRT) Bluetooth (SharpBrick.PoweredUp.WinRT).
/// Same protocol-layer flow as the mobile service - discover, open a protocol, keep it keyed by
/// the hub's MAC address - but WinRT connects by numeric address, so ids are parsed to/from MAC.
/// Wire-level details live in <see cref="LwpProtocol"/>.
/// </summary>
public sealed class WindowsLegoService : ILegoService
{
    private readonly PoweredUpHost _host;
    private readonly IPoweredUpBluetoothAdapter _adapter;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, ConnectedHub> _connectedHubs = new();

    public WindowsLegoService(PoweredUpHost host, IPoweredUpBluetoothAdapter adapter)
    {
        _host = host;
        _adapter = adapter;
    }

    public bool IsSupported => true;

    public async Task<IReadOnlyList<DiscoveredHub>> DiscoverAsync(CancellationToken ct = default)
    {
        var found = new Dictionary<string, DiscoveredHub>();

        // Scan runs until the first hub is seen or the caller cancels - no fixed time window.
        var firstFound = new TaskCompletionSource();
        using var scan = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var stopOnCancel = scan.Token.Register(() => firstFound.TrySetResult());

        _adapter.Discover(deviceInfo =>
        {
            if (deviceInfo is PoweredUpBluetoothDeviceInfoWithMacAddress info && info.MacAddressAsUInt64 != 0)
            {
                lock (found) found[LwpAddressing.FormatMacAddress(info.MacAddressAsUInt64)] = ToDiscoveredHub(info);
                firstFound.TrySetResult();
            }

            return Task.CompletedTask;
        }, scan.Token);

        await firstFound.Task;

        if (!scan.IsCancellationRequested)
            scan.Cancel();

        lock (found)
        {
            return found.Values.OrderBy(h => h.Name ?? h.Id).ToList();
        }
    }

    public async Task ConnectAsync(string hubId, HubType hubType, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_connectedHubs.ContainsKey(hubId))
                return;
        }

        // WinRT identifies devices by numeric address, so connect from the MAC id.
        var deviceInfo = await _adapter.CreateDeviceInfoByKnownStateAsync(LwpAddressing.ParseMacAddress(hubId))
            ?? throw new InvalidOperationException("Ungültige Hub-Adresse.");
        var protocol = _host.CreateProtocol(deviceInfo);

        try
        {
            await LwpProtocol.ConnectWithRetryAsync(protocol, ct);
        }
        catch
        {
            protocol.Dispose();
            throw;
        }

        lock (_gate)
        {
            _connectedHubs[hubId] = new ConnectedHub(protocol, LwpAddressing.RgbLedPortFor(hubType));
        }
    }

    public async Task DisconnectAsync(string hubId, CancellationToken ct = default)
    {
        ConnectedHub? entry;
        lock (_gate)
        {
            _connectedHubs.Remove(hubId, out entry);
        }

        if (entry is null)
            return;

        await entry.Protocol.DisconnectAsync();
        entry.Protocol.Dispose();
    }

    public Task SetSpeedAsync(string hubId, byte port, sbyte power, CancellationToken ct = default)
        => LwpProtocol.StartPowerAsync(RequireHub(hubId).Protocol, port, power);

    public Task SetLedAsync(string hubId, byte red, byte green, byte blue, CancellationToken ct = default)
    {
        var entry = RequireHub(hubId);
        if (entry.RgbPort is not byte rgbPort)
            throw new InvalidOperationException("Dieser Hub hat keine RGB-LED.");

        return LwpProtocol.SetRgbColorAsync(entry.Protocol, rgbPort, red, green, blue);
    }

    private static DiscoveredHub ToDiscoveredHub(PoweredUpBluetoothDeviceInfoWithMacAddress info) => new(
        LwpAddressing.FormatMacAddress(info.MacAddressAsUInt64),
        string.IsNullOrWhiteSpace(info.Name) ? null : info.Name,
        LwpAddressing.FormatMacAddress(info.MacAddressAsUInt64),
        LwpProtocol.MapHubType(info.ManufacturerData));

    private ConnectedHub RequireHub(string hubId)
    {
        lock (_gate)
        {
            return _connectedHubs.TryGetValue(hubId, out var hub)
                ? hub
                : throw new InvalidOperationException("Hub ist nicht verbunden.");
        }
    }

}
#endif
