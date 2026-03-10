namespace JC.Communication.Notifications.Models.Options;

/// <summary>
/// Configuration options for the notification system.
/// </summary>
public class NotificationOptions
{
    /// <summary>Gets or sets the cache TTL in hours. Defaults to 24.</summary>
    public int CacheDurationHours { get; set; } = 24;

    /// <summary>Gets or sets the logging mode for notification read/unread events. Defaults to <see cref="NotificationLoggingMode.All"/>.</summary>
    public NotificationLoggingMode LoggingMode { get; set; } = NotificationLoggingMode.All;

    /// <summary>Gets or sets whether dismissing a notification performs a hard delete instead of a soft delete. Defaults to <c>false</c>.</summary>
    public bool HardDeleteOnDismiss { get; set; }
}

/// <summary>
/// Controls which notification read/unread events are persisted by <see cref="JC.Communication.Logging.Services.NotificationLogService"/>.
/// </summary>
public enum NotificationLoggingMode
{
    /// <summary>No notification events are logged.</summary>
    None,

    /// <summary>Only read events are logged.</summary>
    ReadOnly,

    /// <summary>Only unread events are logged.</summary>
    UnreadOnly,

    /// <summary>Both read and unread events are logged.</summary>
    All
}