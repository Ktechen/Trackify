using System.Globalization;

namespace Trackify.Helpers;

/// <summary>
/// Builds the SVG path data for the speed-profile widget from accel/brake phase functions. This is a
/// presentation concern (pixel coordinates, path strings); the underlying math lives in
/// <see cref="Trackify.Domain.SpeedFunction"/>.
/// </summary>
public static class SpeedCurve
{
    public static SpeedProfileGraph BuildGraph(double maxNormalized, Func<double, double> accelAt, Func<double, double> brakeAt)
    {
        const double gw = 300, gh = 130, pl = 34, pr = 12, ptp = 12;
        const double plotW = gw - pl - pr, plotH = gh - ptp - 24;
        const double a = 0.34, bs = 0.66;

        double XAt(double u) => pl + plotW * u;
        double YAt(double v) => ptp + plotH * (1 - v);
        static double Clamp01(double v) => Math.Max(0, Math.Min(1, v));

        var points = new (double X, double Y)[65];
        for (var i = 0; i <= 64; i++)
        {
            var u = i / 64.0;
            double v;
            if (u < a) v = maxNormalized * accelAt(u / a);
            else if (u < bs) v = maxNormalized;
            else v = maxNormalized * (1 - brakeAt((u - bs) / (1 - bs)));
            points[i] = (XAt(u), YAt(Clamp01(v)));
        }

        string F(double d) => d.ToString("0.0", CultureInfo.InvariantCulture);

        var line = "M " + string.Join(" L ", points.Select(p => $"{F(p.X)} {F(p.Y)}"));
        var fill = $"{line} L {F(XAt(1))} {F(YAt(0))} L {F(XAt(0))} {F(YAt(0))} Z";

        return new SpeedProfileGraph(gw, gh, pl, gw - pr, ptp, XAt(a), XAt(bs), YAt(0), YAt(0.5), YAt(1), line, fill);
    }
}

public readonly record struct SpeedProfileGraph(
    double Width,
    double Height,
    double PlotLeft,
    double PlotRight,
    double PlotTop,
    double XAccelEnd,
    double XBrakeStart,
    double Y0,
    double YMid,
    double Y1,
    string LinePathData,
    string FillPathData);
