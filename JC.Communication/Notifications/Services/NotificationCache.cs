using JC.Communication.Notifications.Models;
using JC.Communication.Notifications.Models.Options;
using JC.Core.Models;
using Microsoft.Extensions.Caching.Memory;

namespace JC.Communication.Notifications.Services;

/// <summary>
/// In-memory cache layer for user notifications. Provides read access and cache-only
/// mutation methods. Does not interact with the database directly — all persistence
/// is handled by the calling layer (e.g. <c>NotificationManager</c>).
/// Hydrates from <see cref="NotificationService"/> on first access per user,
/// with a configurable TTL (default 24 hours) as a safety net against stale data.
/// </summary>
public class NotificationCache
{
    private readonly NotificationService _notificationService;
    private readonly IUserInfo _userInfo;
    private readonly IMemoryCache _cache;
    private readonly NotificationOptions _options;

    private const string CacheKeyPrefix = "JC.Notifications:";
    private TimeSpan CacheDuration => TimeSpan.FromHours(_options.CacheDurationHours);

    /// <summary>
    /// Creates a new instance of the notification cache.
    /// </summary>
    /// <param name="notificationService">The underlying data layer service used to hydrate the cache on miss.</param>
    /// <param name="userInfo">The current user's identity, used as the default cache key.</param>
    /// <param name="cache">The memory cache instance.</param>
    /// <param name="options">Notification options containing cache duration configuration.</param>
    public NotificationCache(NotificationService notificationService,
        IUserInfo userInfo,
        IMemoryCache cache,
        NotificationOptions options)
    {
        _notificationService = notificationService;
        _userInfo = userInfo;
        _cache = cache;
        _options = options;
    }


    #region Read Operations

    /// <summary>
    /// Retrieves all cached notifications for the specified user.
    /// Hydrates from the database on cache miss.
    /// </summary>
    /// <param name="userId">The target user. Defaults to the current user.</param>
    /// <returns>A list of notifications for the user.</returns>
    public async Task<List<Notification>> GetNotificationsAsync(string? userId = null)
    {
        var key = GetCacheKey(userId);

        if (_cache.TryGetValue(key, out List<Notification>? cached) && cached != null)
            return cached;

        var notifications = await _notificationService.GetNotifications(userId: userId);
        SetCache(key, notifications);
        return notifications;
    }

    /// <summary>
    /// Returns the count of unread notifications for the specified user.
    /// </summary>
    /// <param name="userId">The target user. Defaults to the current user.</param>
    /// <returns>The number of unread notifications.</returns>
    public async Task<int> GetUnreadCountAsync(string? userId = null)
    {
        var notifications = await GetNotificationsAsync(userId);
        return notifications.Count(n => !n.IsRead);
    }

    /// <summary>
    /// Retrieves a single notification from the cache by its identifier.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    /// <param name="userId">The target user. Defaults to the current user.</param>
    /// <returns>The notification if found in the cache; otherwise <c>null</c>.</returns>
    public async Task<Notification?> GetNotificationByIdAsync(string id, string? userId = null)
    {
        var notifications = await GetNotificationsAsync(userId);
        return notifications.FirstOrDefault(n => n.Id == id);
    }

    #endregion


    #region Cache Mutations

    /// <summary>
    /// Adds a notification to the cached list for the specified user.
    /// Inserts at the beginning of the list (newest first).
    /// This is a cache-only operation — the caller is responsible for persisting to the database.
    /// </summary>
    /// <param name="notification">The notification to add to the cache.</param>
    public async Task AddNotificationAsync(Notification notification)
    {
        var notifications = await GetNotificationsAsync(notification.UserId);
        notifications.Insert(0, notification);
        SetCache(GetCacheKey(notification.UserId), notifications);
    }

    /// <summary>
    /// Updates the read state of a notification in the cache.
    /// This is a cache-only operation — the caller is responsible for persisting to the database.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    /// <param name="isRead">The new read state.</param>
    /// <param name="userId">The target user. Defaults to the current user.</param>
    public async Task UpdateReadStateAsync(string id, bool isRead, string? userId = null)
    {
        var notifications = await GetNotificationsAsync(userId);
        var notification = notifications.FirstOrDefault(n => n.Id == id);
        if (notification == null) return;

        if (isRead)
            notification.Read();
        else
            notification.Unread();

        SetCache(GetCacheKey(userId), notifications);
    }

    /// <summary>
    /// Removes a notification from the cached list for the specified user.
    /// This is a cache-only operation — the caller is responsible for persisting to the database.
    /// </summary>
    /// <param name="id">The notification identifier to remove.</param>
    /// <param name="userId">The target user. Defaults to the current user.</param>
    public async Task RemoveNotificationAsync(string id, string? userId = null)
    {
        var notifications = await GetNotificationsAsync(userId);
        var notification = notifications.FirstOrDefault(n => n.Id == id);
        if (notification == null) return;

        notifications.Remove(notification);
        SetCache(GetCacheKey(userId), notifications);
    }

    /// <summary>
    /// Marks all notifications in the cache as read for the specified user.
    /// This is a cache-only operation — the caller is responsible for persisting to the database.
    /// </summary>
    /// <param name="userId">The target user. Defaults to the current user.</param>
    public async Task MarkAllAsReadAsync(string? userId = null)
    {
        var notifications = await GetNotificationsAsync(userId);
        foreach (var notification in notifications.Where(n => !n.IsRead))
            notification.Read();

        SetCache(GetCacheKey(userId), notifications);
    }

    /// <summary>
    /// Invalidates the cached notification list for the specified user,
    /// forcing a fresh load from the database on next access.
    /// </summary>
    /// <param name="userId">The target user. Defaults to the current user.</param>
    public void Invalidate(string? userId = null)
        => _cache.Remove(GetCacheKey(userId));

    #endregion


    #region Internal

    /// <summary>
    /// Builds the cache key for the specified user.
    /// </summary>
    /// <param name="userId">The user identifier. Defaults to the current user.</param>
    /// <returns>The cache key string.</returns>
    private string GetCacheKey(string? userId = null)
        => $"{CacheKeyPrefix}{userId ?? _userInfo.UserId}";

    /// <summary>
    /// Sets the cached notification list with the configured TTL.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="notifications">The notification list to cache.</param>
    private void SetCache(string key, List<Notification> notifications)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration
        };

        _cache.Set(key, notifications, cacheOptions);
    }

    #endregion
}
