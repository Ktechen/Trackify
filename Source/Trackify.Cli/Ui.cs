using Trackify.Application.Lego;

namespace Trackify.Cli;

/// <summary>Shared Spectre.Console visuals so every command has a consistent, polished look.</summary>
internal static class Ui
{
    public static readonly Color Accent = Color.SpringGreen2;

    /// <summary>The figlet banner + subtitle shown on the dashboard.</summary>
    public static void Banner()
    {
        AnsiConsole.Write(new FigletText("Trackify").LeftJustified().Color(Accent));
        AnsiConsole.Write(new Rule("[springgreen2]LEGO Powered Up · train control[/]")
            .LeftJustified()
            .RuleStyle("grey37"));
        AnsiConsole.WriteLine();
    }

    /// <summary>A styled table of saved trains.</summary>
    public static Table TrainsTable(IReadOnlyList<TrainDto> trains)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey37)
            .Title("[springgreen2]Trains[/]");
        table.AddColumn("[grey]Id[/]");
        table.AddColumn("[grey]Name[/]");
        table.AddColumn("[grey]Hub[/]");
        table.AddColumn("[grey]Address[/]");
        table.AddColumn("[grey]Active[/]");

        foreach (var train in trains)
        {
            table.AddRow(
                $"[grey]{Escape(train.Id.ToString())}[/]",
                $"[white]{Escape(train.Name)}[/]",
                Escape(train.Hub.ToString()),
                $"[grey]{Escape(string.IsNullOrWhiteSpace(train.HubId) ? train.BleAddress : train.HubId)}[/]",
                train.IsActive ? "[springgreen2]●[/] yes" : "[grey]○ no[/]");
        }

        return table;
    }

    /// <summary>A styled table of hubs found during a scan.</summary>
    public static Table HubsTable(IReadOnlyList<DiscoveredHub> hubs)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey37)
            .Title("[springgreen2]Discovered hubs[/]");
        table.AddColumn("[grey]Name[/]");
        table.AddColumn("[grey]Address[/]");
        table.AddColumn("[grey]Hub type[/]");

        foreach (var hub in hubs)
        {
            table.AddRow(
                $"[white]{Escape(hub.Name ?? "(unnamed)")}[/]",
                $"[grey]{Escape(hub.MacAddress ?? hub.Id)}[/]",
                Escape(hub.HubType?.ToString() ?? "unknown"));
        }

        return table;
    }

    private static string Escape(string value) => Markup.Escape(value);
}
