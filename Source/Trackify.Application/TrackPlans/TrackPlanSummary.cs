namespace Trackify.Application.TrackPlans;

/// <summary>What the saved plan currently looks like — the shape behind <c>trackify track show</c>.</summary>
public sealed record TrackPlanSummary
{
    /// <summary>A plan with nothing in it.</summary>
    public static TrackPlanSummary Empty { get; } = new();

    /// <summary>Total parts laid.</summary>
    public int PartCount { get; init; }

    /// <summary>Total connections.</summary>
    public int ConnectionCount { get; init; }

    /// <summary>Ports with nothing plugged into them.</summary>
    public int OpenEndCount { get; init; }

    /// <summary>Points in the plan.</summary>
    public int SwitchCount { get; init; }

    /// <summary>Points that have a motor assigned and so can actually be thrown.</summary>
    public int MotorisedSwitchCount { get; init; }

    /// <summary>Parts carrying a sensor.</summary>
    public int SensorCount { get; init; }

    /// <summary>Total rail, in studs.</summary>
    public double TotalLengthStuds { get; init; }

    /// <summary>
    /// The same length in metres. Converted here rather than by the caller so a front-end never has
    /// to reach into the Domain's track metrics to print it.
    /// </summary>
    public double TotalLengthMetres => TotalLengthStuds * TrackMetrics.MillimetresPerStud / 1000;

    /// <summary>Whether every claimed connection actually works geometrically.</summary>
    public bool IsBuildable { get; init; } = true;

    /// <summary>How many of each catalogue part the plan needs — the shopping list.</summary>
    public IReadOnlyDictionary<string, int> PartsByKey { get; init; } = new Dictionary<string, int>();

    /// <summary>Joints the geometry contradicts, described for a person.</summary>
    public IReadOnlyList<string> Problems { get; init; } = [];
}
