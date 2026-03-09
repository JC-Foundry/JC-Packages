using JC.Communication.Email.Models;
using JC.Communication.Email.Models.Options;
using JC.Communication.Logging.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using MimeKit;

namespace JC.Communication.Email.Services;

public class MicrosoftEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly EmailOptions _options;
    private readonly EmailLogService _logService;
    private readonly ILogger<MicrosoftEmailService> _logger;

    private readonly IConfidentialClientApplication _msalClient;

    public MicrosoftEmailService(IConfiguration config,
        IOptions<EmailOptions> options,
        EmailLogService logService,
        ILogger<MicrosoftEmailService> logger)
    {
        _config = config;
        _options = options.Value;
        _logService = logService;
        _logger = logger;

        if (!_options.EnableSsl)
            throw new InvalidOperationException("SSL must be enabled for Microsoft email provider.");

        _msalClient = ConfidentialClientApplicationBuilder
            .Create(_config[MicrosoftOptions.ClientId])
            .WithClientSecret(_config[MicrosoftOptions.ClientSecret])
            .WithTenantId(_config[MicrosoftOptions.TenantId])
            .Build();
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        EmailSendResult result;

        try
        {
            var msg = BuildMimeMessage(message);

            using var client = new SmtpClient();
            client.Timeout = _options.TimeoutMs;

            await client.ConnectAsync(_options.Host, _options.Port,
                SecureSocketOptions.StartTls, cancellationToken);

            var tokenResult = await _msalClient
                .AcquireTokenForClient(["https://outlook.office365.com/.default"])
                .ExecuteAsync(cancellationToken);

            var oauth2 = new SaslMechanismOAuth2(
                message.FromAddress, tokenResult.AccessToken);

            await client.AuthenticateAsync(oauth2, cancellationToken);
            var serverResponse = await client.SendAsync(msg, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            result = new EmailSendResult(EmailProvider.Microsoft, serverResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients} with subject: {Subject}",
                string.Join(", ", message.ToAddresses.Select(r => r.Address)),
                message.Subject);

            result = new EmailSendResult(ex.Message, EmailProvider.Microsoft);
        }

        await _logService.LogAsync(message, result, cancellationToken);
        return result;
    }

    private MimeMessage BuildMimeMessage(EmailMessage message)
    {
        var msg = new MimeMessage();

        msg.From.Add(new MailboxAddress(
            _config[EmailOptions.ConfigFromDisplayName] ?? message.FromAddress,
            message.FromAddress));

        foreach (var recipient in message.ToAddresses)
            msg.To.Add(new MailboxAddress(recipient.DisplayName ?? recipient.Address, recipient.Address));

        foreach (var cc in message.CcAddresses)
            msg.Cc.Add(new MailboxAddress(cc.DisplayName ?? cc.Address, cc.Address));

        foreach (var bcc in message.BccAddresses)
            msg.Bcc.Add(new MailboxAddress(bcc.DisplayName ?? bcc.Address, bcc.Address));

        msg.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = message.PlainBody,
            HtmlBody = message.HtmlBody
        };

        msg.Body = bodyBuilder.ToMessageBody();

        return msg;
    }
}
