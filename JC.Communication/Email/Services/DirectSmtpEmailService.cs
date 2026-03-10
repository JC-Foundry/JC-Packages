using JC.Communication.Email.Models;
using JC.Communication.Email.Models.Options;
using JC.Communication.Logging.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Communication.Email.Services;

public class DirectSmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly EmailOptions _options;
    private readonly EmailLogService _logService;
    private readonly ILogger<DirectSmtpEmailService> _logger;

    public DirectSmtpEmailService(IConfiguration config,
        IOptions<EmailOptions> options,
        EmailLogService logService,
        ILogger<DirectSmtpEmailService> logger)
    {
        _config = config;
        _options = options.Value;
        _logService = logService;
        _logger = logger;
    }

    public Task<EmailSendResult> SendAsync(IEnumerable<EmailRecipient> recipients, string subject, 
        string plainBody, string? htmlBody = null, IEnumerable<EmailRecipient>? ccRecipients = null, 
        IEnumerable<EmailRecipient>? bccRecipients = null)
    {
        var fromAddress = _config[EmailOptions.ConfigFromAddress];
        if(string.IsNullOrEmpty(fromAddress))
            throw new InvalidOperationException("From address is not configured.");
        
        var message = new EmailMessage(fromAddress, htmlBody ?? string.Empty, plainBody, subject, 
            recipients, ccRecipients ?? [], bccRecipients ?? []);
        return SendAsync(message);
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = message.ValidateEmailMessage();
        if (validationErrors != null)
        {
            var failed = new EmailSendResult(validationErrors, EmailProvider.DirectSmtp);
            await _logService.LogAsync(message, failed, cancellationToken);
            return failed;
        }

        EmailSendResult result;

        try
        {
            var msg = BuildEmail.BuildMimeMessage(message, _config);

            using var client = new SmtpClient();
            client.Timeout = _options.TimeoutMs;
            client.SslProtocols = _options.SslProtocol;

            var socketOptions = _options.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(_options.Host, _options.Port,
                socketOptions, cancellationToken);

            var timeStamp = DateTime.UtcNow;
            var serverResponse = await client.SendAsync(msg, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            result = new EmailSendResult(timeStamp, EmailProvider.DirectSmtp, serverResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients} with subject: {Subject}",
                string.Join(", ", message.ToAddresses.Select(r => r.Address)),
                message.Subject);

            result = new EmailSendResult(ex.Message, EmailProvider.DirectSmtp);
        }

        await _logService.LogAsync(message, result, cancellationToken);
        return result;
    }
}
