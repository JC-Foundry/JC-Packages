namespace JC.Communication.Notifications.Models;

/// <summary>
/// Defines the type of a notification, used to determine default styling
/// and semantic meaning via <see cref="JC.Communication.Notifications.Helpers.NotificationUIHelper"/>.
/// </summary>
public enum NotificationType
{
    /// <summary>A direct message notification.</summary>
    Message,

    /// <summary>An informational notification.</summary>
    Info,

    /// <summary>A success notification.</summary>
    Success,

    /// <summary>A warning notification.</summary>
    Warning,

    /// <summary>An error notification.</summary>
    Error,

    /// <summary>A system-level notification.</summary>
    System,

    /// <summary>A task-related notification.</summary>
    Task
}