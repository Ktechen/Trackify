namespace Trackify.Domain.Tracks;

/// <summary>
/// Two parts plugged together at a named port each. This is the plan's only structural truth —
/// everything geometric follows from it.
/// </summary>
public readonly record struct TrackPlanLink(Guid FromPartId, string FromPort, Guid ToPartId, string ToPort)
{
    /// <summary>The same connection stated from the other end.</summary>
    public TrackPlanLink Flipped() => new(ToPartId, ToPort, FromPartId, FromPort);

    /// <summary>Whether this link uses <paramref name="portName"/> on <paramref name="partId"/>.</summary>
    public bool Uses(Guid partId, string portName)
        => (FromPartId == partId && string.Equals(FromPort, portName, StringComparison.Ordinal))
            || (ToPartId == partId && string.Equals(ToPort, portName, StringComparison.Ordinal));
}
