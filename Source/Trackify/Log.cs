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

    [LoggerMessage(EventId = 4003, Level = LogLevel.Information, Message = "Connection mode changed (server: {UseServer}, url: {ServerUrl})")]
    public static partial void ConnectionModeChanged(ILogger logger, bool useServer, string serverUrl);

    [LoggerMessage(EventId = 4004, Level = LogLevel.Information, Message = "Synced {Count} trains from the backend")]
    public static partial void SyncCompleted(ILogger logger, int count);

    [LoggerMessage(EventId = 4005, Level = LogLevel.Warning, Message = "Backend train sync failed")]
    public static partial void SyncFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 4006, Level = LogLevel.Warning, Message = "Could not load connection settings")]
    public static partial void ConnectionSettingsLoadFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 4007, Level = LogLevel.Warning, Message = "Could not save connection settings")]
    public static partial void ConnectionSettingsSaveFailed(ILogger logger, Exception exception);
}
