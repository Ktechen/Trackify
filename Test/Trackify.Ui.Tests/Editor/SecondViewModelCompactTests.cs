using System.ComponentModel;
using Trackify.Domain.Tracks;
using Trackify.Presentation.ViewModels;
using Trackify.Ui.Tests.Fakes;
using Xunit;

namespace Trackify.Ui.Tests.Editor;

/// <summary>
/// The phone layout's own rules. Everything the narrow editor hides, shows or wraps hangs off
/// <see cref="SecondViewModel.IsCompact"/> and the bound parts palette, and that is deliberate: the
/// rendered UI cannot be inspected in this environment, so the decisions live where a test can reach
/// them instead of only in XAML that has to be eyeballed on a device.
/// </summary>
public class SecondViewModelCompactTests
{
    [Fact]
    public void An_editor_starts_roomy_so_a_desktop_is_never_stripped_by_default()
    {
        // Arrange / Act
        var viewModel = Editor();

        // Assert — the page publishes the real width on load; until then, assume there is room.
        Assert.False(viewModel.IsCompact);
        Assert.True(viewModel.IsRoomy);
    }

    [Fact]
    public void Roomy_is_the_inverse_of_compact()
    {
        // Arrange
        var viewModel = Editor();

        // Act
        viewModel.IsCompact = true;

        // Assert
        Assert.False(viewModel.IsRoomy);
    }

    [Fact]
    public void Going_compact_announces_roomy_as_well()
    {
        // Arrange — IsRoomy is what the header, the legend and the button labels actually bind to.
        var viewModel = Editor();
        var announced = new List<string?>();
        ((INotifyPropertyChanged)viewModel).PropertyChanged += (_, e) => announced.Add(e.PropertyName);

        // Act
        viewModel.IsCompact = true;

        // Assert — without this the toolbar would keep its words on a phone and overflow again.
        Assert.Contains(nameof(SecondViewModel.IsRoomy), announced);
    }

    [Fact]
    public void The_parts_list_starts_closed_on_a_phone()
    {
        // Arrange / Act
        var viewModel = Editor();

        // Assert — you build first; the list is for inspecting afterwards.
        Assert.False(viewModel.IsPieceListOpen);
    }

    [Fact]
    public void The_list_button_opens_and_closes_the_sheet()
    {
        // Arrange
        var viewModel = Editor();

        // Act
        viewModel.TogglePieceListCommand.Execute(null);

        // Assert
        Assert.True(viewModel.IsPieceListOpen);

        // Act — the same button is the way back out.
        viewModel.TogglePieceListCommand.Execute(null);

        // Assert
        Assert.False(viewModel.IsPieceListOpen);
    }

    [Fact]
    public void The_palette_offers_every_part_the_toolbar_used_to_hard_code()
    {
        // Arrange
        var viewModel = Editor();

        // Act
        var keys = viewModel.Palette.Select(item => item.PartKey).ToList();

        // Assert — the palette replaced seven hand-written buttons, so it must still carry all seven.
        Assert.Equal(
            [
                TrackPartCatalog.StraightKey,
                TrackPartCatalog.CurveLeftKey,
                TrackPartCatalog.CurveRightKey,
                TrackPartCatalog.StationKey,
                TrackPartCatalog.SwitchLeftKey,
                TrackPartCatalog.SwitchRightKey,
                TrackPartCatalog.CrossingKey,
            ],
            keys);
    }

    [Fact]
    public void Every_palette_entry_names_a_real_part_and_a_port_that_part_has()
    {
        // Arrange
        var viewModel = Editor();

        // Act / Assert — a typo here would be a button that throws when tapped, which is exactly the
        // kind of thing that cannot be caught by looking at the screen.
        foreach (var item in viewModel.Palette)
        {
            Assert.True(TrackPartCatalog.TryGet(item.PartKey, out var spec), item.PartKey);
            Assert.Contains(item.EntryPort, spec.Ports.Select(port => port.Name));
            Assert.False(string.IsNullOrWhiteSpace(item.Label));
        }
    }

    [Fact]
    public void Tapping_a_palette_entry_appends_that_part()
    {
        // Arrange
        var viewModel = Editor();
        var point = viewModel.Palette.Single(item => item.PartKey == TrackPartCatalog.SwitchLeftKey);
        var before = viewModel.PieceCount;

        // Act
        viewModel.AddPartCommand.Execute(point);

        // Assert
        Assert.Equal(before + 1, viewModel.PieceCount);
        Assert.True(viewModel.SelectedPiece?.IsSwitch);
        Assert.True(viewModel.IsBuildable);
    }

    [Fact]
    public void The_palette_and_the_named_commands_lay_the_same_part()
    {
        // Arrange — two ways in must not drift apart.
        var viaPalette = Editor();
        var viaCommand = Editor();
        var straight = viaPalette.Palette.Single(item => item.PartKey == TrackPartCatalog.StraightKey);

        // Act
        viaPalette.AddPartCommand.Execute(straight);
        viaCommand.AddStraightCommand.Execute(null);

        // Assert
        Assert.Equal(viaCommand.PieceCount, viaPalette.PieceCount);
        Assert.Equal(viaCommand.SelectedPiece?.TypeLabel, viaPalette.SelectedPiece?.TypeLabel);
    }

    [Fact]
    public void A_palette_tap_with_nothing_behind_it_lays_nothing()
    {
        // Arrange — CommandParameter can arrive null while a template is still being realised.
        var viewModel = Editor();
        var before = viewModel.PieceCount;

        // Act
        viewModel.AddPartCommand.Execute(null);

        // Assert
        Assert.Equal(before, viewModel.PieceCount);
        Assert.True(viewModel.IsBuildable);
    }

    private static SecondViewModel Editor()
        => new(new FakeNavigator(), new FakeTrackPlanRepository());
}
