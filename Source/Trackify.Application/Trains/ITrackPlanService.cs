namespace Trackify.Application.Trains;

/// <summary>Result of inserting a Weiche: the updated segment list plus the new branch strand's id.</summary>
public sealed record SwitchInsertResult(IReadOnlyList<TrackSegmentDto> Segments, Guid BranchStrandId);

/// <summary>
/// Shared use-case for building a track plan: every "smart" rule from the Streckenplaner design
/// (appending inherits speed/direction/f(x) from the predecessor, a Weiche spawns its own branch
/// strand, templates lay out a whole loop in one step) lives here instead of the ViewModel — the same
/// separation <see cref="ITrainControlService"/> gives train control. Operates on plain
/// <see cref="TrackSegmentDto"/> lists; callers own persistence via <see cref="ITrackSegmentRepository"/>.
/// </summary>
public interface ITrackPlanService
{
    /// <summary>
    /// Appends a new part to <paramref name="strandId"/>'s open (last) end. The new segment inherits
    /// max speed, direction, and accel/brake functions from the strand's current last segment (or,
    /// for a fresh branch strand, from the Weiche it branches off). <paramref name="curve"/> is
    /// required when <paramref name="type"/> is <see cref="SegmentType.Curve"/>.
    /// </summary>
    IReadOnlyList<TrackSegmentDto> AppendPart(
        IReadOnlyList<TrackSegmentDto> segments, Guid strandId, SegmentType type, CurveDirection? curve = null);

    /// <summary>
    /// Inserts a Weiche at <paramref name="strandId"/>'s open end and creates the branch strand it
    /// peels off into (<paramref name="branchDirection"/>). The branch starts empty; the caller
    /// decides whether to keep building on the trunk or switch the "active strand" to the branch.
    /// </summary>
    SwitchInsertResult InsertSwitch(IReadOnlyList<TrackSegmentDto> segments, Guid strandId, CurveDirection branchDirection);

    /// <summary>Replaces every strand with a canned layout (Oval/Acht/Punkt-zu-Punkt), all on the main strand.</summary>
    IReadOnlyList<TrackSegmentDto> ApplyTemplate(TemplateType template);

    /// <summary>Duplicates one segment, inserting the copy immediately after it in the same strand.</summary>
    IReadOnlyList<TrackSegmentDto> Duplicate(IReadOnlyList<TrackSegmentDto> segments, Guid segmentId);

    /// <summary>Swaps a segment with its neighbour in travel order (<paramref name="up"/> = toward the start).</summary>
    IReadOnlyList<TrackSegmentDto> Reorder(IReadOnlyList<TrackSegmentDto> segments, Guid segmentId, bool up);

    /// <summary>Removes a segment. Deleting a Weiche also removes the branch strand it anchors.</summary>
    IReadOnlyList<TrackSegmentDto> Delete(IReadOnlyList<TrackSegmentDto> segments, Guid segmentId);

    /// <summary>Empties the plan entirely ("Leeren").</summary>
    IReadOnlyList<TrackSegmentDto> Clear();
}
