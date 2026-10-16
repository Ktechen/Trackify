using Trackify.Application.Catalog;

namespace Trackify.Tests.Application;

/// <summary>
/// The catalogue the UI builds its pickers from. It is plain data, so what is worth pinning is that
/// every enum value a picker can hold is actually in it — a missing entry makes the lookups throw at
/// runtime, in front of the user.
/// </summary>
public class LegoinoCatalogTests
{
    [Fact]
    public void Every_list_has_entries_and_no_duplicate_values()
    {
        Assert.NotEmpty(LegoinoCatalog.Hubs);
        Assert.NotEmpty(LegoinoCatalog.Devices);
        Assert.NotEmpty(LegoinoCatalog.Colors);
        Assert.NotEmpty(LegoinoCatalog.SpeedFunctions);
        Assert.NotEmpty(LegoinoCatalog.Directions);
        Assert.NotEmpty(LegoinoCatalog.SensorTypes);
        Assert.NotEmpty(LegoinoCatalog.SensorActions);

        Assert.Equal(LegoinoCatalog.Hubs.Count, LegoinoCatalog.Hubs.Select(hub => hub.Value).Distinct().Count());
        Assert.Equal(LegoinoCatalog.Colors.Count, LegoinoCatalog.Colors.Select(color => color.Value).Distinct().Count());
    }

    [Fact]
    public void Every_hub_in_the_list_can_be_looked_up_and_has_a_name()
    {
        foreach (var hub in LegoinoCatalog.Hubs)
        {
            Assert.Equal(hub.Value, LegoinoCatalog.Hub(hub.Value).Value);
            Assert.False(string.IsNullOrWhiteSpace(hub.Label));
        }
    }

    [Fact]
    public void Every_device_type_can_be_looked_up()
    {
        foreach (var device in LegoinoCatalog.Devices)
        {
            Assert.Equal(device.Value, LegoinoCatalog.Device(device.Value).Value);
            Assert.False(string.IsNullOrWhiteSpace(device.Label));
        }
    }

    [Fact]
    public void Every_colour_has_a_six_digit_hex_a_picker_can_paint()
    {
        foreach (var colour in LegoinoCatalog.Colors)
        {
            Assert.Equal(colour.Value, LegoinoCatalog.Color(colour.Value).Value);
            Assert.StartsWith("#", colour.Hex, StringComparison.Ordinal);
            Assert.Equal(7, colour.Hex.Length);
        }
    }

    [Fact]
    public void Every_speed_function_has_a_formula_to_show_except_the_custom_one()
    {
        // Arrange
        var options = LegoinoCatalog.SpeedFunctions;

        foreach (var option in options)
        {
            // Act
            var lookedUp = LegoinoCatalog.SpeedFunction(option.Value);

            // Assert — Custom carries no formula on purpose: the user types their own.
            Assert.Equal(option.Value, lookedUp.Value);
            Assert.Equal(
                option.Value == SpeedFunctionType.Custom,
                string.IsNullOrWhiteSpace(option.Formula));
        }
    }

    [Fact]
    public void Every_speed_function_type_is_offered()
    {
        // The editor binds a picker straight to this list, so a missing enum value would be an
        // option the user can never choose again once a train already has it.
        Assert.Equal(
            Enum.GetValues<SpeedFunctionType>().ToHashSet(),
            LegoinoCatalog.SpeedFunctions.Select(option => option.Value).ToHashSet());
    }

    [Fact]
    public void Every_direction_sensor_type_and_sensor_action_is_offered()
    {
        Assert.Equal(
            Enum.GetValues<TrackDirection>().ToHashSet(),
            LegoinoCatalog.Directions.Select(option => option.Value).ToHashSet());
        Assert.Equal(
            Enum.GetValues<SensorType>().ToHashSet(),
            LegoinoCatalog.SensorTypes.Select(option => option.Value).ToHashSet());
        Assert.Equal(
            Enum.GetValues<SensorActionType>().ToHashSet(),
            LegoinoCatalog.SensorActions.Select(option => option.Value).ToHashSet());
    }

    [Fact]
    public void Looking_up_something_the_catalogue_does_not_have_throws_rather_than_returning_a_blank()
        => Assert.Throws<InvalidOperationException>(() => LegoinoCatalog.Hub((HubType)999));
}
