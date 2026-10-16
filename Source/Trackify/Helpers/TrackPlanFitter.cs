namespace Trackify.Helpers;

/// <summary>
/// Turns a walked plan (world studs) into something the canvas can draw: path data per part, the
/// open-end dots, and the ballast underneath.
///
/// Two deliberate properties:
/// <list type="bullet">
/// <item>Coordinates are baked in here, not applied as a <c>RenderTransform</c> — a
/// <c>{Binding}</c> inside a transform has no DataContext to inherit on every Uno target.</item>
/// <item>The view is computed from the <em>committed</em> parts only, so the plan does not shift and
/// rescale under the user's finger while a ghost preview grows past its edge.</item>
/// </list>
/// Replaces the old <c>TrackGeometry</c>, which walked a chain of segments with invented lengths; the
/// walking now happens in the Domain, over real parts, and only the fitting is left here.
/// </summary>
public static class TrackPlanFitter
{
    /// <summary>The fixed coordinate space the canvas draws in; zoom scales it via a Viewbox.</summary>
    public const double CanvasWidth = 900;

    /// <inheritdoc cref="CanvasWidth" />
    public const double CanvasHeight = 600;

    /// <summary>Kept free on every side, so labels and sensor markers hanging off the centreline stay on.</summary>
    private const double Margin = 70;

    /// <summary>
    /// How big track is drawn when the plan is small enough not to need shrinking: a 16-stud
    /// straight comes out 80 px long, which is a comfortable finger target.
    /// </summary>
    private const double DefaultPixelsPerStud = 5.0;

    /// <summary>Gap left between neighbouring parts so a plan reads as parts, not one smear of rail.</summary>
    private const double TrimPixels = 5;

    /// <summary>Samples per arc when bounding it — a quarter of an R40 circle deviates well under a pixel.</summary>
    private const int ArcSamples = 8;

    /// <summary>
    /// Lays <paramref name="layout"/> out on the canvas. Parts whose id is in
    /// <paramref name="previewPartIds"/> are marked as preview and excluded from the fit.
    /// </summary>
    public static FittedPlan Fit(TrackPlanLayout layout, IReadOnlyCollection<Guid>? previewPartIds = null)
    {
        if (layout.Placements.Count == 0) return FittedPlan.Empty;

        var preview = previewPartIds ?? [];
        var committed = layout.Placements.Values.Where(placement => !preview.Contains(placement.PartId)).ToList();
        var view = BuildView(committed.Count > 0 ? committed : [.. layout.Placements.Values]);

        var pieces = new List<FittedPiece>(layout.Placements.Count);
        var bed = new List<string>(committed.Count);
        var ghost = new List<string>();

        foreach (var placement in layout.Placements.Values)
        {
            var isPreview = preview.Contains(placement.PartId);
            var piece = Emit(placement, view, isPreview);

            pieces.Add(piece);
            (isPreview ? ghost : bed).Add(piece.HitPathData);
        }

        return new FittedPlan
        {
            Pieces = pieces,
            Anchors =
            [
                .. layout.OpenEnds.Select(end => new FittedAnchor(
                    end.PartId, end.PortName, view.X(end.WorldPose.X), view.Y(end.WorldPose.Y))),
            ],
            BedPathData = string.Join(" ", bed),
            PreviewPathData = string.Join(" ", ghost),
            StudsPerPixel = 1 / view.Scale,
            OriginX = view.OffsetX,
            OriginY = view.OffsetY,
        };
    }

    // Centres the plan and shrinks it (never enlarges past the default) so it stays inside the margin.
    private static View BuildView(IReadOnlyList<TrackPartPlacement> placements)
    {
        var extent = Extent.Of(placements.SelectMany(Outline));
        var scale = Math.Min(
            DefaultPixelsPerStud,
            Math.Min(
                (CanvasWidth - (2 * Margin)) / Math.Max(extent.MaxX - extent.MinX, 1),
                (CanvasHeight - (2 * Margin)) / Math.Max(extent.MaxY - extent.MinY, 1)));

        return new View(
            scale,
            (CanvasWidth / 2) - (scale * (extent.MinX + extent.MaxX) / 2),
            (CanvasHeight / 2) - (scale * (extent.MinY + extent.MaxY) / 2));
    }

    // Enough world points to bound a part: a straight's two ends, an arc's sampled sweep.
    private static IEnumerable<(double X, double Y)> Outline(TrackPartPlacement placement)
    {
        foreach (var span in placement.Spans)
        {
            if (!span.IsArc)
            {
                var end = span.End;
                yield return (span.Start.X, span.Start.Y);
                yield return (end.X, end.Y);
                continue;
            }

            var (centreX, centreY, from, to) = (span.CentreX, span.CentreY, span.StartAngle, span.EndAngle);

            for (var step = 0; step <= ArcSamples; step++)
            {
                var angle = from + ((to - from) * step / ArcSamples);
                yield return (
                    centreX + (span.RadiusStuds * Math.Cos(angle)),
                    centreY + (span.RadiusStuds * Math.Sin(angle)));
            }
        }
    }

    private static FittedPiece Emit(TrackPartPlacement placement, in View view, bool isPreview)
    {
        var drawn = new List<string>(placement.Spans.Count);
        var hit = new List<string>(placement.Spans.Count);

        foreach (var span in placement.Spans)
        {
            drawn.Add(Path(span, view, TrimPixels));
            hit.Add(Path(span, view, 0));
        }

        // The first span is the part's main run, so its middle is where a label belongs: along a
        // straight, along the main route of a point, along the first route of a crossing.
        var (midX, midY, tangent) = Middle(placement.Spans[0]);
        var tanX = Math.Cos(tangent);
        var tanY = Math.Sin(tangent);

        return new FittedPiece(
            placement.PartId,
            string.Join(" ", drawn),
            string.Join(" ", hit),
            MidX: view.X(midX),
            MidY: view.Y(midY),
            // Left-hand normal of travel, for straights and curves alike, so labels and sensor
            // markers stay on one side of the track throughout.
            OutwardX: tanY,
            OutwardY: -tanX,
            TanX: tanX,
            TanY: tanY,
            IsPreview: isPreview);
    }

    private static (double X, double Y, double Tangent) Middle(in TrackSpan span)
    {
        if (!span.IsArc)
        {
            var mid = span.Start.Advance(span.LengthStuds / 2);
            return (mid.X, mid.Y, span.Start.HeadingRadians);
        }

        var midAngle = span.StartAngle + ((span.EndAngle - span.StartAngle) / 2);

        return (
            span.CentreX + (span.RadiusStuds * Math.Cos(midAngle)),
            span.CentreY + (span.RadiusStuds * Math.Sin(midAngle)),
            span.Start.HeadingRadians + (span.SweepSteps * TrackMetrics.RadiansPerHeadingStep / 2));
    }

    // The trim is applied after scaling, so the gap between parts stays the same few pixels at any
    // zoom rather than growing with the track.
    private static string Path(in TrackSpan span, in View view, double trim)
    {
        if (!span.IsArc)
        {
            var start = span.Start;
            var end = span.End;

            return Line(view.X(start.X), view.Y(start.Y), view.X(end.X), view.Y(end.Y), trim);
        }

        var radius = span.RadiusStuds * view.Scale;
        var trimAngle = radius > 0 ? trim / radius : 0;
        var sign = span.Sign;

        return Arc(
            view.X(span.CentreX),
            view.Y(span.CentreY),
            radius,
            span.StartAngle + (sign * trimAngle),
            span.EndAngle - (sign * trimAngle),
            sign);
    }

    private static string Line(double x0, double y0, double x1, double y1, double trim)
    {
        if (trim > 0)
        {
            var length = Math.Sqrt(((x1 - x0) * (x1 - x0)) + ((y1 - y0) * (y1 - y0)));

            if (length > trim * 2)
            {
                var t = trim / length;
                (x0, y0, x1, y1) = (
                    x0 + ((x1 - x0) * t), y0 + ((y1 - y0) * t),
                    x1 - ((x1 - x0) * t), y1 - ((y1 - y0) * t));
            }
        }

        return FormattableString.Invariant($"M {x0:0.0} {y0:0.0} L {x1:0.0} {y1:0.0}");
    }

    private static string Arc(double centreX, double centreY, double radius, double from, double to, int sign)
    {
        var startX = centreX + (radius * Math.Cos(from));
        var startY = centreY + (radius * Math.Sin(from));
        var endX = centreX + (radius * Math.Cos(to));
        var endY = centreY + (radius * Math.Sin(to));

        return FormattableString.Invariant(
            $"M {startX:0.0} {startY:0.0} A {radius:0.0},{radius:0.0} 0 0,{(sign > 0 ? 1 : 0)} {endX:0.0} {endY:0.0}");
    }

    /// <summary>The studs-to-canvas transform the fit settled on.</summary>
    private readonly record struct View(double Scale, double OffsetX, double OffsetY)
    {
        public double X(double studs) => (studs * Scale) + OffsetX;

        public double Y(double studs) => (studs * Scale) + OffsetY;
    }

    private readonly record struct Extent(double MinX, double MinY, double MaxX, double MaxY)
    {
        public static Extent Of(IEnumerable<(double X, double Y)> points)
        {
            var (minX, minY, maxX, maxY) = (double.MaxValue, double.MaxValue, double.MinValue, double.MinValue);

            foreach (var (x, y) in points)
            {
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }

            return minX > maxX ? new Extent(0, 0, 0, 0) : new Extent(minX, minY, maxX, maxY);
        }
    }
}
