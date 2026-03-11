namespace JC.Communication.Notifications.Services;

/// <summary>
/// Defines the contract for managing notification state changes (read, unread, dismiss).
/// The default implementation is <see cref="NotificationManager"/>, which can be replaced
/// with a custom implementation at service registration.
/// </summary>
public interface INotificationManager
{
    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    /// <param name="userId">Optional user identifier for cross-user operations. The default <see cref="NotificationManager"/> does not support this and will throw; implement a custom <see cref="INotificationManager"/> to enable cross-user access.</param>
    /// <returns><c>true</c> if the notification was found and marked as read; otherwise <c>false</c>.</returns>
    Task<bool> TryMarkAsReadAsync(string id, string? userId = null);

    /// <summary>
    /// Marks a notification as unread.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    /// <param name="userId">Optional user identifier for cross-user operations. The default <see cref="NotificationManager"/> does not support this and will throw; implement a custom <see cref="INotificationManager"/> to enable cross-user access.</param>
    /// <returns><c>true</c> if the notification was found and marked as unread; otherwise <c>false</c>.</returns>
    Task<bool> TryMarkAsUnreadAsync(string id, string? userId = null);

    /// <summary>
    /// Dismisses a notification.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    /// <param name="userId">Optional user identifier for cross-user operations. The default <see cref="NotificationManager"/> does not support this and will throw; implement a custom <see cref="INotificationManager"/> to enable cross-user access.</param>
    /// <returns><c>true</c> if the notification was found and dismissed; otherwise <c>false</c>.</returns>
    Task<bool> TryDismissAsync(string id, string? userId = null);

    /// <summary>
    /// Marks all notifications as read for the current user, or the specified user.
    /// </summary>
    /// <param name="userId">Optional user identifier for cross-user operations. The default <see cref="NotificationManager"/> does not support this and will throw; implement a custom <see cref="INotificationManager"/> to enable cross-user access.</param>
    /// <returns><c>true</c> if at least one notification was marked as read; otherwise <c>false</c>.</returns>
    Task<bool> TryMarkAllAsReadAsync(string? userId = null);

    /// <summary>
    /// Marks all notifications as unread for the current user, or the specified user.
    /// </summary>
    /// <param name="userId">Optional user identifier for cross-user operations. The default <see cref="NotificationManager"/> does not support this and will throw; implement a custom <see cref="INotificationManager"/> to enable cross-user access.</param>
    /// <returns><c>true</c> if at least one notification was marked as unread; otherwise <c>false</c>.</returns>
    Task<bool> TryMarkAllAsUnreadAsync(string? userId = null);

    /// <summary>
    /// Dismisses all notifications for the current user, or the specified user.
    /// </summary>
    /// <param name="userId">Optional user identifier for cross-user operations. The default <see cref="NotificationManager"/> does not support this and will throw; implement a custom <see cref="INotificationManager"/> to enable cross-user access.</param>
    /// <returns><c>true</c> if at least one notification was dismissed; otherwise <c>false</c>.</returns>
    Task<bool> TryDismissAllAsync(string? userId = null);
}