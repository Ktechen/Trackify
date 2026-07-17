using Trackify.Infrastructure.Persistence;

namespace Trackify.Tests.Infrastructure;

public class JsonTrainStoreTests
{
    [Fact]
    public async Task Load_returns_empty_when_file_is_missing()
    {
        var store = new JsonTrainStore(Path.Combine(Path.GetTempPath(), $"trackify-missing-{Guid.NewGuid():N}.json"));
        Assert.Empty(await store.LoadAsync());
    }

    [Fact]
    public async Task Save_then_load_round_trips_all_fields()
    {
        var path = Path.Combine(Path.GetTempPath(), $"trackify-store-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonTrainStore(path);
            var original = new TrainConfig
            {
                Id = "trn-1",
                Name = "Blauer Zug",
                Hub = HubType.ControlPlusHub,
                BleAddress = "90:84:2B:11:22:33",
                HubId = "device-abc",
                Color = LedColorType.Blue,
                PortA = DeviceType.TrainMotor,
                PortB = DeviceType.Light,
                Speed = -40,
                AccelFn = SpeedFunctionType.Custom,
                AccelExpression = "x^2",
                BrakeFn = SpeedFunctionType.SCurve,
                IsActive = false,
            };

            await store.SaveAsync([original]);
            var loaded = await store.LoadAsync();

            var only = Assert.Single(loaded);
            Assert.Equal(original, only);
            Assert.Contains("ControlPlusHub", await File.ReadAllTextAsync(path)); // enums persisted as names
        }
        finally
        {
            File.Delete(path);
        }
    }
}
