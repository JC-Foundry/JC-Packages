using JC.Communication.Email.Models;
using JC.Communication.Email.Models.Options;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace JC.Communication.Email.Services;

public interface IEmailService
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

internal static class BuildEmail
{
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
