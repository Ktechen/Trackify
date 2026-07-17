using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Trackify.Application.Catalog;
using Trackify.Application.Lego;
using Trackify.Application.Logging;
using Trackify.Domain.Trains;

namespace Trackify.Application.Trains;

/// <summary>
/// The shared use-case layer for driving a train's hub: discover, connect/disconnect, live speed and
/// LED. Operates on a pure <see cref="TrainConfig"/> and talks only to <see cref="ILegoService"/>, so
/// both the Uno app and the CLI reuse it without duplicating control logic. UI-neutral: failures
/// surface as exceptions/no-ops, never as localized status text.
/// </summary>
public sealed class TrainControlService(ILegoService lego, ILogger<TrainControlService>? logger = null)
{
    /// <summary>Port A — the motor driven by the speed slider / speed command.</summary>
    public const byte MotorPort = 0;

    private const int SpeedDebounceMs = 200;

    // Per-hub debounce so a rapidly-changing slider only sends its final value; keyed by hub key.
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _speedDebounce = new();
    private readonly ILogger _log = logger ?? NullLogger<TrainControlService>.Instance;

    /// <summary>Whether hub control is available on the current platform at all.</summary>
    public bool IsSupported => lego.IsSupported;

    /// <summary>Scans for nearby hubs until one is found or <paramref name="ct"/> is cancelled.</summary>
    public Task<IReadOnlyList<DiscoveredHub>> DiscoverAsync(CancellationToken ct = default)
    {
        Log.Discovering(_log);
        return lego.DiscoverAsync(ct);
    }

    /// <summary>Connects the train's hub. Throws if the train has no hub id/address yet.</summary>
    public Task ConnectAsync(TrainConfig train, CancellationToken ct = default)
    {
        var key = HubKey(train);
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Train has no hub id or BLE address; discover a hub first.");
        Log.Connecting(_log, train.Id, key);
        return lego.ConnectAsync(key, train.Hub, ct);
    }

    /// <summary>Disconnects the train's hub; a no-op if it has no key.</summary>
    public Task DisconnectAsync(TrainConfig train, CancellationToken ct = default)
    {
        var key = HubKey(train);
        if (string.IsNullOrWhiteSpace(key)) return Task.CompletedTask;
        Log.Disconnecting(_log, train.Id);
        return lego.DisconnectAsync(key, ct);
    }

    /// <summary>Sends the motor speed immediately (percentage: 1..100 fwd, -1..-100 rev, 0 stop).</summary>
    public Task SetSpeedAsync(TrainConfig train, int speed, CancellationToken ct = default)
    {
        var key = HubKey(train);
        if (string.IsNullOrWhiteSpace(key)) return Task.CompletedTask;
        Log.SettingSpeed(_log, train.Id, speed);
        return lego.SetSpeedAsync(key, MotorPort, (sbyte)Math.Clamp(speed, -100, 100), ct);
    }

    /// <summary>
    /// Sends the motor speed after a short pause, cancelling any pending send for the same hub. Use
    /// for a dragged slider so only the final value reaches the hardware. Best-effort (drops on error).
    /// </summary>
    public void SetSpeedDebounced(TrainConfig train, int speed)
    {
        var key = HubKey(train);
        if (string.IsNullOrWhiteSpace(key)) return;

        var cts = new CancellationTokenSource();
        if (_speedDebounce.TryRemove(key, out var previous))
        {
            previous.Cancel();
            previous.Dispose();
        }
        _speedDebounce[key] = cts;
        _ = SendSpeedAfterDelayAsync(train, speed, cts.Token);
    }

    private async Task SendSpeedAfterDelayAsync(TrainConfig train, int speed, CancellationToken ct)
    {
        try { await Task.Delay(SpeedDebounceMs, ct); }
        catch (TaskCanceledException) { return; }

        try { await SetSpeedAsync(train, speed, ct); }
        catch { /* best effort — the next change will resend */ }
    }

    /// <summary>Sets the hub's built-in RGB LED to the train's configured colour.</summary>
    public Task SetLedAsync(TrainConfig train, CancellationToken ct = default)
    {
        var key = HubKey(train);
        if (string.IsNullOrWhiteSpace(key)) return Task.CompletedTask;

        Log.SettingLed(_log, train.Id, train.Color.ToString());
        var (r, g, b) = ParseHexColor(LegoinoCatalog.Color(train.Color).Hex);
        return lego.SetLedAsync(key, r, g, b, ct);
    }

    /// <summary>Whether a discovered hub is the same physical device as this train (by id or MAC).</summary>
    public static bool IsSameDevice(TrainConfig train, DiscoveredHub hub)
        => string.Equals(train.HubId, hub.Id, StringComparison.OrdinalIgnoreCase)
        || (hub.MacAddress is not null && string.Equals(train.BleAddress, hub.MacAddress, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The key a hub is addressed by: the platform device id captured during discovery, or the typed
    /// BLE address as a fallback (Android accepts a MAC string; iOS needs discovery first).
    /// </summary>
    public static string HubKey(TrainConfig train)
        => !string.IsNullOrWhiteSpace(train.HubId) ? train.HubId : train.BleAddress;

    private static (byte R, byte G, byte B) ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        return (
            byte.Parse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }
}
