#if __IOS__
using SharpBrick.PoweredUp.Mobile;

namespace Trackify.Services;

/// <summary>Supplies the iOS BLE device id (a CoreBluetooth UUID) to Plugin.BLE / SharpBrick.</summary>
internal sealed class IosDeviceInfoProvider : INativeDeviceInfoProvider
{
    public NativeDeviceInfo GetNativeDeviceInfo(object device)
        => new() { DeviceIdentifier = ((CoreBluetooth.CBPeripheral)device).Identifier.AsString() };
}
#endif
