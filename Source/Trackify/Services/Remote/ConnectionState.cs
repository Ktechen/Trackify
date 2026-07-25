using Windows.Storage;

namespace Trackify.Services.Remote;

/// <summary>
/// The app's live connection mode — Direct (this device's own Bluetooth) vs. Server (a Trackify
/// backend on a Pi) — plus the backend URL. Persisted in local app settings and read by
/// <see cref="SwitchingLegoService"/> on every call, so toggling takes effect immediately (no restart).
/// </summary>
public sealed partial class ConnectionState : ObservableObject
{
    private const string UseServerKey = "trackify.useServer";
    private const string ServerUrlKey = "trackify.serverUrl";

    private readonly ILogger<ConnectionState> _log;

    [ObservableProperty] private bool useServer;
    [ObservableProperty] private string serverUrl = "";

    public ConnectionState(ILogger<ConnectionState> logger, string? defaultServerUrl = null)
    {
        _log = logger;

        try
        {
            var values = ApplicationData.Current.LocalSettings.Values;
            useServer = values[UseServerKey] is bool stored && stored;
            serverUrl = values[ServerUrlKey] as string ?? defaultServerUrl ?? "";
        }
        catch (Exception ex)
        {
            // No persisted settings yet (or storage unavailable) — start from the configured default.
            serverUrl = defaultServerUrl ?? "";
            Log.ConnectionSettingsLoadFailed(_log, ex);
        }
    }

    /// <summary>Persists the current mode + URL so it survives a restart.</summary>
    public void Save()
    {
        try
        {
            var values = ApplicationData.Current.LocalSettings.Values;
            values[UseServerKey] = UseServer;
            values[ServerUrlKey] = ServerUrl;
        }
        catch (Exception ex)
        {
            Log.ConnectionSettingsSaveFailed(_log, ex);
        }
    }
}
