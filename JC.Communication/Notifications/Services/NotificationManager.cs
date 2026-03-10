using JC.Communication.Logging.Services;
using JC.Communication.Notifications.Helpers;
using JC.Communication.Notifications.Models.Options;
using JC.Core.Models;
using Microsoft.Extensions.Logging;

namespace JC.Communication.Notifications.Services;

/// <summary>
/// Default implementation of <see cref="INotificationManager"/>.
/// Orchestrates notification state changes (read, unread, dismiss) by delegating
/// persistence to <see cref="NotificationService"/>, logging events via
/// <see cref="NotificationLogService"/>, and keeping <see cref="NotificationCache"/> in sync.
/// Can be replaced with a custom implementation via the service collection registration.
/// </summary>
public class NotificationManager : INotificationManager
{
    private readonly NotificationService _notificationService;
    private readonly NotificationLogService _logService;
    private readonly NotificationCache _cache;
    private readonly NotificationOptions _options;
    private readonly ILogger<NotificationManager> _logger;
    private readonly IUserInfo _userInfo;

    /// <summary>
    /// Creates a new instance of the notification manager.
    /// </summary>
    /// <param name="notificationService">The data layer service for persistence operations.</param>
    /// <param name="logService">The log service for recording read/unread events.</param>
    /// <param name="cache">The notification cache for keeping in-memory state in sync.</param>
    /// <param name="options">Notification options containing dismiss behaviour configuration.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="userInfo">The current user context information.</param>
    public NotificationManager(NotificationService notificationService,
        NotificationLogService logService,
        NotificationCache cache,
        NotificationOptions options,
        ILogger<NotificationManager> logger,
        IUserInfo userInfo)
    {
        _notificationService = notificationService;
        _logService = logService;
        _cache = cache;
        _options = options;
        _logger = logger;
        _userInfo = userInfo;
    }

    /// <summary>
    /// Marks a notification as read. Persists the change to the database,
    /// logs the read event, then updates the cached state.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    /// <returns><c>true</c> if the notification was found and marked as read; otherwise <c>false</c>.</returns>
    public async Task<bool> TryMarkAsReadAsync(string id)
    {
        var valid = NotificationValidator.ValidateUserId(_userInfo.UserId);
        if(!valid) return false;
        
        var result = await _notificationService.MarkNotificationAsRead(id);
        if (!result)
        {
            _logger.LogWarning("Failed to mark notification {NotificationId} as read.", id);
            return false;
        }

        await _logService.LogReadAsync(id);
        await _cache.UpdateReadStateAsync(id, isRead: true);
        return true;
    }

    /// <summary>
    /// Marks a notification as unread. Persists the change to the database,
    /// logs the unread event, then updates the cached state.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    /// <returns><c>true</c> if the notification was found and marked as unread; otherwise <c>false</c>.</returns>
    public async Task<bool> TryMarkAsUnreadAsync(string id)
    {
        var valid = NotificationValidator.ValidateUserId(_userInfo.UserId);
        if(!valid) return false;
        
        var result = await _notificationService.UnmarkNotificationAsRead(id);
        if (!result)
        {
            _logger.LogWarning("Failed to mark notification {NotificationId} as unread.", id);
            return false;
        }

        await _logService.LogUnreadAsync(id);
        await _cache.UpdateReadStateAsync(id, isRead: false);
        return true;
    }

    /// <summary>
    /// Dismisses a notification. Deletes it from the database using the deletion mode
    /// configured in <see cref="NotificationOptions.HardDeleteOnDismiss"/>,
    /// then removes it from the cache.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    /// <returns><c>true</c> if the notification was found and dismissed; otherwise <c>false</c>.</returns>
    public async Task<bool> TryDismissAsync(string id)
    {
        var valid = NotificationValidator.ValidateUserId(_userInfo.UserId);
        if(!valid) return false;
        
        var softDelete = !_options.HardDeleteOnDismiss;
        var result = await _notificationService.TryDeleteNotification(id, softDelete);
        if (!result)
        {
            _logger.LogWarning("Failed to dismiss notification {NotificationId}.", id);
            return false;
        }

        await _cache.RemoveNotificationAsync(id);
        return true;
    }
}
