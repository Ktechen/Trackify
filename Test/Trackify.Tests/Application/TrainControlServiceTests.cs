using Trackify.Application.Lego;
using Trackify.Application.Trains;
using Trackify.Tests.Fakes;

namespace Trackify.Tests.Application;

public class TrainControlServiceTests
{
    [Fact]
    public void HubKey_prefers_HubId_then_falls_back_to_BleAddress()
    {
        Assert.Equal("dev-1", TrainControlService.HubKey(new TrainDto { HubId = "dev-1", BleAddress = "AA:BB" }));
        Assert.Equal("AA:BB", TrainControlService.HubKey(new TrainDto { HubId = "", BleAddress = "AA:BB" }));
    }

    [Fact]
    public void IsSameDevice_matches_by_id_or_mac()
    {
        var train = new TrainDto { HubId = "dev-1", BleAddress = "AA:BB:CC" };
        Assert.True(TrainControlService.IsSameDevice(train, new DiscoveredHub("dev-1", null, null, null)));
        Assert.True(TrainControlService.IsSameDevice(train, new DiscoveredHub("other", null, "aa:bb:cc", null)));
        Assert.False(TrainControlService.IsSameDevice(train, new DiscoveredHub("other", null, "99:99:99", null)));
    }

    [Fact]
    public async Task SetSpeedAsync_clamps_and_targets_the_motor_port()
    {
        var fake = new FakeLegoService();
        var service = new TrainControlService(fake, NullLogger<TrainControlService>.Instance);

        await service.SetSpeedAsync(new TrainDto { HubId = "dev-1" }, 150);

        Assert.Equal("dev-1", fake.LastHubId);
        Assert.Equal(TrainControlService.MotorPort, fake.LastPort);
        Assert.Equal(100, fake.LastPower);
    }

    [Fact]
    public async Task ConnectAsync_throws_when_train_has_no_hub()
        => await Assert.ThrowsAsync<InvalidOperationException>(
            () => new TrainControlService(new FakeLegoService(), NullLogger<TrainControlService>.Instance)
                .ConnectAsync(new TrainDto()));
}
