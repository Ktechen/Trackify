namespace Trackify.Application.Remote;

/// <summary>
/// SignalR method names for the train hub — shared by server and client so the two never drift.
/// The first group is invoked by clients (real-time control), the second is broadcast by the server
/// (so every connected client sees live speed). All are keyed by <c>hubId</c>, matching ILegoService.
/// </summary>
public static class TrainHubMethods
{
    // client → server (real-time control)
    public const string SetSpeed = nameof(SetSpeed);
    public const string SetLed = nameof(SetLed);
    public const string Stop = nameof(Stop);

    // server → client (broadcasts)
    public const string SpeedChanged = nameof(SpeedChanged);
}
