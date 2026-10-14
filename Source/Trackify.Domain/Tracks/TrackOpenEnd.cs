namespace Trackify.Domain.Tracks;

/// <summary>
/// A port with nothing plugged into it — where the plan can still grow. These are what the editor
/// draws as the big tappable "Andockpunkte" and what a drag starts from.
/// </summary>
public readonly record struct TrackOpenEnd(Guid PartId, string PortName, StudPose WorldPose);
