using System.Collections.Concurrent;
using Trackify.Application.Remote;

namespace Trackify.Cli.Server;

/// <summary>
/// Tracks the last speed applied to each train so a freshly-connected client (or <c>GET /api/state</c>)
/// can show the current state without waiting for the next change. In-memory, per server run.
/// </summary>
public sealed class TrainStateStore
{
    private readonly ConcurrentDictionary<string, int> _speeds = new();

    public void SetSpeed(string trainId, int speed) => _speeds[trainId] = speed;

    public IReadOnlyList<TrainSpeedState> Snapshot()
        => _speeds.Select(kv => new TrainSpeedState(kv.Key, kv.Value)).ToList();
}
