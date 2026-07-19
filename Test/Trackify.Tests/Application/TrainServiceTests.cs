using Trackify.Application.Trains;
using Trackify.Tests.Fakes;

namespace Trackify.Tests.Application;

public class TrainServiceTests
{
    private static readonly Guid BlueId = Guid.NewGuid();

    // The repository is seeded with Domain entities; the service returns DTOs at the boundary.
    private static readonly TrainService Query = new(new FakeTrainRepository(
        new Train { Id = BlueId, Name = "Blauer Zug" },
        new Train { Id = Guid.NewGuid(), Name = "Roter Zug" }));

    [Fact]
    public async Task Resolves_by_id()
    {
        var train = await Query.FindAsync(BlueId.ToString());
        Assert.IsType<TrainDto>(train);
        Assert.Equal("Blauer Zug", train?.Name);
    }

    [Fact]
    public async Task Resolves_by_name_case_insensitively()
    {
        var train = await Query.FindAsync("blauer zug");
        Assert.Equal(BlueId, train?.Id);
    }

    [Fact]
    public async Task Returns_null_for_unknown_train()
        => Assert.Null(await Query.FindAsync("Grüner Zug"));

    [Fact]
    public async Task GetAll_returns_every_saved_train_as_dtos()
    {
        var trains = await Query.GetAllAsync();
        Assert.Equal(2, trains.Count);
        Assert.Contains(trains, train => train.Name == "Blauer Zug");
    }
}
