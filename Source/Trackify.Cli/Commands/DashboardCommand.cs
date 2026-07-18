
namespace Trackify.Cli.Commands;

/// <summary>Default view (no command given): the banner, saved trains, and a command cheat-sheet.</summary>
public sealed class DashboardCommand(ITrainRepository repository) : AsyncCommand
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        Ui.Banner();

        var trains = await repository.GetAllAsync(cancellationToken);
        if (trains.Count == 0)
        {
            AnsiConsole.Write(new Panel("[yellow]No trains saved yet.[/]\nConfigure trains in the app, or copy its [grey]trackify.db[/] here.")
                .Header("[springgreen2]Trains[/]")
                .BorderColor(Color.Grey37));
        }
        else
        {
            AnsiConsole.Write(Ui.TrainsTable(trains));
        }

        AnsiConsole.Write(new Panel(new Markup(string.Join('\n',
                "[springgreen2]discover[/]            scan for nearby hubs",
                "[springgreen2]list[/]                list saved trains",
                "[springgreen2]drive[/] <train>       run a train until Ctrl+C",
                "[springgreen2]stop[/] <train>        stop a train's motor",
                "[springgreen2]color[/] <train> <c>   set the hub LED colour",
                "[springgreen2]connect[/] <train>     reachability test")))
            .Header("[springgreen2]Commands[/]")
            .BorderColor(Color.Grey37));

        AnsiConsole.MarkupLine("[grey]Run[/] [springgreen2]trackify --help[/] [grey]for full usage.[/]");
        return 0;
    }
}
