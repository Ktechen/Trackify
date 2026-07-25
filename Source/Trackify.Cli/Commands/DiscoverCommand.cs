using Trackify.Application.Lego;
using Trackify.Cli.Commands.Settings;

namespace Trackify.Cli.Commands;

/// <summary>Scans for nearby hubs over Bluetooth, prints what turns up, and optionally saves them.</summary>
public sealed class DiscoverCommand(ITrainControlService control, ITrainService trains) : AsyncCommand<DiscoverSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, DiscoverSettings settings, CancellationToken cancellationToken)
    {
        if (!control.IsSupported)
        {
            AnsiConsole.MarkupLine("[red]Bluetooth is not available on this machine.[/]");
            return 1;
        }

        // Ctrl+C (outer token) or the --timeout, whichever comes first, ends the scan.
        using var scan = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        scan.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

        IReadOnlyList<DiscoveredHubDto> hubs;
        try
        {
            hubs = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("springgreen2"))
                .StartAsync("Scanning for hubs… (Ctrl+C to stop)", async _ => await control.DiscoverAsync(scan.Token));
        }
        catch (OperationCanceledException)
        {
            return 0; // Ctrl+C / timeout during the scan — nothing to report.
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]✗ Scan failed:[/] {ex.Message}");
            return 1;
        }

        if (hubs.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No hubs found.[/] Make sure a hub is powered on and in range.");
            return 0;
        }

        AnsiConsole.Write(Ui.HubsTable(hubs));

        if (settings.Save)
            await SaveHubsAsync(hubs, cancellationToken);

        return 0;
    }

    private async Task SaveHubsAsync(IReadOnlyList<DiscoveredHubDto> hubs, CancellationToken cancellationToken)
    {
        var saved = 0;
        foreach (var hub in hubs)
        {
            try
            {
                var train = await trains.SaveDiscoveredAsync(hub, cancellationToken);
                saved++;
                AnsiConsole.MarkupLineInterpolated($"[springgreen2]✓ Saved[/] {train.Name} [grey]({train.HubId})[/]");
            }
            catch (Exception ex)
            {
                // Report the specific hub that failed (never swallow silently) and keep going.
                AnsiConsole.MarkupLineInterpolated($"[red]✗ Could not save {hub.Name ?? hub.Id}:[/] {ex.Message}");
            }
        }

        AnsiConsole.MarkupLineInterpolated($"[grey]Saved {saved}/{hubs.Count} hub(s) to the train list.[/]");
    }
}
