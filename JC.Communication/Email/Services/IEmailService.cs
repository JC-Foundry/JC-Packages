using JC.Communication.Email.Models;

namespace JC.Communication.Email.Services;

public interface IEmailService
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
