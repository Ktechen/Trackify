using Spectre.Console.Cli;
using Trackify.Application.TrackPlans;
using Trackify.Cli.Commands;
using Trackify.Cli.Commands.Settings;
using Trackify.Tests.Fakes;

namespace Trackify.Tests.Cli;

/// <summary>
/// The track commands, asserted through their exit codes and what they ask of the exchange service.
/// The exit code is the CLI's real contract — it is what a script or a systemd unit reacts to.
/// </summary>
public class TrackCommandsTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), "trackify-cli-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    // ---- show ----------------------------------------------------------------------------------

    [Fact]
    public async Task Show_succeeds_when_no_plan_is_saved()
    {
        var exit = await Run(new TrackShowCommand(new FakeTrackPlanExchangeService()));

        Assert.Equal(0, exit);
    }

    [Fact]
    public async Task Show_succeeds_for_a_plan_whose_joints_all_line_up()
    {
        var exchange = new FakeTrackPlanExchangeService
        {
            Summary = new TrackPlanSummary
            {
                PartCount = 18,
                ConnectionCount = 18,
                IsBuildable = true,
                PartsByKey = new Dictionary<string, int> { ["straight"] = 2, ["curve-r40-right"] = 16 },
            },
        };

        var exit = await Run(new TrackShowCommand(exchange));

        Assert.Equal(0, exit);
    }

    [Fact]
    public async Task Show_fails_for_a_plan_that_cannot_be_built()
    {
        // The non-zero exit is the point: a script has to be able to notice this.
        var exchange = new FakeTrackPlanExchangeService
        {
            Summary = new TrackPlanSummary
            {
                PartCount = 15,
                IsBuildable = false,
                PartsByKey = new Dictionary<string, int> { ["curve-r40-right"] = 15 },
                Problems = ["A joint does not line up: 15.6 studs apart."],
            },
        };

        var exit = await Run(new TrackShowCommand(exchange));

        Assert.Equal(1, exit);
    }

    // ---- import --------------------------------------------------------------------------------

    [Fact]
    public async Task Import_fails_and_touches_nothing_when_the_file_is_missing()
    {
        var exchange = new FakeTrackPlanExchangeService();

        var exit = await Run(
            new TrackImportCommand(exchange),
            new TrackImportSettings { File = Path.Combine(_directory, "nope.json") });

        Assert.Equal(1, exit);
        Assert.Empty(exchange.Imported);
        Assert.Empty(exchange.Validated);
    }

    [Fact]
    public async Task Import_reads_the_file_and_succeeds()
    {
        var exchange = new FakeTrackPlanExchangeService
        {
            Result = new TrackPlanImportResult { Succeeded = true, PartCount = 4, ConnectionCount = 3 },
        };
        var file = Write("plan.json", """{ "format": "trackify.trackplan" }""");

        var exit = await Run(new TrackImportCommand(exchange), new TrackImportSettings { File = file });

        Assert.Equal(0, exit);
        var (json, replace) = Assert.Single(exchange.Imported);
        Assert.Contains("trackify.trackplan", json, StringComparison.Ordinal);
        Assert.False(replace);
    }

    [Fact]
    public async Task Import_with_replace_says_so_to_the_store()
    {
        var exchange = new FakeTrackPlanExchangeService();
        var file = Write("plan.json", "{}");

        await Run(new TrackImportCommand(exchange), new TrackImportSettings { File = file, Replace = true });

        Assert.True(Assert.Single(exchange.Imported).Replace);
    }

    [Fact]
    public async Task A_dry_run_validates_without_writing_anything()
    {
        var exchange = new FakeTrackPlanExchangeService();
        var file = Write("plan.json", "{}");

        var exit = await Run(new TrackImportCommand(exchange), new TrackImportSettings { File = file, DryRun = true });

        Assert.Equal(0, exit);
        Assert.Single(exchange.Validated);
        Assert.Empty(exchange.Imported);
    }

    [Fact]
    public async Task Import_fails_when_the_plan_is_rejected()
    {
        var exchange = new FakeTrackPlanExchangeService
        {
            Result = TrackPlanImportResult.Failed("Not a track plan."),
        };
        var file = Write("plan.json", "{}");

        var exit = await Run(new TrackImportCommand(exchange), new TrackImportSettings { File = file });

        Assert.Equal(1, exit);
    }

    [Fact]
    public async Task Import_reports_warnings_but_still_succeeds()
    {
        var exchange = new FakeTrackPlanExchangeService
        {
            Result = new TrackPlanImportResult
            {
                Succeeded = true,
                PartCount = 15,
                OpenEndCount = 2,
                Warnings = ["The loop does not really close."],
            },
        };
        var file = Write("plan.json", "{}");

        var exit = await Run(new TrackImportCommand(exchange), new TrackImportSettings { File = file });

        Assert.Equal(0, exit);
    }

    // ---- export --------------------------------------------------------------------------------

    [Fact]
    public async Task Export_without_a_path_prints_the_plan()
    {
        var exchange = new FakeTrackPlanExchangeService { Exported = """{ "format": "trackify.trackplan" }""" };

        var exit = await Run(new TrackExportCommand(exchange), new TrackExportSettings());

        Assert.Equal(0, exit);
        Assert.Single(exchange.ExportNames);
    }

    [Fact]
    public async Task Export_with_a_path_writes_the_file_and_passes_the_name_through()
    {
        var exchange = new FakeTrackPlanExchangeService { Exported = """{ "name": "Kellerbahn" }""" };
        var file = Path.Combine(EnsureDirectory(), "out.json");

        var exit = await Run(
            new TrackExportCommand(exchange),
            new TrackExportSettings { File = file, Name = "Kellerbahn" });

        Assert.Equal(0, exit);
        Assert.Equal("""{ "name": "Kellerbahn" }""", await File.ReadAllTextAsync(file));
        Assert.Equal("Kellerbahn", Assert.Single(exchange.ExportNames));
    }

    // ---- plumbing ------------------------------------------------------------------------------

    // ExecuteAsync is protected on AsyncCommand, so the test goes in through ICommand, which is how
    // Spectre's own executor calls it.
    private static Task<int> Run(ICommand command, CommandSettings? settings = null)
        => command.ExecuteAsync(Context(), settings ?? new EmptySettings(), CancellationToken.None);

    private static CommandContext Context() => new([], new NoRemainingArguments(), "track", null);

    private string EnsureDirectory()
    {
        Directory.CreateDirectory(_directory);

        return _directory;
    }

    private string Write(string name, string content)
    {
        var path = Path.Combine(EnsureDirectory(), name);
        File.WriteAllText(path, content);

        return path;
    }

    private sealed class EmptySettings : CommandSettings;

    private sealed class NoRemainingArguments : IRemainingArguments
    {
        public ILookup<string, string?> Parsed => Array.Empty<string>().ToLookup(_ => "", _ => (string?)null);

        public IReadOnlyList<string> Raw => [];
    }
}
