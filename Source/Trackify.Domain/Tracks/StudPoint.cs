namespace Trackify.Domain.Tracks;

/// <summary>A plain point on the plan, in studs — what a drawing gesture is made of.</summary>
public readonly record struct StudPoint(double X, double Y)
{
    /// <summary>Distance to <paramref name="other"/>.</summary>
    public double DistanceTo(in StudPoint other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;

        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>This point as a pose facing <paramref name="heading"/>.</summary>
    public StudPose WithHeading(int heading) => new(X, Y, heading);
}
