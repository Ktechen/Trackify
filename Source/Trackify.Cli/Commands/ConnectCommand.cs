using Trackify.Cli.Commands.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using Trackify.Application.Trains;

namespace Trackify.Cli.Commands;

/// <summary>Connects a train's hub and disconnects again — a quick reachability check.</summary>
public sealed class ConnectCommand(TrainControlService control, ITrainStore store) : AsyncCommand<TrainSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TrainSettings settings)
    {
        var train = await CliHelpers.ResolveTrainAsync(store, settings.Train);
        if (train is null) return 1;

        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("springgreen2"))
                .StartAsync($"Connecting to {train.Name}…", async _ => await control.ConnectAsync(train));
            AnsiConsole.MarkupLineInterpolated($"[springgreen2]✓ Connected[/] to {train.Name}.");
            await control.DisconnectAsync(train);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]✗ Connect failed:[/] {ex.Message}");
            return 1;
        }
    }
}
