namespace Trackify.Cli.Commands;

/// <summary>
/// Describes the saved track plan: how much of each part it needs, how much rail that is, what is
/// still open, and whether every claimed joint actually works.
/// </summary>
public sealed class TrackShowCommand(ITrackPlanExchangeService exchange) : AsyncCommand
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var summary = await exchange.SummariseAsync(cancellationToken);

        if (summary.PartCount == 0)
        {
            AnsiConsole.MarkupLine(
                "[yellow]No track plan saved yet.[/] Build one in the app, then 'track import' it here.");
            return 0;
        }

        var parts = new Table().Border(TableBorder.Rounded);
        parts.AddColumn("Part");
        parts.AddColumn(new TableColumn("Count").RightAligned());

        foreach (var entry in summary.PartsByKey.OrderByDescending(entry => entry.Value))
        {
            parts.AddRow(Markup.Escape(entry.Key), $"{entry.Value}");
        }

        AnsiConsole.Write(parts);

        AnsiConsole.MarkupLineInterpolated(
            $"{summary.PartCount} parts · {summary.ConnectionCount} connections · {summary.OpenEndCount} open end(s)");

        // Studs and metres both come from the summary: converting here would mean reaching into the
        // Domain's track metrics, which is exactly the dependency the DTO boundary forbids the CLI.
        AnsiConsole.MarkupLineInterpolated(
            $"{summary.TotalLengthStuds:0} studs of rail ({summary.TotalLengthMetres:0.00} m) · {summary.SensorCount} sensor(s)");
        AnsiConsole.MarkupLineInterpolated(
            $"{summary.SwitchCount} point(s), {summary.MotorisedSwitchCount} of them motorised");

        if (summary.IsBuildable)
        {
            AnsiConsole.MarkupLine("[green]✓ Every connection lines up — this plan can be built.[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]✗ This plan cannot be built as connected:[/]");
        foreach (var problem in summary.Problems) AnsiConsole.MarkupLineInterpolated($"  {problem}");

        return 1;
    }
}
