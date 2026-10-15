namespace Trackify.Domain.Tracks;

/// <summary>
/// A saved connection between two parts' ports — the persisted form of
/// <see cref="TrackPlanLink"/>, which is the same fact without the entity bookkeeping.
/// </summary>
public sealed record TrackLink : BaseEntity
{
    /// <summary>The part one side of the joint belongs to.</summary>
    public Guid FromPartId { get; set; }

    /// <summary>Which of its ports (see <see cref="TrackPortNames"/>).</summary>
    public string FromPort { get; set; } = TrackPortNames.End;

    /// <summary>The part on the other side.</summary>
    public Guid ToPartId { get; set; }

    /// <summary>Which of its ports.</summary>
    public string ToPort { get; set; } = TrackPortNames.Start;

    /// <summary>This connection as the geometry layer wants it.</summary>
    public TrackPlanLink ToPlanLink() => new(FromPartId, FromPort, ToPartId, ToPort);
}
