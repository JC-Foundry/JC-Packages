using JC.Communication.Logging.Models.Notifications;
using JC.Communication.Notifications.Data;
using JC.Communication.Notifications.Models.Options;
using JC.Core.Data;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Communication.Notifications.Services;

public class NotificationLogCleanupJob(IRepositoryManager repos,
    IOptions<NotificationBackgroundJobOptions> options,
    ILogger<NotificationLogCleanupJob<DbContext>> logger)
    : NotificationLogCleanupJob<DbContext>(repos, options, logger)
{
}


public class NotificationLogCleanupJob<TContext> : IBackgroundJob
    where TContext : DbContext
{
    private readonly IRepositoryContext<NotificationLog> _logs;
    private readonly NotificationBackgroundJobOptions _options;
    private readonly ILogger<NotificationLogCleanupJob<TContext>> _logger;

    public NotificationLogCleanupJob(IRepositoryManager repos,
        IOptions<NotificationBackgroundJobOptions> options,
        ILogger<NotificationLogCleanupJob<TContext>> logger)
    {
        _logs = repos.For<TContext>().GetRepository<NotificationLog>();
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