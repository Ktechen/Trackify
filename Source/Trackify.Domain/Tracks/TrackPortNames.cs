namespace Trackify.Domain.Tracks;

/// <summary>
/// The port names used across the part catalogue. They are plain lowercase strings because they
/// travel verbatim through the exchange format ("p1:end"), where a readable name beats an enum
/// ordinal that would silently change meaning if the enum were ever reordered.
/// </summary>
public static class TrackPortNames
{
    /// <summary>Entry end of a straight, curve or station.</summary>
    public const string Start = "start";

    /// <summary>Exit end of a straight, curve or station.</summary>
    public const string End = "end";

    /// <summary>A point's single end — where the two routes meet.</summary>
    public const string Toe = "toe";

    /// <summary>A point's straight-through end.</summary>
    public const string Main = "main";

    /// <summary>A point's diverging end.</summary>
    public const string Branch = "branch";

    /// <summary>Crossing, first route, entry end.</summary>
    public const string CrossA1 = "a1";

    /// <summary>Crossing, first route, exit end.</summary>
    public const string CrossA2 = "a2";

    /// <summary>Crossing, second route, entry end.</summary>
    public const string CrossB1 = "b1";

    /// <summary>Crossing, second route, exit end.</summary>
    public const string CrossB2 = "b2";
}
