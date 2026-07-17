using Trackify.Cli.Commands.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using Trackify.Application.Trains;

namespace Trackify.Cli.Commands;

/// <summary>Connects a train, stops its motor, and disconnects.</summary>
public sealed class StopCommand(TrainControlService control, ITrainStore store) : AsyncCommand<TrainSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TrainSettings settings)
    {
        var train = await CliHelpers.ResolveTrainAsync(store, settings.Train);
        if (train is null) return 1;

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
