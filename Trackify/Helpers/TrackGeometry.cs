using System.Globalization;
using System.Text;
using Trackify.Models.Trains.Enums;

namespace Trackify.Helpers;

/// <summary>Builds the static stadium-shaped 8-segment track layout (900x600 canvas) used by the Streckenplaner.</summary>
public static class TrackGeometry
{
    private const double StraightLength = 400;
    private const double CurveRadius = 180;
    private static readonly double ArcLength = Math.PI * CurveRadius;
    private static readonly double TotalLength = 2 * StraightLength + 2 * ArcLength;

    public static readonly IReadOnlyList<string> SegmentIds =
    [
        "SEG-1", "SEG-2", "SEG-3", "SEG-4", "SEG-5", "SEG-6", "SEG-7", "SEG-8",
    ];

    public static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>
    {
        ["SEG-1"] = "Gerade Nordwest",
        ["SEG-2"] = "Bahnhof Nord",
        ["SEG-3"] = "Kurve Ost (oben)",
        ["SEG-4"] = "Kurve Ost (unten)",
        ["SEG-5"] = "Gerade Südost",
        ["SEG-6"] = "Gerade Südwest",
        ["SEG-7"] = "Kurve West (unten)",
        ["SEG-8"] = "Kurve West (oben)",
    };

    public static readonly IReadOnlyDictionary<string, SegmentType> Types = new Dictionary<string, SegmentType>
    {
        ["SEG-1"] = SegmentType.Straight,
        ["SEG-2"] = SegmentType.Station,
        ["SEG-3"] = SegmentType.Curve,
        ["SEG-4"] = SegmentType.Curve,
        ["SEG-5"] = SegmentType.Straight,
        ["SEG-6"] = SegmentType.Straight,
        ["SEG-7"] = SegmentType.Curve,
        ["SEG-8"] = SegmentType.Curve,
    };

    public static string BuildTrackBed()
    {
        var n = (int)Math.Ceiling(TotalLength / 8);
        var sb = new StringBuilder();
        for (var i = 0; i <= n; i++)
        {
            var p = PointAt(TotalLength * i / n);
            AppendPoint(sb, i > 0, p.X, p.Y);
        }
        sb.Append(" Z");
        return sb.ToString();
    }

    public static IReadOnlyList<SegmentGeometry> BuildSegments()
    {
        var arc = ArcLength;
        var l = StraightLength;
        var defs = new (string Id, double A, double B)[]
        {
            ("SEG-1", 0, l / 2), ("SEG-2", l / 2, l),
            ("SEG-3", l, l + arc / 2), ("SEG-4", l + arc / 2, l + arc),
            ("SEG-5", l + arc, l + arc + l / 2), ("SEG-6", l + arc + l / 2, 2 * l + arc),
            ("SEG-7", 2 * l + arc, 2 * l + arc + arc / 2), ("SEG-8", 2 * l + arc + arc / 2, TotalLength),
        };

        var result = new List<SegmentGeometry>(defs.Length);
        foreach (var (id, a, b) in defs)
        {
            var mid = PointAt((a + b) / 2);
            result.Add(new SegmentGeometry(id, Build(a, b, 12, 8), Build(a, b, 0, 12), mid.X, mid.Y, mid.OutX, mid.OutY, mid.TanX, mid.TanY));
        }
        return result;
    }

    private static TrackPoint PointAt(double d)
    {
        d = ((d % TotalLength) + TotalLength) % TotalLength;
        double x, y, ox, oy, tx, ty;
        if (d <= StraightLength)
        {
            x = 250 + d; y = 120; ox = 0; oy = -1; tx = 1; ty = 0;
        }
        else if (d <= StraightLength + ArcLength)
        {
            var th = -Math.PI / 2 + (d - StraightLength) / CurveRadius;
            x = 650 + CurveRadius * Math.Cos(th); y = 300 + CurveRadius * Math.Sin(th);
            ox = Math.Cos(th); oy = Math.Sin(th); tx = -Math.Sin(th); ty = Math.Cos(th);
        }
        else if (d <= 2 * StraightLength + ArcLength)
        {
            var dd = d - StraightLength - ArcLength;
            x = 650 - dd; y = 480; ox = 0; oy = 1; tx = -1; ty = 0;
        }
        else
        {
            var th = Math.PI / 2 + (d - 2 * StraightLength - ArcLength) / CurveRadius;
            x = 250 + CurveRadius * Math.Cos(th); y = 300 + CurveRadius * Math.Sin(th);
            ox = Math.Cos(th); oy = Math.Sin(th); tx = -Math.Sin(th); ty = Math.Cos(th);
        }
        return new TrackPoint(x, y, ox, oy, tx, ty);
    }

    private static string Build(double a, double b, double trim, double step)
    {
        var s = a + trim;
        var e = b - trim;
        var n = Math.Max(1, (int)Math.Ceiling((e - s) / step));
        var sb = new StringBuilder();
        for (var i = 0; i <= n; i++)
        {
            var p = PointAt(s + (e - s) * i / n);
            AppendPoint(sb, i > 0, p.X, p.Y);
        }
        return sb.ToString();
    }

    private static void AppendPoint(StringBuilder sb, bool isLineTo, double x, double y)
    {
        sb.Append(isLineTo ? " L " : "M ");
        sb.Append(x.ToString("0.0", CultureInfo.InvariantCulture));
        sb.Append(' ');
        sb.Append(y.ToString("0.0", CultureInfo.InvariantCulture));
    }

    private readonly record struct TrackPoint(double X, double Y, double OutX, double OutY, double TanX, double TanY);
}

[ImplicitKeys(IsEnabled = false)]
public readonly record struct SegmentGeometry(
    string Id,
    string PathData,
    string HitPathData,
    double MidX,
    double MidY,
    double OutwardX,
    double OutwardY,
    double TanX,
    double TanY)
{
}
