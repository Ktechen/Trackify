using Microsoft.Extensions.DependencyInjection;
using Trackify.Application.Trains;

namespace Trackify.Application;

/// <summary>Application layer composition (use-case services).</summary>
public static class DependencyInjection
{
    /// <summary>Registers the Application use-case services (the shared hub control logic).</summary>
    public static IServiceCollection AddTrackifyApplication(this IServiceCollection services)
    {
        services.AddSingleton<TrainControlService>();
        return services;
    }
}
