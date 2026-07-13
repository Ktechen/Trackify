using System.Globalization;
using Linux.Bluetooth;
using Linux.Bluetooth.Extensions;
using SharpBrick.PoweredUp.Bluetooth;
using Trackify.Application.Lego;

namespace Trackify.Infrastructure.Ble.Linux;

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
                var name = await device.GetNameAsync();
                var address = await device.GetAddressAsync();
                var manufacturerData = await GetLegoManufacturerDataAsync(device);
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

/// <summary>SharpBrick device-info for a BlueZ device, keyed by its 48-bit MAC address.</summary>
public sealed class BlueZDeviceInfo(ulong macAddressAsUInt64, string? name, byte[] manufacturerData)
    : IPoweredUpBluetoothDeviceInfo
{
    public ulong MacAddressAsUInt64 { get; } = macAddressAsUInt64;
    public string Name { get; } = name ?? string.Empty;
    public byte[] ManufacturerData { get; } = manufacturerData;

    public bool Equals(IPoweredUpBluetoothDeviceInfo? other)
        => other is BlueZDeviceInfo o && o.MacAddressAsUInt64 == MacAddressAsUInt64;

    public override bool Equals(object? obj) => Equals(obj as IPoweredUpBluetoothDeviceInfo);
    public override int GetHashCode() => MacAddressAsUInt64.GetHashCode();
}

/// <summary>Wraps a <c>Linux.Bluetooth</c> device, connecting it lazily on first service request.</summary>
internal sealed class BlueZDevice(Device device) : IPoweredUpBluetoothDevice
{
    private bool _connected;

    public string Name => device.GetNameAsync().GetAwaiter().GetResult();

    public async Task<IPoweredUpBluetoothService> GetServiceAsync(Guid serviceId)
    {
        if (!_connected)
        {
            await device.ConnectAsync();
            await device.WaitForPropertyValueAsync("Connected", value: true, TimeSpan.FromSeconds(15));
            _connected = true;
        }

        var uuid = serviceId.ToString("D", CultureInfo.InvariantCulture);
        var service = await device.GetServiceAsync(uuid)
            ?? throw new InvalidOperationException($"GATT service {uuid} not found on the hub.");

        return new BlueZService(service, serviceId);
    }

    public void Dispose()
    {
        try { device.DisconnectAsync().GetAwaiter().GetResult(); } catch { /* best effort */ }
        device.Dispose();
    }
}

/// <summary>Wraps a BlueZ GATT service.</summary>
internal sealed class BlueZService(IGattService1 service, Guid uuid) : IPoweredUpBluetoothService
{
    public Guid Uuid { get; } = uuid;

    public async Task<IPoweredUpBluetoothCharacteristic> GetCharacteristicAsync(Guid guid)
    {
        var characteristic = await service.GetCharacteristicAsync(guid.ToString("D", CultureInfo.InvariantCulture))
            ?? throw new InvalidOperationException($"GATT characteristic {guid} not found.");

        return new BlueZCharacteristic(characteristic, guid);
    }

    public void Dispose() { /* the BlueZ service handle is owned by the device connection */ }
}

/// <summary>Wraps a BlueZ GATT characteristic: write-without-response + notifications.</summary>
internal sealed class BlueZCharacteristic(GattCharacteristic characteristic, Guid uuid) : IPoweredUpBluetoothCharacteristic
{
    public Guid Uuid { get; } = uuid;

    public async Task<bool> NotifyValueChangeAsync(Func<byte[], Task> notificationHandler)
    {
        characteristic.Value += (_, eventArgs) => notificationHandler(eventArgs.Value);
        await characteristic.StartNotifyAsync();
        return true;
    }

    public async Task<bool> WriteValueAsync(byte[] data)
    {
        // "command" = write-without-response, which is how LWP messages are sent.
        await characteristic.WriteValueAsync(data, new Dictionary<string, object> { ["type"] = "command" });
        return true;
    }
}
