using Trackify.Models.Trains.Enums;

namespace Trackify.Services;

/// <summary>
/// Used on platforms without a usable Bluetooth LE stack (desktop, WebAssembly). Discovery yields
/// nothing and every control call fails with a clear message rather than silently doing nothing.
/// </summary>
public sealed class UnsupportedLegoService : ILegoService
{
    private const string Message = "Bluetooth-Steuerung ist auf dieser Plattform nicht verfügbar. Bitte die Android- oder iOS-App verwenden.";

    public bool IsSupported => false;

    public Task<IReadOnlyList<DiscoveredHub>> DiscoverAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DiscoveredHub>>([]);

    public Task ConnectAsync(string hubId, HubType hubType, CancellationToken ct = default)
        => throw new NotSupportedException(Message);

    public Task DisconnectAsync(string hubId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SetSpeedAsync(string hubId, byte port, sbyte power, CancellationToken ct = default)
        => throw new NotSupportedException(Message);

    public Task SetLedAsync(string hubId, byte red, byte green, byte blue, CancellationToken ct = default)
        => throw new NotSupportedException(Message);
}
