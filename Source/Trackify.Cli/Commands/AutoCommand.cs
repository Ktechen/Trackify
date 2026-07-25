using Trackify.Cli.Commands.Settings;

namespace Trackify.Cli.Commands;

/// <summary>
/// Auto-pilot: a long-running loop for unattended operation (systemd / Docker, no typing). Every
/// <c>--interval</c> seconds it re-reads the saved trains from the store and applies each one's saved
/// configuration — connect, set the hub LED, drive at its saved speed — reconnecting any hub that has
/// dropped. Runs until Ctrl+C / SIGINT, then stops every motor and disconnects cleanly.
/// </summary>
public sealed class AutoCommand(ITrainControlService control, ITrainService query) : AsyncCommand<AutoSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, AutoSettings settings, CancellationToken cancellationToken)
    {
        if (!control.IsSupported)
        {
            AnsiConsole.MarkupLine("[red]Bluetooth is not available on this machine.[/]");
            return 1;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(1, settings.IntervalSeconds));
        // Trains connected during this run, kept so shutdown can stop + disconnect every one of them.
        var live = new Dictionary<Guid, TrainDto>();

        AnsiConsole.Write(new Rule("[springgreen2]▶ Auto-pilot[/]").LeftJustified());
        AnsiConsole.MarkupLineInterpolated(
            $"[grey]Re-applying saved trains every {interval.TotalSeconds:0}s. Press[/] [springgreen2]Ctrl+C[/] [grey]to stop.[/]");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await SweepAsync(settings, live, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // A daemon must survive a transient failure (e.g. a store read) and retry next cycle.
                    AnsiConsole.MarkupLineInterpolated($"[red]Sweep failed:[/] {ex.Message}");
                }

                await Task.Delay(interval, cancellationToken);
            }
        }
        catch (OperationCanceledException) { /* clean shutdown below */ }
        finally
        {
            await ShutdownAsync(live);
        }

        return 0;
    }

    private async Task SweepAsync(AutoSettings settings, Dictionary<Guid, TrainDto> live, CancellationToken ct)
    {
        var trains = await query.GetAllAsync(ct);
        var targets = (settings.All ? trains : trains.Where(t => t.IsActive)).ToList();

        if (targets.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No active trains to run.[/] Save/activate trains in the app (or use [springgreen2]--all[/]).");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey37)
            .Title($"[springgreen2]Auto-pilot[/] [grey]{DateTimeOffset.Now:HH:mm:ss}[/]");
        table.AddColumn("[grey]Name[/]");
        table.AddColumn("[grey]Speed[/]");
        table.AddColumn("[grey]Status[/]");

        foreach (var train in targets)
        {
            ct.ThrowIfCancellationRequested();
            var status = await ApplyAsync(train, live, ct);
            table.AddRow($"[white]{Markup.Escape(train.Name)}[/]", $"[grey]{train.Speed}%[/]", status);
        }

        AnsiConsole.Write(table);
    }

    /// <summary>Connects (if needed), re-asserts colour + speed, and reports the outcome for one train.</summary>
    private async Task<string> ApplyAsync(TrainDto train, Dictionary<Guid, TrainDto> live, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(train.HubId) && string.IsNullOrWhiteSpace(train.BleAddress))
            return "[grey]○ no address[/]";

        try
        {
            // ConnectAsync is idempotent (a no-op if already connected); SetSpeed doubles as a health
            // check — if the link dropped it throws, we disconnect, and the next sweep reconnects fresh.
            await control.ConnectAsync(train, ct);
            try { await control.SetLedAsync(train, ct); } catch { /* the hub may have no RGB LED */ }
            await control.SetSpeedAsync(train, train.Speed, ct);
            live[train.Id] = train;
            return "[springgreen2]● running[/]";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            live.Remove(train.Id);
            try { await control.DisconnectAsync(train, CancellationToken.None); } catch { /* best effort */ }
            return $"[red]✗ {Markup.Escape(Short(ex.Message))}[/]";
        }
    }

    /// <summary>Stops every motor and disconnects on shutdown — deliberately without the cancelled token.</summary>
    private async Task ShutdownAsync(Dictionary<Guid, TrainDto> live)
    {
        if (live.Count == 0)
            return;

        AnsiConsole.MarkupLine("[grey]■ Stopping all trains…[/]");
        foreach (var train in live.Values)
        {
            try { await control.SetSpeedAsync(train, 0); } catch { /* best effort */ }
            try { await control.DisconnectAsync(train); } catch { /* best effort */ }
        }

        AnsiConsole.MarkupLine("[grey]■ Auto-pilot stopped.[/]");
    }

    private static string Short(string message)
        => message.Length <= 50 ? message : message[..49] + "…";
}
