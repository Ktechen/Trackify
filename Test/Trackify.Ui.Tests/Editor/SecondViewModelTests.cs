using Trackify.Domain.Enums;
using Trackify.Domain.Tracks;
using Trackify.Ui.Tests.Fakes;
using Trackify.Models.Trains;
using Trackify.Presentation.ViewModels;
using Windows.Foundation;
using Xunit;

namespace Trackify.Ui.Tests.Editor;

/// <summary>
/// Drives the Streckenplaner the way the canvas does — press, drag, release — and checks what the
/// plan became. This is as close to a UI test as is worth having here: the Skia surface cannot be
/// inspected, but every decision the editor makes happens in this ViewModel.
/// </summary>
public class SecondViewModelTests
{
    [Fact]
    public void A_fresh_planner_seeds_one_piece_so_there_is_something_to_pull_from()
    {
        var viewModel = Editor();

        Assert.Equal(1, viewModel.PieceCount);
        Assert.Equal(2, viewModel.Anchors.Count);
        Assert.True(viewModel.IsBuildable);
    }

    [Fact]
    public void A_drag_from_an_open_end_lays_track_that_can_actually_be_built()
    {
        var viewModel = Editor();

        Drag(viewModel, Forward(viewModel), dx: 300, dy: 0);

        Assert.True(viewModel.PieceCount > 1, "Dragging should have laid at least one part.");
        Assert.True(viewModel.IsBuildable);
        Assert.All(viewModel.Pieces, piece => Assert.False(piece.IsPreview));
    }

    [Fact]
    public void A_drag_that_curves_lays_curves()
    {
        var viewModel = Editor();

        // Pull out and down: the snapper should answer with curves, not only straights.
        Drag(viewModel, Forward(viewModel), dx: 200, dy: 200);

        Assert.Contains(viewModel.Pieces, piece => piece.SpecKey == TrackPartCatalog.CurveRightKey);
    }

    [Fact]
    public void A_press_far_from_every_open_end_lays_nothing()
    {
        var viewModel = Editor();

        Drag(viewModel, new Point(20, 560), dx: 300, dy: 0);

        Assert.Equal(1, viewModel.PieceCount);
    }

    [Fact]
    public void While_a_drag_is_in_flight_the_preview_does_not_count_as_laid()
    {
        var viewModel = Editor();
        var from = Forward(viewModel);

        viewModel.DrawStartCommand.Execute(new Point(from.X, from.Y));
        for (var step = 1; step <= 10; step++)
        {
            viewModel.DrawMoveCommand.Execute(new Point(from.X + (step * 25), from.Y));
        }

        Assert.NotEqual("", viewModel.PreviewPathData);
        Assert.Equal(1, viewModel.PieceCount);
        Assert.Contains(viewModel.Pieces, piece => piece.IsPreview);
    }

    [Fact]
    public void Releasing_without_having_moved_lays_nothing_and_clears_the_preview()
    {
        var viewModel = Editor();
        var from = Forward(viewModel);

        viewModel.DrawStartCommand.Execute(new Point(from.X, from.Y));
        viewModel.DrawEndCommand.Execute(new Point(from.X, from.Y));

        Assert.Equal(1, viewModel.PieceCount);
        Assert.Equal("", viewModel.PreviewPathData);
    }

    [Fact]
    public void Undo_takes_the_last_run_back_and_redo_puts_it_again()
    {
        var viewModel = Editor();

        Assert.False(viewModel.CanUndo);

        Drag(viewModel, Forward(viewModel), dx: 300, dy: 0);
        var afterDrag = viewModel.PieceCount;

        Assert.True(viewModel.CanUndo);

        viewModel.UndoCommand.Execute(null);

        Assert.Equal(1, viewModel.PieceCount);
        Assert.True(viewModel.CanRedo);

        viewModel.RedoCommand.Execute(null);

        Assert.Equal(afterDrag, viewModel.PieceCount);
    }

    [Fact]
    public void Closing_the_inspector_clears_the_selection_and_keeps_the_plan()
    {
        // On a phone the inspector is a bottom sheet over the canvas, shown while a piece is
        // selected — and every drawn run selects the piece it laid. Without a way to put the sheet
        // away, it would cover the canvas after the first stroke.
        var viewModel = Editor();
        Drag(viewModel, Forward(viewModel), dx: 300, dy: 0);
        var laid = viewModel.PieceCount;

        Assert.True(viewModel.HasSelection);

        viewModel.ClearSelectionCommand.Execute(null);

        Assert.Null(viewModel.SelectedPiece);
        Assert.False(viewModel.HasSelection);
        Assert.Equal(laid, viewModel.PieceCount);
    }

    [Fact]
    public void The_legend_names_every_colour_the_track_can_take()
    {
        // The rail is coloured by top speed; a colour on the canvas that the legend does not list is
        // a colour nobody can read.
        var viewModel = Editor();
        var piece = viewModel.Pieces[0];
        var colours = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var speed = 0; speed <= 100; speed += 5)
        {
            piece.MaxSpeed = speed;
            colours.Add(piece.SpeedColor);
        }

        Assert.Equal(
            colours.OrderBy(colour => colour, StringComparer.OrdinalIgnoreCase),
            viewModel.Legend.Select(item => item.Color).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(colour => colour, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void The_oval_template_is_a_closed_loop_of_real_parts()
    {
        var viewModel = Editor();

        viewModel.TemplateOvalCommand.Execute(null);

        Assert.Equal(18, viewModel.PieceCount);
        Assert.Equal(0, viewModel.OpenEndCount);
        Assert.True(viewModel.IsBuildable);
    }

    [Fact]
    public void Clearing_empties_the_plan()
    {
        var viewModel = Editor();

        viewModel.TemplateOvalCommand.Execute(null);
        viewModel.ClearCommand.Execute(null);

        Assert.Equal(0, viewModel.PieceCount);
        Assert.Empty(viewModel.Pieces);
    }

    [Fact]
    public async Task A_point_appended_at_an_open_end_is_stored_with_a_switch_configuration()
    {
        var repository = new FakeTrackPlanRepository();
        var viewModel = Editor(repository);

        viewModel.SelectAnchorCommand.Execute(Forward(viewModel));
        viewModel.AddSwitchLeftCommand.Execute(null);
        await viewModel.SaveCommand.ExecuteAsync(null);

        var point = Assert.Single(repository.Stored.Parts, part => part.SpecKey == TrackPartCatalog.SwitchLeftKey);

        Assert.NotNull(point.Switch);
        Assert.Equal(SwitchRoute.Main, point.Switch!.DefaultRoute);
        Assert.True(viewModel.SelectedPiece?.IsSwitch);
    }

    [Fact]
    public async Task Deleting_a_piece_takes_its_connections_with_it()
    {
        var repository = new FakeTrackPlanRepository();
        var viewModel = Editor(repository);

        Drag(viewModel, Forward(viewModel), dx: 300, dy: 0);

        var victim = viewModel.Pieces[^1];
        viewModel.DeletePieceCommand.Execute(victim);
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.DoesNotContain(repository.Stored.Parts, part => part.Id == victim.Id);
        Assert.DoesNotContain(
            repository.Stored.Links,
            link => link.FromPartId == victim.Id || link.ToPartId == victim.Id);
    }

    [Fact]
    public async Task Saving_writes_the_parts_and_their_connections()
    {
        var repository = new FakeTrackPlanRepository();
        var viewModel = Editor(repository);

        viewModel.TemplateOvalCommand.Execute(null);
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(1, repository.ReplaceCalls);
        Assert.Equal(18, repository.Stored.Parts.Count);
        Assert.Equal(18, repository.Stored.Links.Count);
        Assert.True(repository.Stored.ToLayout().IsBuildable);
    }

    [Fact]
    public void A_store_that_cannot_be_read_is_reported_instead_of_silently_ignored()
    {
        // The ViewModel starts its load without awaiting it, so this is the case that would
        // otherwise vanish as an unobserved task exception.
        var repository = new FakeTrackPlanRepository
        {
            LoadFailure = new InvalidOperationException("store is broken"),
        };

        var viewModel = Editor(repository);

        Assert.Contains("store is broken", viewModel.Problem, StringComparison.Ordinal);
        Assert.False(viewModel.IsBuildable);
        // Still usable: a piece was seeded so the canvas can be drawn on.
        Assert.Equal(1, viewModel.PieceCount);
    }

    private static SecondViewModel Editor(FakeTrackPlanRepository? repository = null)
        => new(new FakeNavigator(), repository ?? new FakeTrackPlanRepository());

    // The open end the seeded straight points forward from — the larger X of the two anchors.
    private static TrackAnchor Forward(SecondViewModel viewModel)
        => viewModel.Anchors.OrderByDescending(anchor => anchor.X).First();

    private static void Drag(SecondViewModel viewModel, TrackAnchor from, double dx, double dy)
        => Drag(viewModel, new Point(from.X, from.Y), dx, dy);

    private static void Drag(SecondViewModel viewModel, Point from, double dx, double dy)
    {
        const int steps = 15;

        viewModel.DrawStartCommand.Execute(from);

        for (var step = 1; step <= steps; step++)
        {
            viewModel.DrawMoveCommand.Execute(
                new Point(from.X + (dx * step / steps), from.Y + (dy * step / steps)));
        }

        viewModel.DrawEndCommand.Execute(new Point(from.X + dx, from.Y + dy));
    }
}
