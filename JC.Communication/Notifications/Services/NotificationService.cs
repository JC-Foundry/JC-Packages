using JC.Communication.Logging.Models.Notifications;
using JC.Communication.Notifications.Helpers;
using JC.Communication.Notifications.Models;
using JC.Core.Enums;
using JC.Core.Extensions;
using JC.Core.Models;
using JC.Core.Models.Pagination;
using JC.Core.Services.DataRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JC.Communication.Notifications.Services;

/// <summary>
/// Data layer service responsible for notification persistence operations.
/// Handles querying, creating, updating read status, deleting, and restoring notifications
/// and their associated <see cref="NotificationStyle"/> entities.
/// </summary>
public class NotificationService
{
    private readonly IRepositoryManager _repos;
    private readonly IUserInfo _userInfo;
    private readonly ILogger<NotificationService> _logger;

    /// <summary>
    /// Creates a new instance of the notification service.
    /// </summary>
    /// <param name="repos">The repository manager for database operations.</param>
    /// <param name="userInfo">The current user's identity, used as the default filter for notification queries.</param>
    /// <param name="logger">The logger instance.</param>
    public NotificationService(IRepositoryManager repos,
        IUserInfo userInfo,
        ILogger<NotificationService> logger)
    {
        _repos = repos;
        _userInfo = userInfo;
        _logger = logger;
    }

    #region Queries

    /// <summary>
    /// Builds a base query for notifications filtered by user, deletion state, and sort order.
    /// Includes the associated <see cref="NotificationStyle"/> if present.
    /// </summary>
    /// <param name="orderByNewest">When <c>true</c>, orders by <see cref="Notification.CreatedUtc"/> descending; otherwise ascending.</param>
    /// <param name="asNoTracking">When <c>true</c>, applies <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}"/> for read-only queries.</param>
    /// <param name="userId">The user to filter by. Defaults to the current user from <see cref="IUserInfo"/>.</param>
    /// <param name="deletedQueryType">Controls whether soft-deleted notifications are included.</param>
    /// <returns>A composable <see cref="IQueryable{Notification}"/>.</returns>
    private IQueryable<Notification> QueryNotifications(bool orderByNewest, bool asNoTracking,
        string? userId, DeletedQueryType deletedQueryType)
    {
        var query = _repos.GetRepository<Notification>().AsQueryable();
        query = query.FilterDeleted(deletedQueryType);

        if (asNoTracking)
            query = query.AsNoTracking();
        
        query = query
            .Include(n => n.Style)
            .Where(n => n.UserId == (userId ?? _userInfo.UserId));
        
        return orderByNewest 
            ? query.OrderByDescending(n => n.CreatedUtc)
            : query.OrderBy(n => n.CreatedUtc);
    }
    
    /// <summary>
    /// Retrieves all non-expired notifications for the specified user.
    /// Excludes notifications where <see cref="Notification.ExpiresAtUtc"/> has passed.
    /// </summary>
    /// <param name="orderByNewest">When <c>true</c>, returns newest notifications first. Defaults to <c>true</c>.</param>
    /// <param name="asNoTracking">When <c>true</c>, uses a no-tracking query. Defaults to <c>true</c>.</param>
    /// <param name="userId">The target user. Defaults to the current user.</param>
    /// <param name="deletedQueryType">Controls whether soft-deleted notifications are included. Defaults to active only.</param>
    /// <returns>A list of notifications for the user.</returns>
    public async Task<List<Notification>> GetNotifications(bool orderByNewest = true, bool asNoTracking = true,
        string? userId = null, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
    {
        var query = QueryNotifications(orderByNewest, asNoTracking, userId, deletedQueryType);
        return await query
            .Where(n => !n.ExpiresAtUtc.HasValue || (n.ExpiresAtUtc.HasValue && n.ExpiresAtUtc.Value > DateTime.UtcNow))
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves a paginated list of non-expired notifications for the specified user.
    /// Excludes notifications where <see cref="Notification.ExpiresAtUtc"/> has passed.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of notifications per page.</param>
    /// <param name="orderByNewest">When <c>true</c>, returns newest notifications first. Defaults to <c>true</c>.</param>
    /// <param name="userId">The target user. Defaults to the current user.</param>
    /// <param name="asNoTracking">When <c>true</c>, uses a no-tracking query. Defaults to <c>true</c>.</param>
    /// <param name="deletedQueryType">Controls whether soft-deleted notifications are included. Defaults to active only.</param>
    /// <returns>A paginated result of notifications.</returns>
    public async Task<IPagination<Notification>> GetNotifications(int pageNumber, int pageSize, bool orderByNewest = true,
        string? userId = null, bool asNoTracking = true, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
    {
        var query = QueryNotifications(orderByNewest, asNoTracking, userId, deletedQueryType);
        return await query
            .Where(n => !n.ExpiresAtUtc.HasValue || (n.ExpiresAtUtc.HasValue && n.ExpiresAtUtc.Value > DateTime.UtcNow))
            .ToPagedListAsync(pageNumber, pageSize);
    }

    /// <summary>
    /// Retrieves all expired notifications for the specified user.
    /// A notification is considered expired when <see cref="Notification.ExpiresAtUtc"/> is set and has passed.
    /// </summary>
    /// <param name="orderByNewest">When <c>true</c>, returns newest notifications first. Defaults to <c>true</c>.</param>
    /// <param name="asNoTracking">When <c>true</c>, uses a no-tracking query. Defaults to <c>true</c>.</param>
    /// <param name="userId">The target user. Defaults to the current user.</param>
    /// <param name="deletedQueryType">Controls whether soft-deleted notifications are included. Defaults to active only.</param>
    /// <returns>A list of expired notifications for the user.</returns>
    public async Task<List<Notification>> GetExpiredNotifications(bool orderByNewest = true, bool asNoTracking = true,
        string? userId = null, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
    {
        var query = QueryNotifications(orderByNewest, asNoTracking, userId, deletedQueryType);
        return await query
            .Where(n => n.ExpiresAtUtc.HasValue && n.ExpiresAtUtc.Value <= DateTime.UtcNow)
            .ToListAsync();
    }
    
    /// <summary>
    /// Retrieves a paginated list of expired notifications for the specified user.
    /// A notification is considered expired when <see cref="Notification.ExpiresAtUtc"/> is set and has passed.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of notifications per page.</param>
    /// <param name="orderByNewest">When <c>true</c>, returns newest notifications first. Defaults to <c>true</c>.</param>
    /// <param name="asNoTracking">When <c>true</c>, uses a no-tracking query. Defaults to <c>true</c>.</param>
    /// <param name="userId">The target user. Defaults to the current user.</param>
    /// <param name="deletedQueryType">Controls whether soft-deleted notifications are included. Defaults to active only.</param>
    /// <returns>A paginated result of expired notifications.</returns>
    public async Task<IPagination<Notification>> GetExpiredNotifications(int pageNumber, int pageSize, bool orderByNewest = true,
        bool asNoTracking = true, string? userId = null, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
    {
        var query = QueryNotifications(orderByNewest, asNoTracking, userId, deletedQueryType);
        return await query
            .Where(n => n.ExpiresAtUtc.HasValue && n.ExpiresAtUtc.Value <= DateTime.UtcNow)
            .ToPagedListAsync(pageNumber, pageSize);
    }

    /// <summary>
    /// Retrieves a single notification by its identifier. Returns a tracked entity suitable for modification.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    /// <param name="userId">The target user. Defaults to the current user.</param>
    /// <param name="deletedQueryType">Controls whether soft-deleted notifications are included. Defaults to active only.</param>
    /// <returns>The notification if found; otherwise <c>null</c>.</returns>
    public async Task<Notification?> GetNotificationById(string id, string? userId = null, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
    {
        var query = QueryNotifications(true, false, userId, deletedQueryType);
        return await query.FirstOrDefaultAsync(n => n.Id == id);
    }

    #endregion


    #region Notification Actions

    /// <summary>
    /// Marks a notification as read by invoking <see cref="Notification.Read"/> and persisting the change.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    /// <returns><c>true</c> if the notification was found and updated; <c>false</c> if not found.</returns>
    public async Task<bool> MarkNotificationAsRead(string id)
    {
        var notification = await GetNotificationById(id);
        if (notification == null) return false;
        
        notification.Read();
        await UpdateNotificationReadStatus(notification);
        return true;
    }

    /// <summary>
    /// Marks a notification as unread by invoking <see cref="Notification.Unread"/> and persisting the change.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    /// <returns><c>true</c> if the notification was found and updated; <c>false</c> if not found.</returns>
    public async Task<bool> UnmarkNotificationAsRead(string id)
    {
        var notification = await GetNotificationById(id);
        if (notification == null) return false;
        
        notification.Unread();
        await UpdateNotificationReadStatus(notification);
        return true;
    }

    /// <summary>
    /// Persists the current read/unread state of a notification to the database.
    /// </summary>
    /// <param name="notification">The notification with updated read state.</param>
    private async Task UpdateNotificationReadStatus(Notification notification)
    {
        await _repos.GetRepository<Notification>()
            .UpdateAsync(notification);
    }

    #endregion
    

    #region Basic Add/Delete/Restore

    /// <summary>
    /// Validates and persists a new notification, optionally with custom styling.
    /// The notification and style are saved within a transaction.
    /// </summary>
    /// <param name="notification">The notification to create.</param>
    /// <param name="style">Optional custom styling to associate with the notification.</param>
    /// <returns>A <see cref="NotificationValidationResponse"/> indicating success or containing validation errors.</returns>
    public async Task<NotificationValidationResponse> TryAddNotification(Notification notification, NotificationStyle? style = null)
    {
        var response = style == null 
            ? NotificationValidator.Validate(notification)
            : NotificationValidator.Validate(notification, style);
        if(!response.IsValid) return response;

        await _repos.BeginTransactionAsync();
        try
        {
            await _repos.GetRepository<Notification>()
                .AddAsync(notification, saveNow: false);
            
            if(style != null)
                await _repos.GetRepository<NotificationStyle>()
                    .AddAsync(style, saveNow: false);

            await _repos.SaveChangesAsync();
            await _repos.CommitTransactionAsync();
            return response;
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to create notification and its styling.");
            return new NotificationValidationResponse("Unable to save notification and style.");
        }
    }

    /// <summary>
    /// Deletes a notification and its associated style within a transaction.
    /// Supports both soft and hard deletion. Hard deletion also permanently removes
    /// any related <see cref="NotificationLog"/> entries to satisfy FK constraints.
    /// </summary>
    /// <param name="notificationId">The notification identifier.</param>
    /// <param name="softDelete">When <c>true</c>, performs a soft delete; otherwise permanently removes the entities. Defaults to <c>true</c>.</param>
    /// <returns><c>true</c> if the notification was found and deleted; <c>false</c> if not found or an error occurred.</returns>
    public async Task<bool> TryDeleteNotification(string notificationId, bool softDelete = true)
    {
        var notification = await GetNotificationById(notificationId);
        if (notification == null) return false;

        var style = notification.Style;
        await _repos.BeginTransactionAsync();
        try
        {
            if (softDelete)
            {
                await _repos.GetRepository<Notification>()
                    .SoftDeleteAsync(notification, saveNow: false);
                
                if(style != null)
                    await _repos.GetRepository<NotificationStyle>()
                        .SoftDeleteAsync(style, saveNow: false);
            }
            else
            {
                var logRepo = _repos.GetRepository<NotificationLog>();
                var logs = await logRepo.GetAllAsync(n => n.NotificationId == notificationId);
                await logRepo.DeleteRangeAsync(logs, false);
                
                if(style != null)
                    await _repos.GetRepository<NotificationStyle>()
                        .DeleteAsync(style, saveNow: false);

                await _repos.GetRepository<Notification>()
                    .DeleteAsync(notification, saveNow: false);
            }
            
            await _repos.SaveChangesAsync();
            await _repos.CommitTransactionAsync();
            return true;
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Unable to {DeleteType} delete notification and its styling.",
                softDelete ? "soft" : "hard");
            return false;
        }
    }

    /// <summary>
    /// Restores a soft-deleted notification and its associated style within a transaction.
    /// Returns <c>true</c> if the notification is already active (not deleted).
    /// </summary>
    /// <param name="notificationId">The notification identifier.</param>
    /// <returns><c>true</c> if the notification was restored or was already active; <c>false</c> if not found or an error occurred.</returns>
    public async Task<bool> TryRestoreNotification(string notificationId)
    {
        var notification = await GetNotificationById(notificationId, deletedQueryType: DeletedQueryType.All);
        if (notification == null) return false;

        //Return true if the notification is not soft deleted and is already restored:
        if (!notification.IsDeleted) return true;

        var style = notification.Style;
        await _repos.BeginTransactionAsync();
        try
        {
            await _repos.GetRepository<Notification>()
                .RestoreAsync(notification, saveNow: false);
            
            if(style != null)
                await _repos.GetRepository<NotificationStyle>()
                    .RestoreAsync(style, saveNow: false);
            
            await _repos.SaveChangesAsync();
            await _repos.CommitTransactionAsync();
            return true;
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Unable to restore soft deleted notification and its styling.");
            return false;
        }
    }

    #endregion
}