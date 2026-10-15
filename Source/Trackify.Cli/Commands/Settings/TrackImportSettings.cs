using System.ComponentModel;

namespace Trackify.Cli.Commands.Settings;

/// <summary>Options for <c>track import</c>.</summary>
public sealed class TrackImportSettings : CommandSettings
{
    [CommandArgument(0, "<FILE>")]
    [Description("The track plan file to read (JSON, as exported from the app).")]
    public string File { get; init; } = "";

    [CommandOption("--replace")]
    [Description("Replace the saved plan instead of adding these parts to it.")]
    public bool Replace { get; init; }

    [CommandOption("--dry-run")]
    [Description("Check the file and report what would happen, without changing the store.")]
    public bool DryRun { get; init; }
}
