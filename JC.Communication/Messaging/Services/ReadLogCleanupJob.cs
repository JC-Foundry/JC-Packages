using JC.Communication.Logging.Models.Messaging;
using JC.Communication.Messaging.Models.Options;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Communication.Messaging.Services;

public class ReadLogCleanupJob : IBackgroundJob
{
    private readonly IRepositoryContext<MessageReadLog> _logs;
    private readonly MessagingBackgroundJobOptions _options;
    private readonly ILogger<ReadLogCleanupJob> _logger;

    public ReadLogCleanupJob(IRepositoryContext<MessageReadLog> logs,
        IOptions<MessagingBackgroundJobOptions> options,
        ILogger<ReadLogCleanupJob> logger)
    {
        _logs = logs;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableReadLogCleanupJob)
        {
            _logger.LogDebug("Read log cleanup job is disabled.");
            return;
        }

        var logs = await _logs.GetAllAsync(l => l.CreatedUtc <= ResolveCutoffDate(),
            x => x.OrderBy(l => l.CreatedUtc), cancellationToken);

        if (_options.KeepMostRecentReadLog)
        {
            var mostRecent = logs
                .GroupBy(l => new { l.UserId, l.MessageId })
                .SelectMany(g => g.OrderByDescending(l => l.CreatedUtc).Take(1))
                .ToHashSet();

            logs = logs.Where(l => !mostRecent.Contains(l)).ToList();
        }

        var retention = _options.ReadLogMinimumRetentionRecords;
        if (retention == 0)
        {
            await ProcessCleanup(logs);
            return;
        }

        if (retention >= logs.Count)
        {
            _logger.LogInformation("Skipping read log cleanup as retention ({0}) is greater than existing logs ({1}).",
                retention, logs.Count);
            return;
        }

        if (_options.ReadLogCleanupChunkingValue > 0)
            logs = logs.Take(_options.ReadLogCleanupChunkingValue).ToList();

        logs = logs.OrderByDescending(l => l.CreatedUtc)
            .Skip(retention).ToList();
        await ProcessCleanup(logs);
    }

    private async Task ProcessCleanup(List<MessageReadLog> logs)
    {
        await _logs.DeleteRangeAsync(logs);
        _logger.LogInformation("Deleted {Count} message read logs.", logs.Count);
    }

    private DateTime ResolveCutoffDate()
        => DateTime.UtcNow.AddMonths(-_options.ReadLogRetentionMonths);
}
