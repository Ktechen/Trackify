using Trackify.Domain.Tracks;
using Trackify.Helpers;
using Xunit;

namespace Trackify.Ui.Tests.Rendering;

/// <summary>
/// The fit turns world studs into canvas path data. The property that matters most is the last one:
/// the view must be computed from the committed parts alone, or the whole plan would shift and
/// rescale under the user's finger as the ghost preview grows past its edge.
/// </summary>
public class TrackPlanFitterTests
{
    private const double DefaultPixelsPerStud = 5.0;

    private List<Guid> _ids = [];

    [Fact]
    public void An_empty_layout_fits_to_nothing()
    {
        var fitted = TrackPlanFitter.Fit(TrackPlanLayout.Empty);

        Assert.Empty(fitted.Pieces);
        Assert.Empty(fitted.Anchors);
        Assert.Equal("", fitted.BedPathData);
    }

    [Fact]
    public void Every_part_gets_drawable_and_tappable_path_data()
    {
        var fitted = Fit(Chain(TrackPartCatalog.StraightKey, TrackPartCatalog.CurveRightKey));

        Assert.Equal(2, fitted.Pieces.Count);
        Assert.All(fitted.Pieces, piece =>
        {
            Assert.StartsWith("M ", piece.PathData, StringComparison.Ordinal);
            Assert.StartsWith("M ", piece.HitPathData, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void A_curve_is_drawn_as_an_arc_and_a_straight_as_a_line()
    {
        var fitted = Fit(Chain(TrackPartCatalog.StraightKey, TrackPartCatalog.CurveRightKey));
        var straight = fitted.Pieces.First(piece => piece.PartId == _ids[0]);
        var curve = fitted.Pieces.First(piece => piece.PartId == _ids[1]);

        Assert.Contains(" L ", straight.PathData, StringComparison.Ordinal);
        Assert.Contains(" A ", curve.PathData, StringComparison.Ordinal);
    }

    [Fact]
    public void A_point_is_drawn_as_two_runs_of_rail()
    {
        // Its 32-stud body plus the curve peeling off the midpoint — two subpaths, so two "M".
        var fitted = Fit(Chain(TrackPartCatalog.SwitchRightKey));
        var piece = Assert.Single(fitted.Pieces);

        Assert.Equal(2, piece.PathData.Split("M ", StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void Open_ends_become_anchors_inside_the_canvas()
    {
        var fitted = Fit(Chain(TrackPartCatalog.StraightKey, TrackPartCatalog.StraightKey));

        Assert.Equal(2, fitted.Anchors.Count);
        Assert.All(fitted.Anchors, anchor =>
        {
            Assert.InRange(anchor.X, 0, TrackPlanFitter.CanvasWidth);
            Assert.InRange(anchor.Y, 0, TrackPlanFitter.CanvasHeight);
        });
    }

    [Fact]
    public void ToStuds_is_the_exact_inverse_of_the_fit()
    {
        var fitted = Fit(Chain(TrackPartCatalog.StraightKey, TrackPartCatalog.CurveRightKey));

        // Round-trip a known stud position through the canvas and back.
        var canvasX = fitted.OriginX + (23.5 / fitted.StudsPerPixel);
        var canvasY = fitted.OriginY + (-8.25 / fitted.StudsPerPixel);
        var studs = fitted.ToStuds(canvasX, canvasY);

        Assert.Equal(23.5, studs.X, 9);
        Assert.Equal(-8.25, studs.Y, 9);
    }

    [Fact]
    public void A_small_plan_is_not_blown_up_past_the_default_scale()
    {
        var fitted = Fit(Chain(TrackPartCatalog.StraightKey));

        Assert.Equal(1 / DefaultPixelsPerStud, fitted.StudsPerPixel, 9);
    }

    [Fact]
    public void A_plan_too_big_for_the_canvas_is_shrunk_to_fit()
    {
        var sprawling = Fit(Chain([.. Enumerable.Repeat(TrackPartCatalog.StraightKey, 40)]));

        // Forty straights are 640 studs; at the default scale that would be 3200 px on a 900 px
        // canvas, so the fit has to scale down.
        Assert.True(
            sprawling.StudsPerPixel > 1 / DefaultPixelsPerStud,
            $"A 640-stud plan should have been shrunk, but the scale stayed at {sprawling.StudsPerPixel}.");
        Assert.All(sprawling.Anchors, anchor => Assert.InRange(anchor.X, 0, TrackPlanFitter.CanvasWidth));
    }

    [Fact]
    public void Preview_parts_are_marked_as_such()
    {
        var topology = Chain(TrackPartCatalog.StraightKey, TrackPartCatalog.StraightKey);
        var previewId = _ids[1];

        var fitted = TrackPlanFitter.Fit(TrackPlanWalker.Walk(topology), [previewId]);

        Assert.True(fitted.Pieces.Single(piece => piece.PartId == previewId).IsPreview);
        Assert.False(fitted.Pieces.Single(piece => piece.PartId == _ids[0]).IsPreview);
        Assert.NotEqual("", fitted.PreviewPathData);
    }

    [Fact]
    public void A_growing_preview_never_moves_the_committed_plan()
    {
        // The whole reason the fit ignores preview parts: while a drag is in flight the plan must
        // stay exactly where it is, however far past the canvas edge the ghost reaches.
        var committed = Chain(TrackPartCatalog.StraightKey, TrackPartCatalog.StraightKey);
        var committedFit = TrackPlanFitter.Fit(TrackPlanWalker.Walk(committed));

        var grown = committed;
        var previewIds = new List<Guid>();
        var tail = _ids[^1];

        for (var i = 0; i < 20; i++)
        {
            var part = new TrackPlanPart(Guid.NewGuid(), TrackPartCatalog.StraightKey);

            grown = grown
                .Add(part)
                .Connect(new TrackPlanLink(tail, TrackPortNames.End, part.Id, TrackPortNames.Start));
            previewIds.Add(part.Id);
            tail = part.Id;
        }

        var withPreview = TrackPlanFitter.Fit(TrackPlanWalker.Walk(grown), previewIds);

        Assert.Equal(committedFit.StudsPerPixel, withPreview.StudsPerPixel, 9);
        Assert.Equal(committedFit.OriginX, withPreview.OriginX, 9);
        Assert.Equal(committedFit.OriginY, withPreview.OriginY, 9);
    }

    private FittedPlan Fit(TrackPlanTopology topology) => TrackPlanFitter.Fit(TrackPlanWalker.Walk(topology));

    // A chain of parts plugged end-to-start, remembering the ids in order.
    private TrackPlanTopology Chain(params string[] specKeys)
    {
        var topology = TrackPlanTopology.Empty;
        _ids = [];

        foreach (var specKey in specKeys)
        {
            var part = new TrackPlanPart(Guid.NewGuid(), specKey);
            topology = topology.Add(part);

            if (_ids.Count > 0)
            {
                topology = topology.Connect(
                    new TrackPlanLink(_ids[^1], TrackPortNames.End, part.Id, TrackPortNames.Start));
            }

            _ids.Add(part.Id);
        }

        return topology;
    }
}
