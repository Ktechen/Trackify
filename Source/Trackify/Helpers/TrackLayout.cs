namespace Trackify.Helpers;

/// <summary>A finished layout pass: every segment's geometry, already fitted to the canvas, plus the
/// ballast path drawn behind them all.</summary>
[ImplicitKeys(IsEnabled = false)]
public readonly record struct TrackLayout(
    IReadOnlyDictionary<Guid, SegmentGeometry> Segments,
    string BedPathData);
