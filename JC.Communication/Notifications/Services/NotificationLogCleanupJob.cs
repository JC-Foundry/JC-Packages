using JC.Communication.Logging.Models.Notifications;
using JC.Communication.Notifications.Models.Options;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Communication.Notifications.Services;

public class NotificationLogCleanupJob : IBackgroundJob
{
    private readonly IRepositoryContext<NotificationLog> _logs;
    private readonly NotificationBackgroundJobOptions _options;
    private readonly ILogger<NotificationLogCleanupJob> _logger;

    public NotificationLogCleanupJob(IRepositoryContext<NotificationLog> logs,
        IOptions<NotificationBackgroundJobOptions> options,
        ILogger<NotificationLogCleanupJob> logger)
    {
        _logs = logs;
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableNotificationLogCleanupJob)
        {
            _logger.LogDebug("Notification log cleanup job is disabled.");
            return;
        }
        
        var logs = await _logs.GetAllAsync(l => l.CreatedUtc <= ResolveCutoffDate(),
            x => x.OrderBy(l => l.CreatedUtc), cancellationToken);
        
        var retention = _options.MinimumRetentionRecords;
        if (retention == 0)
        {
            await ProcessCleanup(logs);
            return;
        }

        if (retention >= logs.Count)
        {
            _logger.LogInformation("Skipping notification log cleanup as retention ({0}) is greater than existing logs ({1}).",
                retention, logs.Count);
            return;
        }
        
        if(_options.NotificationLogCleanupChunkingValue > 0)
            logs = logs.Take(_options.NotificationLogCleanupChunkingValue).ToList();
        
        logs = logs.OrderByDescending(l => l.CreatedUtc)
            .Skip(retention).ToList();
        await ProcessCleanup(logs);
    }

    private async Task ProcessCleanup(List<NotificationLog> logs)
    {
        await _logs.DeleteRangeAsync(logs);
        _logger.LogInformation("Deleted {Count} notification logs.", logs.Count);
    }
    
    private DateTime ResolveCutoffDate()
        => DateTime.UtcNow.AddMonths(-(_options.NotificationLogRetentionMonths));
}