#if __ANDROID__
using Microsoft.Extensions.DependencyInjection;
using SharpBrick.PoweredUp;
using SharpBrick.PoweredUp.Mobile;

namespace Trackify.Application.Services;

/// <summary>
/// Composition-root helper for the Android hub transport (SharpBrick .Mobile on Plugin.BLE) — the
/// Android-head counterpart to <c>AddLinuxLego</c>. The Android runtime-permission flow needs the
/// <c>Activity</c>, so the app supplies <c>IBluetoothPermissionService</c> from its own root.
/// </summary>
public static class AndroidLegoServiceExtensions
{
    public static IServiceCollection AddAndroidLego(this IServiceCollection services)
    {
        services.AddPoweredUp().AddXamarinBluetooth(new AndroidDeviceInfoProvider());
        services.AddSingleton<ILegoService, DirectLegoService>();
        return services;
    }
}
#endif
