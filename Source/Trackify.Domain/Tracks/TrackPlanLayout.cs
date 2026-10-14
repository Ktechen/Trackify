namespace Trackify.Domain.Tracks;

/// <summary>
/// The result of walking a <see cref="TrackPlanTopology"/>: where every part sits, where the plan
/// can still grow, and any connection the geometry could not honour.
/// </summary>
public sealed record TrackPlanLayout
{
    /// <summary>The layout of an empty plan.</summary>
    public static TrackPlanLayout Empty { get; } = new()
    {
        Placements = new Dictionary<Guid, TrackPartPlacement>(),
        OpenEnds = [],
        Mismatches = [],
    };

    /// <summary>Every part's world placement, by part id.</summary>
    public required IReadOnlyDictionary<Guid, TrackPartPlacement> Placements { get; init; }

    /// <summary>Ports with nothing plugged into them.</summary>
    public required IReadOnlyList<TrackOpenEnd> OpenEnds { get; init; }

    /// <summary>Connections the geometry contradicts — in practice, loops that do not really close.</summary>
    public required IReadOnlyList<TrackJoinMismatch> Mismatches { get; init; }

    /// <summary>Whether this plan could be plugged together out of real parts.</summary>
    public bool IsBuildable => Mismatches.Count == 0;
}
