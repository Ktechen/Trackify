namespace Trackify.Application.Catalog;

/// <summary>
/// Selectable track-part option for the Streckenplaner's "Teil anhängen" toolbar (Gerade/Kurve
/// links/Kurve rechts/Weiche/Bahnhof) — the segment <see cref="SegmentType"/> plus, for a curve, which
/// way it bends.
/// </summary>
public sealed record TrackPartOption(SegmentType Type, CurveDirection? Curve, string Label);
