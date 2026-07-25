using Trackify.Application.Trains;

namespace Trackify.Services.Remote;

/// <summary>
/// Syncs the backend's trains into the local SQLite store while in Server mode. Each remote train is
/// upserted, de-duplicated by <b>hub identity</b> (HubId → BleAddress → Name) so repeated syncs never
/// pile up duplicates; a matched local row is updated in place (its id is kept) rather than re-added.
/// </summary>
public sealed class RemoteTrainSync(ITrackifyApi api, ITrainRepository repository)
{
    /// <summary>Pulls all trains from the backend and upserts them locally; returns how many were written.</summary>
    public async Task<int> SyncAsync(CancellationToken ct = default)
    {
        var remote = await api.GetTrainsAsync(ct);
        var local = await repository.GetAllAsync(ct);
        var written = 0;

        foreach (var dto in remote)
        {
            var existing = local.FirstOrDefault(train => IsSameHub(train, dto));
            var entity = dto.ToEntity();

            if (existing is null)
            {
                await repository.AddAsync(entity, ct);
            }
            else
            {
                entity.Id = existing.Id; // keep the local row so any references stay stable
                await repository.UpdateAsync(entity, ct);
            }

            written++;
        }

        return written;
    }

    private static bool IsSameHub(Train train, TrainDto dto)
        => (!string.IsNullOrWhiteSpace(dto.HubId) && string.Equals(train.HubId, dto.HubId, StringComparison.OrdinalIgnoreCase))
        || (!string.IsNullOrWhiteSpace(dto.BleAddress) && string.Equals(train.BleAddress, dto.BleAddress, StringComparison.OrdinalIgnoreCase))
        || string.Equals(train.Name, dto.Name, StringComparison.OrdinalIgnoreCase);
}
