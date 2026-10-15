namespace Trackify.Helpers;

/// <summary>
/// One laid part, ready to draw: its rail as canvas path data plus the anchor points the labels and
/// sensor markers hang off. Everything is a plain pre-computed coordinate, because a
/// <c>{Binding}</c> inside a <c>RenderTransform</c> has no DataContext to inherit on every target.
/// </summary>
[ImplicitKeys(IsEnabled = false)]
public readonly record struct FittedPiece(
    Guid PartId,
    string PathData,
    string HitPathData,
    double MidX,
    double MidY,
    double OutwardX,
    double OutwardY,
    double TanX,
    double TanY,
    bool IsPreview);
