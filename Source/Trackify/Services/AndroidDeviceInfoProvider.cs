#if __ANDROID__
using SharpBrick.PoweredUp.Mobile;

namespace Trackify.Services;

/// <summary>Supplies the Android BLE device id (its MAC address) to Plugin.BLE / SharpBrick.</summary>
internal sealed class AndroidDeviceInfoProvider : INativeDeviceInfoProvider
{
    public NativeDeviceInfo GetNativeDeviceInfo(object device)
        => new() { DeviceIdentifier = ((Android.Bluetooth.BluetoothDevice)device).Address ?? string.Empty };
}
#endif
