using Microsoft.EntityFrameworkCore;
using Trackify.Domain.Enums;
using Trackify.Infrastructure.Persistence;

namespace Trackify.Tests.Infrastructure;

public class EfTrainStoreTests
{
    // Non-pooling factory + Pooling=False so the SQLite file is fully released between stores and
    // the temp db can be deleted (a pooled connection would keep the file locked on Windows).
    private sealed class SimpleFactory(DbContextOptions<TrackifyDbContext> options) : IDbContextFactory<TrackifyDbContext>
    {
        public TrackifyDbContext CreateDbContext() => new(options);
    }

    private static EfTrainStore NewStore(string dbPath)
    {
        var options = new DbContextOptionsBuilder<TrackifyDbContext>()
            .UseSqlite($"Data Source={dbPath};Pooling=False")
            .Options;
        return new EfTrainStore(new SimpleFactory(options), NullLogger<EfTrainStore>.Instance);
    }

    [Fact]
    public async Task Load_returns_empty_for_a_fresh_database()
    {
        var path = TempDb();
        try
        {
            Assert.Empty(await NewStore(path).LoadAsync());
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public async Task Save_then_load_round_trips_all_fields()
    {
        var path = TempDb();
        try
        {
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

            await NewStore(path).SaveAsync([original]);
            var loaded = await NewStore(path).LoadAsync(); // fresh store → reads from disk

            Assert.Equal(original, Assert.Single(loaded));
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public async Task Save_replaces_the_previous_set()
    {
        var path = TempDb();
        try
        {
            await NewStore(path).SaveAsync([new TrainConfig { Id = "a", Name = "A" }, new TrainConfig { Id = "b", Name = "B" }]);
            await NewStore(path).SaveAsync([new TrainConfig { Id = "c", Name = "C" }]);

            var loaded = await NewStore(path).LoadAsync();
            Assert.Equal("c", Assert.Single(loaded).Id);
        }
        finally
        {
            Delete(path);
        }
    }

    private static string TempDb() => Path.Combine(Path.GetTempPath(), $"trackify-{Guid.NewGuid():N}.db");

    private static void Delete(string path)
    {
        try { File.Delete(path); } catch (IOException) { /* left for the OS temp cleanup */ }
    }
}
