#if __ANDROID__
namespace Trackify.Services;

/// <summary>Requests Android runtime BLE permissions via the activity that owns the result callback.</summary>
internal sealed class AndroidBluetoothPermissionService : IBluetoothPermissionService
{
    // The permission grant flow lives on the Activity (it owns OnRequestPermissionsResult).
    public Task<bool> EnsureGrantedAsync()
        => Trackify.Droid.MainActivity.Instance?.EnsureBluetoothPermissionsAsync() ?? Task.FromResult(false);
}
#endif
