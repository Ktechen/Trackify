using Linux.Bluetooth;
using Linux.Bluetooth.Extensions;
using Microsoft.Extensions.Logging;
using SharpBrick.PoweredUp.Bluetooth;

namespace Trackify.Infrastructure.Ble;

/// <summary>
/// A SharpBrick <see cref="IPoweredUpBluetoothAdapter"/> backed by BlueZ (via D-Bus) so the Pi's
/// onboard radio can be used. SharpBrick keeps owning the LEGO Wireless Protocol; this only maps its
/// generic connect/discover/GATT calls onto <c>Linux.Bluetooth</c>. Runs on Linux only.
/// <para>
/// Mirrors the mobile app's transport as closely as BlueZ allows: it makes sure the radio is actually
/// powered on before use, scans on the <b>LE transport</b> (so BLE-only hubs and their manufacturer
/// data surface), and also enumerates devices BlueZ already knows about (a fresh scan never re-emits
/// <c>DeviceFound</c> for cached devices, whereas the mobile stack sees them from advertisements).
/// </para>
/// </summary>
public sealed class BlueZPoweredUpBluetoothAdapter(ILogger<BlueZPoweredUpBluetoothAdapter> logger) : IPoweredUpBluetoothAdapter
{
    // LEGO company id in BLE manufacturer-specific advertising data.
    private const ushort LegoCompanyId = 0x0397;

    private readonly ILogger _log = logger;

    /// <summary>
    /// Verifies the radio is present and powered (like the mobile app's radio-state check). Powers it
    /// on automatically when it's off; throws a clear, actionable message if that isn't possible.
    /// </summary>
    public async Task EnsureReadyAsync(CancellationToken ct = default) => await GetReadyAdapterAsync(ct);

    public void Discover(Func<IPoweredUpBluetoothDeviceInfo, Task> discoveryHandler, CancellationToken cancellationToken = default)
        => _ = DiscoverLoopAsync(discoveryHandler, cancellationToken);

    private async Task DiscoverLoopAsync(Func<IPoweredUpBluetoothDeviceInfo, Task> handler, CancellationToken ct)
    {
        var adapter = await GetReadyAdapterAsync(ct);

        async Task Surface(Device device)
        {
            // Only surface genuine LEGO Powered Up hubs: they advertise manufacturer data under the
            // LEGO company id (0x0397). Every other nearby BLE device (phones, headsets, …) is ignored.
            var manufacturerData = await GetLegoManufacturerDataAsync(device);
            if (manufacturerData.Length == 0)
                return;

            string address;
            try { address = await device.GetAddressAsync(); }
            catch { return; } // device vanished mid-scan.

            string? name = null;
            try { name = await device.GetNameAsync(); } catch { /* hubs may advertise without a Name */ }

            try { await handler(new BlueZDeviceInfo(LwpAddressingMapping.ParseMacAddress(address), name, manufacturerData)); }
            catch { /* the handler's own failure must not kill the scan */ }
        }

        async Task OnDeviceFound(Adapter sender, DeviceFoundEventArgs eventArgs) => await Surface(eventArgs.Device);

        adapter.DeviceFound += OnDeviceFound;
        await StartLeScanAsync(adapter);
        Log.DiscoveryStarted(_log);

        try
        {
            // Surface hubs BlueZ already has cached — DeviceFound won't re-fire for those.
            foreach (var device in await adapter.GetDevicesAsync())
                await Surface(device);

            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            // Expected: the caller cancels the token to stop scanning.
        }
        finally
        {
            adapter.DeviceFound -= OnDeviceFound;
            try { await adapter.StopDiscoveryAsync(); } catch { /* best effort */ }
        }
    }

    public async Task<IPoweredUpBluetoothDevice> GetDeviceAsync(IPoweredUpBluetoothDeviceInfo bluetoothDeviceInfo)
    {
        if (bluetoothDeviceInfo is not BlueZDeviceInfo info)
            throw new ArgumentException($"Expected a {nameof(BlueZDeviceInfo)}.", nameof(bluetoothDeviceInfo));

        var adapter = await GetReadyAdapterAsync();
        var mac = LwpAddressingMapping.FormatMacAddress(info.MacAddressAsUInt64);

        // Connect by id like the mobile app: if BlueZ doesn't know the device yet (e.g. a cold connect
        // from auto mode after boot, with no prior 'discover'), do a brief LE scan to find it first.
        var device = await adapter.GetDeviceAsync(mac) ?? await ScanForDeviceAsync(adapter, mac);
        if (device is null)
            throw new InvalidOperationException(
                $"Hub {mac} not found. Make sure it is powered on and in range (or run 'trackify discover' first).");

        return new BlueZDevice(device);
    }

    public Task<IPoweredUpBluetoothDeviceInfo> CreateDeviceInfoByKnownStateAsync(object state)
        => state is ulong macAddress
            ? Task.FromResult<IPoweredUpBluetoothDeviceInfo>(new BlueZDeviceInfo(macAddress, name: null, manufacturerData: []))
            : throw new NotSupportedException($"Unsupported device-info state '{state}'.");

    private async Task<Device?> ScanForDeviceAsync(Adapter adapter, string mac)
    {
        Log.ScanningForDevice(_log, mac);
        await StartLeScanAsync(adapter);
        try
        {
            // Poll for ~15s for the hub to appear in BlueZ's object tree.
            for (var attempt = 0; attempt < 75; attempt++)
            {
                var device = await adapter.GetDeviceAsync(mac);
                if (device is not null)
                    return device;

                await Task.Delay(200);
            }
        }
        finally
        {
            // Stop scanning before the caller connects — BlueZ connects far more reliably when idle.
            try { await adapter.StopDiscoveryAsync(); } catch { /* best effort */ }
        }

        return null;
    }

    private async Task<Adapter> GetReadyAdapterAsync(CancellationToken ct = default)
    {
        var adapter = (await BlueZManager.GetAdaptersAsync()).FirstOrDefault()
            ?? throw new InvalidOperationException(
                "No BlueZ Bluetooth adapter found. Is the Pi's Bluetooth hardware present and 'bluetoothd' running?");

        if (await adapter.GetPoweredAsync())
            return adapter;

        // Radio is off — power it on ourselves (like a phone enabling Bluetooth).
        Log.RadioPoweringOn(_log);
        try { await adapter.SetPoweredAsync(true); } catch { /* likely rfkill soft-blocked */ }

        // BlueZ flips 'Powered' asynchronously; give it up to ~2s to come up, then re-check.
        for (var attempt = 0; attempt < 10 && !await adapter.GetPoweredAsync(); attempt++)
            await Task.Delay(200, ct);

        if (!await adapter.GetPoweredAsync())
            throw new InvalidOperationException(
                "Bluetooth is off. Enable the radio with 'sudo rfkill unblock bluetooth' then 'bluetoothctl power on', " +
                "and make sure the user is in the 'bluetooth' group.");

        return adapter;
    }

    private static async Task StartLeScanAsync(Adapter adapter)
    {
        // Scan on the LE transport, matching the mobile app: BLE-only LEGO hubs (and their
        // manufacturer data) reliably surface, whereas BlueZ's default "auto" (BR/EDR + LE) often
        // misses them. DuplicateData keeps advertisements flowing so RSSI/data stay fresh.
        try
        {
            await adapter.SetDiscoveryFilterAsync(new Dictionary<string, object>
            {
                ["Transport"] = "le",
                ["DuplicateData"] = true,
            });
        }
        catch
        {
            // An older BlueZ may reject a filter key — fall back to default (auto) discovery.
        }

        if (!await adapter.GetDiscoveringAsync())
            await adapter.StartDiscoveryAsync();
    }

    private static async Task<byte[]> GetLegoManufacturerDataAsync(Device device)
    {
        try
        {
            var byCompany = await device.GetManufacturerDataAsync();
            return byCompany is not null && byCompany.TryGetValue(LegoCompanyId, out var value) && value is byte[] bytes
                ? bytes
                : [];
        }
        catch
        {
            return [];
        }
    }
}
