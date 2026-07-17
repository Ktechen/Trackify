using Trackify.Cli.Commands.Settings;
using Trackify.Domain.Enums;

namespace Trackify.Cli.Commands;

/// <summary>Connects a train, applies colour/speed, and keeps it running until Ctrl+C (then stops + disconnects).</summary>
public sealed class DriveCommand(TrainControlService control, TrainResolver resolver) : AsyncCommand<DriveSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, DriveSettings settings, CancellationToken cancellationToken)
    {
        var train = await resolver.FindAsync(settings.Train, cancellationToken);
        if (train is null)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]No train '{settings.Train}' found.[/] Run [springgreen2]trackify list[/].");
            return 1;
        }

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
            await control.ConnectAsync(train, cancellationToken);
            if (settings.Color is not null) await control.SetLedAsync(train, cancellationToken);
            await control.SetSpeedAsync(train, settings.Speed, cancellationToken);

            AnsiConsole.Write(new Rule($"[springgreen2]▶ {Markup.Escape(train.Name)}[/] [grey]running at {settings.Speed}%[/]").LeftJustified());
            AnsiConsole.MarkupLine("[grey]Press[/] [springgreen2]Ctrl+C[/] [grey]to stop.[/]");

            // Run until Ctrl+C / SIGINT cancels the token (hooked once in Program.cs).
            try { await Task.Delay(Timeout.Infinite, cancellationToken); }
            catch (OperationCanceledException) { /* clean shutdown below */ }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]✗ Drive failed:[/] {ex.Message}");
            return 1;
        }
        finally
        {
            // Deliberately without the (already cancelled) token: the train must still stop cleanly.
            try { await control.SetSpeedAsync(train, 0); } catch { /* best effort */ }
            await control.DisconnectAsync(train);
            AnsiConsole.MarkupLine("[grey]■ Stopped and disconnected.[/]");
        }

        return 0;
    }
}
