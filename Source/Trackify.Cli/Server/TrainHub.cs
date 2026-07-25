using Microsoft.AspNetCore.SignalR;
using Trackify.Application.Lego;
using Trackify.Application.Remote;

namespace Trackify.Cli.Server;

/// <summary>
/// Real-time train control over SignalR: clients set speed/LED by <c>hubId</c> and every client gets a
/// live <c>SpeedChanged</c> broadcast. Forwards straight to <see cref="ILegoService"/> — the same
/// control seam the app uses locally — so nothing about the control logic differs over the network.
/// </summary>
public sealed class TrainHub(ILegoService lego, TrainStateStore state) : Hub
{
    public async Task SetSpeed(string hubId, int port, int power)
    {
        var clamped = Math.Clamp(power, -100, 100);
        await lego.SetSpeedAsync(hubId, (byte)port, (sbyte)clamped, Context.ConnectionAborted);
        state.SetSpeed(hubId, clamped);
        await Clients.All.SendAsync(TrainHubMethods.SpeedChanged, hubId, clamped);
    }

    public Task Stop(string hubId, int port) => SetSpeed(hubId, port, 0);

    public Task SetLed(string hubId, int red, int green, int blue)
        => lego.SetLedAsync(hubId, (byte)red, (byte)green, (byte)blue, Context.ConnectionAborted);
}
