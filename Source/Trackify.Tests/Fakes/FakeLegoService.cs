using Trackify.Application.Lego;

namespace Trackify.Tests.Fakes;

/// <summary>An in-memory <see cref="ILegoService"/> that records the last call, for asserting control logic.</summary>
public sealed class FakeLegoService : ILegoService
{
    public string? LastHubId;
    public byte LastPort;
    public sbyte LastPower;
    public (byte R, byte G, byte B)? LastLed;

    public bool IsSupported => true;

    public Task<IReadOnlyList<DiscoveredHub>> DiscoverAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DiscoveredHub>>([]);

    public Task ConnectAsync(string hubId, HubType hubType, CancellationToken ct = default)
    {
        LastHubId = hubId;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(string hubId, CancellationToken ct = default) => Task.CompletedTask;

    public Task SetSpeedAsync(string hubId, byte port, sbyte power, CancellationToken ct = default)
    {
        (LastHubId, LastPort, LastPower) = (hubId, port, power);
        return Task.CompletedTask;
    }

    public Task SetLedAsync(string hubId, byte red, byte green, byte blue, CancellationToken ct = default)
    {
        LastLed = (red, green, blue);
        return Task.CompletedTask;
    }
}
