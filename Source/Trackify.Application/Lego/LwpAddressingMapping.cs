
namespace Trackify.Application.Lego;

/// <summary>
/// Transport-agnostic LEGO Wireless Protocol addressing helpers (no BLE stack, no SharpBrick).
/// See https://lego.github.io/lego-ble-wireless-protocol-docs/. Shared by every transport.
/// </summary>
public static class LwpAddressingMapping
{
    /// <summary>The built-in RGB LED's port for each hub model, or null when the model has none.</summary>
    public static byte? RgbLedPortFor(HubType hubType) => hubType switch
    {
        HubType.PoweredUpHub => 50,
        HubType.ControlPlusHub => 50,
        HubType.BoostMoveHub => 50,
        HubType.DuploTrainHub => 17,
        HubType.PoweredUpRemote => 52,
        _ => null,
    };

    /// <summary>Formats a 48-bit BLE address as "AA:BB:CC:DD:EE:FF" (most-significant byte first).</summary>
    public static string FormatMacAddress(ulong address)
        => string.Join(":", Enumerable.Range(0, 6).Select(i => ((byte)(address >> ((5 - i) * 8))).ToString("X2")));

    /// <summary>Parses "AA:BB:CC:DD:EE:FF" (or '-' separated) back into a 48-bit address.</summary>
    public static ulong ParseMacAddress(string mac)
        => mac.Split(':', '-')
            .Aggregate<string?, ulong>(0, (current, part) => (current << 8) | Convert.ToByte(part, 16));
}
