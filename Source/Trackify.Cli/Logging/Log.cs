using Microsoft.Extensions.Logging;

namespace Trackify.Cli.Logging;

/// <summary>Source-generated, allocation-free log messages for the CLI host.</summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 3000, Level = LogLevel.Information, Message = "Trackify CLI started (store: {StorePath})")]
    public static partial void Started(ILogger logger, string storePath);
}
