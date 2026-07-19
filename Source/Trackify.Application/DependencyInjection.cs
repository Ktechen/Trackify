using Microsoft.Extensions.DependencyInjection;
using Trackify.Application.Trains;
#if __ANDROID__ || __IOS__ || WINDOWS
using Trackify.Application.Services;
#endif

namespace Trackify.Application;

/// <summary>Application layer composition — use-case services + the per-platform hub transport.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Application use-case services (<see cref="ITrainControlService"/>,
    /// <see cref="ITrainService"/>) and — filtered per platform right here in DI — the matching
    /// <see cref="ILegoService"/> transport via its <c>Add…Lego</c> helper (mirroring the CLI's
    /// <c>AddLinuxLego</c>): Android → <c>AddAndroidLego</c>, iOS → <c>AddIosLego</c>, Windows →
    /// <c>AddWindowsLego</c>. The plain net10.0 flavor registers none — the composition root decides
    /// (the CLI adds BlueZ via <c>AddTrackifyInfrastructure</c>; desktop/wasm add a no-op in the app).
    /// </summary>
    public static IServiceCollection AddTrackifyApplication(this IServiceCollection services)
    {
        services.AddSingleton<ITrainControlService, TrainControlService>();
        services.AddSingleton<ITrainService, TrainService>();

#if __ANDROID__
        services.AddAndroidLego();
#elif __IOS__
        services.AddIosLego();
#elif WINDOWS
        services.AddWindowsLego();
#endif

        return services;
    }
}
