using Trackify.Domain.Tracks;

namespace Trackify.Tests.Domain;

/// <summary>
/// Pins the catalogue to LEGO's real dimensions. If one of these fails, the planner has started
/// inventing track that cannot be bought.
/// </summary>
public class TrackPartCatalogTests
{
    private const double Sin22Point5 = 0.38268343236508977;
    private const double Cos22Point5 = 0.92387953251128674;

    [Fact]
    public void A_straight_is_sixteen_studs_long()
    {
        var straight = TrackPartCatalog.Get(TrackPartCatalog.StraightKey);

        var end = straight.PortPose(TrackPortNames.End);

        Assert.Equal(16, end.X, 6);
        Assert.Equal(0, end.Y, 6);
        Assert.Equal(0, end.Heading);
        Assert.Equal(128, straight.TotalLengthStuds * TrackMetrics.MillimetresPerStud, 6);
    }

    [Fact]
    public void A_curve_sweeps_one_sixteenth_of_a_circle()
    {
        var curve = TrackPartCatalog.Get(TrackPartCatalog.CurveRightKey);

        var end = curve.PortPose(TrackPortNames.End);

        // R40 at 22.5 degrees: the irrational end position that rules out a coordinate grid.
        Assert.Equal(TrackMetrics.CurveRadiusStuds * Sin22Point5, end.X, 6);
        Assert.Equal(TrackMetrics.CurveRadiusStuds * (1 - Cos22Point5), end.Y, 6);
        Assert.Equal(1, end.Heading);
    }

    [Fact]
    public void The_left_curve_mirrors_the_right_one()
    {
        var left = TrackPartCatalog.Get(TrackPartCatalog.CurveLeftKey).PortPose(TrackPortNames.End);
        var right = TrackPartCatalog.Get(TrackPartCatalog.CurveRightKey).PortPose(TrackPortNames.End);

        Assert.Equal(right.X, left.X, 6);
        Assert.Equal(-right.Y, left.Y, 6);
        Assert.Equal(TrackMetrics.NormalizeHeading(-right.Heading), left.Heading);
    }

    [Fact]
    public void Sixteen_curve_sweeps_make_exactly_one_full_circle()
        => Assert.Equal(360, 16 * TrackMetrics.DegreesPerHeadingStep, 9);

    [Fact]
    public void Four_curve_sweeps_make_a_right_angle()
        => Assert.Equal(90, 4 * TrackMetrics.DegreesPerHeadingStep, 9);

    [Fact]
    public void A_point_is_two_straights_long_on_its_main_route()
    {
        var point = TrackPartCatalog.Get(TrackPartCatalog.SwitchRightKey);

        var main = point.PortPose(TrackPortNames.Main);

        Assert.Equal(2 * TrackMetrics.StraightStuds, main.X, 6);
        Assert.Equal(0, main.Y, 6);
        Assert.Equal(0, main.Heading);
    }

    [Fact]
    public void A_point_diverges_by_the_same_angle_as_a_curve()
    {
        var right = TrackPartCatalog.Get(TrackPartCatalog.SwitchRightKey);
        var left = TrackPartCatalog.Get(TrackPartCatalog.SwitchLeftKey);

        Assert.Equal(1, right.PortPose(TrackPortNames.Branch).Heading);
        Assert.Equal(TrackMetrics.HeadingCount - 1, left.PortPose(TrackPortNames.Branch).Heading);
    }

    [Fact]
    public void A_point_has_three_ports_and_two_routes()
    {
        var point = TrackPartCatalog.Get(TrackPartCatalog.SwitchLeftKey);

        Assert.Equal(3, point.Ports.Count);
        Assert.Equal(2, point.Routes.Count);
        Assert.True(point.HasPort(TrackPortNames.Toe));
        Assert.True(point.HasPort(TrackPortNames.Main));
        Assert.True(point.HasPort(TrackPortNames.Branch));
    }

    [Fact]
    public void A_crossing_is_a_sixteen_stud_square_with_two_routes_that_never_meet()
    {
        var crossing = TrackPartCatalog.Get(TrackPartCatalog.CrossingKey);

        var acrossEnd = crossing.PortPose(TrackPortNames.CrossA2);
        var downStart = crossing.PortPose(TrackPortNames.CrossB1);
        var downEnd = crossing.PortPose(TrackPortNames.CrossB2);

        Assert.Equal(4, crossing.Ports.Count);
        Assert.Equal(2, crossing.Routes.Count);

        Assert.Equal(TrackMetrics.CrossingStuds, acrossEnd.X, 6);
        Assert.Equal(0, acrossEnd.Y, 6);

        // The second route runs at a right angle through the middle of the first.
        Assert.Equal(TrackMetrics.CrossingStuds / 2, downStart.X, 6);
        Assert.Equal(-TrackMetrics.CrossingStuds / 2, downStart.Y, 6);
        Assert.Equal(TrackMetrics.CrossingStuds / 2, downEnd.X, 6);
        Assert.Equal(TrackMetrics.CrossingStuds / 2, downEnd.Y, 6);
        Assert.Equal(TrackMetrics.HeadingCount / 4, downEnd.Heading);
    }

    [Fact]
    public void Every_port_faces_outward_so_two_parts_can_be_plugged_together()
    {
        foreach (var spec in TrackPartCatalog.All)
        {
            foreach (var port in spec.Ports)
            {
                // A port's own reverse must join it: that is what "facing each other" means.
                Assert.True(
                    port.LocalPose.Joins(port.LocalPose.Reversed()),
                    $"Port '{port.Name}' of '{spec.Key}' does not face outward.");
            }
        }
    }

    [Fact]
    public void The_catalogue_holds_only_parts_LEGO_actually_makes()
    {
        var keys = TrackPartCatalog.All.Select(spec => spec.Key).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                TrackPartCatalog.StraightKey,
                TrackPartCatalog.StationKey,
                TrackPartCatalog.CurveLeftKey,
                TrackPartCatalog.CurveRightKey,
                TrackPartCatalog.SwitchLeftKey,
                TrackPartCatalog.SwitchRightKey,
                TrackPartCatalog.CrossingKey,
            },
            keys);
    }

    [Fact]
    public void Only_straights_and_curves_may_be_laid_by_a_drag()
    {
        var kinds = TrackPartCatalog.DrawablePieces.Select(spec => spec.Kind).ToList();

        Assert.Equal(3, kinds.Count);
        Assert.DoesNotContain(TrackPartKind.Switch, kinds);
        Assert.DoesNotContain(TrackPartKind.Crossing, kinds);
    }

    [Fact]
    public void An_unknown_part_key_is_rejected_rather_than_guessed()
    {
        Assert.False(TrackPartCatalog.TryGet("curve-r104-right", out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => TrackPartCatalog.Get("curve-r104-right"));
    }
}
