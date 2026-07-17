using System.ComponentModel;
using Spectre.Console.Cli;

namespace Trackify.Cli.Commands.Settings;

/// <summary>Options for the discover command.</summary>
public sealed class DiscoverSettings : CommandSettings
{
    [CommandOption("-t|--timeout <SECONDS>")]
    [Description("Give up scanning after this many seconds (default 30).")]
    public int TimeoutSeconds { get; init; } = 30;
}
