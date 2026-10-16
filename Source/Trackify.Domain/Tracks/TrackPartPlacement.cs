namespace Trackify.Domain.Tracks;

/// <summary>
/// Where a part actually ended up after the walk: its world frame plus its rail in world
/// coordinates, ready to be turned into path data by whoever is drawing.
/// </summary>
public sealed record TrackPartPlacement
{
    /// <summary>The plan part this places.</summary>
    public required Guid PartId { get; init; }

    /// <summary>Which catalogue part it is.</summary>
    public required string SpecKey { get; init; }

    /// <summary>The part's local frame expressed in world studs.</summary>
    public required StudPose Anchor { get; init; }

    /// <summary>The part's rail, in world studs.</summary>
    public required IReadOnlyList<TrackSpan> Spans { get; init; }

    /// <summary>The catalogue geometry behind this placement.</summary>
    public TrackPartSpec Spec => TrackPartCatalog.Get(SpecKey);

    /// <summary>The world pose of <paramref name="portName"/> on this part.</summary>
    public StudPose PortPose(string portName) => Anchor.Compose(Spec.PortPose(portName));
}
