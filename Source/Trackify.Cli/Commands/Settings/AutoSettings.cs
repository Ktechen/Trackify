using System.ComponentModel;

namespace Trackify.Cli.Commands.Settings;

/// <summary>Options for the auto (auto-pilot) command.</summary>
public sealed class AutoSettings : CommandSettings
{
    [CommandOption("-i|--interval <SECONDS>")]
    [Description("Seconds between auto-sweeps (default 60).")]
    public int IntervalSeconds { get; init; } = 60;

    [CommandOption("-a|--all")]
    [Description("Include inactive trains too (default: active only).")]
    public bool All { get; init; }
}
