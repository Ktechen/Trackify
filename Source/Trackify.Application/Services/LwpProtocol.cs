#if __ANDROID__ || __IOS__ || WINDOWS
using SharpBrick.PoweredUp;
using SharpBrick.PoweredUp.Hubs;
using SharpBrick.PoweredUp.Protocol;
using SharpBrick.PoweredUp.Protocol.Messages;
using HubType = Trackify.Domain.Enums.HubType;

namespace Trackify.Application.Services;

/// <summary>
/// LEGO Wireless Protocol details shared by the platform hub services, kept in one place.
/// See https://lego.github.io/lego-ble-wireless-protocol-docs/.
/// Every hub is addressed as hub id 0 (one hub per protocol connection).
/// </summary>
internal static class LwpProtocol
{
    private const byte SingleHub = 0;

    /// <summary>
    /// Drives the motor on <paramref name="port"/>. Power: 1..100 forward, -1..-100 reverse,
    /// 0 = stop (float), 127 = stop (brake).
    /// </summary>
    public static Task StartPowerAsync(ILegoWirelessProtocol protocol, byte port, sbyte power)
        => protocol.SendPortOutputCommandAsync(new PortOutputCommandStartPowerMessage(
            port,
            PortOutputCommandStartupInformation.ExecuteImmediately,
            PortOutputCommandCompletionInformation.CommandFeedback,
            power)
        {
            HubId = SingleHub,
        });

    /// <summary>
    /// Connects the protocol, retrying a transient BLE/GATT hiccup. SharpBrick's BluetoothKernel
    /// connect chain isn't null-hardened (sharpbrick/powered-up#188), so it can throw on the first
    /// try; a short bounded retry makes connecting reliable. Throws a friendly error if it can't.
    /// </summary>
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
                    "Verbindung zum Hub fehlgeschlagen (nicht erreichbar oder GATT nicht bereit). " +
                    "Ist der Hub eingeschaltet und in Reichweite?", ex);
            }
        }
    }

    /// <summary>Sets the built-in RGB LED: select the absolute-RGB mode, then push the color.</summary>
    public static async Task SetRgbColorAsync(ILegoWirelessProtocol protocol, byte rgbPort, byte red, byte green,
        byte blue)
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

    /// <summary>
    /// Maps the advertising "System Type and Device Number" byte (manufacturer data byte 1) to a
    /// hub model, or null if unrecognized.
    /// </summary>
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
#endif
