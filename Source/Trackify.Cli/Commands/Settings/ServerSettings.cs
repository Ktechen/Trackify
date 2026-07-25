using System.ComponentModel;

namespace Trackify.Cli.Commands.Settings;

/// <summary>Options for the server command.</summary>
public sealed class ServerSettings : CommandSettings
{
    [CommandOption("--urls <URLS>")]
    [Description("Bind address(es), e.g. http://0.0.0.0:5000. Overrides the appsettings 'Urls' value.")]
    public string? Urls { get; init; }
}
