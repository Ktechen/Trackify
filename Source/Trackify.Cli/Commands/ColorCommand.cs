using Trackify.Cli.Commands.Settings;
using Trackify.Domain.Enums;

namespace Trackify.Cli.Commands;

/// <summary>Sets a train's hub LED colour (persists on the hub after disconnect).</summary>
public sealed class ColorCommand(TrainControlService control, TrainResolver resolver) : AsyncCommand<ColorSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ColorSettings settings, CancellationToken cancellationToken)
    {
        var train = await resolver.FindAsync(settings.Train, cancellationToken);
        if (train is null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]No train '{settings.Train}' found.[/] Run [springgreen2]trackify list[/].");
            return 1;
        }

        if (!Enum.TryParse<LedColorType>(settings.Color, ignoreCase: true, out var color))
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Unknown colour '{settings.Color}'.[/]");
            return 1;
        }

        train.Color = color;
        try
        {
            await control.ConnectAsync(train, cancellationToken);
            await control.SetLedAsync(train, cancellationToken);
            await control.DisconnectAsync(train, cancellationToken);
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
