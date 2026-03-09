using JC.Communication.Email.Models;

namespace JC.Communication.Email.Services;

public class ConsoleEmailService : IEmailService
{
    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}