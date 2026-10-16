using Trackify.Domain.Tracks;

namespace Trackify.Tests.Domain;

/// <summary>
/// The drawing path: a stroke in, real LEGO parts out. The property that matters most is the last
/// test — whatever a gesture produces has to be buildable, or "just draw it" is a lie.
/// </summary>
public class TrackSnapperTests
{
    [Fact]
    public void A_stroke_straight_ahead_becomes_straights()
    {
        var (topology, start) = Seed();

        var result = TrackSnapper.Snap(start, StraightStroke(48));

        Assert.Equal(3, result.Parts.Count);
        Assert.All(result.Parts, part => Assert.Equal(TrackPartCatalog.StraightKey, part.SpecKey));
        Assert.Equal(0, result.EndPose.Heading);
        Assert.Equal(64, result.EndPose.X, 3);
        Assert.True(result.ApplyTo(topology) is { } grown && TrackPlanWalker.Walk(grown).IsBuildable);
    }

    [Fact]
    public void A_stroke_shorter_than_half_a_part_lays_nothing()
    {
        var (_, start) = Seed();

        var result = TrackSnapper.Snap(start, StraightStroke(4));

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void An_empty_stroke_lays_nothing()
    {
        var (_, start) = Seed();

        Assert.True(TrackSnapper.Snap(start, []).IsEmpty);
    }

    [Fact]
    public void A_stroke_curving_right_becomes_right_curves()
    {
        var (topology, start) = Seed();

        // A quarter circle of LEGO's own radius, which is exactly four curves.
        var result = TrackSnapper.Snap(start, QuarterCircleStroke(turnRight: true));

        Assert.Equal(4, result.Parts.Count);
        Assert.All(result.Parts, part => Assert.Equal(TrackPartCatalog.CurveRightKey, part.SpecKey));
        Assert.Equal(TrackMetrics.HeadingCount / 4, result.EndPose.Heading);
        Assert.True(TrackPlanWalker.Walk(result.ApplyTo(topology)).IsBuildable);
    }

    [Fact]
    public void A_stroke_curving_left_becomes_left_curves()
    {
        var (_, start) = Seed();

        var result = TrackSnapper.Snap(start, QuarterCircleStroke(turnRight: false));

        Assert.All(result.Parts, part => Assert.Equal(TrackPartCatalog.CurveLeftKey, part.SpecKey));
    }

    [Fact]
    public void A_gesture_never_lays_more_than_its_budget()
    {
        var (_, start) = Seed();

        var result = TrackSnapper.Snap(start, StraightStroke(4000), maxParts: 5);

        Assert.Equal(5, result.Parts.Count);
    }

    [Fact]
    public void A_gesture_never_lays_a_point_or_a_crossing()
    {
        var (_, start) = Seed();

        var result = TrackSnapper.Snap(start, QuarterCircleStroke(turnRight: true));

        Assert.All(result.Parts, part =>
        {
            Assert.NotEqual(TrackPartKind.Switch, part.Spec.Kind);
            Assert.NotEqual(TrackPartKind.Crossing, part.Spec.Kind);
        });
    }

    [Fact]
    public void Whatever_a_stroke_produces_can_actually_be_built()
    {
        // The core promise of drawing instead of configuring: a free gesture, but only real parts
        // plugged port to port, so the walk can never find a joint that does not work.
        IReadOnlyList<StudPoint>[] strokes =
        [
            StraightStroke(100),
            QuarterCircleStroke(turnRight: true),
            QuarterCircleStroke(turnRight: false),
            WigglyStroke(),
        ];

        foreach (var stroke in strokes)
        {
            var (topology, start) = Seed();

            var layout = TrackPlanWalker.Walk(TrackSnapper.Snap(start, stroke).ApplyTo(topology));

            Assert.True(layout.IsBuildable, "A snapped stroke produced track that cannot be plugged together.");
        }
    }

    // A single straight, with its far end as the open end a gesture starts from (pose 16,0 heading 0).
    private static (TrackPlanTopology Topology, TrackOpenEnd Start) Seed()
    {
        var id = Guid.NewGuid();
        var topology = TrackPlanTopology.Empty.Add(new TrackPlanPart(id, TrackPartCatalog.StraightKey));
        var open = TrackPlanWalker.Walk(topology).OpenEnds.Single(end => end.PortName == TrackPortNames.End);

        return (topology, open);
    }

    private static List<StudPoint> StraightStroke(double lengthStuds)
    {
        var points = new List<StudPoint>();
        for (var travelled = 0.0; travelled <= lengthStuds; travelled += 2)
        {
            points.Add(new StudPoint(TrackMetrics.StraightStuds + travelled, 0));
        }

        return points;
    }

    // A quarter of an R40 circle leaving the seed's open end, sampled finely.
    private static List<StudPoint> QuarterCircleStroke(bool turnRight)
    {
        var sign = turnRight ? 1 : -1;
        var centreY = sign * TrackMetrics.CurveRadiusStuds;
        var points = new List<StudPoint>();

        for (var step = 0; step <= 32; step++)
        {
            var angle = sign * (Math.PI / 2) * step / 32;

            points.Add(new StudPoint(
                TrackMetrics.StraightStuds + (TrackMetrics.CurveRadiusStuds * Math.Sin(Math.Abs(angle))),
                centreY - (centreY * Math.Cos(angle))));
        }

        return points;
    }

    // Right then left then right, the sort of thing a child's finger actually draws.
    private static List<StudPoint> WigglyStroke()
    {
        var points = new List<StudPoint>();

        for (var step = 0; step <= 120; step++)
        {
            var along = step * 2.0;

            points.Add(new StudPoint(TrackMetrics.StraightStuds + along, 26 * Math.Sin(along / 38)));
        }

        return points;
    }
}
