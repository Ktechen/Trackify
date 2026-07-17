#if __ANDROID__ || __IOS__
using SharpBrick.PoweredUp.Mobile;

namespace Trackify.Services;

/// <summary>
/// Factory for the platform BLE glue: the device-id provider Plugin.BLE needs, and the
/// runtime-permission handler the BLE service awaits before scanning.
/// </summary>
internal static class NativeBluetooth
{
    public static INativeDeviceInfoProvider CreateDeviceInfoProvider()
#if __ANDROID__
        => new AndroidDeviceInfoProvider();
#elif __IOS__
        => new IosDeviceInfoProvider();
#endif

    public static IBluetoothPermissionService CreatePermissionService()
#if __ANDROID__
        => new AndroidBluetoothPermissionService();
#elif __IOS__
        => new IosBluetoothPermissionService();
#endif
}
#endif
