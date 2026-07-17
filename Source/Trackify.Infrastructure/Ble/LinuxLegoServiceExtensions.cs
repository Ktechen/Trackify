using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
#if LINUX
using SharpBrick.PoweredUp;
using SharpBrick.PoweredUp.Bluetooth;
#endif

namespace Trackify.Infrastructure.Ble;

/// <summary>Composition-root helper for the Linux/BlueZ hub transport.</summary>
public static class LinuxLegoServiceExtensions
{
    /// <summary>
    /// Registers the hub transport — but only if no transport is registered yet (TryAdd), so a
    /// platform transport supplied by <c>AddTrackifyApplication</c> (e.g. WinRT on the Windows CLI
    /// build) wins. On a Linux build (the <c>LINUX</c> constant is defined when building on a Linux
    /// host) this adds the real BlueZ onboard-radio adapter; elsewhere a no-op
    /// <see cref="UnsupportedLegoService"/> so the CLI still builds and runs (dashboard/list).
    /// </summary>
    public static IServiceCollection AddLinuxLego(this IServiceCollection services)
    {
#if LINUX
        services.AddPoweredUp();
        services.TryAddSingleton<IPoweredUpBluetoothAdapter, BlueZPoweredUpBluetoothAdapter>();
        services.TryAddSingleton<ILegoService, BlueZLegoService>();
#else
        services.TryAddSingleton<ILegoService, UnsupportedLegoService>();
#endif
        return services;
    }
}
