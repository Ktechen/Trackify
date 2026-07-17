#if __IOS__
namespace Trackify.Services;

/// <summary>
/// iOS shows its own system prompt (backed by NSBluetoothAlwaysUsageDescription) the first time the
/// central manager is used, so there is nothing to request up front here.
/// </summary>
internal sealed class IosBluetoothPermissionService : IBluetoothPermissionService
{
    public Task<bool> EnsureGrantedAsync() => Task.FromResult(true);
}
#endif
