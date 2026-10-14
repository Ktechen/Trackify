namespace Trackify.Domain.Tracks;

/// <summary>
/// A run of parts the planner proposes: what to lay and how it plugs together. The editor shows this
/// as the ghost preview under the user's finger and only commits it when they let go.
/// </summary>
public sealed record TrackSnapResult
{
    /// <summary>Nothing to lay.</summary>
    public static TrackSnapResult None { get; } = new()
    {
        Parts = [],
        Links = [],
        EndPose = StudPose.Origin,
    };

    /// <summary>The parts to add, in the order they are laid.</summary>
    public required IReadOnlyList<TrackPlanPart> Parts { get; init; }

    /// <summary>The connections to make — the first one plugs into the end the drag started from.</summary>
    public required IReadOnlyList<TrackPlanLink> Links { get; init; }

    /// <summary>Where the run leaves off; the plan's new open end unless this run closed a loop.</summary>
    public required StudPose EndPose { get; init; }

    /// <summary>Whether this run's far end was plugged into an already-existing open end.</summary>
    public bool ClosesLoop { get; init; }

    /// <summary>Whether there is anything to lay at all.</summary>
    public bool IsEmpty => Parts.Count == 0;

    /// <summary>Applies this run to <paramref name="topology"/>, returning the grown plan.</summary>
    public TrackPlanTopology ApplyTo(TrackPlanTopology topology)
    {
        var grown = topology;

        foreach (var part in Parts) grown = grown.Add(part);
        foreach (var link in Links) grown = grown.Connect(link);

        return grown;
    }
}
