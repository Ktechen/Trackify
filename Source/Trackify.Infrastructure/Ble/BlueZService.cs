using System.Globalization;
using Linux.Bluetooth;
using Linux.Bluetooth.Extensions;
using SharpBrick.PoweredUp.Bluetooth;

namespace Trackify.Infrastructure.Ble;

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
