using Trackify.Cli.Commands.Settings;

namespace Trackify.Cli.Commands;

/// <summary>Connects a train, stops its motor, and disconnects.</summary>
public sealed class StopCommand(TrainControlService control, TrainResolver resolver) : AsyncCommand<TrainSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TrainSettings settings)
    {
        var train = await resolver.FindAsync(settings.Train);
        if (train is null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]No train '{settings.Train}' found.[/] Run [springgreen2]trackify list[/].");
            return 1;
        }

        try
        {
            await control.ConnectAsync(train);
            await control.SetSpeedAsync(train, 0);
            await control.DisconnectAsync(train);
            AnsiConsole.MarkupLineInterpolated($"[springgreen2]■ {train.Name} stopped.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]✗ Stop failed:[/] {ex.Message}");
            return 1;
        }
    }
}
