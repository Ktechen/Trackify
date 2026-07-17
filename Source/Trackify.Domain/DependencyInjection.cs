using Microsoft.Extensions.DependencyInjection;

namespace Trackify.Domain;

/// <summary>Domain layer composition.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Domain layer. It is pure (entities + speed-function math) and has no services to
    /// register; this entry point exists for symmetry so a composition root can call every layer's
    /// <c>AddTrackify*</c> in one place.
    /// </summary>
    public static IServiceCollection AddTrackifyDomain(this IServiceCollection services) => services;
}
