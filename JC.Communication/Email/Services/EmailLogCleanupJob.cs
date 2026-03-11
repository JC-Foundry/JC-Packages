using JC.Communication.Email.Models.Options;
using JC.Communication.Logging.Models.Email;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Communication.Email.Services;

public class EmailLogCleanupJob : IBackgroundJob
{
    private readonly IRepositoryManager _repos;
    private readonly ILogger<EmailLogCleanupJob> _logger;
    private readonly IRepositoryContext<EmailLog> _emailLogs;
    private readonly IRepositoryContext<EmailRecipientLog> _recipLog;
    private readonly IRepositoryContext<EmailContentLog> _contentLog;
    private readonly IRepositoryContext<EmailSentLog> _sentLog;
    private readonly EmailBackgroundJobOptions _options;

    public EmailLogCleanupJob(IRepositoryManager repos,
        IOptions<EmailBackgroundJobOptions> options,
        ILogger<EmailLogCleanupJob> logger)
    {
        _repos = repos;
        _logger = logger;
        _emailLogs = repos.GetRepository<EmailLog>();
        _recipLog = repos.GetRepository<EmailRecipientLog>();
        _contentLog = repos.GetRepository<EmailContentLog>();
        _sentLog = repos.GetRepository<EmailSentLog>();
        
        _options = options.Value;
    }
    
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if(!_options.RegisterEmailLogCleanupJob)
            return;
        
        var emailLogs = await _emailLogs.GetAllAsync(e => e.CreatedUtc.Date < ResolveCutoffDate(),
            x => x.OrderByDescending(e => e.CreatedUtc), cancellationToken);

        var logIds = emailLogs.Select(e => e.Id).ToList();
        var recipLogs = await _recipLog.GetAllAsync(r => logIds.Contains(r.EmailLogId), 
            x => x.OrderByDescending(r => r.CreatedUtc), cancellationToken);
        var contentLogs = await _contentLog.GetAllAsync(c => logIds.Contains(c.EmailLogId),
            x => x.OrderByDescending(c => c.CreatedUtc), cancellationToken);
        var sentLogs = await _sentLog.GetAllAsync(s => logIds.Contains(s.EmailLogId),
            x => x.OrderByDescending(s => s.CreatedUtc), cancellationToken);
        
        var retention = _options.MinimumRetentionRecords;
        if (retention == 0)
        {
            await ProcessCleanup(emailLogs, recipLogs, contentLogs, sentLogs);
            return;
        }
        
        if(retention >= emailLogs.Count)
            return;
        
        emailLogs = emailLogs.Skip(retention).ToList();
        logIds = emailLogs.Select(e => e.Id).ToList();
        
        recipLogs = recipLogs.Where(r => logIds.Contains(r.EmailLogId)).ToList();
        contentLogs = contentLogs.Where(c => logIds.Contains(c.EmailLogId)).ToList();
        sentLogs = sentLogs.Where(s => logIds.Contains(s.EmailLogId)).ToList();
        
        await ProcessCleanup(emailLogs, recipLogs, contentLogs, sentLogs);
    }

    private async Task ProcessCleanup(List<EmailLog> emailLogs, List<EmailRecipientLog> recipLogs, 
        List<EmailContentLog> contentLogs, List<EmailSentLog> sentLogs)
    {
        await _repos.BeginTransactionAsync();
        try
        {
            if(recipLogs.Count > 0)
                await _recipLog.DeleteRangeAsync(recipLogs, false);
            
            if(contentLogs.Count > 0)
                await _contentLog.DeleteRangeAsync(contentLogs, false);
            
            if(sentLogs.Count > 0)
                await _sentLog.DeleteRangeAsync(sentLogs, false);
            
            await _emailLogs.DeleteRangeAsync(emailLogs, false);
            
            await _repos.SaveChangesAsync();
            await _repos.CommitTransactionAsync();
            _logger.LogInformation("Cleaned up {Count} email logs and related log records.", emailLogs.Count);
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Error cleaning up email logs and related log records.");
            throw;
        }
    }
    
    private DateTime ResolveCutoffDate()
        => DateTime.UtcNow.AddMonths(-(_options.EmailLogRetentionMonths));
}