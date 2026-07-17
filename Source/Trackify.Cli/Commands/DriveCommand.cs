using Trackify.Cli.Commands.Settings;
using Spectre.Console;
using Spectre.Console.Cli;
using Trackify.Application.Trains;
using Trackify.Domain.Enums;

namespace Trackify.Cli.Commands;

/// <summary>Connects a train, applies colour/speed, and keeps it running until Ctrl+C (then stops + disconnects).</summary>
public sealed class DriveCommand(TrainControlService control, ITrainStore store) : AsyncCommand<DriveSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, DriveSettings settings)
    {
        var train = await CliHelpers.ResolveTrainAsync(store, settings.Train);
        if (train is null) return 1;

        if (settings.Color is { } colorName)
        {
            if (!Enum.TryParse<LedColorType>(colorName, ignoreCase: true, out var color))
            {
                AnsiConsole.MarkupLineInterpolated($"[red]Unknown colour '{colorName}'.[/]");
                return 1;
            }
            train.Color = color;
        }

        try
        {
            await control.ConnectAsync(train);
            if (settings.Color is not null) await control.SetLedAsync(train);
            await control.SetSpeedAsync(train, settings.Speed);

            AnsiConsole.Write(new Rule($"[springgreen2]▶ {Markup.Escape(train.Name)}[/] [grey]running at {settings.Speed}%[/]").LeftJustified());
            AnsiConsole.MarkupLine("[grey]Press[/] [springgreen2]Ctrl+C[/] [grey]to stop.[/]");

            using var cts = CliHelpers.CancelOnCtrlC();
            try { await Task.Delay(Timeout.Infinite, cts.Token); }
            catch (OperationCanceledException) { /* Ctrl+C */ }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]✗ Drive failed:[/] {ex.Message}");
            return 1;
        }
        finally
        {
            try { await control.SetSpeedAsync(train, 0); } catch { /* best effort */ }
            await control.DisconnectAsync(train);
            AnsiConsole.MarkupLine("[grey]■ Stopped and disconnected.[/]");
        }

        return 0;
    }
}
