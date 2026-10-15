using Trackify.Application.TrackPlans;

namespace Trackify.Tests.Fakes;

/// <summary>Scriptable stand-in for the plan exchange, so a CLI command can be driven without a store.</summary>
public sealed class FakeTrackPlanExchangeService : ITrackPlanExchangeService
{
    /// <summary>What <see cref="Validate"/> and <see cref="ImportAsync"/> report back.</summary>
    public TrackPlanImportResult Result { get; set; } = new() { Succeeded = true };

    /// <summary>What <see cref="SummariseAsync"/> reports back.</summary>
    public TrackPlanSummary Summary { get; set; } = TrackPlanSummary.Empty;

    /// <summary>What <see cref="ExportAsync"/> hands out.</summary>
    public string Exported { get; set; } = "{}";

    public List<string> Validated { get; } = [];

    public List<(string Json, bool Replace)> Imported { get; } = [];

    public List<string> ExportNames { get; } = [];

    public TrackPlanImportResult Validate(string json)
    {
        Validated.Add(json);

        return Result;
    }

    public Task<TrackPlanImportResult> ImportAsync(
        string json, bool replace, CancellationToken cancellationToken = default)
    {
        Imported.Add((json, replace));

        return Task.FromResult(Result);
    }

    public Task<string> ExportAsync(string planName = "", CancellationToken cancellationToken = default)
    {
        ExportNames.Add(planName);

        return Task.FromResult(Exported);
    }

    public Task<TrackPlanSummary> SummariseAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Summary);
}
