namespace Trackify.Application.TrackPlans;

/// <summary>
/// Moves a track plan between the app and a file — the "build it in the UI, import it into the CLI"
/// path. Its whole surface is DTOs and documents, so a front-end never needs a Domain type.
/// </summary>
public interface ITrackPlanExchangeService
{
    /// <summary>Checks a plan file without touching the store, for <c>--dry-run</c>.</summary>
    TrackPlanImportResult Validate(string json);

    /// <summary>
    /// Imports a plan file. <paramref name="replace"/> swaps the saved plan for this one; otherwise
    /// the parts are added alongside what is already stored.
    /// </summary>
    Task<TrackPlanImportResult> ImportAsync(string json, bool replace, CancellationToken cancellationToken = default);

    /// <summary>Writes the saved plan out as a plan file.</summary>
    Task<string> ExportAsync(string planName = "", CancellationToken cancellationToken = default);

    /// <summary>Describes the saved plan: counts, the parts needed, and anything geometrically wrong.</summary>
    Task<TrackPlanSummary> SummariseAsync(CancellationToken cancellationToken = default);
}
