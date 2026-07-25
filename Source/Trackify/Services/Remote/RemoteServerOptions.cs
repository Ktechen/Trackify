namespace Trackify.Services.Remote;

/// <summary>
/// Where the app finds a Trackify backend (the CLI's <c>trackify serve</c> on a Pi). When a base URL
/// is set the app runs in <b>Server mode</b> (remote transport); empty means <b>Direct mode</b> (the
/// device's own Bluetooth). Persisted in app settings and shown behind the HMI's mode switch.
/// </summary>
public sealed class RemoteServerOptions
{
    /// <summary>Base URL of the backend, e.g. <c>http://192.168.1.50:5000</c>. Empty = Direct (local BLE).</summary>
    public string BaseUrl { get; set; } = "";

    public bool Enabled => !string.IsNullOrWhiteSpace(BaseUrl);
}
