using Trackify.Presentation.Layout;
using Xunit;

namespace Trackify.Ui.Tests.Layout;

/// <summary>
/// The Streckenplaner's responsive rules. These exist because the page itself is a <c>Page</c> — it
/// cannot be instantiated without a UI thread, and the rendered result cannot be inspected in this
/// environment either. Pulling the decision into <see cref="EditorPageLayout"/> is what makes the
/// difference between "checked" and "looked right on someone's phone once".
/// </summary>
public class EditorPageLayoutTests
{
    private const double Phone = 360;
    private const double Tablet = 900;

    [Theory]
    [InlineData(320)]
    [InlineData(360)]
    [InlineData(412)]
    [InlineData(719)]
    public void A_phone_width_is_not_wide(double width)
    {
        // Arrange / Act
        var layout = EditorPageLayout.Resolve(width, hasSelection: false, isPieceListOpen: false);

        // Assert
        Assert.False(layout.IsWide);
    }

    [Theory]
    [InlineData(720)]
    [InlineData(1280)]
    public void The_threshold_itself_counts_as_wide(double width)
    {
        // Arrange / Act
        var layout = EditorPageLayout.Resolve(width, hasSelection: false, isPieceListOpen: false);

        // Assert — >=, so a window at exactly the breakpoint does not flip-flop.
        Assert.True(layout.IsWide);
    }

    [Fact]
    public void Wide_puts_both_panes_in_their_own_columns()
    {
        // Arrange / Act
        var layout = EditorPageLayout.Resolve(Tablet, hasSelection: false, isPieceListOpen: false);

        // Assert — the columns SecondPage.xaml declares.
        Assert.Equal(2, layout.List.Column);
        Assert.Equal(4, layout.Inspector.Column);
        Assert.False(layout.List.IsSheet);
        Assert.False(layout.Inspector.IsSheet);
        Assert.True(layout.ShowSplitters);
    }

    [Fact]
    public void Wide_shows_the_parts_list_whether_or_not_the_sheet_was_left_open()
    {
        // Arrange — the open flag only governs the sheet; a column is simply there.
        var layout = EditorPageLayout.Resolve(Tablet, hasSelection: false, isPieceListOpen: false);

        // Act / Assert
        Assert.True(layout.List.IsVisible);
        Assert.True(layout.Inspector.IsVisible);
    }

    [Fact]
    public void Wide_falls_back_to_the_default_pane_widths()
    {
        // Arrange / Act — nothing dragged yet, so the columns read 0.
        var layout = EditorPageLayout.Resolve(Tablet, hasSelection: false, isPieceListOpen: false);

        // Assert — the literal numbers on purpose, not the constants: these are a floor the panes'
        // content needs (part rows, two-up pill buttons), so lowering one should trip a test rather
        // than quietly start clipping. They also match the splitters' MinWidth in SecondPage.xaml.
        Assert.Equal(260, layout.ListColumn.Value);
        Assert.Equal(340, layout.InspectorColumn.Value);
        Assert.False(layout.ListColumn.IsStar);
    }

    [Fact]
    public void A_width_the_user_dragged_survives_a_trip_through_the_narrow_layout()
    {
        // Arrange — the panes were dragged wider before the window was made narrow.
        var layout = EditorPageLayout.Resolve(
            Tablet, hasSelection: false, isPieceListOpen: false,
            currentListPixels: 400, currentInspectorPixels: 500);

        // Act / Assert — snapping back to the defaults would quietly undo the user's own adjustment.
        Assert.Equal(400, layout.ListColumn.Value);
        Assert.Equal(500, layout.InspectorColumn.Value);
    }

    [Fact]
    public void Narrow_gives_the_canvas_everything_and_collapses_the_pane_columns()
    {
        // Arrange / Act
        var layout = EditorPageLayout.Resolve(Phone, hasSelection: false, isPieceListOpen: false);

        // Assert — this is the whole point of the phone layout: the drawing area is the screen.
        Assert.Equal(0, layout.ListColumn.Value);
        Assert.Equal(0, layout.InspectorColumn.Value);
        Assert.Equal(0, layout.SplitterColumns.Value);
        Assert.Equal(0, layout.ListRowStars);
        Assert.Equal(1, layout.CanvasRowStars);
        Assert.False(layout.ShowSplitters);
    }

    [Fact]
    public void Narrow_hides_both_panes_while_nothing_asks_for_them()
    {
        // Arrange / Act
        var layout = EditorPageLayout.Resolve(Phone, hasSelection: false, isPieceListOpen: false);

        // Assert
        Assert.False(layout.List.IsVisible);
        Assert.False(layout.Inspector.IsVisible);
    }

    [Fact]
    public void Narrow_makes_both_panes_sheets_that_span_the_canvas()
    {
        // Arrange / Act
        var layout = EditorPageLayout.Resolve(Phone, hasSelection: true, isPieceListOpen: true);

        // Assert — row span 2 so a sheet covers the canvas and the toolbar beneath it.
        Assert.True(layout.List.IsSheet);
        Assert.True(layout.Inspector.IsSheet);
        Assert.Equal(2, layout.List.RowSpan);
        Assert.Equal(2, layout.Inspector.RowSpan);
        Assert.Equal(0, layout.List.Column);
        Assert.Equal(0, layout.Inspector.Column);
    }

    [Fact]
    public void The_list_sheet_opens_on_demand()
    {
        // Arrange / Act
        var layout = EditorPageLayout.Resolve(Phone, hasSelection: false, isPieceListOpen: true);

        // Assert
        Assert.True(layout.List.IsVisible);
        Assert.Equal(EditorPageLayout.ListSheetMaxHeight, layout.List.MaxHeight);
    }

    [Fact]
    public void The_inspector_sheet_follows_the_selection()
    {
        // Arrange / Act
        var layout = EditorPageLayout.Resolve(Phone, hasSelection: true, isPieceListOpen: false);

        // Assert
        Assert.True(layout.Inspector.IsVisible);
        Assert.Equal(EditorPageLayout.InspectorSheetMaxHeight, layout.Inspector.MaxHeight);
    }

    [Fact]
    public void Only_one_sheet_shows_at_a_time()
    {
        // Arrange — the list was left open and then a part got selected.
        var layout = EditorPageLayout.Resolve(Phone, hasSelection: true, isPieceListOpen: true);

        // Act / Assert — two stacked sheets would bury the canvas entirely; the selection is the more
        // specific thing to be looking at, so the inspector wins.
        Assert.True(layout.Inspector.IsVisible);
        Assert.False(layout.List.IsVisible);
    }

    [Fact]
    public void A_pane_in_a_column_is_never_height_capped()
    {
        // Arrange / Act
        var layout = EditorPageLayout.Resolve(Tablet, hasSelection: true, isPieceListOpen: true);

        // Assert — the sheet cap must not leak into the wide layout and cut the sidebar short.
        Assert.Equal(double.PositiveInfinity, layout.List.MaxHeight);
        Assert.Equal(double.PositiveInfinity, layout.Inspector.MaxHeight);
    }
}
