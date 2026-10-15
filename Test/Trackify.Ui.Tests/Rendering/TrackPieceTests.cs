using Trackify.Domain.Enums;
using Trackify.Domain.Tracks;
using Trackify.Helpers;
using Trackify.Models.Trains;
using Xunit;

namespace Trackify.Ui.Tests.Rendering;

/// <summary>
/// What the canvas actually shows per part: its colour, its German label, and where the speed label
/// and sensor marker are placed.
/// </summary>
public class TrackPieceTests
{
    [Theory]
    [InlineData(0, "#E5484D")]
    [InlineData(20, "#E5701C")]
    [InlineData(50, "#F5A623")]
    [InlineData(70, "#2FAE4A")]
    [InlineData(100, "#16A34A")]
    public void The_colour_follows_the_top_speed(int maxSpeed, string expected)
    {
        var piece = new TrackPiece { Id = Guid.NewGuid(), MaxSpeed = maxSpeed };

        Assert.Equal(expected, piece.SpeedColor);
    }

    [Theory]
    [InlineData(TrackPartCatalog.StraightKey, "Gerade")]
    [InlineData(TrackPartCatalog.StationKey, "Bahnhof")]
    [InlineData(TrackPartCatalog.CurveLeftKey, "Kurve links")]
    [InlineData(TrackPartCatalog.CurveRightKey, "Kurve rechts")]
    [InlineData(TrackPartCatalog.SwitchLeftKey, "Weiche links")]
    [InlineData(TrackPartCatalog.SwitchRightKey, "Weiche rechts")]
    [InlineData(TrackPartCatalog.CrossingKey, "Kreuzung")]
    public void Every_catalogue_part_has_a_German_label(string specKey, string expected)
    {
        var piece = new TrackPiece { Id = Guid.NewGuid(), SpecKey = specKey };

        Assert.Equal(expected, piece.TypeLabel);
    }

    [Fact]
    public void Only_a_point_counts_as_a_switch()
    {
        Assert.True(new TrackPiece { Id = Guid.NewGuid(), SpecKey = TrackPartCatalog.SwitchLeftKey }.IsSwitch);
        Assert.True(new TrackPiece { Id = Guid.NewGuid(), SpecKey = TrackPartCatalog.SwitchRightKey }.IsSwitch);
        Assert.False(new TrackPiece { Id = Guid.NewGuid(), SpecKey = TrackPartCatalog.CurveLeftKey }.IsSwitch);
        Assert.False(new TrackPiece { Id = Guid.NewGuid(), SpecKey = TrackPartCatalog.CrossingKey }.IsSwitch);
    }

    [Fact]
    public void A_sensor_shows_a_marker_and_a_kind_of_its_own()
    {
        var piece = new TrackPiece { Id = Guid.NewGuid() };

        Assert.False(piece.HasSensor);

        piece.Sensor = SensorType.Color;

        Assert.True(piece.HasSensor);
        Assert.Equal("#0A54C9", piece.SensorColor);
        Assert.Equal("F", piece.SensorGlyph);
        Assert.Contains("COLOR_DISTANCE_SENSOR", piece.SensorToken, StringComparison.Ordinal);

        piece.Sensor = SensorType.Distance;

        Assert.Equal("#E8730C", piece.SensorColor);
        Assert.Equal("A", piece.SensorGlyph);
    }

    [Fact]
    public void The_slow_target_only_matters_when_the_sensor_slows_the_train()
    {
        var piece = new TrackPiece { Id = Guid.NewGuid(), Action = SensorActionType.Notify };

        Assert.False(piece.ShowSlowTarget);

        piece.Action = SensorActionType.Slower;

        Assert.True(piece.ShowSlowTarget);
    }

    [Fact]
    public void The_label_and_the_sensor_marker_sit_on_opposite_sides_of_the_track()
    {
        var piece = new TrackPiece { Id = Guid.NewGuid() };

        // A piece running along +X with its outward normal pointing up. The label is offset
        // *against* that normal and the sensor *along* it, which is what keeps the two from ever
        // landing on top of each other.
        piece.ApplyGeometry(new FittedPiece(
            piece.Id,
            PathData: "M 0 0 L 80 0",
            HitPathData: "M 0 0 L 80 0",
            MidX: 400,
            MidY: 300,
            OutwardX: 0,
            OutwardY: -1,
            TanX: 1,
            TanY: 0,
            IsPreview: false));

        Assert.True(piece.SensorTop < 300, "The sensor marker follows the outward normal, so it sits above.");
        Assert.True(piece.LabelTop > 300, "The speed label is offset against it, so it sits below.");
        Assert.Equal("M 0 0 L 80 0", piece.PathData);
        Assert.False(piece.IsPreview);
    }

    [Fact]
    public void The_speed_profile_follows_the_top_speed()
    {
        var piece = new TrackPiece { Id = Guid.NewGuid(), MaxSpeed = 50 };
        var slow = piece.Graph;

        piece.MaxSpeed = 100;

        Assert.NotEqual(slow.LinePathData, piece.Graph.LinePathData);
    }
}
