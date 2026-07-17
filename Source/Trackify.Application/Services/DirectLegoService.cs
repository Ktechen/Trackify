#if __ANDROID__ || __IOS__
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using SharpBrick.PoweredUp;
using SharpBrick.PoweredUp.Bluetooth;
using SharpBrick.PoweredUp.Mobile;
using SharpBrick.PoweredUp.Protocol;
using HubType = Trackify.Domain.Enums.HubType;

namespace Trackify.Application.Services;

/// <summary>
/// Controls LEGO Powered Up hubs over Bluetooth LE on Android/iOS (SharpBrick.PoweredUp on
/// Plugin.BLE) - no server. Discovers a device, opens a protocol for it, and keeps the connected
/// protocol keyed by its platform device id so later commands reuse the open connection.
/// Wire-level details live in <see cref="LwpProtocol"/>.
/// </summary>
public sealed class DirectLegoService : ILegoService
{
    private readonly PoweredUpHost _host;
    private readonly IPoweredUpBluetoothAdapter _adapter;
    private readonly IBluetoothLE _bluetooth;
    private readonly IBluetoothPermissionService _permissions;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, ConnectedHub> _connectedHubs = new();

    public DirectLegoService(PoweredUpHost host, IPoweredUpBluetoothAdapter adapter, IBluetoothLE bluetooth, IBluetoothPermissionService permissions)
    {
        _host = host;
        _adapter = adapter;
        _bluetooth = bluetooth;
        _permissions = permissions;
    }

    public bool IsSupported => true;

    public async Task<IReadOnlyList<DiscoveredHub>> DiscoverAsync(CancellationToken ct = default)
    {
        await EnsureRadioReadyAsync();

        var found = new Dictionary<string, DiscoveredHub>();

        // Scan runs until the first hub is seen or the caller cancels - no fixed time window.
        var firstFound = new TaskCompletionSource();
        using var scan = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var stopOnCancel = scan.Token.Register(() => firstFound.TrySetResult());

        _adapter.Discover(deviceInfo =>
        {
            if (deviceInfo is XamarinBluetoothDeviceInfo info && !string.IsNullOrEmpty(info.DeviceIdentifier))
            {
                lock (found) found[info.DeviceIdentifier] = ToDiscoveredHub(info);
                firstFound.TrySetResult();
            }

            return Task.CompletedTask;
        }, scan.Token);

        await firstFound.Task;

        // Stop the watcher (its cancellation unhooks the handler); harmless if already cancelled.
        if (!scan.IsCancellationRequested)
            scan.Cancel();

        lock (found)
        {
            return found.Values.OrderBy(h => h.Name ?? h.Id).ToList();
        }
    }

    public async Task ConnectAsync(string hubId, HubType hubType, CancellationToken ct = default)
    {
        await EnsureRadioReadyAsync();

        lock (_gate)
        {
            if (_connectedHubs.ContainsKey(hubId))
                return;
        }

        var deviceInfo = await _adapter.CreateDeviceInfoByKnownStateAsync(hubId)
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

    private static DiscoveredHub ToDiscoveredHub(XamarinBluetoothDeviceInfo info) => new(
        info.DeviceIdentifier,
        string.IsNullOrWhiteSpace(info.Name) ? null : info.Name,
        info.MacAddressAsUInt64 == 0 ? null : LwpAddressing.FormatMacAddress(info.MacAddressAsUInt64),
        LwpProtocol.MapHubType(info.ManufacturerData));

    // Ensures the app may use the radio AND that the radio is actually on before we touch it.
    private async Task EnsureRadioReadyAsync()
    {
        if (!await _permissions.EnsureGrantedAsync())
            throw new InvalidOperationException("Bluetooth-Berechtigung wurde nicht erteilt. Bitte in den Einstellungen erlauben.");

        switch (_bluetooth.State)
        {
            case BluetoothState.Off:
            case BluetoothState.TurningOff:
                throw new InvalidOperationException("Bluetooth ist ausgeschaltet. Bitte einschalten und erneut versuchen.");
            case BluetoothState.Unauthorized:
                throw new InvalidOperationException("Bluetooth-Zugriff ist nicht erlaubt. Bitte die Berechtigung erteilen.");
            case BluetoothState.Unavailable:
                throw new InvalidOperationException("Dieses Gerät unterstützt kein Bluetooth LE.");
        }
    }

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
