using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharpBrick.PoweredUp;
using SharpBrick.PoweredUp.Bluetooth;

namespace Trackify.Infrastructure.Ble;

/// <summary>Composition-root helper for the Linux/BlueZ hub transport.</summary>
public static class LinuxLegoServiceExtensions
{
    /// <summary>
    /// Registers the hub transport — but only if none is registered yet (TryAdd), so a platform
    /// transport supplied by <c>AddTrackifyApplication</c> (e.g. WinRT on the Windows CLI build) wins.
    /// Selection is a plain <b>runtime OS check</b> (no compile-time <c>#if</c> / TrackifyLinux flag):
    /// the BlueZ types compile on every TFM, so on Linux we wire the real onboard-radio adapter and
    /// elsewhere a no-op <see cref="UnsupportedLegoService"/> (so the CLI still runs the dashboard/list
    /// off-Linux). A build cross-published from any host therefore just works on the Pi.
    /// </summary>
    public static IServiceCollection AddLinuxLego(this IServiceCollection services)
    {
        if (OperatingSystem.IsLinux())
        {
            services.AddPoweredUp();
            services.TryAddSingleton<IPoweredUpBluetoothAdapter, BlueZPoweredUpBluetoothAdapter>();
            services.TryAddSingleton<ILegoService, BlueZLegoService>();
        }
        else
        {
            services.TryAddSingleton<ILegoService, UnsupportedLegoService>();
        }

        return services;
    }
}
