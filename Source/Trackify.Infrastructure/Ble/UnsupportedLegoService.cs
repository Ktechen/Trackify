using Trackify.Domain.Enums;

namespace Trackify.Infrastructure.Ble;

/// <summary>
/// Stand-in used when the CLI is built for a non-Linux host: hub control needs BlueZ (Linux only), so
/// discovery is empty and control calls report clearly that they are unsupported here.
/// </summary>
internal sealed class UnsupportedLegoService : ILegoService
{
    private const string Message = "Hub control requires Linux (BlueZ). Build/run the CLI on the Raspberry Pi.";

    public bool IsSupported => false;

    public Task<IReadOnlyList<DiscoveredHub>> DiscoverAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DiscoveredHub>>([]);

    public Task ConnectAsync(string hubId, HubType hubType, CancellationToken ct = default)
        => throw new PlatformNotSupportedException(Message);

    public Task DisconnectAsync(string hubId, CancellationToken ct = default) => Task.CompletedTask;

    public Task SetSpeedAsync(string hubId, byte port, sbyte power, CancellationToken ct = default)
        => throw new PlatformNotSupportedException(Message);

    public Task SetLedAsync(string hubId, byte red, byte green, byte blue, CancellationToken ct = default)
        => throw new PlatformNotSupportedException(Message);
}
