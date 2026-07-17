using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpBrick.PoweredUp;
using SharpBrick.PoweredUp.Bluetooth;
using SharpBrick.PoweredUp.Protocol;
using Trackify.Application.Lego;
using Trackify.Infrastructure.Logging;
using HubType = Trackify.Domain.Enums.HubType;

namespace Trackify.Infrastructure.Ble;

/// <summary>
/// Controls LEGO Powered Up hubs over the Pi's onboard Bluetooth (BlueZ). Same protocol-layer flow as
/// the Windows service — discover, open a SharpBrick protocol, keep it keyed by MAC — but the transport
/// is <see cref="BlueZPoweredUpBluetoothAdapter"/>. Command building lives in <see cref="LwpCommands"/>.
/// </summary>
public sealed class BlueZLegoService(PoweredUpHost host, IPoweredUpBluetoothAdapter adapter, ILogger<BlueZLegoService>? logger = null) : ILegoService
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, ConnectedHub> _connectedHubs = new();
    private readonly ILogger _log = logger ?? NullLogger<BlueZLegoService>.Instance;

    public bool IsSupported => true;

    public async Task<IReadOnlyList<DiscoveredHub>> DiscoverAsync(CancellationToken ct = default)
    {
        var found = new Dictionary<string, DiscoveredHub>();

        // Scan runs until the first hub is seen or the caller cancels - no fixed time window.
        var firstFound = new TaskCompletionSource();
        using var scan = CancellationTokenSource.CreateLinkedTokenSource(ct);
        await using var stopOnCancel = scan.Token.Register(() => firstFound.TrySetResult());

        adapter.Discover(deviceInfo =>
        {
            if (deviceInfo is BlueZDeviceInfo info && info.MacAddressAsUInt64 != 0)
            {
                lock (found) found[LwpAddressing.FormatMacAddress(info.MacAddressAsUInt64)] = ToDiscoveredHub(info);
                firstFound.TrySetResult();
            }

            return Task.CompletedTask;
        }, scan.Token);

        await firstFound.Task;

        if (!scan.IsCancellationRequested)
            await scan.CancelAsync();

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

        var deviceInfo = await adapter.CreateDeviceInfoByKnownStateAsync(LwpAddressing.ParseMacAddress(hubId))
            ?? throw new InvalidOperationException("Invalid hub address.");
        var protocol = host.CreateProtocol(deviceInfo);

        try
        {
            await LwpCommands.ConnectWithRetryAsync(protocol, ct);
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

        Log.HubConnected(_log, hubId);
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
        => LwpCommands.StartPowerAsync(RequireHub(hubId).Protocol, port, power);

    public Task SetLedAsync(string hubId, byte red, byte green, byte blue, CancellationToken ct = default)
    {
        var entry = RequireHub(hubId);
        if (entry.RgbPort is not byte rgbPort)
            throw new InvalidOperationException("This hub has no RGB LED.");

        return LwpCommands.SetRgbColorAsync(entry.Protocol, rgbPort, red, green, blue);
    }

    private static DiscoveredHub ToDiscoveredHub(BlueZDeviceInfo info) => new(
        LwpAddressing.FormatMacAddress(info.MacAddressAsUInt64),
        string.IsNullOrWhiteSpace(info.Name) ? null : info.Name,
        LwpAddressing.FormatMacAddress(info.MacAddressAsUInt64),
        LwpCommands.MapHubType(info.ManufacturerData));

    private ConnectedHub RequireHub(string hubId)
    {
        lock (_gate)
        {
            return _connectedHubs.TryGetValue(hubId, out var hub)
                ? hub
                : throw new InvalidOperationException("Hub is not connected.");
        }
    }
}
