using JC.Communication.Notifications.Models;

namespace JC.Communication.Notifications.Helpers;

/// <summary>
/// Lightweight static helper providing default Bootstrap icon and colour class mappings
/// for each <see cref="NotificationType"/>. These defaults can be overridden per-notification
/// via <see cref="NotificationStyle"/>.
/// </summary>
public static class NotificationUIHelper
{
    /// <summary>
    /// Returns the default Bootstrap icon CSS class for the specified notification type.
    /// </summary>
    /// <param name="type">The notification type.</param>
    /// <returns>The Bootstrap icon class string, or an empty string for unknown types.</returns>
    public static string GetIconClass(NotificationType type)
        => type switch
        {
            NotificationType.Message => "bi-chat-left-text",
            NotificationType.Info => "bi-info-circle",
            NotificationType.Success => "bi-check-circle",
            NotificationType.Warning => "bi-exclamation-triangle",
            NotificationType.Error => "bi-x-circle",
            NotificationType.System => "bi-cpu",
            NotificationType.Task => "bi-list-check",
            _ => ""
        };

    /// <summary>
    /// Returns the default Bootstrap colour CSS class for the specified notification type.
    /// </summary>
    /// <param name="type">The notification type.</param>
    /// <returns>The Bootstrap colour class string, or <c>"secondary"</c> for unknown types.</returns>
    public static string GetColourClass(NotificationType type)
        => type switch
        {
            NotificationType.Message => "primary",
            NotificationType.Info => "info",
            NotificationType.Success => "success",
            NotificationType.Warning => "warning",
            NotificationType.Error => "danger",
            NotificationType.System => "secondary",
            NotificationType.Task => "primary",
            _ => "secondary"
        };
}