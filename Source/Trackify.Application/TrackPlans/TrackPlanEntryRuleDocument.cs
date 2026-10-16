namespace Trackify.Application.TrackPlans;

/// <summary>"A train coming in here, bound for there, needs the point set this way."</summary>
public sealed record TrackPlanEntryRuleDocument
{
    /// <summary>Port the train arrives by: "toe", "main" or "branch".</summary>
    public string FromPort { get; init; } = "";

    /// <summary>Optional document-local id of the part the train is headed for.</summary>
    public string? TowardPart { get; init; }

    /// <summary>How the point must be set.</summary>
    public SwitchRoute SetRoute { get; init; } = SwitchRoute.Main;
}
