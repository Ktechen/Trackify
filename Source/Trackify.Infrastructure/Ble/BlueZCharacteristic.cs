using Linux.Bluetooth;
using SharpBrick.PoweredUp.Bluetooth;

namespace Trackify.Infrastructure.Ble;

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
