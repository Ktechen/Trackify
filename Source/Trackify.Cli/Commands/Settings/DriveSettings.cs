using System.ComponentModel;
using Spectre.Console.Cli;

namespace Trackify.Cli.Commands.Settings;

/// <summary>Options for the drive command.</summary>
public sealed class DriveSettings : TrainSettings
{
    [CommandOption("-s|--speed <PERCENT>")]
    [Description("Motor speed -100..100 (default 60).")]
    public int Speed { get; init; } = 60;

    [CommandOption("-c|--color <COLOR>")]
    [Description("Optional hub LED colour (e.g. Green, Blue, Red).")]
    public string? Color { get; init; }
}
