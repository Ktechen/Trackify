using Trackify.Cli.Commands.Settings;

namespace Trackify.Cli.Commands;

/// <summary>Connects a train's hub and disconnects again — a quick reachability check.</summary>
public sealed class ConnectCommand(TrainControlService control, TrainService resolver) : AsyncCommand<TrainSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, TrainSettings settings, CancellationToken cancellationToken)
    {
        var train = await resolver.FindAsync(settings.Train, cancellationToken);
        if (train is null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]No train '{settings.Train}' found.[/] Run [springgreen2]trackify list[/].");
            return 1;
        }

        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("springgreen2"))
                .StartAsync($"Connecting to {train.Name}…", async _ => await control.ConnectAsync(train, cancellationToken));
            AnsiConsole.MarkupLineInterpolated($"[springgreen2]✓ Connected[/] to {train.Name}.");
            await control.DisconnectAsync(train, cancellationToken);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]✗ Connect failed:[/] {ex.Message}");
            return 1;
        }
    }
}
