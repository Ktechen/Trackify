using Trackify.Application.Lego;
using Trackify.Domain.Enums;
using Trackify.Domain.Trains;
using Trackify.Application.Trains;
using Xunit;

namespace Trackify.Tests;

public class TrainControlServiceTests
{
    private sealed class FakeLegoService : ILegoService
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

    [Fact]
    public void HubKey_prefers_HubId_then_falls_back_to_BleAddress()
    {
        Assert.Equal("dev-1", TrainControlService.HubKey(new TrainConfig { HubId = "dev-1", BleAddress = "AA:BB" }));
        Assert.Equal("AA:BB", TrainControlService.HubKey(new TrainConfig { HubId = "", BleAddress = "AA:BB" }));
    }

    [Fact]
    public void IsSameDevice_matches_by_id_or_mac()
    {
        var train = new TrainConfig { HubId = "dev-1", BleAddress = "AA:BB:CC" };
        Assert.True(TrainControlService.IsSameDevice(train, new DiscoveredHub("dev-1", null, null, null)));
        Assert.True(TrainControlService.IsSameDevice(train, new DiscoveredHub("other", null, "aa:bb:cc", null)));
        Assert.False(TrainControlService.IsSameDevice(train, new DiscoveredHub("other", null, "99:99:99", null)));
    }

    [Fact]
    public async Task SetSpeedAsync_clamps_and_targets_the_motor_port()
    {
        var fake = new FakeLegoService();
        var service = new TrainControlService(fake);

        await service.SetSpeedAsync(new TrainConfig { HubId = "dev-1" }, 150);

        Assert.Equal("dev-1", fake.LastHubId);
        Assert.Equal(TrainControlService.MotorPort, fake.LastPort);
        Assert.Equal(100, fake.LastPower);
    }

    [Fact]
    public async Task ConnectAsync_throws_when_train_has_no_hub()
        => await Assert.ThrowsAsync<InvalidOperationException>(
            () => new TrainControlService(new FakeLegoService()).ConnectAsync(new TrainConfig()));
}
