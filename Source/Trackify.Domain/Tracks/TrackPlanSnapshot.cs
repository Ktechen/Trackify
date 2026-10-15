namespace Trackify.Domain.Tracks;

/// <summary>A whole saved plan: its parts and their connections, loaded or stored as one unit.</summary>
public sealed record TrackPlanSnapshot
{
    /// <summary>A plan with nothing in it.</summary>
    public static TrackPlanSnapshot Empty { get; } = new() { Parts = [], Links = [] };

    /// <summary>The parts, in <see cref="TrackPart.PlanOrder"/> order.</summary>
    public required IReadOnlyList<TrackPart> Parts { get; init; }

    /// <summary>Their connections.</summary>
    public required IReadOnlyList<TrackLink> Links { get; init; }

    /// <summary>The bare graph the geometry layer works on.</summary>
    public TrackPlanTopology ToTopology() => new()
    {
        Parts = [.. Parts.OrderBy(part => part.PlanOrder).Select(part => part.ToPlanPart())],
        Links = [.. Links.Select(link => link.ToPlanLink())],
    };

    /// <summary>Walks this plan into real coordinates.</summary>
    public TrackPlanLayout ToLayout() => TrackPlanWalker.Walk(ToTopology());
}
