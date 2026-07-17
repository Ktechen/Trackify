using Trackify.Cli.Commands;
using Trackify.Domain.Trains;
using Trackify.Tests.Fakes;
using Xunit;

namespace Trackify.Tests.Cli;

public class TrainResolutionTests
{
    private static readonly FakeTrainStore Store = new(
        new TrainConfig { Id = "trn-1", Name = "Blauer Zug" },
        new TrainConfig { Id = "trn-2", Name = "Roter Zug" });

    [Fact]
    public async Task Resolves_by_id()
    {
        var train = await CliHelpers.ResolveTrainAsync(Store, "trn-2");
        Assert.Equal("Roter Zug", train?.Name);
    }

    [Fact]
    public async Task Resolves_by_name_case_insensitively()
    {
        var train = await CliHelpers.ResolveTrainAsync(Store, "blauer zug");
        Assert.Equal("trn-1", train?.Id);
    }

    [Fact]
    public async Task Returns_null_for_unknown_train()
        => Assert.Null(await CliHelpers.ResolveTrainAsync(Store, "Grüner Zug"));
}
