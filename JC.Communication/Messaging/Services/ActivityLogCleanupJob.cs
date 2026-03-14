using JC.Communication.Logging.Models.Messaging;
using JC.Communication.Messaging.Models.Options;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Communication.Messaging.Services;

public class ActivityLogCleanupJob : IBackgroundJob
{
    private readonly IRepositoryContext<ThreadActivityLog> _logs;
    private readonly MessagingBackgroundJobOptions _options;
    private readonly ILogger<ActivityLogCleanupJob> _logger;

    public ActivityLogCleanupJob(IRepositoryContext<ThreadActivityLog> logs,
        IOptions<MessagingBackgroundJobOptions> options,
        ILogger<ActivityLogCleanupJob> logger)
    {
        _logs = logs;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableActivityLogCleanupJob)
        {
            _logger.LogDebug("Activity log cleanup job is disabled.");
            return;
        }

        var logs = await _logs.GetAllAsync(l => l.CreatedUtc <= ResolveCutoffDate(),
            x => x.OrderBy(l => l.CreatedUtc), cancellationToken);

        var retention = _options.ActivityLogMinimumRetentionRecords;
        if (retention == 0)
        {
            await ProcessCleanup(logs);
            return;
        }

        if (retention >= logs.Count)
        {
            _logger.LogInformation("Skipping activity log cleanup as retention ({0}) is greater than existing logs ({1}).",
                retention, logs.Count);
            return;
        }

        if (_options.ActivityLogCleanupChunkingValue > 0)
            logs = logs.Take(_options.ActivityLogCleanupChunkingValue).ToList();

        logs = logs.OrderByDescending(l => l.CreatedUtc)
            .Skip(retention).ToList();
        await ProcessCleanup(logs);
    }

    private async Task ProcessCleanup(List<ThreadActivityLog> logs)
    {
        await _logs.DeleteRangeAsync(logs);
        _logger.LogInformation("Deleted {Count} thread activity logs.", logs.Count);
    }

    private DateTime ResolveCutoffDate()
        => DateTime.UtcNow.AddMonths(-_options.ActivityLogRetentionMonths);
}
