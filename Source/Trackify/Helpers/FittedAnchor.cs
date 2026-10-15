namespace Trackify.Helpers;

/// <summary>
/// An open end as the canvas shows it — the big tappable dot a drag starts from. Carries the part
/// and port it belongs to so a gesture can be handed straight back to the geometry layer.
/// </summary>
[ImplicitKeys(IsEnabled = false)]
public readonly record struct FittedAnchor(Guid PartId, string PortName, double X, double Y);
