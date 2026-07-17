using System.ComponentModel;
using Spectre.Console.Cli;

namespace Trackify.Cli.Commands.Settings;

/// <summary>Arguments for the color command.</summary>
public sealed class ColorSettings : TrainSettings
{
    [CommandArgument(1, "<color>")]
    [Description("Hub LED colour (e.g. Green, Blue, Red).")]
    public string Color { get; init; } = "";
}
