using JC.Communication.Logging.Models.Notifications;
using JC.Communication.Notifications.Models.Options;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Communication.Logging.Services;

/// <summary>
/// Handles persistence of notification read/unread events to the database.
/// Respects the configured <see cref="NotificationLoggingMode"/> to determine which events are logged.
/// </summary>
public class NotificationLogService
{
    private readonly IRepositoryManager _repos;
    private readonly IUserInfo _userInfo;
    private readonly ILogger<NotificationLogService> _logger;
    private readonly NotificationLoggingMode _loggingMode;

    /// <summary>
    /// Creates a new instance of the notification log service.
    /// </summary>
    /// <param name="repos">The repository manager for database operations.</param>
    /// <param name="userInfo">The current user's identity, used as the default for recording who performed the action.</param>
    /// <param name="options">The notification options containing the logging mode configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public NotificationLogService(IRepositoryManager repos,
        IUserInfo userInfo,
        IOptions<NotificationOptions> options,
        ILogger<NotificationLogService> logger)
    {
        _repos = repos;
        _userInfo = userInfo;
        _logger = logger;
        _loggingMode = options.Value.LoggingMode;
    }

    /// <summary>
    /// Logs a notification read event to the database.
    /// Does nothing if <see cref="NotificationLoggingMode"/> is <see cref="NotificationLoggingMode.None"/>
    /// or <see cref="NotificationLoggingMode.UnreadOnly"/>.
    /// </summary>
    /// <param name="notificationId">The identifier of the notification that was read.</param>
    /// <param name="userId">The user who performed the action. Defaults to the current user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public async Task LogReadAsync(string notificationId, string? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (_loggingMode is NotificationLoggingMode.None or NotificationLoggingMode.UnreadOnly)
            return;

        await LogAsync(notificationId, isRead: true, userId, cancellationToken);
    }

    /// <summary>
    /// Logs a notification unread event to the database.
    /// Does nothing if <see cref="NotificationLoggingMode"/> is <see cref="NotificationLoggingMode.None"/>
    /// or <see cref="NotificationLoggingMode.ReadOnly"/>.
    /// </summary>
    /// <param name="notificationId">The identifier of the notification that was marked as unread.</param>
    /// <param name="userId">The user who performed the action. Defaults to the current user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public async Task LogUnreadAsync(string notificationId, string? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (_loggingMode is NotificationLoggingMode.None or NotificationLoggingMode.ReadOnly)
            return;

        await LogAsync(notificationId, isRead: false, userId, cancellationToken);
    }

    /// <summary>
    /// Persists a <see cref="NotificationLog"/> entry to the database.
    /// </summary>
    /// <param name="notificationId">The identifier of the notification.</param>
    /// <param name="isRead">Whether the event represents a read or unread action.</param>
    /// <param name="userId">The user who performed the action. Defaults to the current user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    private async Task LogAsync(string notificationId, bool isRead, string? userId,
        CancellationToken cancellationToken)
    {
        var log = new NotificationLog
        {
            NotificationId = notificationId,
            UserId = userId ?? _userInfo.UserId,
            IsRead = isRead
        };

        try
        {
            await _repos.GetRepository<NotificationLog>()
                .AddAsync(log, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to create notification log for notification {NotificationId}.", notificationId);
        }
    }
}
