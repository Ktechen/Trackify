namespace Trackify.Application.Trains;

/// <summary>
/// Shared use-case for driving a train's hub over the <see cref="ILegoService"/> seam: discover,
/// connect/disconnect, live speed and LED — all over a <see cref="TrainDto"/>. Front-ends depend on
/// this abstraction, not the concrete service.
/// </summary>
public interface ITrainControlService
{
    /// <summary>Whether hub control is available on the current platform at all.</summary>
    bool IsSupported { get; }

    /// <summary>Scans for nearby hubs until one is found or <paramref name="ct"/> is cancelled.</summary>
    Task<IReadOnlyList<DiscoveredHubDto>> DiscoverAsync(CancellationToken ct = default);

    /// <summary>Connects the train's hub. Throws if the train has no hub id/address yet.</summary>
    Task ConnectAsync(TrainDto train, CancellationToken ct = default);

    /// <summary>Disconnects the train's hub; a no-op if it has no key.</summary>
    Task DisconnectAsync(TrainDto train, CancellationToken ct = default);

    /// <summary>Sends the motor speed immediately (percentage: 1..100 fwd, -1..-100 rev, 0 stop).</summary>
    Task SetSpeedAsync(TrainDto train, int speed, CancellationToken ct = default);

    /// <summary>Sends the motor speed after a short debounce (for a dragged slider). Best-effort.</summary>
    void SetSpeedDebounced(TrainDto train, int speed);

    /// <summary>Sets the hub's built-in RGB LED to the train's configured colour.</summary>
    Task SetLedAsync(TrainDto train, CancellationToken ct = default);
}
