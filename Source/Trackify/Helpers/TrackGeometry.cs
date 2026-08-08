using System.Globalization;
using System.Text;

namespace Trackify.Helpers;

/// <summary>Builds the static stadium-shaped 8-segment track layout (900x600 canvas) used by the Streckenplaner.</summary>
public static class TrackGeometry
{
    private const double StraightLength = 400;
    private const double CurveRadius = 180;
    private static readonly double ArcLength = Math.PI * CurveRadius;
    private static readonly double TotalLength = 2 * StraightLength + 2 * ArcLength;

    // Segment ids, in travel order around the loop. Named constants so the four tables below
    // (ids, names, types, arc-length ranges) can't drift apart on a typo.
    public const string Seg1 = "SEG-1";
    public const string Seg2 = "SEG-2";
    public const string Seg3 = "SEG-3";
    public const string Seg4 = "SEG-4";
    public const string Seg5 = "SEG-5";
    public const string Seg6 = "SEG-6";
    public const string Seg7 = "SEG-7";
    public const string Seg8 = "SEG-8";

    public static readonly IReadOnlyList<string> SegmentIds =
    [
        Seg1, Seg2, Seg3, Seg4, Seg5, Seg6, Seg7, Seg8,
    ];

    public static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>
    {
        [Seg1] = "Gerade Nordwest",
        [Seg2] = "Bahnhof Nord",
        [Seg3] = "Kurve Ost (oben)",
        [Seg4] = "Kurve Ost (unten)",
        [Seg5] = "Gerade Südost",
        [Seg6] = "Gerade Südwest",
        [Seg7] = "Kurve West (unten)",
        [Seg8] = "Kurve West (oben)",
    };

    public static readonly IReadOnlyDictionary<string, SegmentType> Types = new Dictionary<string, SegmentType>
    {
        [Seg1] = SegmentType.Straight,
        [Seg2] = SegmentType.Station,
        [Seg3] = SegmentType.Curve,
        [Seg4] = SegmentType.Curve,
        [Seg5] = SegmentType.Straight,
        [Seg6] = SegmentType.Straight,
        [Seg7] = SegmentType.Curve,
        [Seg8] = SegmentType.Curve,
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
            (Seg1, 0, l / 2), (Seg2, l / 2, l),
            (Seg3, l, l + arc / 2), (Seg4, l + arc / 2, l + arc),
            (Seg5, l + arc, l + arc + l / 2), (Seg6, l + arc + l / 2, 2 * l + arc),
            (Seg7, 2 * l + arc, 2 * l + arc + arc / 2), (Seg8, 2 * l + arc + arc / 2, TotalLength),
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
