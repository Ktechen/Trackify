namespace Trackify.Application.TrackPlans;

/// <summary>One part in an exported plan.</summary>
public sealed record TrackPlanPartDocument
{
    /// <summary>Document-local id ("p1"), referenced by the connections. Readable on purpose.</summary>
    public string Id { get; init; } = "";

    /// <summary>Catalogue key, e.g. "curve-r40-right".</summary>
    public string Part { get; init; } = "";

    /// <summary>The planner's label for it.</summary>
    public string Name { get; init; } = "";

    /// <summary>Drive behaviour; omitted when it is all defaults.</summary>
    public TrackPlanBehaviourDocument? Behaviour { get; init; }

    /// <summary>Point configuration; omitted for every part that is not a point.</summary>
    public TrackPlanSwitchDocument? Switch { get; init; }

    /// <summary>
    /// Optional measured world pose. Nothing needs it — the geometry comes from the connections —
    /// but it is where a reconstruction from a photograph can hand over what it measured, letting
    /// the importer derive the connections instead of demanding them.
    /// </summary>
    public TrackPlanPoseDocument? Pose { get; init; }
}
