using Microsoft.Extensions.Logging;

namespace Trackify.Infrastructure;

/// <summary>Source-generated, allocation-free log messages for the Infrastructure layer.</summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Hub {HubId} connected over BlueZ")]
    public static partial void HubConnected(ILogger logger, string hubId);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Warning, Message = "Bluetooth adapter was off; powering it on")]
    public static partial void RadioPoweringOn(ILogger logger);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "BlueZ LE discovery started")]
    public static partial void DiscoveryStarted(ILogger logger);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Scanning (LE) for hub {Mac}…")]
    public static partial void ScanningForDevice(ILogger logger, string mac);
}
