using JC.Communication.Email.Models;
using JC.Communication.Email.Models.Options;
using JC.Communication.Logging.Models.Email;
using JC.Core.Services.DataRepositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Communication.Logging.Services;

/// <summary>
/// Handles persistence of email send attempts to the database.
/// Respects the configured <see cref="EmailLoggingMode"/> to determine what data is logged.
/// </summary>
public class EmailLogService
{
    private readonly IRepositoryManager _repos;
    private readonly ILogger<EmailLogService> _logger;
    private readonly EmailLoggingMode _loggingMode;

    /// <summary>
    /// Creates a new instance of the email log service.
    /// </summary>
    /// <param name="repos">The repository manager for database operations.</param>
    /// <param name="options">The email options containing the logging mode configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public EmailLogService(IRepositoryManager repos,
        IOptions<EmailOptions> options,
        ILogger<EmailLogService> logger)
    {
        _repos = repos;
        _logger = logger;
        _loggingMode = options.Value.LoggingMode;
    }

    /// <summary>
    /// Logs an email send attempt to the database within a transaction.
    /// Creates an <see cref="EmailLog"/>, associated <see cref="EmailRecipientLog"/> entries,
    /// an optional <see cref="EmailContentLog"/> (when using <see cref="EmailLoggingMode.FullLog"/>),
    /// and an <see cref="EmailSentLog"/> recording the send result.
    /// Does nothing if <see cref="EmailLoggingMode.None"/> is configured.
    /// </summary>
    /// <param name="message">The email message that was sent or attempted.</param>
    /// <param name="result">The result of the send attempt.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public async Task LogAsync(EmailMessage message, EmailSendResult result,
        CancellationToken cancellationToken = default)
    {
        if (_loggingMode == EmailLoggingMode.None)
            return;

        EmailLog log;
        List<EmailRecipientLog> recipientLogs;
        EmailContentLog? contentLog = null;
        if (_loggingMode == EmailLoggingMode.FullLog)
        {
            (log, recipientLogs, contentLog) = message.ToFullLog();
        }
        else
        {
            (log, recipientLogs) = message.ToSafeLog();
        }

        var sentLog = new EmailSentLog(log.Id, result);

        await _repos.BeginTransactionAsync(cancellationToken);
        try
        {
            await _repos.GetRepository<EmailLog>()
                .AddAsync(log, saveNow: false, cancellationToken: cancellationToken);

            await _repos.GetRepository<EmailRecipientLog>()
                .AddAsync(recipientLogs, saveNow: false, cancellationToken: cancellationToken);

            if(contentLog != null)
                await _repos.GetRepository<EmailContentLog>()
                    .AddAsync(contentLog, saveNow: false, cancellationToken: cancellationToken);

            await _repos.GetRepository<EmailSentLog>()
                .AddAsync(sentLog, saveNow: false, cancellationToken: cancellationToken);

            await _repos.SaveChangesAsync(cancellationToken);
            await _repos.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Unable to create email log");
        }
    }
}
