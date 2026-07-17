namespace Trackify.Helpers;

/// <summary>Geometry + SVG path data for the speed-profile widget, produced by <see cref="SpeedCurve.BuildGraph"/>.</summary>
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
