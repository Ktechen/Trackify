using Microsoft.EntityFrameworkCore;
using Trackify.Domain.Tracks;
using Trackify.Infrastructure.Persistence;

namespace Trackify.Tests.Infrastructure;

/// <summary>
/// The plan store against a real SQLite file. A plan is an aggregate, so what is worth proving is
/// that parts and connections survive together — including the point configuration, which is stored
/// as a nested JSON column rather than its own table.
/// </summary>
public class SqliteTrackPlanRepositoryTests
{
    // Non-pooling factory + Pooling=False so the file is released between repositories and the temp
    // db can be deleted (a pooled connection keeps it locked on Windows).
    private sealed class SimpleFactory(DbContextOptions<TrackifyDbContext> options) : IDbContextFactory<TrackifyDbContext>
    {
        public TrackifyDbContext CreateDbContext() => new(options);
    }

    [Fact]
    public async Task A_fresh_database_holds_no_plan()
        => await WithStore(async repository => Assert.Empty((await repository.LoadAsync()).Parts));

    [Fact]
    public async Task Replace_then_Load_round_trips_parts_and_connections()
        => await WithStore(async repository =>
        {
            await repository.ReplaceAsync(Plan());
            var loaded = await repository.LoadAsync();

            Assert.Equal(2, loaded.Parts.Count);
            Assert.Single(loaded.Links);
            Assert.True(loaded.ToLayout().IsBuildable);
        });

    [Fact]
    public async Task A_points_configuration_survives_the_json_column()
        => await WithStore(async repository =>
        {
            var point = new TrackPart
            {
                SpecKey = TrackPartCatalog.SwitchLeftKey,
                Name = "Weiche 1",
                Switch = new SwitchConfig
                {
                    DefaultRoute = SwitchRoute.Branch,
                    MotorHubId = "90:84:2B:00:11:22",
                    MotorPort = "A",
                    SensorType = SensorType.Color,
                    SensorHubId = "90:84:2B:00:11:22",
                    SensorPort = "C",
                    SensorTrigger = LedColorType.Red,
                    EntryRules = [new SwitchEntryRule { FromPort = TrackPortNames.Toe, SetRoute = SwitchRoute.Branch }],
                },
            };

            await repository.ReplaceAsync(new TrackPlanSnapshot { Parts = [point], Links = [] });
            var stored = Assert.Single((await repository.LoadAsync()).Parts);

            Assert.NotNull(stored.Switch);
            Assert.Equal(SwitchRoute.Branch, stored.Switch!.DefaultRoute);
            Assert.Equal("A", stored.Switch.MotorPort);
            Assert.Equal(LedColorType.Red, stored.Switch.SensorTrigger);
            Assert.True(stored.Switch.IsMotorised);
            Assert.True(stored.Switch.HasSensor);

            var rule = Assert.Single(stored.Switch.EntryRules);
            Assert.Equal(TrackPortNames.Toe, rule.FromPort);
            Assert.Equal(SwitchRoute.Branch, rule.SetRoute);
        });

    [Fact]
    public async Task Replace_throws_the_previous_plan_away()
        => await WithStore(async repository =>
        {
            await repository.ReplaceAsync(Plan());
            await repository.ReplaceAsync(new TrackPlanSnapshot
            {
                Parts = [new TrackPart { SpecKey = TrackPartCatalog.StationKey, Name = "Bahnhof 1" }],
                Links = [],
            });

            var loaded = await repository.LoadAsync();

            Assert.Equal(TrackPartCatalog.StationKey, Assert.Single(loaded.Parts).SpecKey);
            Assert.Empty(loaded.Links);
        });

    [Fact]
    public async Task Append_keeps_what_was_there_and_does_not_reuse_its_positions()
        => await WithStore(async repository =>
        {
            await repository.ReplaceAsync(Plan());
            Assert.Equal(2, (await repository.LoadAsync()).Parts.Count);

            await repository.AppendAsync(Plan());

            var loaded = await repository.LoadAsync();

            Assert.Equal(4, loaded.Parts.Count);
            Assert.Equal(2, loaded.Links.Count);
            // Appended parts continue past the existing positions rather than reusing them.
            Assert.Equal([0, 1, 2, 3], loaded.Parts.Select(part => part.PlanOrder));
        });

    [Fact]
    public async Task Parts_come_back_in_plan_order()
        => await WithStore(async repository =>
        {
            await repository.ReplaceAsync(new TrackPlanSnapshot
            {
                Parts =
                [
                    new TrackPart { SpecKey = TrackPartCatalog.StraightKey, Name = "zweite", PlanOrder = 1 },
                    new TrackPart { SpecKey = TrackPartCatalog.StationKey, Name = "erste", PlanOrder = 0 },
                ],
                Links = [],
            });

            var loaded = await repository.LoadAsync();

            Assert.Equal("erste", loaded.Parts[0].Name);
            Assert.Equal("zweite", loaded.Parts[1].Name);
        });

    [Fact]
    public async Task Behaviour_fields_round_trip_with_their_enum_names()
        => await WithStore(async repository =>
        {
            await repository.ReplaceAsync(new TrackPlanSnapshot
            {
                Parts =
                [
                    new TrackPart
                    {
                        SpecKey = TrackPartCatalog.StraightKey,
                        MaxSpeed = 42,
                        Direction = TrackDirection.Reverse,
                        AccelFn = SpeedFunctionType.SCurve,
                        BrakeFn = SpeedFunctionType.Exponential,
                        Sensor = SensorType.Distance,
                        Action = SensorActionType.ReverseDirection,
                        SlowTarget = 17,
                    },
                ],
                Links = [],
            });

            var part = Assert.Single((await repository.LoadAsync()).Parts);

            Assert.Equal(42, part.MaxSpeed);
            Assert.Equal(TrackDirection.Reverse, part.Direction);
            Assert.Equal(SpeedFunctionType.SCurve, part.AccelFn);
            Assert.Equal(SpeedFunctionType.Exponential, part.BrakeFn);
            Assert.Equal(SensorType.Distance, part.Sensor);
            Assert.Equal(SensorActionType.ReverseDirection, part.Action);
            Assert.Equal(17, part.SlowTarget);
        });

    private static TrackPlanSnapshot Plan()
    {
        var first = new TrackPart { SpecKey = TrackPartCatalog.StraightKey, Name = "Gerade 1", PlanOrder = 0 };
        var second = new TrackPart { SpecKey = TrackPartCatalog.CurveRightKey, Name = "Kurve rechts 1", PlanOrder = 1 };

        return new TrackPlanSnapshot
        {
            Parts = [first, second],
            Links =
            [
                new TrackLink
                {
                    FromPartId = first.Id,
                    FromPort = TrackPortNames.End,
                    ToPartId = second.Id,
                    ToPort = TrackPortNames.Start,
                },
            ],
        };
    }

    private static async Task WithStore(Func<SqliteTrackPlanRepository, Task> body)
    {
        var path = Path.Combine(Path.GetTempPath(), $"trackify-plan-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<TrackifyDbContext>()
            .UseSqlite($"Data Source={path};Pooling=False")
            .Options;

        try
        {
            await body(new SqliteTrackPlanRepository(new SimpleFactory(options)));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
