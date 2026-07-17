using Microsoft.Extensions.Logging;

namespace Trackify.Infrastructure.Logging;

/// <summary>Source-generated, allocation-free log messages for the Infrastructure layer.</summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 2000, Level = LogLevel.Debug, Message = "Loaded {Count} trains from {Path}")]
    public static partial void StoreLoaded(ILogger logger, int count, string path);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Saved {Count} trains to {Path}")]
    public static partial void StoreSaved(ILogger logger, int count, string path);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Hub {HubId} connected over BlueZ")]
    public static partial void HubConnected(ILogger logger, string hubId);
}
