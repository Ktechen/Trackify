using System.Globalization;
using Linux.Bluetooth;
using Linux.Bluetooth.Extensions;
using SharpBrick.PoweredUp.Bluetooth;

namespace Trackify.Infrastructure.Ble;

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
