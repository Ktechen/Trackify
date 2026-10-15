using Microsoft.Extensions.Logging.Abstractions;
using Trackify.Application.Lego;
using Trackify.Domain.Enums;
using Trackify.Presentation.ViewModels;
using Trackify.Services.Remote;
using Trackify.Ui.Tests.Fakes;
using Xunit;

namespace Trackify.Ui.Tests.Editor;

/// <summary>
/// The train screen: the list and its filter, the editor commands, and the hub control that lives in
/// the MainViewModel.Hub partial.
/// </summary>
public class MainViewModelTests
{
    [Fact]
    public void A_new_screen_starts_empty_with_a_swatch_per_catalogue_colour()
    {
        var viewModel = Screen();

        Assert.Empty(viewModel.Trains);
        Assert.Equal(0, viewModel.TotalCount);
        Assert.Equal(0, viewModel.ActiveCount);
        Assert.NotEmpty(viewModel.ColorSwatches);
    }

    [Fact]
    public void Adding_a_train_selects_it_and_counts_it()
    {
        var viewModel = Screen();

        viewModel.AddTrainCommand.Execute(null);

        var train = Assert.Single(viewModel.Trains);
        Assert.Same(train, viewModel.SelectedTrain);
        Assert.Equal(1, viewModel.TotalCount);
        Assert.Equal(1, viewModel.ActiveCount);
        Assert.Contains(train, viewModel.FilteredTrains);
    }

    [Fact]
    public void Adding_a_train_clears_the_search_so_the_new_one_is_visible()
    {
        var viewModel = Screen();

        viewModel.AddTrainCommand.Execute(null);
        viewModel.SelectedTrain!.Name = "Blauer Zug";
        viewModel.Search = "etwas-das-nicht-passt";

        Assert.Empty(viewModel.FilteredTrains);

        viewModel.AddTrainCommand.Execute(null);

        Assert.Equal("", viewModel.Search);
        Assert.Equal(2, viewModel.FilteredTrains.Count);
    }

    [Fact]
    public void Duplicating_puts_the_copy_right_after_the_original_and_selects_it()
    {
        var viewModel = Screen();

        viewModel.AddTrainCommand.Execute(null);
        viewModel.AddTrainCommand.Execute(null);
        viewModel.SelectedTrain = viewModel.Trains[0];

        viewModel.DuplicateTrainCommand.Execute(null);

        Assert.Equal(3, viewModel.Trains.Count);
        Assert.Same(viewModel.Trains[1], viewModel.SelectedTrain);
    }

    [Fact]
    public void Deleting_removes_the_train_and_falls_back_to_the_first_one_left()
    {
        var viewModel = Screen();

        viewModel.AddTrainCommand.Execute(null);
        viewModel.AddTrainCommand.Execute(null);
        var survivor = viewModel.Trains[0];
        viewModel.SelectedTrain = viewModel.Trains[1];

        viewModel.DeleteTrainCommand.Execute(null);

        Assert.Same(survivor, Assert.Single(viewModel.Trains));
        Assert.Same(survivor, viewModel.SelectedTrain);
    }

    [Fact]
    public void Duplicate_and_delete_stay_disabled_until_something_is_selected()
    {
        var viewModel = Screen();

        Assert.False(viewModel.DuplicateTrainCommand.CanExecute(null));
        Assert.False(viewModel.DeleteTrainCommand.CanExecute(null));

        viewModel.AddTrainCommand.Execute(null);

        Assert.True(viewModel.DuplicateTrainCommand.CanExecute(null));
        Assert.True(viewModel.DeleteTrainCommand.CanExecute(null));
    }

    [Theory]
    [InlineData("blau", 1)]
    [InlineData("BLAU", 1)]
    [InlineData("  blau  ", 1)]
    [InlineData("PoweredUpHub", 2)]
    [InlineData("gibt-es-nicht", 0)]
    public void The_search_filters_by_name_and_by_hub_type(string query, int expected)
    {
        var viewModel = Screen();

        viewModel.AddTrainCommand.Execute(null);
        viewModel.Trains[0].Name = "Blauer Zug";
        viewModel.AddTrainCommand.Execute(null);
        viewModel.Trains[1].Name = "Roter Zug";

        viewModel.Search = query;

        Assert.Equal(expected, viewModel.FilteredTrains.Count);
    }

    [Fact]
    public void The_active_count_follows_a_train_being_switched_off()
    {
        var viewModel = Screen();

        viewModel.AddTrainCommand.Execute(null);
        viewModel.AddTrainCommand.Execute(null);

        Assert.Equal(2, viewModel.ActiveCount);

        viewModel.Trains[0].IsActive = false;

        Assert.Equal(1, viewModel.ActiveCount);
        Assert.Equal(2, viewModel.TotalCount);
    }

    [Fact]
    public void Choosing_a_colour_sets_it_on_the_train_and_marks_the_swatch()
    {
        var viewModel = Screen();

        viewModel.AddTrainCommand.Execute(null);
        viewModel.SetColorCommand.Execute(LedColorType.Red);

        Assert.Equal(LedColorType.Red, viewModel.SelectedTrain!.Color);
        Assert.True(viewModel.ColorSwatches.Single(swatch => swatch.Value == LedColorType.Red).IsSelected);
        Assert.All(
            viewModel.ColorSwatches.Where(swatch => swatch.Value != LedColorType.Red),
            swatch => Assert.False(swatch.IsSelected));
    }

    [Fact]
    public void Going_back_to_the_list_clears_the_selection()
    {
        var viewModel = Screen();

        viewModel.AddTrainCommand.Execute(null);
        viewModel.BackToListCommand.Execute(null);

        Assert.Null(viewModel.SelectedTrain);
    }

    [Fact]
    public void The_connection_panel_toggles()
    {
        var viewModel = Screen();

        Assert.False(viewModel.IsConnectionPanelOpen);

        viewModel.ToggleConnectionPanelCommand.Execute(null);

        Assert.True(viewModel.IsConnectionPanelOpen);

        viewModel.ToggleConnectionPanelCommand.Execute(null);

        Assert.False(viewModel.IsConnectionPanelOpen);
    }

    [Fact]
    public async Task Direct_mode_reports_that_this_device_does_the_driving()
    {
        var viewModel = Screen();
        viewModel.Connection.UseServer = false;

        await viewModel.ApplyConnectionCommand.ExecuteAsync(null);

        Assert.Contains("Direktmodus", viewModel.ConnectionStatus!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Server_mode_without_an_address_asks_for_one_instead_of_syncing()
    {
        // Only the two offline branches are exercised: with a URL set, the sync really dials out,
        // which a test must not depend on.
        var viewModel = Screen();
        viewModel.Connection.UseServer = true;
        viewModel.Connection.ServerUrl = "";

        await viewModel.ApplyConnectionCommand.ExecuteAsync(null);

        Assert.Contains("Server-Adresse", viewModel.ConnectionStatus!, StringComparison.Ordinal);
    }

    // ---- hub control ---------------------------------------------------------------------------

    [Fact]
    public async Task A_platform_without_Bluetooth_says_so_rather_than_scanning()
    {
        var viewModel = Screen(new FakeLegoService { IsSupported = false });

        await viewModel.DiscoverCommand.ExecuteAsync(null);

        Assert.Contains("nicht unterstützt", viewModel.DiscoverStatus!, StringComparison.Ordinal);
        Assert.False(viewModel.IsDiscovering);
    }

    [Fact]
    public async Task A_scan_that_finds_nothing_says_so_and_offers_no_hub()
    {
        var viewModel = Screen(new FakeLegoService());

        await viewModel.DiscoverCommand.ExecuteAsync(null);

        Assert.Contains("kein Hub gefunden", viewModel.DiscoverStatus!, StringComparison.Ordinal);
        Assert.Null(viewModel.PendingHub);
        Assert.False(viewModel.IsDiscovering);
    }

    [Fact]
    public async Task A_found_hub_is_offered_for_confirmation_and_becomes_a_train()
    {
        var lego = new FakeLegoService();
        lego.Discoverable.Add(new DiscoveredHubDto("dev-1", "Zug Hub", "AA:BB:CC:DD:EE:FF", HubType.PoweredUpHub));
        var viewModel = Screen(lego);

        await viewModel.DiscoverCommand.ExecuteAsync(null);

        Assert.NotNull(viewModel.PendingHub);
        Assert.Null(viewModel.DiscoverStatus);

        await viewModel.ConfirmAddHubCommand.ExecuteAsync(null);

        var train = Assert.Single(viewModel.Trains);
        Assert.Equal("Zug Hub", train.Name);
        Assert.Equal("dev-1", train.HubId);
        Assert.Equal("AA:BB:CC:DD:EE:FF", train.BleAddress);
        Assert.Same(train, viewModel.SelectedTrain);
    }

    [Fact]
    public async Task A_hub_already_known_to_a_train_is_bound_to_it_instead_of_adding_a_second()
    {
        var lego = new FakeLegoService();
        lego.Discoverable.Add(new DiscoveredHubDto("dev-1", "Zug Hub", "AA:BB:CC:DD:EE:FF", HubType.PoweredUpHub));
        var viewModel = Screen(lego);

        viewModel.AddTrainCommand.Execute(null);
        viewModel.Trains[0].BleAddress = "AA:BB:CC:DD:EE:FF";

        await viewModel.DiscoverCommand.ExecuteAsync(null);
        await viewModel.ConfirmAddHubCommand.ExecuteAsync(null);

        var train = Assert.Single(viewModel.Trains);
        Assert.Equal("dev-1", train.HubId);
    }

    [Fact]
    public async Task Cancelling_the_hub_prompt_drops_it()
    {
        var lego = new FakeLegoService();
        lego.Discoverable.Add(new DiscoveredHubDto("dev-1", "Zug Hub", null, null));
        var viewModel = Screen(lego);

        await viewModel.DiscoverCommand.ExecuteAsync(null);
        viewModel.CancelAddHubCommand.Execute(null);

        Assert.Null(viewModel.PendingHub);
        Assert.Empty(viewModel.Trains);
    }

    [Fact]
    public async Task Connecting_a_train_with_no_hub_chosen_says_so_instead_of_dialling_out()
    {
        var lego = new FakeLegoService();
        var viewModel = Screen(lego);

        viewModel.AddTrainCommand.Execute(null);
        viewModel.SelectedTrain!.BleAddress = "";
        viewModel.SelectedTrain.HubId = "";

        await viewModel.ConnectCommand.ExecuteAsync(null);

        Assert.Contains("Kein Hub gewählt", viewModel.SelectedTrain.ConnectionStatus!, StringComparison.Ordinal);
        Assert.Empty(lego.Connected);
        Assert.False(viewModel.SelectedTrain.IsHardwareConnected);
    }

    [Fact]
    public async Task Connecting_addresses_the_hub_by_its_discovered_device_id()
    {
        var lego = new FakeLegoService();
        var viewModel = Screen(lego);

        viewModel.AddTrainCommand.Execute(null);
        viewModel.SelectedTrain!.BleAddress = "AA:BB:CC:DD:EE:FF";
        viewModel.SelectedTrain.HubId = "dev-1";

        await viewModel.ConnectCommand.ExecuteAsync(null);

        // The platform device id wins over the typed-in address.
        Assert.Equal("dev-1", Assert.Single(lego.Connected));
        Assert.True(viewModel.SelectedTrain.IsHardwareConnected);
        Assert.Equal("Verbunden", viewModel.SelectedTrain.ConnectionStatus);
    }

    [Fact]
    public async Task Connecting_falls_back_to_the_typed_in_address_when_nothing_was_discovered()
    {
        var lego = new FakeLegoService();
        var viewModel = Screen(lego);

        viewModel.AddTrainCommand.Execute(null);
        viewModel.SelectedTrain!.HubId = "";
        viewModel.SelectedTrain.BleAddress = "AA:BB:CC:DD:EE:FF";

        await viewModel.ConnectCommand.ExecuteAsync(null);

        Assert.Equal("AA:BB:CC:DD:EE:FF", Assert.Single(lego.Connected));
    }

    [Fact]
    public async Task Disconnecting_clears_the_flag_and_connect_becomes_available_again()
    {
        var lego = new FakeLegoService();
        var viewModel = Screen(lego);

        viewModel.AddTrainCommand.Execute(null);
        viewModel.SelectedTrain!.HubId = "dev-1";

        await viewModel.ConnectCommand.ExecuteAsync(null);

        Assert.False(viewModel.ConnectCommand.CanExecute(null));
        Assert.True(viewModel.DisconnectCommand.CanExecute(null));

        await viewModel.DisconnectCommand.ExecuteAsync(null);

        Assert.Equal("dev-1", Assert.Single(lego.Disconnected));
        Assert.False(viewModel.SelectedTrain.IsHardwareConnected);
        Assert.Equal("Getrennt", viewModel.SelectedTrain.ConnectionStatus);
        Assert.True(viewModel.ConnectCommand.CanExecute(null));
    }

    [Fact]
    public async Task Clearing_the_discover_status_hides_it()
    {
        var viewModel = Screen(new FakeLegoService { IsSupported = false });

        await viewModel.DiscoverCommand.ExecuteAsync(null);

        Assert.NotNull(viewModel.DiscoverStatus);

        viewModel.ClearDiscoverCommand.Execute(null);

        Assert.Null(viewModel.DiscoverStatus);
    }

    private static MainViewModel Screen(FakeLegoService? lego = null)
    {
        // ConnectionState reads WinRT local settings and falls back to its default when they are
        // unavailable, which is exactly what happens in a test host.
        var connection = new ConnectionState(NullLogger<ConnectionState>.Instance);

        return new MainViewModel(
            new FakeNavigator(),
            lego ?? new FakeLegoService(),
            connection,
            new RemoteTrainSync(connection, new FakeTrainRepository()),
            NullLogger<MainViewModel>.Instance);
    }
}
