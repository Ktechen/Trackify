namespace Trackify.Application.TrackPlans;

/// <summary>
/// The on-disk, versioned exchange format for a track plan — what the app writes and the CLI reads.
///
/// It stores the <em>graph</em> (parts plus port connections), not screen coordinates, for the same
/// reason the planner derives geometry rather than storing it: a coordinate could describe a layout
/// that cannot be plugged together, a connection cannot.
/// </summary>
public sealed record TrackPlanDocument
{
    /// <summary>Marker so a stray JSON file is recognised as not being a track plan.</summary>
    public const string FormatName = "trackify.trackplan";

    /// <summary>The only version written today.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Always <see cref="FormatName"/>.</summary>
    public string Format { get; init; } = FormatName;

    /// <summary>Format version; an importer refuses what it does not know.</summary>
    public int Version { get; init; } = CurrentVersion;

    /// <summary>Length unit for any <see cref="TrackPlanPoseDocument"/>. Always "stud" in version 1.</summary>
    public string Units { get; init; } = "stud";

    /// <summary>What the user called this plan.</summary>
    public string Name { get; init; } = "";

    /// <summary>The parts, each with a document-local id.</summary>
    public IReadOnlyList<TrackPlanPartDocument> Parts { get; init; } = [];

    /// <summary>How they plug together.</summary>
    public IReadOnlyList<TrackPlanConnectionDocument> Connections { get; init; } = [];
}
