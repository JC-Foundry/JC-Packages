namespace JC.Communication.Notifications.Models;

/// <summary>
/// Defines the type of a notification, carrying semantic meaning and driving default styling.
/// </summary>
/// <remarks>
/// The default icon and colour for each type live in JC.Communication.Web, on the icon and class
/// dictionaries for the configured frameworks. They are not here because this package references
/// only JC.Core and cannot see them — and because a presentation default is not this enum's
/// business. Override per notification with <see cref="NotificationStyle"/>.
/// </remarks>
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