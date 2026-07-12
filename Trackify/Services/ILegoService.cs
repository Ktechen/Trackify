using Trackify.Models.Trains.Enums;

namespace Trackify.Services;

/// <summary>
/// A LEGO hub found during a Bluetooth scan. <see cref="Id"/> is the platform's stable device
/// identifier (Android: MAC string; iOS: a CoreBluetooth UUID) and is the key used to connect and
/// send commands. <see cref="MacAddress"/> is only populated on platforms that expose it (Android).
/// </summary>
public partial record DiscoveredHub(string Id, string? Name, string? MacAddress, HubType? HubType);

/// <summary>
/// Controls LEGO Powered Up hubs. The transport is an implementation detail: mobile heads talk
/// Bluetooth LE directly in-process (<c>DirectLegoService</c>), platforms without a usable BLE
/// stack get <c>UnsupportedLegoService</c>. The view model depends only on this abstraction.
/// </summary>
public interface ILegoService
{
    /// <summary>Whether hub control is available on the current platform at all.</summary>
    bool IsSupported { get; }

    /// <summary>
    /// Scans for nearby hubs until the first one is found or <paramref name="ct"/> is cancelled,
    /// then returns what was seen (empty if cancelled before anything appeared). There is no fixed
    /// timeout - cancel the token to stop scanning.
    /// </summary>
    Task<IReadOnlyList<DiscoveredHub>> DiscoverAsync(CancellationToken ct = default);

    /// <summary>Connects to the hub with the given id, treating it as <paramref name="hubType"/>.</summary>
    Task ConnectAsync(string hubId, HubType hubType, CancellationToken ct = default);

    /// <summary>Disconnects the hub if connected; a no-op otherwise.</summary>
    Task DisconnectAsync(string hubId, CancellationToken ct = default);

    /// <summary>
    /// Drives the motor on <paramref name="port"/> of a connected hub. Power is a percentage:
    /// 1..100 forward, -1..-100 reverse, 0 = stop (float), 127 = stop (brake).
    /// </summary>
    Task SetSpeedAsync(string hubId, byte port, sbyte power, CancellationToken ct = default);

    /// <summary>Sets the built-in hub RGB LED of a connected hub.</summary>
    Task SetLedAsync(string hubId, byte red, byte green, byte blue, CancellationToken ct = default);
}
