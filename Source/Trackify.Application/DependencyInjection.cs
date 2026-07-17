using Microsoft.Extensions.DependencyInjection;
using Trackify.Application.Trains;
#if __ANDROID__ || __IOS__ || WINDOWS
using SharpBrick.PoweredUp;
using Trackify.Application.Services;
#endif
#if __ANDROID__ || __IOS__
using SharpBrick.PoweredUp.Mobile;
#endif

namespace Trackify.Application;

/// <summary>Application layer composition (use-case services + the platform hub transport).</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Application use-case services and — filtered per platform right here in DI —
    /// the matching <see cref="ILegoService"/> transport:
    /// Android/iOS → <c>DirectLegoService</c> (SharpBrick .Mobile / Plugin.BLE),
    /// Windows → <c>WindowsLegoService</c> (SharpBrick .WinRT).
    /// The plain net10.0 flavor registers no transport — that composition root decides
    /// (the CLI adds BlueZ via <c>AddTrackifyInfrastructure</c>; desktop/wasm add <c>UnsupportedLegoService</c>).
    /// </summary>
    public static IServiceCollection AddTrackifyApplication(this IServiceCollection services)
    {
        services.AddSingleton<TrainControlService>();
        services.AddSingleton<TrainResolver>();

#if __ANDROID__ || __IOS__
        services
            .AddPoweredUp()
            .AddXamarinBluetooth(CreateDeviceInfoProvider());
        services.AddSingleton<ILegoService, DirectLegoService>();
#if __IOS__
        // iOS prompts by itself (NSBluetoothAlwaysUsageDescription); Android supplies an
        // Activity-based IBluetoothPermissionService from the app's composition root.
        services.AddSingleton<IBluetoothPermissionService, IosBluetoothPermissionService>();
#endif
#elif WINDOWS
        services
            .AddPoweredUp()
            .AddWinRTBluetooth();
        services.AddSingleton<ILegoService, WindowsLegoService>();
#endif

        return services;
    }

#if __ANDROID__ || __IOS__
    private static INativeDeviceInfoProvider CreateDeviceInfoProvider()
#if __ANDROID__
        => new AndroidDeviceInfoProvider();
#else
        => new IosDeviceInfoProvider();
#endif
#endif
}
