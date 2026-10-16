namespace Trackify.Domain.Tracks;

/// <summary>
/// One place a part can be plugged into another, in the part's own local frame. The pose faces
/// <em>outward</em> from the part, so joining two ports means one pose is the other's
/// <see cref="StudPose.Reversed"/> — see <see cref="StudPose.Joins"/>.
/// </summary>
public readonly record struct TrackPort(string Name, StudPose LocalPose);
