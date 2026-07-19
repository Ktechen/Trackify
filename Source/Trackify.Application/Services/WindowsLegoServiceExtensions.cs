#if WINDOWS
using Microsoft.Extensions.DependencyInjection;
using SharpBrick.PoweredUp;

namespace Trackify.Application.Services;

/// <summary>
/// Composition-root helper for the Windows (WinRT) hub transport — the Windows-head counterpart to
/// <c>AddLinuxLego</c>. Compile-time gated: <c>SharpBrick.PoweredUp.WinRT</c> only exists on this head.
/// </summary>
public static class WindowsLegoServiceExtensions
{
    public static IServiceCollection AddWindowsLego(this IServiceCollection services)
    {
        services.AddPoweredUp().AddWinRTBluetooth();
        services.AddSingleton<ILegoService, WindowsLegoService>();
        return services;
    }
}
#endif
