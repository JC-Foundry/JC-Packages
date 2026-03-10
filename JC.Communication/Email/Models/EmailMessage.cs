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


    public string? ValidateEmailMessage()
    {
        var errors = string.Empty;

        try
        {
            if(string.IsNullOrWhiteSpace(FromAddress))
                errors = AppendError(errors, "From address is required.");
        
            if(FromAddress?.Contains('@') != false)
                errors = AppendError(errors, "Invalid From address.");
        
            if(string.IsNullOrWhiteSpace(PlainBody))
                errors = AppendError(errors, "Email body is required.");
        
            var allAddresses = ToAddresses.Select(r => r.Address)
                .Concat(CcAddresses.Select(r => r.Address))
                .Concat(BccAddresses.Select(r => r.Address))
                .ToList();

            var invalid = allAddresses.Where(a => string.IsNullOrWhiteSpace(a) || !a.Contains('@')).ToList();
            if (invalid.Count > 0)
                errors = AppendError(errors, $"Invalid recipient addresses: {string.Join(", ", invalid.Select(a => string.IsNullOrWhiteSpace(a) ? "(empty)" : a))}");

            var duplicates = allAddresses
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .GroupBy(a => a, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count > 0)
                errors = AppendError(errors, $"Duplicate recipients found: {string.Join(", ", duplicates)}");
        }
        catch (NullReferenceException)
        {
            errors = AppendError(errors, "One or more email addresses are invalid.");    
        }
        
        return string.IsNullOrEmpty(errors) ? null : errors;
    }

    private string AppendError(string errors, string err)
    {
        if(!string.IsNullOrEmpty(errors)) errors += Environment.NewLine;
        return errors + err;
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