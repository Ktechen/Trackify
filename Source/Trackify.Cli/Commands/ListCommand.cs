using Spectre.Console;
using Spectre.Console.Cli;
using Trackify.Application.Trains;

namespace Trackify.Cli.Commands;

/// <summary>Lists the trains saved in the shared store.</summary>
public sealed class ListCommand(ITrainStore store) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var trains = await store.LoadAsync();
        if (trains.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No trains saved yet.[/] Configure trains in the app (or copy its trains.json here).");
            return 0;
        }

        AnsiConsole.Write(Ui.TrainsTable(trains));
        return 0;
    }
}
