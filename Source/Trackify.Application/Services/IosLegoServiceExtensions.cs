#if __IOS__
using Microsoft.Extensions.DependencyInjection;
using SharpBrick.PoweredUp;
using SharpBrick.PoweredUp.Mobile;

namespace Trackify.Application.Services;

/// <summary>
/// Composition-root helper for the iOS hub transport (SharpBrick .Mobile on Plugin.BLE) — the iOS-head
/// counterpart to <c>AddLinuxLego</c>. iOS prompts for Bluetooth itself (NSBluetoothAlwaysUsageDescription),
/// so its <c>IBluetoothPermissionService</c> is a no-op registered here.
/// </summary>
public static class IosLegoServiceExtensions
{
    public static IServiceCollection AddIosLego(this IServiceCollection services)
    {
        services.AddPoweredUp().AddXamarinBluetooth(new IosDeviceInfoProvider());
        services.AddSingleton<ILegoService, DirectLegoService>();
        services.AddSingleton<IBluetoothPermissionService, IosBluetoothPermissionService>();
        return services;
    }
}
#endif
