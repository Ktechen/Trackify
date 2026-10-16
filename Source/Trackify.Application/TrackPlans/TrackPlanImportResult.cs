namespace Trackify.Application.TrackPlans;

/// <summary>
/// What came of reading a plan file. Errors mean nothing was imported; warnings mean it was, but
/// something is worth saying — a loop that does not really close, most usefully.
/// </summary>
public sealed record TrackPlanImportResult
{
    /// <summary>Whether the file was good enough to import.</summary>
    public bool Succeeded { get; init; }

    /// <summary>The plan's name from the file.</summary>
    public string PlanName { get; init; } = "";

    /// <summary>How many parts it holds.</summary>
    public int PartCount { get; init; }

    /// <summary>How many connections it holds.</summary>
    public int ConnectionCount { get; init; }

    /// <summary>How many ports are still free.</summary>
    public int OpenEndCount { get; init; }

    /// <summary>Why it could not be imported.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Things that are true but worth knowing.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>A refusal carrying the reasons.</summary>
    public static TrackPlanImportResult Failed(params string[] errors)
        => new() { Succeeded = false, Errors = errors };
}
