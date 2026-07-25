using Trackify.Application.Lego;
using Trackify.Application.Trains;
using Trackify.Tests.Fakes;

namespace Trackify.Tests.Application;

/// <summary>Covers <see cref="TrainService.SaveDiscoveredAsync"/> — the `discover --save` use-case.</summary>
public class TrainSaveDiscoveredTests
{
    [Fact]
    public async Task Saves_a_new_train_from_a_discovered_hub()
    {
        var repository = new FakeTrainRepository();
        var service = new TrainService(repository);
        var hub = new DiscoveredHubDto("AA:BB:CC:DD:EE:FF", "Blauer Zug", "AA:BB:CC:DD:EE:FF", HubType.PoweredUpHub);

        var saved = await service.SaveDiscoveredAsync(hub);

        Assert.Equal("Blauer Zug", saved.Name);
        Assert.Equal("AA:BB:CC:DD:EE:FF", saved.HubId);
        Assert.Equal(HubType.PoweredUpHub, saved.Hub);
        Assert.Single(await service.GetAllAsync());
    }

    [Fact]
    public async Task Does_not_duplicate_a_hub_already_saved_by_HubId()
    {
        var repository = new FakeTrainRepository(new Train { Name = "Existing", HubId = "AA:BB" });
        var service = new TrainService(repository);
        var hub = new DiscoveredHubDto("AA:BB", "Fresh Name", "AA:BB", HubType.PoweredUpHub);

        var saved = await service.SaveDiscoveredAsync(hub);

        Assert.Equal("Existing", saved.Name); // the existing train is returned, not a new duplicate
        Assert.Single(await service.GetAllAsync());
    }

    [Fact]
    public async Task Does_not_duplicate_a_hub_already_saved_by_MAC()
    {
        var repository = new FakeTrainRepository(new Train { Name = "Existing", BleAddress = "AA:BB:CC" });
        var service = new TrainService(repository);
        var hub = new DiscoveredHubDto("some-id", "Fresh Name", "AA:BB:CC", HubType.PoweredUpHub);

        var saved = await service.SaveDiscoveredAsync(hub);

        Assert.Equal("Existing", saved.Name);
        Assert.Single(await service.GetAllAsync());
    }

    [Fact]
    public async Task Falls_back_to_address_for_the_name_when_the_hub_is_unnamed()
    {
        var repository = new FakeTrainRepository();
        var service = new TrainService(repository);
        var hub = new DiscoveredHubDto("device-id", null, "AA:BB:CC", HubType.PoweredUpHub);

        var saved = await service.SaveDiscoveredAsync(hub);

        Assert.Equal("AA:BB:CC", saved.Name);
    }
}
