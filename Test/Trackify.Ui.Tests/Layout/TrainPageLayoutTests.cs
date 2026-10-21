using Trackify.Presentation.Layout;
using Xunit;

namespace Trackify.Ui.Tests.Layout;

/// <summary>
/// The Zugkonfiguration page's responsive rules: a list rail beside the editor when there is room,
/// master-detail when there is not. Testable for the same reason as
/// <see cref="EditorPageLayoutTests"/> — the page cannot be instantiated here and cannot be looked at.
/// </summary>
public class TrainPageLayoutTests
{
    private const double Phone = 360;
    private const double Desktop = 1280;

    [Fact]
    public void Wide_shows_the_rail_and_the_editor_together()
    {
        // Arrange / Act
        var layout = TrainPageLayout.Resolve(Desktop, hasSelection: false);

        // Assert
        Assert.True(layout.IsWide);
        Assert.True(layout.ShowList);
        Assert.True(layout.ShowEditor);
        Assert.True(layout.ShowSplitter);
        // The literal, not the constant, so changing the rail's default has to be deliberate.
        Assert.Equal(346, layout.ListColumn.Value);
        Assert.True(layout.EditorColumn.IsStar);
    }

    [Fact]
    public void A_rail_width_the_user_dragged_is_kept()
    {
        // Arrange / Act
        var layout = TrainPageLayout.Resolve(Desktop, hasSelection: false, currentListPixels: 420);

        // Assert
        Assert.Equal(420, layout.ListColumn.Value);
    }

    [Fact]
    public void Narrow_with_nothing_chosen_is_the_list_full_width()
    {
        // Arrange / Act
        var layout = TrainPageLayout.Resolve(Phone, hasSelection: false);

        // Assert — the list is the home pane you land on.
        Assert.True(layout.ShowList);
        Assert.True(layout.ListColumn.IsStar);
        Assert.False(layout.ShowEditor);
        Assert.Equal(0, layout.EditorColumn.Value);
        Assert.False(layout.ShowSplitter);
    }

    [Fact]
    public void Narrow_with_a_train_chosen_is_the_editor_full_width()
    {
        // Arrange / Act
        var layout = TrainPageLayout.Resolve(Phone, hasSelection: true);

        // Assert
        Assert.True(layout.ShowEditor);
        Assert.True(layout.EditorColumn.IsStar);
        Assert.False(layout.ShowList);
        Assert.Equal(0, layout.ListColumn.Value);
    }

    [Fact]
    public void The_back_arrow_appears_only_where_it_leads_somewhere()
    {
        // Arrange / Act / Assert — narrow + a train showing is the one state you can go "back" from.
        Assert.True(TrainPageLayout.Resolve(Phone, hasSelection: true).ShowBackButton);
        Assert.False(TrainPageLayout.Resolve(Phone, hasSelection: false).ShowBackButton);
        Assert.False(TrainPageLayout.Resolve(Desktop, hasSelection: true).ShowBackButton);
        Assert.False(TrainPageLayout.Resolve(Desktop, hasSelection: false).ShowBackButton);
    }

    [Fact]
    public void The_header_sheds_its_ornament_when_narrow()
    {
        // Arrange / Act
        var layout = TrainPageLayout.Resolve(Phone, hasSelection: true);

        // Assert — with the back arrow showing, the emblem's 56px is what pushed this header past a
        // 360dp phone.
        Assert.False(layout.ShowHeaderEmblem);
        Assert.False(layout.ShowHeaderStats);
        Assert.False(layout.ShowConnectionChip);
    }

    [Fact]
    public void The_header_keeps_its_ornament_when_wide()
    {
        // Arrange / Act
        var layout = TrainPageLayout.Resolve(Desktop, hasSelection: true);

        // Assert
        Assert.True(layout.ShowHeaderEmblem);
        Assert.True(layout.ShowHeaderStats);
        Assert.True(layout.ShowConnectionChip);
    }

    [Fact]
    public void The_connection_chip_needs_a_train_to_talk_about()
    {
        // Arrange / Act
        var layout = TrainPageLayout.Resolve(Desktop, hasSelection: false);

        // Assert — unlike the counts, the chip reports the selected train's state.
        Assert.True(layout.ShowHeaderStats);
        Assert.False(layout.ShowConnectionChip);
    }

    [Fact]
    public void Both_pages_switch_over_at_the_same_width()
    {
        // Arrange / Act / Assert — two different thresholds would make the app feel broken halfway.
        Assert.Equal(EditorPageLayout.WideThreshold, TrainPageLayout.WideThreshold);
    }
}
