#if __ANDROID__ || __IOS__
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using SharpBrick.PoweredUp;
using SharpBrick.PoweredUp.Bluetooth;
using SharpBrick.PoweredUp.Hubs;
using SharpBrick.PoweredUp.Mobile;
using SharpBrick.PoweredUp.Protocol;
using SharpBrick.PoweredUp.Protocol.Messages;
using HubType = Trackify.Models.Trains.Enums.HubType;

namespace Trackify.Services;

/// <summary>
/// Drives LEGO Powered Up hubs directly from the app over Bluetooth LE (SharpBrick.PoweredUp on
/// Plugin.BLE) - no server. Follows SharpBrick's protocol-layer sample: discover a device, create a
/// protocol for it, connect, then send raw port-output commands. Connected protocols are kept in a
/// registry keyed by the platform device id so later commands reuse the open connection.
///
/// All wire-level details (advertising layout, hub/system-type ids, port-output command value
/// ranges, GATT service/characteristic uuids) follow the LEGO Wireless Protocol v3.0.00 spec:
/// https://lego.github.io/lego-ble-wireless-protocol-docs/
/// </summary>
public sealed class DirectLegoService : ILegoService
{
    private readonly PoweredUpHost _host;
    private readonly IPoweredUpBluetoothAdapter _adapter;
    private readonly IBluetoothLE _bluetooth;
    private readonly IBluetoothPermissions _permissions;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, ConnectedHub> _connectedHubs = new();

    public DirectLegoService(PoweredUpHost host, IPoweredUpBluetoothAdapter adapter, IBluetoothLE bluetooth, IBluetoothPermissions permissions)
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
            if (deviceInfo is not XamarinBluetoothDeviceInfo info || string.IsNullOrEmpty(info.DeviceIdentifier))
            {
                return Task.CompletedTask;
            }

            lock (found)
            {
                found[info.DeviceIdentifier] = new DiscoveredHub(
                    info.DeviceIdentifier,
                    string.IsNullOrWhiteSpace(info.Name) ? null : info.Name,
                    info.MacAddressAsUInt64 == 0 ? null : FormatMacAddress(info.MacAddressAsUInt64),
                    TryMapHubType(info.ManufacturerData));
            }

            firstFound.TrySetResult();

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

        // Protocol.ConnectAsync forwards into SharpBrick's BluetoothKernel, whose connect chain is
        // not null-hardened (sharpbrick/powered-up#188): a transient BLE/GATT hiccup can throw on the
        // first try. A short bounded retry makes connecting reliable in practice.
        const int maxAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await protocol.ConnectAsync();
                break;
            }
            catch (Exception ex) when (ex is NullReferenceException or ArgumentNullException && attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(300), ct);
            }
            catch (Exception ex) when (ex is NullReferenceException or ArgumentNullException)
            {
                protocol.Dispose();
                throw new InvalidOperationException(
                    "Verbindung zum Hub fehlgeschlagen (nicht erreichbar oder GATT nicht bereit). " +
                    "Ist der Hub eingeschaltet und in Reichweite?", ex);
            }
        }

        lock (_gate)
        {
            _connectedHubs[hubId] = new ConnectedHub(protocol, RgbPortFor(hubType));
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

    public async Task SetSpeedAsync(string hubId, byte port, sbyte power, CancellationToken ct = default)
    {
        // Raw StartPower to the port - works with any motor without needing device knowledge first.
        // LWP power (Int8): 1..100 CW, -1..-100 CCW, 0 = float/stop, 127 = brake. The caller clamps
        // to [-100..100]; see the LWP "Output Sub Command - StartPower" section.
        await RequireHub(hubId).Protocol.SendPortOutputCommandAsync(new PortOutputCommandStartPowerMessage(
            port,
            PortOutputCommandStartupInformation.ExecuteImmediately,
            PortOutputCommandCompletionInformation.CommandFeedback,
            power)
        {
            HubId = 0,
        });
    }

    public async Task SetLedAsync(string hubId, byte red, byte green, byte blue, CancellationToken ct = default)
    {
        var entry = RequireHub(hubId);
        if (entry.RgbPort is not byte rgbPort)
            throw new InvalidOperationException("Dieser Hub hat keine RGB-LED.");

        // Select the RGB (3-channel) mode, then push the color - mirrors the typed RgbLight device.
        await entry.Protocol.SendMessageAsync(new PortInputFormatSetupSingleMessage(rgbPort, 0x01, 10000, false) { HubId = 0 });
        await entry.Protocol.SendPortOutputCommandAsync(new PortOutputCommandSetRgbColorNo2Message(
            rgbPort,
            PortOutputCommandStartupInformation.ExecuteImmediately,
            PortOutputCommandCompletionInformation.CommandFeedback,
            red, green, blue)
        {
            HubId = 0,
        });
    }

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

    // The built-in RGB LED sits on a model-specific port (from each hub's LWP "Hub Attached I/O"
    // port map, as captured in SharpBrick's hub definitions); null when the model has none.
    private static byte? RgbPortFor(HubType hubType) => hubType switch
    {
        HubType.PoweredUpHub => 50,
        HubType.ControlPlusHub => 50,
        HubType.BoostMoveHub => 50,
        HubType.DuploTrainHub => 17,
        HubType.PoweredUpRemote => 52,
        _ => null,
    };

    // In the LWP advertising packet, byte 1 (after the 0x0397 manufacturer id is stripped) is the
    // "System Type and Device Number" field that identifies the hub model. Map it to the app's hub
    // models via HubFactory; returns null for anything unrecognized.
    private static HubType? TryMapHubType(byte[]? manufacturerData)
    {
        if (manufacturerData is null || manufacturerData.Length < 2)
            return null;

        try
        {
            var type = HubFactory.GetTypeFromSystemType((SystemType)manufacturerData[1]);
            return type.Name switch
            {
                nameof(TwoPortHub) => HubType.PoweredUpHub,
                nameof(TechnicMediumHub) => HubType.ControlPlusHub,
                nameof(MoveHub) => HubType.BoostMoveHub,
                nameof(DuploTrainBaseHub) => HubType.DuploTrainHub,
                nameof(TwoPortHandset) => HubType.PoweredUpRemote,
                _ => (HubType?)null,
            };
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static string FormatMacAddress(ulong address)
        => string.Join(":", Enumerable.Range(0, 6).Select(i => ((byte)(address >> ((5 - i) * 8))).ToString("X2")));

    private sealed record ConnectedHub(ILegoWirelessProtocol Protocol, byte? RgbPort);
}
#endif
