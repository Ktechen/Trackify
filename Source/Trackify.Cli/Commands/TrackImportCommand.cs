using Trackify.Cli.Commands.Settings;

namespace Trackify.Cli.Commands;

/// <summary>
/// Reads a track plan built in the app into this machine's store — the "generate it in the UI,
/// import it into the CLI" path.
/// </summary>
public sealed class TrackImportCommand(ITrackPlanExchangeService exchange) : AsyncCommand<TrackImportSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context, TrackImportSettings settings, CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.File))
        {
            AnsiConsole.MarkupLineInterpolated($"[red]✗[/] No such file: {settings.File}");
            return 1;
        }

        var json = await File.ReadAllTextAsync(settings.File, cancellationToken);

        var result = settings.DryRun
            ? exchange.Validate(json)
            : await exchange.ImportAsync(json, settings.Replace, cancellationToken);

        // Messages carry part keys and port names straight out of the file, so they are escaped.
        foreach (var error in result.Errors) AnsiConsole.MarkupLineInterpolated($"[red]✗[/] {error}");
        foreach (var warning in result.Warnings) AnsiConsole.MarkupLineInterpolated($"[yellow]![/] {warning}");

        if (!result.Succeeded)
        {
            AnsiConsole.MarkupLine("[red]Nothing was imported.[/]");
            return 1;
        }

        var verb = settings.DryRun
            ? "Would import"
            : settings.Replace ? "Replaced the plan with" : "Imported";
        var name = string.IsNullOrWhiteSpace(result.PlanName) ? "(unnamed)" : result.PlanName;

        AnsiConsole.MarkupLineInterpolated(
            $"[green]✓[/] {verb} {result.PartCount} part(s) and {result.ConnectionCount} connection(s) from {name}.");

        if (result.OpenEndCount > 0)
        {
            AnsiConsole.MarkupLineInterpolated(
                $"  {result.OpenEndCount} open end(s) — the track is not a closed loop.");
        }

        return 0;
    }
}
