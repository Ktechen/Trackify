using Microsoft.EntityFrameworkCore;
using Trackify.Domain.Enums;
using Trackify.Infrastructure.Persistence;

namespace Trackify.Tests.Infrastructure;

public class SqliteTrainRepositoryTests
{
    // Non-pooling factory + Pooling=False so the SQLite file is fully released between repositories
    // and the temp db can be deleted (a pooled connection would keep the file locked on Windows).
    private sealed class SimpleFactory(DbContextOptions<TrackifyDbContext> options) : IDbContextFactory<TrackifyDbContext>
    {
        public TrackifyDbContext CreateDbContext() => new(options);
    }

    private static SqliteTrainRepository NewRepository(string dbPath)
    {
        var options = new DbContextOptionsBuilder<TrackifyDbContext>()
            .UseSqlite($"Data Source={dbPath};Pooling=False")
            .Options;
        return new SqliteTrainRepository(new SimpleFactory(options));
    }

    [Fact]
    public async Task GetAll_is_empty_for_a_fresh_database()
    {
        var path = TempDb();
        try
        {
            Assert.Empty(await NewRepository(path).GetAllAsync());
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public async Task Add_then_GetById_round_trips_all_fields()
    {
        var path = TempDb();
        try
        {
            var original = new Train
            {
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

            await NewRepository(path).AddAsync(original);
            var loaded = await NewRepository(path).GetByIdAsync(original.Id); // fresh repo → reads from disk

            Assert.Equal(original, loaded);
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public async Task AddRange_then_GetAll_returns_all()
    {
        var path = TempDb();
        try
        {
            await NewRepository(path).AddRangeAsync([new Train { Name = "A" }, new Train { Name = "B" }]);
            Assert.Equal(2, (await NewRepository(path).GetAllAsync()).Count);
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public async Task Update_changes_the_stored_entity()
    {
        var path = TempDb();
        try
        {
            var train = new Train { Name = "Old", Speed = 10 };
            await NewRepository(path).AddAsync(train);

            train.Name = "New";
            train.Speed = 80;
            Assert.True(await NewRepository(path).UpdateAsync(train));

            var loaded = await NewRepository(path).GetByIdAsync(train.Id);
            Assert.Equal("New", loaded?.Name);
            Assert.Equal(80, loaded?.Speed);
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public async Task Delete_removes_the_entity()
    {
        var path = TempDb();
        try
        {
            var train = new Train { Name = "Doomed" };
            await NewRepository(path).AddAsync(train);

            Assert.True(await NewRepository(path).DeleteAsync(train.Id));
            Assert.Null(await NewRepository(path).GetByIdAsync(train.Id));
            Assert.False(await NewRepository(path).DeleteAsync(train.Id)); // already gone
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
