using System.ComponentModel;

namespace Trackify.Cli.Commands.Settings;

/// <summary>Options for <c>track export</c>.</summary>
public sealed class TrackExportSettings : CommandSettings
{
    [CommandArgument(0, "[FILE]")]
    [Description("Where to write the plan. Omit it to print to standard output.")]
    public string? File { get; init; }

    [CommandOption("-n|--name <NAME>")]
    [Description("Name to record in the exported plan.")]
    public string Name { get; init; } = "";
}
