using Trackify.Cli.Commands.Settings;

namespace Trackify.Cli.Commands;

/// <summary>Connects a train, stops its motor, and disconnects.</summary>
public sealed class StopCommand(ITrainControlService control, ITrainService resolver) : AsyncCommand<TrainSettings>
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
            await control.ConnectAsync(train, cancellationToken);
            await control.SetSpeedAsync(train, 0, cancellationToken);
            await control.DisconnectAsync(train, cancellationToken);
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
