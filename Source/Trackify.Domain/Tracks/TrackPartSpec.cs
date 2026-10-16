namespace Trackify.Domain.Tracks;

/// <summary>
/// The fixed geometry of one kind of LEGO track part, authored in the part's own local frame:
/// where it can be plugged in (<see cref="Ports"/>), what it looks like (<see cref="Spans"/>), and
/// how a train can run through it (<see cref="Routes"/>).
///
/// This is data, not code — adding a radius or a half-straight later is a catalogue entry in
/// <see cref="TrackPartCatalog"/>, not a change to the walker, the snapper or the renderer.
/// </summary>
public sealed record TrackPartSpec
{
    /// <summary>Stable identifier, also what the exchange format writes (e.g. "curve-r40-right").</summary>
    public required string Key { get; init; }

    /// <summary>Which moulded piece this is.</summary>
    public required TrackPartKind Kind { get; init; }

    /// <summary>Which way a curve bends or a point diverges; null for symmetric parts.</summary>
    public CurveDirection? Hand { get; init; }

    /// <summary>Every pluggable end, with its outward-facing local pose.</summary>
    public required IReadOnlyList<TrackPort> Ports { get; init; }

    /// <summary>The rail to draw, in local coordinates.</summary>
    public required IReadOnlyList<TrackSpan> Spans { get; init; }

    /// <summary>The port pairs a train can travel between.</summary>
    public required IReadOnlyList<TrackRoute> Routes { get; init; }

    /// <summary>How much rail this part contributes, in studs — the straight-route length for a point.</summary>
    public double TotalLengthStuds => Spans.Count == 0 ? 0 : Spans[0].LengthStuds;

    /// <summary>Whether <paramref name="portName"/> is one of this part's ports.</summary>
    public bool HasPort(string portName) => TryPortPose(portName, out _);

    /// <summary>The local pose of <paramref name="portName"/>, or false if this part has no such port.</summary>
    public bool TryPortPose(string portName, out StudPose pose)
    {
        foreach (var port in Ports)
        {
            if (!string.Equals(port.Name, portName, StringComparison.Ordinal)) continue;

            pose = port.LocalPose;
            return true;
        }

        pose = default;
        return false;
    }

    /// <summary>The local pose of <paramref name="portName"/>. Throws when the port is unknown.</summary>
    public StudPose PortPose(string portName)
        => TryPortPose(portName, out var pose)
            ? pose
            : throw new ArgumentOutOfRangeException(nameof(portName), portName, $"Part '{Key}' has no port '{portName}'.");
}
