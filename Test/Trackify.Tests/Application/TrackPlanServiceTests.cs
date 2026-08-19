using Trackify.Application.Trains;

namespace Trackify.Tests.Application;

public class TrackPlanServiceTests
{
    private static readonly TrackPlanService Plan = new();

    // The turtle-graphics layout walks each part from the previous one's end pose, so two lobes that
    // both open with the same parts from the same pose land on top of each other. The Acht's second
    // lobe therefore has to turn away first — a straight in slot 9 means it retraces the first lobe.
    [Fact]
    public void Acht_second_lobe_turns_away_instead_of_retracing_the_first()
    {
        var oval = Plan.ApplyTemplate(TemplateType.Oval);
        var acht = Plan.ApplyTemplate(TemplateType.Acht);

        Assert.Equal(2 * oval.Count, acht.Count);
        Assert.Equal(SegmentType.Curve, acht[oval.Count].Type);
        Assert.Equal(CurveDirection.Left, acht[oval.Count].Curve);
    }

    // Both lobes turn a full circle (four 90° pieces each) and are closed off by their straights, so
    // the walk returns to its start pose — that is what makes the two lobes meet in one point.
    [Fact]
    public void Acht_lobes_each_turn_a_full_circle_in_opposite_directions()
    {
        var acht = Plan.ApplyTemplate(TemplateType.Acht);

        Assert.Equal(4, acht.Count(s => s.Curve == CurveDirection.Right));
        Assert.Equal(4, acht.Count(s => s.Curve == CurveDirection.Left));
    }

    [Fact]
    public void Appended_part_lands_at_the_end_of_its_own_strand()
    {
        var branchId = Guid.CreateVersion7();
        var main = Plan.AppendPart([], Guid.Empty, SegmentType.Straight);
        var branched = Plan.AppendPart(main, branchId, SegmentType.Station);

        var branch = branched.Where(s => s.StrandId == branchId).ToList();
        Assert.Equal(SegmentType.Station, Assert.Single(branch).Type);
        Assert.Equal(0, branch[0].Order);
        Assert.Equal(0, branched.Single(s => s.StrandId == Guid.Empty).Order);
    }
}
