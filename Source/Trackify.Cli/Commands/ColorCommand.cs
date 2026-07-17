using Trackify.Cli.Commands.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using Trackify.Application.Trains;
using Trackify.Domain.Enums;

namespace Trackify.Cli.Commands;

/// <summary>Sets a train's hub LED colour (persists on the hub after disconnect).</summary>
public sealed class ColorCommand(TrainControlService control, ITrainStore store) : AsyncCommand<ColorSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ColorSettings settings)
    {
        var train = await CliHelpers.ResolveTrainAsync(store, settings.Train);
        if (train is null) return 1;

        if (!Enum.TryParse<LedColorType>(settings.Color, ignoreCase: true, out var color))
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Unknown colour '{settings.Color}'.[/]");
            return 1;
        }

        train.Color = color;
        try
        {
            await control.ConnectAsync(train);
            await control.SetLedAsync(train);
            await control.DisconnectAsync(train);
            AnsiConsole.MarkupLineInterpolated($"[springgreen2]✓ {train.Name} LED set to {color}.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]✗ Colour failed:[/] {ex.Message}");
            return 1;
        }
    }
}
