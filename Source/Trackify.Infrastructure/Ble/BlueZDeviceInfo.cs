using SharpBrick.PoweredUp.Bluetooth;

namespace Trackify.Infrastructure.Ble;

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
