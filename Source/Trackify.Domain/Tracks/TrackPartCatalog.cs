using System.Diagnostics.CodeAnalysis;

namespace Trackify.Domain.Tracks;

/// <summary>
/// Every track part the planner knows, with LEGO's real dimensions. Deliberately limited to parts
/// LEGO itself makes, because that is what someone actually owns: the 16-stud straight (53401), the
/// R40 / 22.5 degree curve (53400), the left and right point (53407 / 53404), the 90 degree crossing
/// (9V 32087) and a station, which is a straight with a different meaning.
///
/// Community radii (R56..R104 from ME Models, BrickTracks, 4DBrix) and part-length straights are
/// deliberately absent, not unsupported — each would be one more <see cref="TrackPartSpec"/> here.
///
/// Port poses are <em>derived</em> from the spans rather than typed in, so the two can never drift
/// apart: a curve's end is wherever its arc actually finishes.
/// </summary>
public static class TrackPartCatalog
{
    /// <summary>Straight, 16 studs (53401).</summary>
    public const string StraightKey = "straight";

    /// <summary>Straight used as a stop.</summary>
    public const string StationKey = "station";

    /// <summary>R40 curve bending left (53400, mirrored).</summary>
    public const string CurveLeftKey = "curve-r40-left";

    /// <summary>R40 curve bending right (53400).</summary>
    public const string CurveRightKey = "curve-r40-right";

    /// <summary>Point diverging left (53407).</summary>
    public const string SwitchLeftKey = "switch-left";

    /// <summary>Point diverging right (53404).</summary>
    public const string SwitchRightKey = "switch-right";

    /// <summary>90 degree crossing, 16x16 studs (9V 32087).</summary>
    public const string CrossingKey = "crossing-x90";

    // Initialised in textual order: Specs first, then the two views over it.
    private static readonly Dictionary<string, TrackPartSpec> Specs = BuildSpecs();

    private static readonly TrackPartSpec[] AllSpecs = [.. Specs.Values];

    private static readonly TrackPartSpec[] Drawable =
    [
        Specs[StraightKey],
        Specs[CurveLeftKey],
        Specs[CurveRightKey],
    ];

    /// <summary>Every known part.</summary>
    public static IReadOnlyList<TrackPartSpec> All => AllSpecs;

    /// <summary>
    /// The parts the drawing snapper may lay while following a drag: a straight and the two curves.
    /// Points, crossings and stations are placed deliberately, never guessed from a gesture.
    /// </summary>
    public static IReadOnlyList<TrackPartSpec> DrawablePieces => Drawable;

    /// <summary>The spec for <paramref name="key"/>. Throws when the key is unknown.</summary>
    public static TrackPartSpec Get(string key)
        => Specs.TryGetValue(key, out var spec)
            ? spec
            : throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown track part key.");

    /// <summary>The spec for <paramref name="key"/>, or false when it is not a known part.</summary>
    public static bool TryGet(string key, [MaybeNullWhen(false)] out TrackPartSpec spec)
        => Specs.TryGetValue(key, out spec);

    /// <summary>The catalogue key for a curve bending <paramref name="hand"/>.</summary>
    public static string CurveKey(CurveDirection hand) => hand == CurveDirection.Left ? CurveLeftKey : CurveRightKey;

    /// <summary>The catalogue key for a point diverging <paramref name="hand"/>.</summary>
    public static string SwitchKey(CurveDirection hand) => hand == CurveDirection.Left ? SwitchLeftKey : SwitchRightKey;

    private static Dictionary<string, TrackPartSpec> BuildSpecs()
    {
        var specs = new[]
        {
            RunOfRail(StraightKey, TrackPartKind.Straight, TrackMetrics.StraightStuds),
            RunOfRail(StationKey, TrackPartKind.Station, TrackMetrics.StraightStuds),
            Curve(CurveLeftKey, CurveDirection.Left),
            Curve(CurveRightKey, CurveDirection.Right),
            Point(SwitchLeftKey, CurveDirection.Left),
            Point(SwitchRightKey, CurveDirection.Right),
            Crossing(),
        };

        return specs.ToDictionary(spec => spec.Key, StringComparer.Ordinal);
    }

    // A plain piece of rail: enters at the origin, leaves `lengthStuds` later on the same heading.
    private static TrackPartSpec RunOfRail(string key, TrackPartKind kind, double lengthStuds)
    {
        var span = TrackSpan.Straight(StudPose.Origin, lengthStuds);

        return new TrackPartSpec
        {
            Key = key,
            Kind = kind,
            Ports =
            [
                new TrackPort(TrackPortNames.Start, StudPose.Origin.Reversed()),
                new TrackPort(TrackPortNames.End, span.End),
            ],
            Spans = [span],
            Routes = [new TrackRoute(TrackPortNames.Main, TrackPortNames.Start, TrackPortNames.End)],
        };
    }

    // R40, 22.5 degrees — one sixteenth of a circle, which is why 16 of these close a loop.
    private static TrackPartSpec Curve(string key, CurveDirection hand)
    {
        var span = TrackSpan.Arc(StudPose.Origin, TrackMetrics.CurveRadiusStuds, SweepOf(hand));

        return new TrackPartSpec
        {
            Key = key,
            Kind = TrackPartKind.Curve,
            Hand = hand,
            Ports =
            [
                new TrackPort(TrackPortNames.Start, StudPose.Origin.Reversed()),
                new TrackPort(TrackPortNames.End, span.End),
            ],
            Spans = [span],
            Routes = [new TrackRoute(TrackPortNames.Main, TrackPortNames.Start, TrackPortNames.End)],
        };
    }

    // A point is 32 studs long on the straight route (exactly two straights) and diverges by 22.5
    // degrees — the same sweep as an R40 curve. The diverging route is therefore modelled as half the
    // body straight followed by one R40 curve, which is the equivalence every LEGO track planner
    // uses. It is the one number in this file worth checking against a physical point.
    private static TrackPartSpec Point(string key, CurveDirection hand)
    {
        var mainSpan = TrackSpan.Straight(StudPose.Origin, TrackMetrics.SwitchStuds);
        var branchLead = StudPose.Origin.Advance(TrackMetrics.SwitchStuds / 2);
        var branchSpan = TrackSpan.Arc(branchLead, TrackMetrics.CurveRadiusStuds, SweepOf(hand));

        return new TrackPartSpec
        {
            Key = key,
            Kind = TrackPartKind.Switch,
            Hand = hand,
            Ports =
            [
                new TrackPort(TrackPortNames.Toe, StudPose.Origin.Reversed()),
                new TrackPort(TrackPortNames.Main, mainSpan.End),
                new TrackPort(TrackPortNames.Branch, branchSpan.End),
            ],
            // Both spans are drawn: the full 32-stud body plus the curve peeling off its midpoint.
            // The diverging route shares the body's first half, so there is nothing to draw twice.
            Spans = [mainSpan, branchSpan],
            Routes =
            [
                new TrackRoute(TrackPortNames.Main, TrackPortNames.Toe, TrackPortNames.Main),
                new TrackRoute(TrackPortNames.Branch, TrackPortNames.Toe, TrackPortNames.Branch),
            ],
        };
    }

    // 16x16 studs, two straight routes meeting at the centre and never joining.
    private static TrackPartSpec Crossing()
    {
        var half = TrackMetrics.CrossingStuds / 2;
        var acrossSpan = TrackSpan.Straight(StudPose.Origin, TrackMetrics.CrossingStuds);
        var downSpan = TrackSpan.Straight(
            new StudPose(half, -half, TrackMetrics.HeadingCount / 4),
            TrackMetrics.CrossingStuds);

        return new TrackPartSpec
        {
            Key = CrossingKey,
            Kind = TrackPartKind.Crossing,
            Ports =
            [
                new TrackPort(TrackPortNames.CrossA1, StudPose.Origin.Reversed()),
                new TrackPort(TrackPortNames.CrossA2, acrossSpan.End),
                new TrackPort(TrackPortNames.CrossB1, downSpan.Start.Reversed()),
                new TrackPort(TrackPortNames.CrossB2, downSpan.End),
            ],
            Spans = [acrossSpan, downSpan],
            Routes =
            [
                new TrackRoute(TrackPortNames.CrossA1, TrackPortNames.CrossA1, TrackPortNames.CrossA2),
                new TrackRoute(TrackPortNames.CrossB1, TrackPortNames.CrossB1, TrackPortNames.CrossB2),
            ],
        };
    }

    // Right bends clockwise on screen, which is a positive heading step (Y runs down).
    private static int SweepOf(CurveDirection hand) => hand == CurveDirection.Left ? -1 : 1;
}
