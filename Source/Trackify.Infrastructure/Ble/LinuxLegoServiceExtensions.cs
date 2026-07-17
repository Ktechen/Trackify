using Microsoft.Extensions.DependencyInjection;
#if LINUX
using SharpBrick.PoweredUp;
using SharpBrick.PoweredUp.Bluetooth;
#endif

namespace Trackify.Infrastructure.Ble;

/// <summary>Composition-root helper for the Linux/BlueZ hub transport.</summary>
public static class LinuxLegoServiceExtensions
{
    /// <summary>
    /// Registers the hub transport. On a Linux build (the <c>LINUX</c> constant is defined when
    /// building on a Linux host) this is the real BlueZ onboard-radio adapter; elsewhere it is a
    /// no-op <see cref="UnsupportedLegoService"/> so the CLI still builds and runs (dashboard/list).
    /// </summary>
    public static IServiceCollection AddLinuxLego(this IServiceCollection services)
    {
#if LINUX
        services.AddPoweredUp();
        services.AddSingleton<IPoweredUpBluetoothAdapter, BlueZPoweredUpBluetoothAdapter>();
        services.AddSingleton<ILegoService, BlueZLegoService>();
#else
        services.AddSingleton<ILegoService, UnsupportedLegoService>();
#endif
        return services;
    }
}
