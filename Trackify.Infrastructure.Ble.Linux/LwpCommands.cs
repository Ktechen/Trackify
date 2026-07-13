using SharpBrick.PoweredUp;
using SharpBrick.PoweredUp.Hubs;
using SharpBrick.PoweredUp.Protocol;
using SharpBrick.PoweredUp.Protocol.Messages;
using HubType = Trackify.Domain.Enums.HubType;

namespace Trackify.Infrastructure.Ble.Linux;

/// <summary>
/// LEGO Wireless Protocol commands built on SharpBrick's typed messages, for the BlueZ transport.
/// Mirrors the app's in-project protocol helper; kept here because the shared Application layer must
/// stay free of the SharpBrick dependency. See https://lego.github.io/lego-ble-wireless-protocol-docs/.
/// </summary>
internal static class LwpCommands
{
    private const byte SingleHub = 0; // one hub per protocol connection.

    public static Task StartPowerAsync(ILegoWirelessProtocol protocol, byte port, sbyte power)
        => protocol.SendPortOutputCommandAsync(new PortOutputCommandStartPowerMessage(
            port,
            PortOutputCommandStartupInformation.ExecuteImmediately,
            PortOutputCommandCompletionInformation.CommandFeedback,
            power)
        {
            HubId = SingleHub,
        });

    public static async Task SetRgbColorAsync(ILegoWirelessProtocol protocol, byte rgbPort, byte red, byte green, byte blue)
    {
        await protocol.SendMessageAsync(new PortInputFormatSetupSingleMessage(rgbPort, 0x01, 10000, false)
            { HubId = SingleHub });
        await protocol.SendPortOutputCommandAsync(new PortOutputCommandSetRgbColorNo2Message(
            rgbPort,
            PortOutputCommandStartupInformation.ExecuteImmediately,
            PortOutputCommandCompletionInformation.CommandFeedback,
            red, green, blue)
        {
            HubId = SingleHub,
        });
    }

    /// <summary>Connects the protocol, retrying the transient null-deref in SharpBrick's connect chain (sharpbrick/powered-up#188).</summary>
    public static async Task ConnectWithRetryAsync(ILegoWirelessProtocol protocol, CancellationToken ct)
    {
        const int maxAttempts = 3;
        for (var attempt = 1;; attempt++)
        {
            try
            {
                await protocol.ConnectAsync();
                return;
            }
            catch (Exception ex) when (ex is NullReferenceException or ArgumentNullException && attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(300), ct);
            }
            catch (Exception ex) when (ex is NullReferenceException or ArgumentNullException)
            {
                throw new InvalidOperationException(
                    "Connecting to the hub failed (unreachable or GATT not ready). Is it powered on and in range?", ex);
            }
        }
    }

    /// <summary>Maps the advertising "System Type" byte (manufacturer data byte 1) to a hub model, or null.</summary>
    public static HubType? MapHubType(byte[]? manufacturerData)
    {
        if (manufacturerData is null || manufacturerData.Length < 2)
            return null;

        try
        {
            var type = HubFactory.GetTypeFromSystemType((SystemType)manufacturerData[1]);
            return type.Name switch
            {
                nameof(TwoPortHub) => HubType.PoweredUpHub,
                nameof(TechnicMediumHub) => HubType.ControlPlusHub,
                nameof(MoveHub) => HubType.BoostMoveHub,
                nameof(DuploTrainBaseHub) => HubType.DuploTrainHub,
                nameof(TwoPortHandset) => HubType.PoweredUpRemote,
                _ => (HubType?)null,
            };
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }
}
