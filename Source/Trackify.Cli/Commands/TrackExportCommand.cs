using Trackify.Cli.Commands.Settings;

namespace Trackify.Cli.Commands;

/// <summary>Writes the saved track plan out as a plan file.</summary>
public sealed class TrackExportCommand(ITrackPlanExchangeService exchange) : AsyncCommand<TrackExportSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context, TrackExportSettings settings, CancellationToken cancellationToken)
    {
        var json = await exchange.ExportAsync(settings.Name, cancellationToken);

        if (string.IsNullOrWhiteSpace(settings.File))
        {
            // Plain, unmarkupped, so the output can be piped straight into a file.
            AnsiConsole.WriteLine(json);
            return 0;
        }

        await File.WriteAllTextAsync(settings.File, json, cancellationToken);
        AnsiConsole.MarkupLineInterpolated($"[green]✓[/] Wrote the plan to {settings.File}.");

        return 0;
    }
}
