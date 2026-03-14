using JC.Core.Models;
using JC.Core.Models.Auditing;
using JC.Core.Models.Options;
using JC.Core.Services.DataRepositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Core.Services;

public class AuditCleanupJob : IBackgroundJob
{
    private readonly IRepositoryContext<AuditEntry> _audits;
    private readonly ILogger<AuditCleanupJob> _logger;
    private readonly CoreBackgroundJobOptions _options;

    public AuditCleanupJob(IRepositoryContext<AuditEntry> audits,
        IOptions<CoreBackgroundJobOptions> options,
        ILogger<AuditCleanupJob> logger)
    {
        _audits = audits;
        _logger = logger;
        _options = options.Value;
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableAuditCleanupJob)
        {
            _logger.LogDebug("Audit cleanup job is disabled.");
            return;
        }
        
        //Get audits for cleanup:
        var oldAudits = await _audits.GetAllAsync(a => a.AuditDate.Date < ResolveCutoffDate(),
            x => x.OrderByDescending(a => a.AuditDate), 
            cancellationToken);
        
        //Determine if a minimum number of entries should be kept:
        var retention = _options.MinimumRetentionRecords;
        if (retention == 0)
        {
            await ProcessCleanup(oldAudits);
            return;
        }
        
        //Skip if retention is greater than number of existing audits:
        if (retention >= oldAudits.Count)
        {
            _logger.LogInformation("Skipping audit cleanup as retention ({0}) is greater than existing audits ({1}).", 
                retention, oldAudits.Count);
            return;
        }

        //Chunk the cleanup if value provided:
        if (_options.AuditCleanupChunkingValue > 0)
            oldAudits = oldAudits.Take(_options.AuditCleanupChunkingValue).ToList();
        
        var entriesToRemove = new List<AuditEntry>();
        if (_options.RetentionRecordsPerTable)
        {
            var grouped = oldAudits.GroupBy(a => a.TableName).ToList();
            foreach (var entries in grouped
                         .Select(g => g.OrderByDescending(a => a.AuditDate).ToList())
                         .Where(entries => retention < entries.Count))
            {
                var rm = entries.Skip(retention);
                entriesToRemove.AddRange(rm);
            }
        }
        else
        {
            entriesToRemove =  oldAudits.OrderByDescending(a => a.AuditDate)
                .Skip(retention).ToList();
        }

        await ProcessCleanup(entriesToRemove);
    }

    private async Task ProcessCleanup(List<AuditEntry> entries)
    {
        await _audits.DeleteRangeAsync(entries);
        _logger.LogInformation("Deleted {Count} audit entries.", entries.Count);
    }

    private DateTime ResolveCutoffDate()
        => DateTime.UtcNow.AddMonths(-(_options.AuditRetentionMonths));
}