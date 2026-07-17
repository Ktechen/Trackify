using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Trackify.Application.Trains;
using Trackify.Domain.Trains;
using Trackify.Infrastructure.Logging;

namespace Trackify.Infrastructure.Persistence;

/// <summary>
/// Stores the configured trains as a human-readable JSON file. The Uno app and the CLI use the same
/// schema, so a <c>trains.json</c> authored in one can be copied to (or edited on) the other.
/// Writes atomically (temp file + move) so an interrupted save can't corrupt the store.
/// </summary>
public sealed class JsonTrainStore : ITrainStore
{
    private readonly string _path;
    private readonly ILogger _log;

    /// <summary>Uses the default per-user location (<see cref="DefaultPath"/>).</summary>
    public JsonTrainStore(ILogger<JsonTrainStore>? logger = null) : this(DefaultPath(), logger) { }

    /// <summary>Uses an explicit file path (used by tests and by the CLI's TRACKIFY_STORE override).</summary>
    public JsonTrainStore(string filePath, ILogger<JsonTrainStore>? logger = null)
    {
        _path = filePath;
        _log = logger ?? NullLogger<JsonTrainStore>.Instance;
    }

    /// <summary>Per-user store location: <c>&lt;ApplicationData&gt;/Trackify/trains.json</c> (on Linux: <c>~/.config</c>).</summary>
    public static string DefaultPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Trackify", "trains.json");

    public async Task<IReadOnlyList<TrainConfig>> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path)) return [];

        await using var stream = File.OpenRead(_path);
        var trains = await JsonSerializer.DeserializeAsync(stream, TrainStoreJsonContext.Default.ListTrainConfig, ct);
        Log.StoreLoaded(_log, trains?.Count ?? 0, _path);
        return trains ?? [];
    }

    public async Task SaveAsync(IReadOnlyList<TrainConfig> trains, CancellationToken ct = default)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var list = trains as List<TrainConfig> ?? [.. trains];
        var tempPath = _path + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, list, TrainStoreJsonContext.Default.ListTrainConfig, ct);
        }
        File.Move(tempPath, _path, overwrite: true);
        Log.StoreSaved(_log, list.Count, _path);
    }
}
