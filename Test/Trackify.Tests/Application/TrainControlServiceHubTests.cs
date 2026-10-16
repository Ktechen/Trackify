using Trackify.Application.Catalog;
using Trackify.Application.Lego;
using Trackify.Application.Trains;
using Trackify.Tests.Fakes;

namespace Trackify.Tests.Application;

/// <summary>
/// The hub-facing half of <see cref="TrainControlService"/>: addressing, clamping, the debounce and
/// the LED. A sister file to TrainControlServiceTests so the two can be edited independently.
/// </summary>
public class TrainControlServiceHubTests
{
    [Fact]
    public void Support_is_whatever_the_transport_reports()
    {
        // Arrange
        var lego = new FakeLegoService();

        // Act
        var service = new TrainControlService(lego, NullLogger<TrainControlService>.Instance);

        // Assert
        Assert.Equal(lego.IsSupported, service.IsSupported);
    }

    [Fact]
    public async Task Discovering_asks_the_transport()
    {
        // Arrange
        var service = Service(out _);

        // Act
        var hubs = await service.DiscoverAsync();

        // Assert
        Assert.Empty(hubs);
    }

    [Fact]
    public async Task Connecting_addresses_the_hub_by_its_device_id()
    {
        // Arrange
        var service = Service(out var lego);

        // Act
        await service.ConnectAsync(Train(hubId: "dev-1", bleAddress: "AA:BB:CC:DD:EE:FF"));

        // Assert — the discovered device id wins over the typed-in address.
        Assert.Equal("dev-1", lego.LastHubId);
    }

    [Fact]
    public async Task Connecting_falls_back_to_the_typed_in_address()
    {
        // Arrange
        var service = Service(out var lego);

        // Act
        await service.ConnectAsync(Train(hubId: "", bleAddress: "AA:BB:CC:DD:EE:FF"));

        // Assert
        Assert.Equal("AA:BB:CC:DD:EE:FF", lego.LastHubId);
    }

    [Fact]
    public async Task Connecting_a_train_with_no_hub_at_all_is_refused_loudly()
    {
        // Arrange
        var service = Service(out _);
        var train = Train(hubId: "", bleAddress: "");

        // Act / Assert — a silent no-op here would look like a connection that worked.
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConnectAsync(train));
    }

    [Fact]
    public async Task Disconnecting_a_train_with_no_hub_is_a_quiet_no_op()
    {
        // Arrange
        var service = Service(out var lego);

        // Act
        await service.DisconnectAsync(Train(hubId: "", bleAddress: ""));

        // Assert
        Assert.Null(lego.LastHubId);
    }

    [Theory]
    [InlineData(40, (sbyte)40)]
    [InlineData(0, (sbyte)0)]
    [InlineData(-40, (sbyte)-40)]
    [InlineData(250, (sbyte)100)]
    [InlineData(-250, (sbyte)-100)]
    public async Task The_speed_is_clamped_to_what_a_motor_accepts(int requested, sbyte expected)
    {
        // Arrange
        var service = Service(out var lego);

        // Act
        await service.SetSpeedAsync(Train(), requested);

        // Assert
        Assert.Equal(expected, lego.LastPower);
        Assert.Equal(TrainControlService.MotorPort, lego.LastPort);
    }

    [Fact]
    public async Task Setting_the_speed_of_a_train_with_no_hub_sends_nothing()
    {
        // Arrange
        var service = Service(out var lego);

        // Act
        await service.SetSpeedAsync(Train(hubId: "", bleAddress: ""), 50);

        // Assert
        Assert.Null(lego.LastHubId);
    }

    [Fact]
    public async Task A_debounced_speed_reaches_the_hub_after_the_pause()
    {
        // Arrange
        var service = Service(out var lego);

        // Act
        service.SetSpeedDebounced(Train(), 55);
        await Task.Delay(600);

        // Assert
        Assert.Equal((sbyte)55, lego.LastPower);
    }

    [Fact]
    public async Task Only_the_last_value_of_a_dragged_slider_reaches_the_hub()
    {
        // Arrange
        var service = Service(out var lego);
        var train = Train();

        // Act — three changes in quick succession, as dragging a slider produces.
        service.SetSpeedDebounced(train, 10);
        service.SetSpeedDebounced(train, 40);
        service.SetSpeedDebounced(train, 70);
        await Task.Delay(600);

        // Assert — the earlier two were cancelled, which is the whole point of the debounce.
        Assert.Equal((sbyte)70, lego.LastPower);
    }

    [Fact]
    public async Task A_debounced_speed_for_a_train_with_no_hub_is_dropped()
    {
        // Arrange
        var service = Service(out var lego);

        // Act
        service.SetSpeedDebounced(Train(hubId: "", bleAddress: ""), 55);
        await Task.Delay(400);

        // Assert
        Assert.Null(lego.LastHubId);
    }

    [Fact]
    public async Task The_led_is_sent_as_the_rgb_of_the_configured_colour()
    {
        // Arrange
        var service = Service(out var lego);
        var expected = LegoinoCatalog.Color(LedColorType.Red).Hex.TrimStart('#');

        // Act
        await service.SetLedAsync(Train() with { Color = LedColorType.Red });

        // Assert — the catalogue's hex, parsed into three bytes.
        Assert.NotNull(lego.LastLed);
        var (r, g, b) = lego.LastLed!.Value;
        Assert.Equal(expected, $"{r:X2}{g:X2}{b:X2}", ignoreCase: true);
    }

    [Fact]
    public async Task The_led_of_a_train_with_no_hub_is_not_sent()
    {
        // Arrange
        var service = Service(out var lego);

        // Act
        await service.SetLedAsync(Train(hubId: "", bleAddress: ""));

        // Assert
        Assert.Null(lego.LastLed);
    }

    [Theory]
    [InlineData("dev-1", "", "dev-1")]
    [InlineData("", "AA:BB", "AA:BB")]
    [InlineData("   ", "AA:BB", "AA:BB")]
    public void The_hub_key_prefers_the_device_id_over_the_address(string hubId, string address, string expected)
    {
        // Arrange
        var train = Train(hubId: hubId, bleAddress: address);

        // Act
        var key = TrainControlService.HubKey(train);

        // Assert
        Assert.Equal(expected, key);
    }

    [Fact]
    public void A_hub_is_the_same_device_when_its_id_matches_whatever_the_case()
    {
        // Arrange
        var train = Train(hubId: "DEV-1", bleAddress: "");

        // Act
        var same = TrainControlService.IsSameDevice(train, new DiscoveredHubDto("dev-1", null, null, null));

        // Assert
        Assert.True(same);
    }

    [Fact]
    public void A_hub_is_the_same_device_when_only_the_mac_matches()
    {
        // Arrange
        var train = Train(hubId: "", bleAddress: "aa:bb:cc:dd:ee:ff");

        // Act
        var same = TrainControlService.IsSameDevice(
            train, new DiscoveredHubDto("dev-9", null, "AA:BB:CC:DD:EE:FF", null));

        // Assert
        Assert.True(same);
    }

    [Fact]
    public void An_unrelated_hub_is_not_the_same_device()
    {
        // Arrange
        var train = Train(hubId: "dev-1", bleAddress: "AA:BB:CC:DD:EE:FF");

        // Act
        var same = TrainControlService.IsSameDevice(train, new DiscoveredHubDto("dev-9", null, "11:22", null));

        // Assert
        Assert.False(same);
    }

    private static TrainControlService Service(out FakeLegoService lego)
    {
        lego = new FakeLegoService();

        return new TrainControlService(lego, NullLogger<TrainControlService>.Instance);
    }

    private static TrainDto Train(string hubId = "dev-1", string bleAddress = "AA:BB:CC:DD:EE:FF") => new()
    {
        Id = Guid.CreateVersion7(),
        Name = "Blauer Zug",
        Hub = HubType.PoweredUpHub,
        HubId = hubId,
        BleAddress = bleAddress,
        Color = LedColorType.Green,
    };
}
