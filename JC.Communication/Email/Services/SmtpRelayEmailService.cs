using JC.Communication.Email.Models;
using JC.Communication.Email.Models.Options;
using JC.Communication.Logging.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace JC.Communication.Email.Services;

public class SmtpRelayEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly EmailOptions _options;
    private readonly EmailLogService _logService;
    private readonly ILogger<SmtpRelayEmailService> _logger;

    public SmtpRelayEmailService(IConfiguration config,
        IOptions<EmailOptions> options,
        EmailLogService logService,
        ILogger<SmtpRelayEmailService> logger)
    {
        _config = config;
        _options = options.Value;
        _logService = logService;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = message.ValidateEmailMessage();
        if (validationErrors != null)
        {
            var failed = new EmailSendResult(validationErrors, EmailProvider.SmtpRelay);
            await _logService.LogAsync(message, failed, cancellationToken);
            return failed;
        }

        EmailSendResult result;

        try
        {
            var msg = BuildEmail.BuildMimeMessage(message, _config);

            using var client = new SmtpClient();
            client.Timeout = _options.TimeoutMs;

            var socketOptions = _options.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(_options.Host, _options.Port,
                socketOptions, cancellationToken);

            var username = _config[SmtpRelayOptions.Username];
            var secret = _config[SmtpRelayOptions.Password]
                         ?? _config[SmtpRelayOptions.ApiKey]
                         ?? _config[SmtpRelayOptions.Secret];

            if (!string.IsNullOrEmpty(username))
                await client.AuthenticateAsync(username, secret, cancellationToken);
            else if (!string.IsNullOrEmpty(secret))
                await client.AuthenticateAsync("apikey", secret, cancellationToken);

            var serverResponse = await client.SendAsync(msg, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            result = new EmailSendResult(EmailProvider.SmtpRelay, serverResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients} with subject: {Subject}",
                string.Join(", ", message.ToAddresses.Select(r => r.Address)),
                message.Subject);

            result = new EmailSendResult(ex.Message, EmailProvider.SmtpRelay);
        }

        await _logService.LogAsync(message, result, cancellationToken);
        return result;
    }
}
