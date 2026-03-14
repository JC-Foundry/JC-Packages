using JC.Communication.Logging.Models.Messaging;
using JC.Communication.Messaging.Models.DomainModels;
using JC.Communication.Messaging.Models.Options;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Communication.Logging.Services;

/// <summary>
/// Handles persistence of messaging-related log entries to the database.
/// Respects the configured <see cref="MessagingOptions"/> to determine which events are logged.
/// </summary>
public class MessagingLogService
{
    private readonly IRepositoryManager _repos;
    private readonly IUserInfo _userInfo;
    private readonly ILogger<MessagingLogService> _logger;
    private readonly MessagingOptions _options;

    /// <summary>
    /// Creates a new instance of the messaging log service.
    /// </summary>
    /// <param name="repos">The repository manager for database operations.</param>
    /// <param name="userInfo">The current user's identity.</param>
    /// <param name="options">The messaging options containing logging configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public MessagingLogService(IRepositoryManager repos,
        IUserInfo userInfo,
        IOptions<MessagingOptions> options,
        ILogger<MessagingLogService> logger)
    {
        _repos = repos;
        _userInfo = userInfo;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Logs a thread activity event (message sent, participant added/removed) to the database.
    /// Does nothing if the configured <see cref="MessagingOptions.ThreadActivityLoggingMode"/>
    /// does not include the given <paramref name="activityType"/>.
    /// <para>
    /// <b>Important:</b> This method does not call <c>SaveChanges</c>. The caller is responsible
    /// for persisting changes, typically as part of a wider transaction.
    /// </para>
    /// </summary>
    /// <param name="threadId">The ID of the thread the activity occurred in.</param>
    /// <param name="activityType">The type of activity to log.</param>
    /// <param name="activityDetails">Optional descriptive details about the activity.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public async Task LogThreadActivityAsync(string threadId, ThreadActivityType activityType,
        string? activityDetails = null, CancellationToken cancellationToken = default)
    {
        if (_options.ThreadActivityLoggingMode == ThreadActivityLoggingMode.None)
            return;

        var modeFlag = activityType switch
        {
            ThreadActivityType.Message => ThreadActivityLoggingMode.Message,
            ThreadActivityType.ParticipantAdded => ThreadActivityLoggingMode.ParticipantAdded,
            ThreadActivityType.ParticipantRemoved => ThreadActivityLoggingMode.ParticipantRemoved,
            _ => throw new ArgumentOutOfRangeException(nameof(activityType), activityType, null)
        };

        if (!_options.ThreadActivityLoggingMode.HasFlag(modeFlag))
            return;

        var log = new ThreadActivityLog
        {
            ThreadId = threadId,
            ActivityTimestampUtc = DateTime.UtcNow,
            ActivityType = activityType,
            ActivityDetails = activityDetails
        };

        try
        {
            await _repos.GetRepository<ThreadActivityLog>()
                .AddAsync(log, saveNow: false, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to create thread activity log for thread {ThreadId}.", threadId);
        }
    }

    /// <summary>
    /// Logs message read events for the current user. Accepts a list of messages (typically all messages
    /// in a thread) and only creates log entries for messages that the user has not already read.
    /// Does nothing if <see cref="MessagingOptions.LogChatReads"/> is <c>false</c>.
    /// </summary>
    /// <param name="messages">The messages to mark as read.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public async Task LogMessageReadsAsync(List<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (!_options.LogChatReads || messages.Count == 0)
            return;

        var userId = _userInfo.UserId;
        var messageIds = messages.Select(m => m.Id).ToList();

        var alreadyRead = await _repos.GetRepository<MessageReadLog>()
            .AsQueryable()
            .Where(r => r.UserId == userId && messageIds.Contains(r.MessageId))
            .Select(r => r.MessageId)
            .ToListAsync(cancellationToken);

        var newLogs = messageIds
            .Where(id => !alreadyRead.Contains(id))
            .Select(id => new MessageReadLog
            {
                MessageId = id,
                UserId = userId,
                ReadAtUtc = DateTime.UtcNow
            })
            .ToList();

        if (newLogs.Count == 0)
            return;

        try
        {
            await _repos.GetRepository<MessageReadLog>()
                .AddRangeAsync(newLogs, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to create message read logs for user {UserId}.", userId);
        }
    }
}