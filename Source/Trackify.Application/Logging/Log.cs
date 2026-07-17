using Microsoft.Extensions.Logging;

namespace Trackify.Application.Logging;

/// <summary>
/// Source-generated, allocation-free log messages for the Application layer (the
/// <see cref="Microsoft.Extensions.Logging.LoggerMessageAttribute"/> generator emits the fast path).
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Discovering hubs")]
    public static partial void Discovering(ILogger logger);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Connecting train {TrainId} via hub {HubKey}")]
    public static partial void Connecting(ILogger logger, string trainId, string hubKey);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Disconnecting train {TrainId}")]
    public static partial void Disconnecting(ILogger logger, string trainId);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Debug, Message = "Train {TrainId} speed → {Speed}")]
    public static partial void SettingSpeed(ILogger logger, string trainId, int speed);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Debug, Message = "Train {TrainId} LED → {Color}")]
    public static partial void SettingLed(ILogger logger, string trainId, string color);
}
