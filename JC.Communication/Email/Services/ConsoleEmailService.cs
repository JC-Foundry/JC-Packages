using JC.Communication.Email.Models;
using JC.Communication.Email.Models.Options;
using JC.Communication.Logging.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Communication.Email.Services;

public class ConsoleEmailService : IEmailService
{
    private readonly EmailLogService _logService;
    private readonly ILogger<ConsoleEmailService> _logger;
    private readonly LogLevel _logLevel;

    public ConsoleEmailService(IOptions<EmailOptions> options,
        EmailLogService logService,
        ILogger<ConsoleEmailService> logger)
    {
        _logService = logService;
        _logger = logger;
        _logLevel = options.Value.LogLevel;
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