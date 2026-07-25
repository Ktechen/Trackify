namespace Trackify.Application.Remote;

/// <summary>
/// Route templates for the Trackify REST API, shared by the server (CLI) and the Refit client (app)
/// so the two never drift. These are the <b>one-shot</b> actions (Refit); real-time speed/LED go over
/// the SignalR hub — see <see cref="TrainHubMethods"/>. Control routes are keyed by <c>hubId</c> so
/// they map 1:1 onto <c>ILegoService</c> (the app's existing control seam).
/// </summary>
public static class ApiRoutes
{
    /// <summary>All saved trains (for the app to sync into its local store).</summary>
    public const string Trains = "/api/trains";
    public const string Discover = "/api/discover";
    public const string Connect = "/api/hubs/{hubId}/connect";
    public const string Disconnect = "/api/hubs/{hubId}/disconnect";

    /// <summary>Last speed applied to each hub, so a freshly-connected client can show current state.</summary>
    public const string State = "/api/state";

    /// <summary>The SignalR hub endpoint for real-time train control.</summary>
    public const string TrainHub = "/hubs/trains";
}
