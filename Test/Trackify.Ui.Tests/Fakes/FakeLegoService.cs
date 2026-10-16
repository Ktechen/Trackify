using Trackify.Application.Lego;
using Trackify.Domain.Enums;

namespace Trackify.Ui.Tests.Fakes;

/// <summary>
/// In-memory hub transport that records what the ViewModel asked of it. A copy of the one in
/// Trackify.Tests rather than a shared file, because the two test projects target different
/// frameworks; this one can also pretend to be unsupported and to find hubs.
/// </summary>
public sealed class FakeLegoService : ILegoService
{
    /// <summary>Set false to exercise the "this device has no Bluetooth" path.</summary>
    public bool IsSupported { get; set; } = true;

    /// <summary>What <see cref="DiscoverAsync"/> reports back.</summary>
    public List<DiscoveredHubDto> Discoverable { get; } = [];

    public List<string> Connected { get; } = [];

    public List<string> Disconnected { get; } = [];

    public (string HubId, byte Port, sbyte Power)? LastSpeed { get; private set; }

    public (byte R, byte G, byte B)? LastLed { get; private set; }

    public Task<IReadOnlyList<DiscoveredHubDto>> DiscoverAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DiscoveredHubDto>>([.. Discoverable]);

    public Task ConnectAsync(string hubId, HubType hubType, CancellationToken ct = default)
    {
        Connected.Add(hubId);

        return Task.CompletedTask;
    }

    public Task DisconnectAsync(string hubId, CancellationToken ct = default)
    {
        Disconnected.Add(hubId);

        return Task.CompletedTask;
    }

    public Task SetSpeedAsync(string hubId, byte port, sbyte power, CancellationToken ct = default)
    {
        LastSpeed = (hubId, port, power);

        return Task.CompletedTask;
    }

    public Task SetLedAsync(string hubId, byte red, byte green, byte blue, CancellationToken ct = default)
    {
        LastLed = (red, green, blue);

        return Task.CompletedTask;
    }
}
