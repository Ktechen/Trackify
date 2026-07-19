using Linux.Bluetooth;
using Linux.Bluetooth.Extensions;
using SharpBrick.PoweredUp.Bluetooth;

namespace Trackify.Infrastructure.Ble;

/// <summary>
/// A SharpBrick <see cref="IPoweredUpBluetoothAdapter"/> backed by BlueZ (via D-Bus) so the Pi's
/// onboard radio can be used. SharpBrick keeps owning the LEGO Wireless Protocol; this only maps its
/// generic connect/discover/GATT calls onto <c>Linux.Bluetooth</c>. Runs on Linux only.
/// </summary>
public sealed class BlueZPoweredUpBluetoothAdapter : IPoweredUpBluetoothAdapter
{
    // LEGO company id in BLE manufacturer-specific advertising data.
    private const ushort LegoCompanyId = 0x0397;

    public void Discover(Func<IPoweredUpBluetoothDeviceInfo, Task> discoveryHandler, CancellationToken cancellationToken = default)
        => _ = DiscoverLoopAsync(discoveryHandler, cancellationToken);

    private static async Task DiscoverLoopAsync(Func<IPoweredUpBluetoothDeviceInfo, Task> handler, CancellationToken ct)
    {
        var adapter = await GetAdapterAsync();

        async Task OnDeviceFound(Adapter sender, DeviceFoundEventArgs eventArgs)
        {
            try
            {
                var device = eventArgs.Device;

                // Only surface genuine LEGO Powered Up hubs: they advertise manufacturer data under the
                // LEGO company id (0x0397). Every other nearby BLE device (phones, headsets, …) is
                // ignored so discovery never invents a hub.
                var manufacturerData = await GetLegoManufacturerDataAsync(device);
                if (manufacturerData.Length == 0)
                    return;

                var name = await device.GetNameAsync();
                var address = await device.GetAddressAsync();
                await handler(new BlueZDeviceInfo(LwpAddressing.ParseMacAddress(address), name, manufacturerData));
            }
            catch
            {
                // Ignore a device that vanished or wouldn't answer; the scan keeps running.
            }
        }

        adapter.DeviceFound += OnDeviceFound;
        await adapter.StartDiscoveryAsync();
        try
        {
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

        var adapter = await GetAdapterAsync();
        var device = await adapter.GetDeviceAsync(LwpAddressing.FormatMacAddress(info.MacAddressAsUInt64))
            ?? throw new InvalidOperationException($"BLE device {info.Name} is not known to BlueZ; discover it first.");

        return new BlueZDevice(device);
    }

    public Task<IPoweredUpBluetoothDeviceInfo> CreateDeviceInfoByKnownStateAsync(object state)
        => state is ulong macAddress
            ? Task.FromResult<IPoweredUpBluetoothDeviceInfo>(new BlueZDeviceInfo(macAddress, name: null, manufacturerData: []))
            : throw new NotSupportedException($"Unsupported device-info state '{state}'.");

    private static async Task<Adapter> GetAdapterAsync()
        => (await BlueZManager.GetAdaptersAsync()).FirstOrDefault()
            ?? throw new InvalidOperationException("No BlueZ Bluetooth adapter found. Is bluetoothd running?");

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
