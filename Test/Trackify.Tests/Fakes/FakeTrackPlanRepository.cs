using Trackify.Application.TrackPlans;
using Trackify.Domain.Tracks;

namespace Trackify.Tests.Fakes;

/// <summary>In-memory stand-in for the plan store, so the exchange service can be tested without EF.</summary>
public sealed class FakeTrackPlanRepository : ITrackPlanRepository
{
    public TrackPlanSnapshot Stored { get; private set; } = TrackPlanSnapshot.Empty;

    public int ReplaceCalls { get; private set; }

    public int AppendCalls { get; private set; }

    public Task<TrackPlanSnapshot> LoadAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Stored);

    public Task ReplaceAsync(TrackPlanSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ReplaceCalls++;
        Stored = snapshot;

        return Task.CompletedTask;
    }

    public Task AppendAsync(TrackPlanSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        AppendCalls++;

        // Mirrors what the SQLite repository does: appended parts continue the existing order
        // instead of fighting the originals for the same positions.
        var offset = Stored.Parts.Count == 0 ? 0 : Stored.Parts.Max(part => part.PlanOrder) + 1;
        foreach (var part in snapshot.Parts) part.PlanOrder += offset;

        Stored = new TrackPlanSnapshot
        {
            Parts = [.. Stored.Parts, .. snapshot.Parts],
            Links = [.. Stored.Links, .. snapshot.Links],
        };

        return Task.CompletedTask;
    }
}
