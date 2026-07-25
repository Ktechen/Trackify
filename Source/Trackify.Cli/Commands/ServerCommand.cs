using Trackify.Cli.Commands.Settings;
using Trackify.Cli.Server;

namespace Trackify.Cli.Commands;

/// <summary>
/// Runs the network backend (REST + SignalR) so the Uno app can drive this Pi remotely. The backend
/// composes the same Domain/Application/Infrastructure layers over its own ASP.NET host.
/// </summary>
public sealed class ServerCommand : AsyncCommand<ServerSettings>
{
    protected override Task<int> ExecuteAsync(CommandContext context, ServerSettings settings, CancellationToken cancellationToken)
    {
        var storePath = Environment.GetEnvironmentVariable("TRACKIFY_STORE");
        var hostArgs = settings.Urls is { Length: > 0 } urls ? new[] { "--urls", urls } : Array.Empty<string>();
        return TrackifyServer.RunAsync(hostArgs, storePath, cancellationToken);
    }
}
