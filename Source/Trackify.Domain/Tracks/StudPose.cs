namespace Trackify.Domain.Tracks;

/// <summary>
/// A position and facing on the track plan, in studs. <paramref name="Heading"/> is a 22.5 degree
/// step 0..15 (see <see cref="TrackMetrics.HeadingCount"/>), not an angle: LEGO track can only ever
/// point in those 16 directions, so keeping it an integer makes a heading comparison exact. The
/// position cannot be made exact the same way — a curve's end lands on
/// 40*sin(22.5 degrees) = 15.3073... studs — which is why positions are compared with
/// <see cref="TrackMetrics.JoinToleranceStuds"/> and never rounded to a grid.
///
/// X runs right, Y runs down (matching the canvas), so an increasing heading turns clockwise on
/// screen and <see cref="CurveDirection.Right"/> is a positive sweep.
/// </summary>
public readonly record struct StudPose(double X, double Y, int Heading)
{
    /// <summary>The plan's anchor: origin, pointing along +X.</summary>
    public static StudPose Origin => new(0, 0, 0);

    /// <summary>This heading as an angle in radians.</summary>
    public double HeadingRadians => Heading * TrackMetrics.RadiansPerHeadingStep;

    /// <summary>Unit vector along the heading.</summary>
    public double DirectionX => Math.Cos(HeadingRadians);

    /// <inheritdoc cref="DirectionX" />
    public double DirectionY => Math.Sin(HeadingRadians);

    /// <summary>
    /// Reads this pose as a coordinate frame and maps <paramref name="local"/> into it — the one
    /// operation that places a part: a part's ports and spans are authored in its own local frame,
    /// and composing them with the part's world anchor yields world geometry.
    /// </summary>
    public StudPose Compose(in StudPose local)
    {
        var cos = Math.Cos(HeadingRadians);
        var sin = Math.Sin(HeadingRadians);

        return new StudPose(
            X + (local.X * cos) - (local.Y * sin),
            Y + (local.X * sin) + (local.Y * cos),
            TrackMetrics.NormalizeHeading(Heading + local.Heading));
    }

    /// <summary>
    /// The frame that undoes this one: <c>pose.Compose(pose.Inverse())</c> is the origin. Used to
    /// place a part <em>by one of its ports</em> — given where the port must end up, the part's
    /// anchor is that target composed with the port's inverse local pose.
    /// </summary>
    public StudPose Inverse()
    {
        var cos = Math.Cos(HeadingRadians);
        var sin = Math.Sin(HeadingRadians);

        return new StudPose(
            -((X * cos) + (Y * sin)),
            (X * sin) - (Y * cos),
            TrackMetrics.NormalizeHeading(-Heading));
    }

    /// <summary>
    /// Same point, opposite facing. Ports face <em>outward</em> from their part, so two ports are
    /// plugged together exactly when one's pose is the other's <see cref="Reversed"/>.
    /// </summary>
    public StudPose Reversed() => this with
    {
        Heading = TrackMetrics.NormalizeHeading(Heading + (TrackMetrics.HeadingCount / 2)),
    };

    /// <summary>Moves <paramref name="studs"/> along the heading, keeping the facing.</summary>
    public StudPose Advance(double studs) => this with
    {
        X = X + (DirectionX * studs),
        Y = Y + (DirectionY * studs),
    };

    /// <summary>Distance between the two positions, ignoring heading.</summary>
    public double DistanceTo(in StudPose other) => Math.Sqrt(DistanceSquaredTo(other));

    /// <summary>Squared distance — for comparisons and inner loops, where the square root is waste.</summary>
    public double DistanceSquaredTo(in StudPose other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;

        return (dx * dx) + (dy * dy);
    }

    /// <summary>
    /// Whether a port at this pose plugs into a port at <paramref name="other"/>: positions within
    /// <paramref name="toleranceStuds"/> and headings exactly opposite.
    /// </summary>
    public bool Joins(in StudPose other, double toleranceStuds = TrackMetrics.JoinToleranceStuds)
        => Heading == TrackMetrics.NormalizeHeading(other.Heading + (TrackMetrics.HeadingCount / 2))
            && DistanceSquaredTo(other) <= toleranceStuds * toleranceStuds;

    /// <summary>Whether both poses describe the same place and facing, within tolerance.</summary>
    public bool Matches(in StudPose other, double toleranceStuds = TrackMetrics.JoinToleranceStuds)
        => Heading == other.Heading && DistanceSquaredTo(other) <= toleranceStuds * toleranceStuds;
}
