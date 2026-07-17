using Trackify.Application.Trains;
using Trackify.Tests.Fakes;

namespace Trackify.Tests.Application;

public class TrainResolverTests
{
    private static readonly TrainResolver Resolver = new(new FakeTrainStore(
        new TrainConfig { Id = "trn-1", Name = "Blauer Zug" },
        new TrainConfig { Id = "trn-2", Name = "Roter Zug" }));

    [Fact]
    public async Task Resolves_by_id()
    {
        var train = await Resolver.FindAsync("trn-2");
        Assert.Equal("Roter Zug", train?.Name);
    }

    [Fact]
    public async Task Resolves_by_name_case_insensitively()
    {
        var train = await Resolver.FindAsync("blauer zug");
        Assert.Equal("trn-1", train?.Id);
    }

    [Fact]
    public async Task Returns_null_for_unknown_train()
        => Assert.Null(await Resolver.FindAsync("Grüner Zug"));
}
