using JC.Communication.Email.Models;
using JC.Communication.Email.Models.Options;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace JC.Communication.Email.Services;

/// <summary>
/// Provides email sending capabilities. Implementations handle provider-specific delivery
/// (e.g. Microsoft OAuth, SMTP relay, direct SMTP, or console logging).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email using the default from address configured in <c>Communication:Email:DefaultFromAddress</c>.
    /// Constructs an <see cref="EmailMessage"/> internally from the provided parameters.
    /// </summary>
    /// <param name="recipients">The primary recipients of the email. Must contain at least one recipient.</param>
    /// <param name="subject">The email subject line.</param>
    /// <param name="plainBody">The plain text body of the email. Also used as the HTML body if <paramref name="htmlBody"/> is not provided.</param>
    /// <param name="htmlBody">Optional HTML body. When null, <paramref name="plainBody"/> is used for both plain and HTML content.</param>
    /// <param name="ccRecipients">Optional carbon copy recipients.</param>
    /// <param name="bccRecipients">Optional blind carbon copy recipients.</param>
    /// <returns>An <see cref="EmailSendResult"/> indicating whether the send succeeded or failed, including any error details.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the default from address is not configured.</exception>
    Task<EmailSendResult> SendAsync(
        IEnumerable<EmailRecipient> recipients,
        string subject,
        string plainBody,
        string? htmlBody = null,
        IEnumerable<EmailRecipient>? ccRecipients = null,
        IEnumerable<EmailRecipient>? bccRecipients = null);

    /// <summary>
    /// Sends a fully constructed <see cref="EmailMessage"/>. Use this overload when you need full control
    /// over the from address, recipients, and body content.
    /// The message is validated before sending. If validation fails, a failed <see cref="EmailSendResult"/>
    /// is returned and the attempt is logged.
    /// </summary>
    /// <param name="message">The email message to send.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An <see cref="EmailSendResult"/> indicating whether the send succeeded or failed, including any error details.</returns>
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Internal helper for building <see cref="MimeMessage"/> instances from <see cref="EmailMessage"/> models.
/// Shared across all SMTP-based email service implementations.
/// </summary>
internal static class BuildEmail
{
    /// <summary>
    /// Builds a <see cref="MimeMessage"/> from an <see cref="EmailMessage"/>, setting the from address
    /// (using the configured display name if available), all recipients (To, CC, BCC), subject, and body content.
    /// </summary>
    /// <param name="message">The email message to convert.</param>
    /// <param name="config">The application configuration, used to resolve the default from display name.</param>
    /// <returns>A fully constructed <see cref="MimeMessage"/> ready for sending via SMTP.</returns>
    internal static MimeMessage BuildMimeMessage(EmailMessage message, IConfiguration config)
    {
        var msg = new MimeMessage();

        msg.From.Add(new MailboxAddress(
            config[EmailOptions.ConfigFromDisplayName] ?? message.FromAddress,
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
