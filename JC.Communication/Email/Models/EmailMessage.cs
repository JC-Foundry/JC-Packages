using JC.Communication.Logging.Models.Email;

namespace JC.Communication.Email.Models;

public sealed class EmailMessage
{
    public const string NoSubject = "NO SUBJECT";
    
    public string FromAddress { get; }
    public List<EmailRecipient> ToAddresses { get; }
    
    public string? Cc { get; }
    public string? Bcc { get; }

    public string Subject { get; }
    public string PlainBody { get; }
    public string HtmlBody { get; }

    public EmailMessage(string from, string plainBody, string? subject = null, params IEnumerable<EmailRecipient> toAddresses)
    {
        FromAddress = from;
        Subject = string.IsNullOrEmpty(subject) ? NoSubject : subject;
        PlainBody = plainBody;
        HtmlBody = plainBody;

        var addresses = toAddresses.ToList();
        if (addresses.Count == 0)
            throw new ArgumentException("You must provide at least one email recipient.", nameof(toAddresses));

        ToAddresses = addresses;
    }

    public EmailMessage(string from, string htmlBody, string plainBody, string? subject = null,
        params IEnumerable<EmailRecipient> toAddresses)
        : this(from, plainBody, subject, toAddresses)
    {
        HtmlBody = htmlBody;
    }

    public EmailMessage(string from, string htmlBody, string plainBody, string subject, 
        string? cc = null, string? bcc = null, params IEnumerable<EmailRecipient> toAddresses)
        : this(from, htmlBody, plainBody, subject, toAddresses)
    {
        Cc = cc;
        Bcc = bcc;
    }


    public (EmailLog Log, List<EmailRecipientLog> Recipients) ToSafeLog()
    {
        var log = new EmailLog
        {
            FromAddress = FromAddress,
            Subject = Subject,
            Bcc = Bcc,
            Cc = Cc
        };

        var recipients = ToAddresses
            .Select(r => new EmailRecipientLog(log.Id, r))
            .ToList();

        return (log, recipients);
    }

    public (EmailLog Log, List<EmailRecipientLog> Recipients, EmailContentLog ContentLog) ToFullLog()
    {
        var (log, recipients) = ToSafeLog();

        var contentLog = new EmailContentLog
        {
            EmailLogId = log.Id,
            HtmlBodyRaw = string.Equals(HtmlBody, PlainBody) ? null : HtmlBody,
            PlainBody = PlainBody
        };

        return (log, recipients, contentLog);
    }
}

public record EmailRecipient(string Address, string? DisplayName = null);