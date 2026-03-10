using JC.Communication.Logging.Models.Email;

namespace JC.Communication.Email.Models;

/// <summary>
/// Represents a single outbound email message with sender, recipients, subject, and body content.
/// </summary>
public sealed class EmailMessage
{
    /// <summary>
    /// Default subject used when no subject is provided.
    /// </summary>
    public const string NoSubject = "NO SUBJECT";

    /// <summary>
    /// The sender's email address.
    /// </summary>
    public string FromAddress { get; }

    /// <summary>
    /// The primary recipients of the email. Must contain at least one recipient.
    /// </summary>
    public List<EmailRecipient> ToAddresses { get; }

    /// <summary>
    /// Carbon copy recipients. Defaults to an empty list.
    /// </summary>
    public List<EmailRecipient> CcAddresses { get; } = [];

    /// <summary>
    /// Blind carbon copy recipients. Defaults to an empty list.
    /// </summary>
    public List<EmailRecipient> BccAddresses { get; } = [];

    /// <summary>
    /// The email subject line. Defaults to <see cref="NoSubject"/> if not provided or empty.
    /// </summary>
    public string Subject { get; }

    /// <summary>
    /// The plain text body of the email.
    /// </summary>
    public string PlainBody { get; }

    /// <summary>
    /// The HTML body of the email. Defaults to <see cref="PlainBody"/> if not explicitly provided.
    /// </summary>
    public string HtmlBody { get; }

    /// <summary>
    /// Creates an email message with a plain text body. The HTML body is set to the same value as the plain body.
    /// </summary>
    /// <param name="from">The sender's email address.</param>
    /// <param name="plainBody">The plain text body content.</param>
    /// <param name="subject">Optional subject line. Defaults to <see cref="NoSubject"/> if null or empty.</param>
    /// <param name="toAddresses">One or more recipients. Must contain at least one.</param>
    /// <exception cref="ArgumentException">Thrown if no recipients are provided.</exception>
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

    /// <summary>
    /// Creates an email message with separate HTML and plain text bodies.
    /// </summary>
    /// <param name="from">The sender's email address.</param>
    /// <param name="htmlBody">The HTML body content.</param>
    /// <param name="plainBody">The plain text body content.</param>
    /// <param name="subject">Optional subject line. Defaults to <see cref="NoSubject"/> if null or empty.</param>
    /// <param name="toAddresses">One or more recipients. Must contain at least one.</param>
    /// <exception cref="ArgumentException">Thrown if no recipients are provided.</exception>
    public EmailMessage(string from, string htmlBody, string plainBody, string? subject = null,
        params IEnumerable<EmailRecipient> toAddresses)
        : this(from, plainBody, subject, toAddresses)
    {
        HtmlBody = htmlBody;
    }

    /// <summary>
    /// Creates an email message with separate HTML and plain text bodies, including CC and BCC recipients.
    /// </summary>
    /// <param name="from">The sender's email address.</param>
    /// <param name="htmlBody">The HTML body content.</param>
    /// <param name="plainBody">The plain text body content.</param>
    /// <param name="subject">The subject line.</param>
    /// <param name="toAddresses">The primary recipients.</param>
    /// <param name="ccAddresses">Carbon copy recipients.</param>
    /// <param name="bccAddresses">Blind carbon copy recipients.</param>
    /// <exception cref="ArgumentException">Thrown if no primary recipients are provided.</exception>
    public EmailMessage(string from, string htmlBody, string plainBody, string subject,
        IEnumerable<EmailRecipient> toAddresses, IEnumerable<EmailRecipient> ccAddresses, IEnumerable<EmailRecipient> bccAddresses)
        : this(from, htmlBody, plainBody, subject, toAddresses)
    {
        CcAddresses = ccAddresses.ToList();
        BccAddresses = bccAddresses.ToList();
    }


    /// <summary>
    /// Validates the email message for common issues including missing from address, missing body,
    /// invalid recipient addresses (missing '@'), and duplicate recipients across To, CC, and BCC.
    /// </summary>
    /// <returns>A string containing all validation errors separated by newlines, or null if the message is valid.</returns>
    public string? ValidateEmailMessage()
    {
        var errors = string.Empty;

        try
        {
            if(string.IsNullOrWhiteSpace(FromAddress))
                errors = AppendError(errors, "From address is required.");

            if(FromAddress?.Contains('@') == false)
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


    /// <summary>
    /// Creates a log entry excluding email body content. Includes the from address, subject,
    /// and all recipients (To, CC, BCC) with their recipient types.
    /// </summary>
    /// <returns>A tuple containing the <see cref="EmailLog"/> and a list of <see cref="EmailRecipientLog"/> entries.</returns>
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

    /// <summary>
    /// Creates a full log entry including email body content. Extends <see cref="ToSafeLog"/>
    /// with an <see cref="EmailContentLog"/> containing the plain and HTML bodies.
    /// The HTML body is only stored if it differs from the plain body.
    /// </summary>
    /// <returns>A tuple containing the <see cref="EmailLog"/>, a list of <see cref="EmailRecipientLog"/> entries, and the <see cref="EmailContentLog"/>.</returns>
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

/// <summary>
/// Represents an email recipient with an address and optional display name.
/// </summary>
/// <param name="Address">The email address of the recipient.</param>
/// <param name="DisplayName">Optional display name for the recipient.</param>
public record EmailRecipient(string Address, string? DisplayName = null);
