using JC.Communication.Logging.Models.Email;

namespace JC.Communication.Email.Models;

public sealed class EmailMessage
{
    public const string NoSubject = "NO SUBJECT";
    
    public string FromAddress { get; }
    public List<EmailRecipient> ToAddresses { get; }

    public List<EmailRecipient> CcAddresses { get; } = [];
    public List<EmailRecipient> BccAddresses { get; } = [];

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
        IEnumerable<EmailRecipient> toAddresses, IEnumerable<EmailRecipient> ccAddresses, IEnumerable<EmailRecipient> bccAddresses)
        : this(from, htmlBody, plainBody, subject, toAddresses)
    {
        CcAddresses = ccAddresses.ToList();
        BccAddresses = bccAddresses.ToList();
    }


    public (EmailLog Log, List<EmailRecipientLog> Recipients) ToSafeLog()
    {
        var log = new EmailLog
        {
            FromAddress = FromAddress,
            Subject = Subject
        };

        var recipients = ToAddresses
            .Select(r => new EmailRecipientLog(log.Id, r))
            .ToList();

        var ccRecipients = CcAddresses
            .Select(cc => new EmailRecipientLog(log.Id, cc, RecipientLogType.Cc))
            .ToList();

        var bccRecipients = BccAddresses
            .Select(bcc => new EmailRecipientLog(log.Id, bcc, RecipientLogType.Bcc))
            .ToList();

        recipients.AddRange(ccRecipients);
        recipients.AddRange(bccRecipients);
        
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