namespace Trackify.Domain.Tracks;

/// <summary>
/// A way a train can run through a part, from one port to another. Straights and curves have one;
/// a point has two (<see cref="TrackPortNames.Main"/> and <see cref="TrackPortNames.Branch"/>, which
/// is what its stored route setting selects); a crossing has two that never meet.
/// </summary>
public readonly record struct TrackRoute(string Name, string FromPort, string ToPort);
