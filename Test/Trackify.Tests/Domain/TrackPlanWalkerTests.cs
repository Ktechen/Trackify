using Trackify.Domain.Tracks;

namespace Trackify.Tests.Domain;

/// <summary>
/// Proves the walk derives buildable geometry: parts land end to end, real LEGO loops close, and
/// loops that only look closed are reported instead of being rounded into place.
/// </summary>
public class TrackPlanWalkerTests
{
    private const string Straight = TrackPartCatalog.StraightKey;
    private const string CurveRight = TrackPartCatalog.CurveRightKey;

    [Fact]
    public void An_empty_plan_walks_to_an_empty_layout()
    {
        var layout = TrackPlanWalker.Walk(TrackPlanTopology.Empty);

        Assert.Empty(layout.Placements);
        Assert.Empty(layout.OpenEnds);
        Assert.True(layout.IsBuildable);
    }

    [Fact]
    public void Parts_are_placed_end_to_end()
    {
        var (topology, ids) = Chain(Straight, Straight, Straight);

        var layout = TrackPlanWalker.Walk(topology);

        Assert.Equal(0, layout.Placements[ids[0]].Anchor.X, 6);
        Assert.Equal(16, layout.Placements[ids[1]].Anchor.X, 6);
        Assert.Equal(32, layout.Placements[ids[2]].Anchor.X, 6);
        Assert.True(layout.IsBuildable);
    }

    [Fact]
    public void A_chain_leaves_exactly_two_open_ends()
    {
        var (topology, ids) = Chain(Straight, CurveRight, Straight);

        var layout = TrackPlanWalker.Walk(topology);

        Assert.Equal(2, layout.OpenEnds.Count);
        Assert.Contains(layout.OpenEnds, end => end.PartId == ids[0] && end.PortName == TrackPortNames.Start);
        Assert.Contains(layout.OpenEnds, end => end.PartId == ids[2] && end.PortName == TrackPortNames.End);
    }

    [Fact]
    public void Sixteen_curves_bring_the_track_back_to_where_it_started()
    {
        var (topology, ids) = Chain(Enumerable.Repeat(CurveRight, 16).ToArray());

        var layout = TrackPlanWalker.Walk(topology);
        var firstStart = layout.Placements[ids[0]].PortPose(TrackPortNames.Start);
        var lastEnd = layout.Placements[ids[^1]].PortPose(TrackPortNames.End);

        // The whole point of a 22.5 degree curve: sixteen of them are a circle.
        Assert.True(
            lastEnd.Joins(firstStart),
            $"Sixteen R40 curves should close; the last end sits at {lastEnd} and the first start at {firstStart}.");
    }

    [Fact]
    public void A_closed_circle_of_sixteen_curves_is_buildable()
    {
        var layout = TrackPlanWalker.Walk(Closed(Enumerable.Repeat(CurveRight, 16).ToArray()));

        Assert.True(layout.IsBuildable);
        Assert.Empty(layout.OpenEnds);
    }

    [Fact]
    public void A_stadium_oval_of_curves_and_straights_is_buildable()
    {
        // Eight curves is a half turn, so two half turns joined by equal straights close exactly.
        string[] parts =
        [
            ..Enumerable.Repeat(CurveRight, 8), Straight,
            ..Enumerable.Repeat(CurveRight, 8), Straight,
        ];

        var layout = TrackPlanWalker.Walk(Closed(parts));

        Assert.True(layout.IsBuildable);
        Assert.Empty(layout.OpenEnds);
        Assert.Equal(18, layout.Placements.Count);
    }

    [Fact]
    public void A_loop_that_does_not_really_close_is_reported_instead_of_forced()
    {
        // Fifteen curves fall 22.5 degrees short of a circle. The plan claims the loop is shut; the
        // geometry says otherwise, and that disagreement is exactly what the user needs to see.
        var layout = TrackPlanWalker.Walk(Closed(Enumerable.Repeat(CurveRight, 15).ToArray()));

        Assert.False(layout.IsBuildable);

        // One physical joint, so one problem — not one report per side of it.
        var mismatch = Assert.Single(layout.Mismatches);

        Assert.Equal(1, Math.Abs(mismatch.HeadingStepsOff));

        // And the gap is one curve's chord: 2 * 40 * sin(11.25 degrees), i.e. "one curve short".
        Assert.Equal(
            2 * TrackMetrics.CurveRadiusStuds * Math.Sin(TrackMetrics.RadiansPerHeadingStep / 2),
            mismatch.GapStuds,
            6);
    }

    [Fact]
    public void A_point_offers_all_three_of_its_ports_as_open_ends()
    {
        var pointId = Guid.NewGuid();
        var topology = TrackPlanTopology.Empty.Add(new TrackPlanPart(pointId, TrackPartCatalog.SwitchLeftKey));

        var layout = TrackPlanWalker.Walk(topology);

        Assert.Equal(3, layout.OpenEnds.Count);
    }

    [Fact]
    public void A_branch_hanging_off_a_point_is_placed_from_the_branch_port()
    {
        var pointId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var topology = TrackPlanTopology.Empty
            .Add(new TrackPlanPart(pointId, TrackPartCatalog.SwitchRightKey))
            .Add(new TrackPlanPart(branchId, Straight))
            .Connect(new TrackPlanLink(pointId, TrackPortNames.Branch, branchId, TrackPortNames.Start));

        var layout = TrackPlanWalker.Walk(topology);
        var branchPort = layout.Placements[pointId].PortPose(TrackPortNames.Branch);
        var branchStart = layout.Placements[branchId].PortPose(TrackPortNames.Start);

        Assert.True(layout.IsBuildable);
        Assert.True(branchPort.Joins(branchStart));
        // The branch leaves on the diverging heading, not straight on.
        Assert.Equal(1, layout.Placements[branchId].Anchor.Heading);
    }

    [Fact]
    public void Unconnected_groups_are_parked_on_separate_lanes_so_they_stay_visible()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var topology = TrackPlanTopology.Empty
            .Add(new TrackPlanPart(first, Straight))
            .Add(new TrackPlanPart(second, Straight));

        var layout = TrackPlanWalker.Walk(topology);

        Assert.Equal(0, layout.Placements[first].Anchor.Y, 6);
        Assert.True(
            layout.Placements[second].Anchor.Y > 0,
            "A disconnected part must not be stacked on top of the first one.");
    }

    [Fact]
    public void Removing_a_part_also_removes_its_connections()
    {
        var (topology, ids) = Chain(Straight, Straight, Straight);

        var trimmed = topology.Remove(ids[1]);

        Assert.Equal(2, trimmed.Parts.Count);
        Assert.Empty(trimmed.Links);
    }

    // Builds a chain of parts, each plugged end-to-start into the previous one.
    private static (TrackPlanTopology Topology, List<Guid> Ids) Chain(params string[] specKeys)
    {
        var topology = TrackPlanTopology.Empty;
        var ids = new List<Guid>(specKeys.Length);

        foreach (var key in specKeys)
        {
            var id = Guid.NewGuid();
            topology = topology.Add(new TrackPlanPart(id, key));

            if (ids.Count > 0)
            {
                topology = topology.Connect(new TrackPlanLink(ids[^1], TrackPortNames.End, id, TrackPortNames.Start));
            }

            ids.Add(id);
        }

        return (topology, ids);
    }

    // The same chain with its two loose ends plugged into each other.
    private static TrackPlanTopology Closed(params string[] specKeys)
    {
        var (topology, ids) = Chain(specKeys);

        return topology.Connect(new TrackPlanLink(ids[^1], TrackPortNames.End, ids[0], TrackPortNames.Start));
    }
}
