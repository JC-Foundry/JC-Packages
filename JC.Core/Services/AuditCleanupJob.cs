using JC.Core.Models;
using JC.Core.Models.Auditing;
using JC.Core.Models.Options;
using JC.Core.Services.DataRepositories;
using Microsoft.Extensions.Options;

namespace JC.Core.Services;

public class AuditCleanupJob : IBackgroundJob
{
    private readonly IRepositoryContext<AuditEntry> _audits;
    private readonly CoreBackgroundJobOptions _options;

    public AuditCleanupJob(IRepositoryContext<AuditEntry> audits,
        IOptions<CoreBackgroundJobOptions> options)
    {
        _audits = audits;
        _options = options.Value;
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if(_options.RegisterAuditCleanupJob)
            return;
        
        //Get audits for cleanup:
        var oldAudits = await _audits.GetAllAsync(a => a.AuditDate.Date < ResolveCutoffDate(),
            x => x.OrderByDescending(a => a.AuditDate), 
            cancellationToken);

        //Chunk the cleanup if value provided:
        if (_options.CleanupChunkingValue > 0)
            oldAudits = oldAudits.Take(_options.CleanupChunkingValue).ToList();
        
        //Determine if a minimum number of entries should be kept:
        var retention = _options.MinimumRetentionRecords;
        if (retention == 0)
        {
            await ProcessCleanup(oldAudits);
            return;
        }
        
        //Skip if retention is greater than number of existing audits:
        if (retention >= oldAudits.Count)
            return;

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
    }

    private DateTime ResolveCutoffDate()
        => DateTime.UtcNow.AddMonths(-(_options.AuditRetentionMonths));
}