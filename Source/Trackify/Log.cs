namespace Trackify;

/// <summary>Source-generated, allocation-free log messages for the app's view models.</summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 4000, Level = LogLevel.Information, Message = "Discovering hubs")]
    public static partial void DiscoverStarted(ILogger logger);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Train {TrainId} connected")]
    public static partial void HubConnected(ILogger logger, string trainId);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "Train {TrainId} connect failed")]
    public static partial void HubConnectFailed(ILogger logger, string trainId, Exception exception);
}
