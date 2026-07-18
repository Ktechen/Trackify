using Trackify.Application.Trains;
using Trackify.Tests.Fakes;

namespace Trackify.Tests.Application;

public class TrainResolverTests
{
    private static readonly Guid BlueId = Guid.NewGuid();
    private static readonly TrainResolver Resolver = new(new FakeTrainRepository(
        new Train { Id = BlueId, Name = "Blauer Zug" },
        new Train { Id = Guid.NewGuid(), Name = "Roter Zug" }));

    [Fact]
    public async Task Resolves_by_id()
    {
        var train = await Resolver.FindAsync(BlueId.ToString());
        Assert.Equal("Blauer Zug", train?.Name);
    }

    [Fact]
    public async Task Resolves_by_name_case_insensitively()
    {
        var train = await Resolver.FindAsync("blauer zug");
        Assert.Equal(BlueId, train?.Id);
    }

    [Fact]
    public async Task Returns_null_for_unknown_train()
        => Assert.Null(await Resolver.FindAsync("Grüner Zug"));
}
