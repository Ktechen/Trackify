using Trackify.Cli.Server;

namespace Trackify.Tests.Cli;

/// <summary>Covers the backend's per-hub speed tracking used for live SignalR state.</summary>
public class TrainStateStoreTests
{
    [Fact]
    public void Records_and_snapshots_the_latest_speed_per_hub()
    {
        var store = new TrainStateStore();
        store.SetSpeed("hub-a", 40);
        store.SetSpeed("hub-b", -20);
        store.SetSpeed("hub-a", 55); // overwrites the earlier value

        var snapshot = store.Snapshot();

        Assert.Equal(2, snapshot.Count);
        Assert.Equal(55, snapshot.Single(state => state.HubId == "hub-a").Speed);
        Assert.Equal(-20, snapshot.Single(state => state.HubId == "hub-b").Speed);
    }

    [Fact]
    public void Snapshot_is_empty_before_any_speed_is_set()
        => Assert.Empty(new TrainStateStore().Snapshot());
}
