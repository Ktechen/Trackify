using Microsoft.Extensions.Logging;

namespace Trackify.Infrastructure;

/// <summary>Source-generated, allocation-free log messages for the Infrastructure layer.</summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Hub {HubId} connected over BlueZ")]
    public static partial void HubConnected(ILogger logger, string hubId);
}
