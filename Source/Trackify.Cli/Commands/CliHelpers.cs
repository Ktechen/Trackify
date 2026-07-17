using Spectre.Console;
using Trackify.Application.Trains;
using Trackify.Domain.Trains;

namespace Trackify.Cli.Commands;

/// <summary>Helpers shared across the train commands.</summary>
internal static class CliHelpers
{
    /// <summary>Finds a saved train by id or (case-insensitive) name, printing a hint if none matches.</summary>
    public static async Task<TrainConfig?> ResolveTrainAsync(ITrainStore store, string nameOrId)
    {
        var trains = await store.LoadAsync();
        var match = trains.FirstOrDefault(t =>
            string.Equals(t.Id, nameOrId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.Name, nameOrId, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            AnsiConsole.MarkupLineInterpolated($"[red]No train '{nameOrId}' found.[/] Run [springgreen2]trackify list[/] to see saved trains.");

        return match;
    }

    /// <summary>A token that cancels on Ctrl+C so long-running commands can shut down cleanly.</summary>
    public static CancellationTokenSource CancelOnCtrlC()
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };
        return cts;
    }
}
