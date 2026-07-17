using Trackify.Cli.Commands.Settings;

namespace Trackify.Cli.Commands;

/// <summary>Scans for nearby hubs over Bluetooth and prints what turns up.</summary>
public sealed class DiscoverCommand(TrainControlService control) : AsyncCommand<DiscoverSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, DiscoverSettings settings, CancellationToken cancellationToken)
    {
        if (!control.IsSupported)
        {
            AnsiConsole.MarkupLine("[red]Bluetooth is not available on this machine.[/]");
            return 1;
        }

        // Ctrl+C (outer token) or the --timeout, whichever comes first, ends the scan.
        using var scan = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        scan.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

        var hubs = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("springgreen2"))
            .StartAsync("Scanning for hubs… (Ctrl+C to stop)", async _ => await control.DiscoverAsync(scan.Token));

        if (hubs.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No hubs found.[/] Make sure a hub is powered on and in range.");
            return 0;
        }

        AnsiConsole.Write(Ui.HubsTable(hubs));
        return 0;
    }
}
