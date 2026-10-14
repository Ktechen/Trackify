namespace Trackify.Domain.Enums;

/// <summary>
/// A physical LEGO track part the planner can lay. Distinct from <see cref="SegmentType"/>, which
/// describes a segment's *role* in the old strand chain — this names the actual moulded piece, so
/// its geometry (length, radius, sweep, number of ports) is fixed by LEGO and not by us.
/// </summary>
public enum TrackPartKind
{
    /// <summary>Straight, 16 studs (part 53401).</summary>
    Straight,

    /// <summary>R40 curve, 22.5 degrees, 16 to the full circle (part 53400).</summary>
    Curve,

    /// <summary>Point/turnout, 32 studs on the main route, 22.5 degree diverging route (53407 / 53404).</summary>
    Switch,

    /// <summary>90 degree crossing, 16x16 studs (9V part 32087; no current plastic equivalent).</summary>
    Crossing,

    /// <summary>A straight used as a stop: same geometry, different meaning.</summary>
    Station,
}
