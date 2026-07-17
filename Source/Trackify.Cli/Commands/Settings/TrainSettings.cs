using System.ComponentModel;
using Spectre.Console.Cli;

namespace Trackify.Cli.Commands.Settings;

/// <summary>Settings shared by every command that targets a single saved train.</summary>
public class TrainSettings : CommandSettings
{
    [CommandArgument(0, "<train>")]
    [Description("Train name or id, as shown by 'trackify list'.")]
    public string Train { get; init; } = "";
}
