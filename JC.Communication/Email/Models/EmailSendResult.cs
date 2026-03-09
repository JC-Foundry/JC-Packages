namespace JC.Communication.Email.Models;

public class EmailSendResult
{
    public bool Succeeded { get; } = true;
    public EmailProvider Provider { get; }
    public DateTime SentAtUtc { get; } = DateTime.UtcNow;
    public string? MessageId { get; }
    
    public string? ErrorMessage { get; }

    public EmailSendResult(EmailProvider provider = EmailProvider.Microsoft, string? messageId = null)
    {
        Provider = provider;
        MessageId = messageId;
    }

    public EmailSendResult(DateTime sentAtUtc, EmailProvider provider = EmailProvider.Microsoft, 
        string? messageId = null)
        : this(provider, messageId)
    {
        SentAtUtc = sentAtUtc;
    }

    public EmailSendResult(string errorMsg, EmailProvider provider = EmailProvider.Microsoft, 
        string? messageId = null)
        : this(provider, messageId)
    {
        Succeeded = false;
        ErrorMessage = errorMsg;
    }

    public EmailSendResult(string errorMsg, DateTime sentAtUtc, 
        EmailProvider provider = EmailProvider.Microsoft, string? messageId = null)
        : this(sentAtUtc, provider, messageId)
    {
        Succeeded = false;
        ErrorMessage = errorMsg;
    }
}