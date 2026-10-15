using Trackify.Application.TrackPlans;
using Trackify.Domain.Tracks;
using Trackify.Tests.Fakes;

namespace Trackify.Tests.Application;

/// <summary>
/// The import/export path: build it in the app, carry it in a file, read it in the CLI. A plan file
/// comes from outside, so most of these are about refusing a bad one clearly rather than guessing.
/// </summary>
public class TrackPlanExchangeServiceTests
{
    [Fact]
    public async Task A_plan_survives_a_round_trip_through_the_file_format()
    {
        var service = new TrackPlanExchangeService(new FakeTrackPlanRepository());

        var imported = await service.ImportAsync(OvalJson(), replace: true);
        var exported = await service.ExportAsync("Kleines Oval");
        var reimported = await service.ImportAsync(exported, replace: true);

        Assert.True(imported.Succeeded);
        Assert.True(reimported.Succeeded);
        Assert.Equal(imported.PartCount, reimported.PartCount);
        Assert.Equal(imported.ConnectionCount, reimported.ConnectionCount);
        Assert.Equal(imported.OpenEndCount, reimported.OpenEndCount);
    }

    [Fact]
    public async Task A_points_sensor_and_routing_rule_survive_the_round_trip()
    {
        var repository = new FakeTrackPlanRepository();
        var service = new TrackPlanExchangeService(repository);

        await service.ImportAsync(OvalJson(), replace: true);
        var exported = await service.ExportAsync();
        await service.ImportAsync(exported, replace: true);

        var point = Assert.Single(repository.Stored.Parts, part => part.Switch is not null);
        var config = point.Switch!;

        Assert.Equal("90:84:2B:00:11:22", config.MotorHubId);
        Assert.Equal("A", config.MotorPort);
        Assert.Equal(SensorType.Color, config.SensorType);
        Assert.Equal(LedColorType.Red, config.SensorTrigger);
        Assert.True(config.IsMotorised);
        Assert.True(config.HasSensor);

        var rule = Assert.Single(config.EntryRules);
        Assert.Equal(TrackPortNames.Toe, rule.FromPort);
        Assert.Equal(SwitchRoute.Branch, rule.SetRoute);
    }

    [Fact]
    public void A_routing_rule_decides_which_way_the_point_is_thrown()
    {
        var target = Guid.CreateVersion7();
        var config = new SwitchConfig
        {
            DefaultRoute = SwitchRoute.Main,
            EntryRules =
            [
                new SwitchEntryRule
                {
                    FromPort = TrackPortNames.Toe,
                    TowardPartId = target,
                    SetRoute = SwitchRoute.Branch,
                },
            ],
        };

        Assert.Equal(SwitchRoute.Branch, config.ResolveRoute(TrackPortNames.Toe, target));
        Assert.Equal(SwitchRoute.Main, config.ResolveRoute(TrackPortNames.Toe, Guid.CreateVersion7()));
        Assert.Equal(SwitchRoute.Main, config.ResolveRoute(TrackPortNames.Main, target));
    }

    [Fact]
    public void A_destination_specific_rule_wins_over_a_catch_all_one()
    {
        var target = Guid.CreateVersion7();
        var config = new SwitchConfig
        {
            DefaultRoute = SwitchRoute.Main,
            EntryRules =
            [
                // Catch-all first, so list order alone cannot be what makes the specific rule win.
                new SwitchEntryRule { FromPort = TrackPortNames.Toe, SetRoute = SwitchRoute.Main },
                new SwitchEntryRule
                {
                    FromPort = TrackPortNames.Toe,
                    TowardPartId = target,
                    SetRoute = SwitchRoute.Branch,
                },
            ],
        };

        Assert.Equal(SwitchRoute.Branch, config.ResolveRoute(TrackPortNames.Toe, target));
        Assert.Equal(SwitchRoute.Main, config.ResolveRoute(TrackPortNames.Toe));
    }

    [Theory]
    [InlineData("", "empty")]
    [InlineData("{ not json", "not valid JSON")]
    public void A_file_that_is_not_a_plan_is_refused_with_a_reason(string json, string expectedFragment)
    {
        var result = new TrackPlanExchangeService(new FakeTrackPlanRepository()).Validate(json);

        Assert.False(result.Succeeded);
        Assert.Contains(expectedFragment, string.Join(" ", result.Errors), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_json_file_that_is_not_a_track_plan_is_refused()
    {
        var result = new TrackPlanExchangeService(new FakeTrackPlanRepository())
            .Validate("""{ "format": "something.else", "version": 1 }""");

        Assert.False(result.Succeeded);
        Assert.Contains("Not a track plan", string.Join(" ", result.Errors), StringComparison.Ordinal);
    }

    [Fact]
    public void A_future_format_version_is_refused_rather_than_read_hopefully()
    {
        var result = new TrackPlanExchangeService(new FakeTrackPlanRepository())
            .Validate("""{ "format": "trackify.trackplan", "version": 99 }""");

        Assert.False(result.Succeeded);
        Assert.Contains("not supported", string.Join(" ", result.Errors), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void An_unknown_track_part_is_named_in_the_error()
    {
        // R104 is a community radius, deliberately not in the catalogue — so this must be refused,
        // not silently turned into the nearest part LEGO does make.
        var result = new TrackPlanExchangeService(new FakeTrackPlanRepository()).Validate(
            """
            {
              "format": "trackify.trackplan", "version": 1,
              "parts": [ { "id": "p1", "part": "curve-r104-right" } ],
              "connections": []
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Contains("curve-r104-right", string.Join(" ", result.Errors), StringComparison.Ordinal);
    }

    [Fact]
    public void A_port_that_the_part_does_not_have_is_refused()
    {
        var result = new TrackPlanExchangeService(new FakeTrackPlanRepository()).Validate(
            """
            {
              "format": "trackify.trackplan", "version": 1,
              "parts": [
                { "id": "p1", "part": "straight" },
                { "id": "p2", "part": "straight" }
              ],
              "connections": [ { "from": "p1:branch", "to": "p2:start" } ]
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Contains("has no port 'branch'", string.Join(" ", result.Errors), StringComparison.Ordinal);
    }

    [Fact]
    public void A_port_connected_twice_is_refused()
    {
        var result = new TrackPlanExchangeService(new FakeTrackPlanRepository()).Validate(
            """
            {
              "format": "trackify.trackplan", "version": 1,
              "parts": [
                { "id": "p1", "part": "straight" },
                { "id": "p2", "part": "straight" },
                { "id": "p3", "part": "straight" }
              ],
              "connections": [
                { "from": "p1:end", "to": "p2:start" },
                { "from": "p1:end", "to": "p3:start" }
              ]
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Contains("connected more than once", string.Join(" ", result.Errors), StringComparison.Ordinal);
    }

    [Fact]
    public void A_malformed_endpoint_is_refused()
    {
        var result = new TrackPlanExchangeService(new FakeTrackPlanRepository()).Validate(
            """
            {
              "format": "trackify.trackplan", "version": 1,
              "parts": [ { "id": "p1", "part": "straight" } ],
              "connections": [ { "from": "p1", "to": "p1:start" } ]
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Contains("'part:port'", string.Join(" ", result.Errors), StringComparison.Ordinal);
    }

    [Fact]
    public void A_loop_that_does_not_really_close_imports_with_a_warning()
    {
        // Fifteen curves claimed as a closed circle: importable (you may be mid-build), but the
        // planner has to say so rather than store the claim silently.
        var result = new TrackPlanExchangeService(new FakeTrackPlanRepository()).Validate(RingJson(15));

        Assert.True(result.Succeeded);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains("does not really close", string.Join(" ", result.Warnings), StringComparison.Ordinal);
    }

    [Fact]
    public void Sixteen_curves_claimed_as_a_circle_import_without_complaint()
    {
        var result = new TrackPlanExchangeService(new FakeTrackPlanRepository()).Validate(RingJson(16));

        Assert.True(result.Succeeded);
        Assert.Empty(result.Warnings);
        Assert.Equal(0, result.OpenEndCount);
    }

    [Fact]
    public async Task Importing_without_replace_keeps_what_was_already_stored()
    {
        var repository = new FakeTrackPlanRepository();
        var service = new TrackPlanExchangeService(repository);

        await service.ImportAsync(OvalJson(), replace: true);
        await service.ImportAsync(OvalJson(), replace: false);

        Assert.Equal(1, repository.ReplaceCalls);
        Assert.Equal(1, repository.AppendCalls);
        Assert.Equal(8, repository.Stored.Parts.Count);

        // The appended parts must not fight the originals for the same positions.
        Assert.Equal(8, repository.Stored.Parts.Select(part => part.PlanOrder).Distinct().Count());
    }

    [Fact]
    public async Task The_summary_counts_what_the_plan_needs()
    {
        var service = new TrackPlanExchangeService(new FakeTrackPlanRepository());

        await service.ImportAsync(OvalJson(), replace: true);
        var summary = await service.SummariseAsync();

        Assert.Equal(4, summary.PartCount);
        Assert.Equal(3, summary.ConnectionCount);
        Assert.Equal(1, summary.SwitchCount);
        Assert.Equal(1, summary.MotorisedSwitchCount);
        Assert.True(summary.IsBuildable);
        Assert.Equal(2, summary.PartsByKey[TrackPartCatalog.CurveRightKey]);
        Assert.Equal(
            summary.TotalLengthStuds * TrackMetrics.MillimetresPerStud / 1000,
            summary.TotalLengthMetres,
            6);
    }

    [Fact]
    public async Task An_empty_store_summarises_as_empty_rather_than_failing()
    {
        var summary = await new TrackPlanExchangeService(new FakeTrackPlanRepository()).SummariseAsync();

        Assert.Equal(0, summary.PartCount);
        Assert.True(summary.IsBuildable);
    }

    // A straight, two right curves and a wired-up left point — the same file the CLI run used.
    private static string OvalJson() =>
        """
        {
          "format": "trackify.trackplan",
          "version": 1,
          "units": "stud",
          "name": "Kleines Oval",
          "parts": [
            { "id": "p1", "part": "straight", "name": "Gerade 1" },
            { "id": "p2", "part": "curve-r40-right", "name": "Kurve rechts 1" },
            { "id": "p3", "part": "curve-r40-right", "name": "Kurve rechts 2" },
            { "id": "p4", "part": "switch-left", "name": "Weiche 1",
              "switch": {
                "defaultRoute": "Main",
                "motorHubId": "90:84:2B:00:11:22",
                "motorPort": "A",
                "sensorType": "Color",
                "sensorHubId": "90:84:2B:00:11:22",
                "sensorPort": "C",
                "sensorTrigger": "Red",
                "entryRules": [ { "fromPort": "toe", "towardPart": "p1", "setRoute": "Branch" } ]
              } }
          ],
          "connections": [
            { "from": "p1:end", "to": "p2:start" },
            { "from": "p2:end", "to": "p3:start" },
            { "from": "p3:end", "to": "p4:toe" }
          ]
        }
        """;

    // `count` right curves wired into a ring, whether or not that many actually close one.
    private static string RingJson(int count)
    {
        var parts = Enumerable.Range(1, count)
            .Select(index => $"{{ \"id\": \"p{index}\", \"part\": \"curve-r40-right\" }}");
        var connections = Enumerable.Range(1, count)
            .Select(index => $"{{ \"from\": \"p{index}:end\", \"to\": \"p{(index % count) + 1}:start\" }}");

        return $$"""
            {
              "format": "trackify.trackplan",
              "version": 1,
              "parts": [ {{string.Join(", ", parts)}} ],
              "connections": [ {{string.Join(", ", connections)}} ]
            }
            """;
    }
}
