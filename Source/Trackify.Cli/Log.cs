using Microsoft.Extensions.Logging;

namespace Trackify.Cli;

/// <summary>Source-generated, allocation-free log messages for the CLI host.</summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 3000, Level = LogLevel.Information, Message = "Trackify CLI started (store: {StorePath})")]
    public static partial void Started(ILogger logger, string storePath);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Trackify backend starting (store: {StorePath})")]
    public static partial void ServerStarting(ILogger logger, string storePath);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Error, Message = "Unhandled exception running command")]
    public static partial void Unhandled(ILogger logger, Exception exception);
}
