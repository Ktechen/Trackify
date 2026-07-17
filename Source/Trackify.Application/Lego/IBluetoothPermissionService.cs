namespace Trackify.Application.Lego;

/// <summary>
/// Requests the OS Bluetooth permissions a scan/connect needs, on demand. The BLE service awaits
/// this before touching the radio, so the user is prompted exactly when the feature is first used.
/// </summary>
public interface IBluetoothPermissionService
{
    /// <summary>Ensures the required permissions are granted, prompting if necessary. Returns whether granted.</summary>
    Task<bool> EnsureGrantedAsync();
}
