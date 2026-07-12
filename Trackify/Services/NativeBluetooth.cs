#if __ANDROID__ || __IOS__
using SharpBrick.PoweredUp.Mobile;

namespace Trackify.Services;

/// <summary>
/// Platform-specific BLE glue, all in one place: the device-id provider Plugin.BLE needs, and the
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

    public static IBluetoothPermissions CreatePermissions()
#if __ANDROID__
        => new AndroidBluetoothPermissions();
#elif __IOS__
        => new IosBluetoothPermissions();
#endif
}

#if __ANDROID__
internal sealed class AndroidDeviceInfoProvider : INativeDeviceInfoProvider
{
    public NativeDeviceInfo GetNativeDeviceInfo(object device)
        => new() { DeviceIdentifier = ((Android.Bluetooth.BluetoothDevice)device).Address ?? string.Empty };
}

internal sealed class AndroidBluetoothPermissions : IBluetoothPermissions
{
    // The permission grant flow lives on the Activity (it owns OnRequestPermissionsResult).
    public Task<bool> EnsureGrantedAsync()
        => Trackify.Droid.MainActivity.Instance?.EnsureBluetoothPermissionsAsync() ?? Task.FromResult(false);
}
#endif

#if __IOS__
internal sealed class IosDeviceInfoProvider : INativeDeviceInfoProvider
{
    public NativeDeviceInfo GetNativeDeviceInfo(object device)
        => new() { DeviceIdentifier = ((CoreBluetooth.CBPeripheral)device).Identifier.AsString() };
}

internal sealed class IosBluetoothPermissions : IBluetoothPermissions
{
    // iOS shows its own system prompt (backed by NSBluetoothAlwaysUsageDescription) the first time
    // the central manager is used, so there is nothing to request up front here.
    public Task<bool> EnsureGrantedAsync() => Task.FromResult(true);
}
#endif
#endif
