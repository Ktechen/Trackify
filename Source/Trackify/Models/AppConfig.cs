namespace Trackify.Models;

public record AppConfig
{
    public string? Environment { get; init; }

    /// <summary>
    /// Backend base URL for Server mode, e.g. <c>http://192.168.1.50:5000</c>. Empty/absent = Direct
    /// mode (the device's own Bluetooth). The HMI's mode switch writes this.
    /// </summary>
    public string? ServerUrl { get; init; }
}
