namespace Trackify.Domain.Tracks;

/// <summary>
/// The real LEGO track dimensions, in studs. Every number here is a physical property of the moulded
/// parts rather than a drawing choice — the sources are recorded in arc42 section 9.
/// </summary>
public static class TrackMetrics
{
    /// <summary>One stud is 8 mm; the standard gauge is 5 studs (40 mm).</summary>
    public const double MillimetresPerStud = 8.0;

    /// <summary>
    /// LEGO curves sweep 22.5 degrees, so exactly 16 of them close a circle. Every heading a track
    /// can point in is therefore a multiple of 22.5 degrees — which is why a heading is stored as an
    /// integer step 0..15 and compared exactly, never as a float.
    /// </summary>
    public const int HeadingCount = 16;

    /// <summary>22.5 degrees.</summary>
    public const double DegreesPerHeadingStep = 360.0 / HeadingCount;

    /// <summary>22.5 degrees in radians.</summary>
    public const double RadiansPerHeadingStep = Math.PI * 2 / HeadingCount;

    /// <summary>R40 — LEGO's one and only curve radius since 1966, measured to the track centreline.</summary>
    public const double CurveRadiusStuds = 40.0;

    /// <summary>A full straight: 16 studs / 128 mm.</summary>
    public const double StraightStuds = 16.0;

    /// <summary>A point is 32 studs long on its main route — exactly two straights.</summary>
    public const double SwitchStuds = 32.0;

    /// <summary>The 90 degree crossing is a 16x16 stud square.</summary>
    public const double CrossingStuds = 16.0;

    /// <summary>
    /// How close two port positions must be to count as plugged together. Positions accumulate
    /// through sines of 22.5 degrees and are therefore irrational, so they are compared with a
    /// tolerance — while headings, being integers, are compared exactly.
    /// </summary>
    public const double JoinToleranceStuds = 0.3;

    /// <summary>Normalises a heading step into 0..15, so -1 reads as 15 and 16 as 0.</summary>
    public static int NormalizeHeading(int heading) => ((heading % HeadingCount) + HeadingCount) % HeadingCount;

    /// <summary>
    /// The shortest signed turn from <paramref name="from"/> to <paramref name="to"/>, in -8..7.
    /// Signed and shortest because the useful statement is "one step short", which a plain 0..15
    /// difference would report as 15.
    /// </summary>
    public static int ShortestHeadingStep(int from, int to)
    {
        var difference = NormalizeHeading(from - to);

        return difference > HeadingCount / 2 ? difference - HeadingCount : difference;
    }
}
