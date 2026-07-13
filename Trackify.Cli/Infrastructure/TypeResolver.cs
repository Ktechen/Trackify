using Spectre.Console.Cli;

namespace Trackify.Cli.Infrastructure;

/// <summary>Resolves command dependencies from the built service provider.</summary>
public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    public object? Resolve(Type? type) => type is null ? null : provider.GetService(type);

    public void Dispose()
    {
        if (provider is IDisposable disposable) disposable.Dispose();
    }
}
