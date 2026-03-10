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

/// <summary>
/// Sends email via Microsoft 365 / Exchange Online SMTP relay using OAuth2 (MSAL) authentication.
/// Requires Azure AD app registration with <c>Mail.Send</c> application permission and
/// valid tenant ID, client ID, and client secret configuration.
/// </summary>
/// <remarks>
/// SMTP authentication is performed using <see cref="EmailMessage.FromAddress"/> as the OAuth2 identity.
/// The Azure AD app must have permission to send as that address. This means the from address must
/// correspond to a mailbox or shared mailbox that the app has "Send As" or "Send on Behalf Of"
/// permission for. Mismatched addresses will be rejected by the Microsoft SMTP relay at runtime.
/// </remarks>
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
            var failed = new EmailSendResult(validationErrors, EmailProvider.Microsoft);
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
}
