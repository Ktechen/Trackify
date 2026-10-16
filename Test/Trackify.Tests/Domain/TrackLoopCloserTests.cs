using Trackify.Domain.Tracks;

namespace Trackify.Tests.Domain;

/// <summary>
/// "Kreis schließen": the planner works out the parts still missing, and only ever proposes a run
/// that really closes — which is why this is a search and not a fit.
/// </summary>
public class TrackLoopCloserTests
{
    private const string CurveRight = TrackPartCatalog.CurveRightKey;

    [Fact]
    public void A_circle_one_curve_short_is_closed_with_one_curve()
    {
        var (topology, from, to) = OpenCircle(15);

        var result = TrackLoopCloser.Close(from, to);

        var part = Assert.Single(result.Parts);
        Assert.Equal(CurveRight, part.SpecKey);
        Assert.True(result.ClosesLoop);
    }

    [Fact]
    public void A_circle_two_curves_short_is_closed_with_two_curves()
    {
        var (_, from, to) = OpenCircle(14);

        var result = TrackLoopCloser.Close(from, to);

        Assert.Equal(2, result.Parts.Count);
        Assert.All(result.Parts, part => Assert.Equal(CurveRight, part.SpecKey));
    }

    [Fact]
    public void Closing_a_loop_leaves_a_buildable_plan_with_no_open_ends()
    {
        var (topology, from, to) = OpenCircle(15);

        var layout = TrackPlanWalker.Walk(TrackLoopCloser.Close(from, to).ApplyTo(topology));

        Assert.True(layout.IsBuildable);
        Assert.Empty(layout.OpenEnds);
        Assert.Equal(16, layout.Placements.Count);
    }

    [Fact]
    public void Two_ends_already_facing_each_other_only_need_the_connection()
    {
        // Sixteen curves already form a circle, so the two loose ends are nose to nose.
        var (topology, from, to) = OpenCircle(16);

        var result = TrackLoopCloser.Close(from, to);

        Assert.Empty(result.Parts);
        Assert.Single(result.Links);
        Assert.True(result.ClosesLoop);
        Assert.True(TrackPlanWalker.Walk(result.ApplyTo(topology)).IsBuildable);
    }

    [Fact]
    public void An_end_cannot_be_closed_onto_itself()
    {
        var (_, from, _) = OpenCircle(15);

        Assert.True(TrackLoopCloser.Close(from, from).IsEmpty);
    }

    [Fact]
    public void A_gap_too_big_for_the_budget_is_reported_as_no_answer_rather_than_a_bad_fit()
    {
        var (_, from, to) = OpenCircle(15);

        // One curve would do it, but zero parts are allowed and the ends are not already touching.
        Assert.True(TrackLoopCloser.Close(from, to, maxParts: 0).IsEmpty);
    }

    [Fact]
    public void The_two_ends_of_a_single_straight_cannot_be_looped_within_a_small_budget()
    {
        var id = Guid.NewGuid();
        var topology = TrackPlanTopology.Empty.Add(new TrackPlanPart(id, TrackPartCatalog.StraightKey));
        var ends = TrackPlanWalker.Walk(topology).OpenEnds;
        var start = ends.Single(end => end.PortName == TrackPortNames.Start);
        var finish = ends.Single(end => end.PortName == TrackPortNames.End);

        Assert.True(TrackLoopCloser.Close(start, finish, maxParts: 3).IsEmpty);
    }

    // A chain of `count` right curves, returning its two loose ends: the last part's end, and the
    // first part's start, which is what a closed loop has to join.
    private static (TrackPlanTopology Topology, TrackOpenEnd From, TrackOpenEnd To) OpenCircle(int count)
    {
        var topology = TrackPlanTopology.Empty;
        var ids = new List<Guid>(count);

        for (var i = 0; i < count; i++)
        {
            var id = Guid.NewGuid();
            topology = topology.Add(new TrackPlanPart(id, CurveRight));

            if (ids.Count > 0)
            {
                topology = topology.Connect(new TrackPlanLink(ids[^1], TrackPortNames.End, id, TrackPortNames.Start));
            }

            ids.Add(id);
        }

        var openEnds = TrackPlanWalker.Walk(topology).OpenEnds;

        return (
            topology,
            openEnds.Single(end => end.PartId == ids[^1] && end.PortName == TrackPortNames.End),
            openEnds.Single(end => end.PartId == ids[0] && end.PortName == TrackPortNames.Start));
    }
}
