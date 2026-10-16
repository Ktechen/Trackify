namespace Trackify.Domain.Tracks;

/// <summary>
/// One drawable stretch of rail: a straight when <see cref="SweepSteps"/> is 0, otherwise an arc of
/// <see cref="RadiusStuds"/> turning <see cref="SweepSteps"/> heading steps. Most parts are a single
/// span; a point needs two for its diverging route (a straight then a curve), and a crossing needs
/// one per route.
///
/// A span carries its start pose, so moving it into world coordinates is just composing that one
/// pose with the part's anchor — see <see cref="ToFrame"/>.
/// </summary>
public readonly record struct TrackSpan(StudPose Start, double LengthStuds, double RadiusStuds, int SweepSteps)
{
    /// <summary>A straight span of <paramref name="lengthStuds"/> from <paramref name="start"/>.</summary>
    public static TrackSpan Straight(StudPose start, double lengthStuds) => new(start, lengthStuds, 0, 0);

    /// <summary>
    /// An arc from <paramref name="start"/> of <paramref name="radiusStuds"/>, turning
    /// <paramref name="sweepSteps"/> heading steps (positive = right/clockwise on screen).
    /// </summary>
    public static TrackSpan Arc(StudPose start, double radiusStuds, int sweepSteps)
        => new(start, radiusStuds * Math.Abs(sweepSteps) * TrackMetrics.RadiansPerHeadingStep, radiusStuds, sweepSteps);

    /// <summary>Whether this is a curve rather than a straight.</summary>
    public bool IsArc => SweepSteps != 0;

    /// <summary>+1 for a right-hand curve, -1 for left, 0 for a straight.</summary>
    public int Sign => Math.Sign(SweepSteps);

    /// <summary>X of the arc's centre; meaningless for a straight.</summary>
    public double CentreX => Start.X + (RadiusStuds * Math.Cos(Start.HeadingRadians + (Sign * Math.PI / 2)));

    /// <inheritdoc cref="CentreX" />
    public double CentreY => Start.Y + (RadiusStuds * Math.Sin(Start.HeadingRadians + (Sign * Math.PI / 2)));

    /// <summary>Angle from the arc's centre to its start point.</summary>
    public double StartAngle => Math.Atan2(Start.Y - CentreY, Start.X - CentreX);

    /// <summary>Angle from the arc's centre to its end point.</summary>
    public double EndAngle => StartAngle + (SweepSteps * TrackMetrics.RadiansPerHeadingStep);

    /// <summary>Where this span leaves off — the next span's or port's pose.</summary>
    public StudPose End
    {
        get
        {
            if (!IsArc) return Start.Advance(LengthStuds);

            var (centreX, centreY, endAngle) = (CentreX, CentreY, EndAngle);

            return new StudPose(
                centreX + (RadiusStuds * Math.Cos(endAngle)),
                centreY + (RadiusStuds * Math.Sin(endAngle)),
                TrackMetrics.NormalizeHeading(Start.Heading + SweepSteps));
        }
    }

    /// <summary>
    /// The same span seen from <paramref name="frame"/> — used to lift a part's locally authored
    /// spans into world coordinates once the part has an anchor.
    /// </summary>
    public TrackSpan ToFrame(in StudPose frame) => this with { Start = frame.Compose(Start) };
}
