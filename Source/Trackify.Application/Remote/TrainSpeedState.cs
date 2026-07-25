namespace Trackify.Application.Remote;

/// <summary>The last speed applied to a hub, so a freshly-connected client can show current state.</summary>
public sealed record TrainSpeedState(string HubId, int Speed);
