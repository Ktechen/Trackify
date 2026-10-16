using Trackify.Application.Trains;

namespace Trackify.Tests.Application;

/// <summary>
/// The Domain/DTO boundary for trains. A field forgotten in either direction silently loses a
/// user's setting, which is exactly the kind of bug a round-trip test catches and nothing else does.
/// </summary>
public class TrainMappingTests
{
    [Fact]
    public void A_train_survives_the_round_trip_through_its_dto()
    {
        var train = new Train
        {
            Name = "Blauer Zug",
            Hub = HubType.ControlPlusHub,
            BleAddress = "AA:BB:CC:DD:EE:FF",
            HubId = "dev-1",
            Color = LedColorType.Purple,
            PortA = DeviceType.TrainMotor,
            PortB = DeviceType.ColorDistanceSensor,
            Speed = 63,
            AccelFn = SpeedFunctionType.SCurve,
            AccelExpression = "1-(1-x)^2",
            BrakeFn = SpeedFunctionType.Exponential,
            BrakeExpression = "x^3",
            IsActive = true,
        };

        var round = train.ToDto().ToEntity();

        Assert.Equal(train.Id, round.Id);
        Assert.Equal(train.Name, round.Name);
        Assert.Equal(train.Hub, round.Hub);
        Assert.Equal(train.BleAddress, round.BleAddress);
        Assert.Equal(train.HubId, round.HubId);
        Assert.Equal(train.Color, round.Color);
        Assert.Equal(train.PortA, round.PortA);
        Assert.Equal(train.PortB, round.PortB);
        Assert.Equal(train.Speed, round.Speed);
        Assert.Equal(train.AccelFn, round.AccelFn);
        Assert.Equal(train.AccelExpression, round.AccelExpression);
        Assert.Equal(train.BrakeFn, round.BrakeFn);
        Assert.Equal(train.BrakeExpression, round.BrakeExpression);
        Assert.Equal(train.IsActive, round.IsActive);
    }

    [Fact]
    public void A_default_train_round_trips_too()
    {
        // Arrange
        var train = new Train();

        // Act
        var round = train.ToDto().ToEntity();

        // Assert — a fresh train is active by default, so the round trip has to preserve that.
        Assert.Equal("", round.Name);
        Assert.Equal(train.IsActive, round.IsActive);
        Assert.Equal(train.Hub, round.Hub);
    }
}
