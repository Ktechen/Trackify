using Trackify.Application.TrackPlans;
using Trackify.Domain.Tracks;

namespace Trackify.Ui.Tests.Fakes;

/// <summary>
/// In-memory plan store. A copy of the one in Trackify.Tests rather than a shared file, because the
/// two test projects target different frameworks and cannot reference each other.
/// </summary>
public sealed class FakeTrackPlanRepository : ITrackPlanRepository
{
    /// <summary>Set to make <see cref="LoadAsync"/> fail, for the error-surfacing test.</summary>
    public Exception? LoadFailure { get; set; }

    public TrackPlanSnapshot Stored { get; private set; } = TrackPlanSnapshot.Empty;

    public int ReplaceCalls { get; private set; }

    public Task<TrackPlanSnapshot> LoadAsync(CancellationToken cancellationToken = default)
        => LoadFailure is not null
            ? Task.FromException<TrackPlanSnapshot>(LoadFailure)
            : Task.FromResult(Stored);

    public Task ReplaceAsync(TrackPlanSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ReplaceCalls++;
        Stored = snapshot;

        return Task.CompletedTask;
    }

    public Task AppendAsync(TrackPlanSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        Stored = new TrackPlanSnapshot
        {
            Parts = [.. Stored.Parts, .. snapshot.Parts],
            Links = [.. Stored.Links, .. snapshot.Links],
        };

        return Task.CompletedTask;
    }
}
