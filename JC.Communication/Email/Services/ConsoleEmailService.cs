using JC.Communication.Email.Models;
using JC.Communication.Email.Models.Options;
using JC.Communication.Logging.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Communication.Email.Services;

/// <summary>
/// Development/testing email provider that outputs email content to the application logger
/// instead of sending via SMTP. The log level is configurable via <see cref="EmailOptions.LogLevel"/>.
/// </summary>
/// <remarks>
/// This provider always outputs the email body (plain text) to the application logger,
/// regardless of the <see cref="EmailLoggingMode"/> setting. The logging mode only controls what is
/// persisted to the database. If email body content is sensitive, be aware that console output will
/// still contain it.
/// </remarks>
public class ConsoleEmailService : IEmailService
{
    private readonly EmailLogService _logService;
    private readonly IConfiguration _config;
    private readonly ILogger<ConsoleEmailService> _logger;
    private readonly LogLevel _logLevel;

    public ConsoleEmailService(IOptions<EmailOptions> options,
        EmailLogService logService,
        IConfiguration _config,
        ILogger<ConsoleEmailService> logger)
    {
        _logService = logService;
        this._config = _config;
        _logger = logger;
        _logLevel = options.Value.LogLevel;
    }

    public Task<EmailSendResult> SendAsync(IEnumerable<EmailRecipient> recipients, string subject, 
        string plainBody, string? htmlBody = null, IEnumerable<EmailRecipient>? ccRecipients = null, 
        IEnumerable<EmailRecipient>? bccRecipients = null)
    {
        var fromAddress = _config[EmailOptions.ConfigFromAddress];
        if(string.IsNullOrEmpty(fromAddress))
            throw new InvalidOperationException("From address is not configured.");
        
        var message = new EmailMessage(fromAddress, plainBody, subject, recipients);
        return SendAsync(message);
    }
    
    public async Task<EmailSendResult> SendAsync(EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = message.ValidateEmailMessage();
        if (validationErrors != null)
        {
            var failed = new EmailSendResult(validationErrors, EmailProvider.Console);
            await _logService.LogAsync(message, failed, cancellationToken);
            return failed;
        }

        _logger.Log(_logLevel,
            "Email from {From} to {To} | Subject: {Subject}\n\n{Body}",
            message.FromAddress,
            string.Join(", ", message.ToAddresses.Select(r => r.Address)),
            message.Subject,
            message.PlainBody);

        if (message.CcAddresses.Count > 0)
            _logger.Log(_logLevel, "CC: {Cc}",
                string.Join(", ", message.CcAddresses.Select(r => r.Address)));

        if (message.BccAddresses.Count > 0)
            _logger.Log(_logLevel, "BCC: {Bcc}",
                string.Join(", ", message.BccAddresses.Select(r => r.Address)));

        var result = new EmailSendResult(EmailProvider.Console);

        await _logService.LogAsync(message, result, cancellationToken);
        return result;
    }
}