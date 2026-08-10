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

    private readonly List<EmailAttachment> _attachments = [];

    /// <summary>
    /// The files attached to the email. Add to this using <see cref="WithAttachment(EmailAttachment)"/>
    /// and its overloads. Defaults to empty.
    /// </summary>
    public IReadOnlyList<EmailAttachment> Attachments => _attachments;

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
        if(string.IsNullOrWhiteSpace(from))
            throw new ArgumentException("From address is required.", nameof(from));
        
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
        HtmlBody = string.IsNullOrEmpty(htmlBody) ? plainBody : htmlBody;
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
    /// Attaches a file to the email.
    /// </summary>
    /// <param name="attachment">The attachment to add.</param>
    /// <returns>The same message, so calls can be chained.</returns>
    public EmailMessage WithAttachment(EmailAttachment attachment)
    {
        _attachments.Add(attachment);
        return this;
    }

    /// <summary>
    /// Attaches a file to the email from its raw content.
    /// </summary>
    /// <param name="fileName">The file name shown to the recipient, including its extension.</param>
    /// <param name="content">The raw file content.</param>
    /// <param name="contentType">Optional MIME type. Inferred from <paramref name="fileName"/> when omitted.</param>
    /// <returns>The same message, so calls can be chained.</returns>
    public EmailMessage WithAttachment(string fileName, byte[] content, string? contentType = null)
        => WithAttachment(new EmailAttachment(fileName, content, contentType));

    /// <summary>
    /// Attaches several files to the email.
    /// </summary>
    /// <param name="attachments">The attachments to add.</param>
    /// <returns>The same message, so calls can be chained.</returns>
    public EmailMessage WithAttachments(IEnumerable<EmailAttachment> attachments)
    {
        _attachments.AddRange(attachments);
        return this;
    }


    /// <summary>
    /// Validates the email message for common issues including missing from address, missing body,
    /// invalid recipient addresses (missing '@'), duplicate recipients across To, CC, and BCC,
    /// and unusable or oversized attachments.
    /// </summary>
    /// <param name="maxTotalAttachmentBytes">
    /// The combined attachment size allowed, in bytes. Zero or a negative value disables the check.
    /// Providers cap the encoded message rather than the raw bytes, and base64 inflates content by
    /// roughly a third, so this limit should sit comfortably below the provider's advertised cap.
    /// </param>
    /// <returns>A string containing all validation errors separated by newlines, or null if the message is valid.</returns>
    public string? ValidateEmailMessage(long maxTotalAttachmentBytes = 0)
    {
        var errors = string.Empty;

        try
        {
            if(string.IsNullOrWhiteSpace(FromAddress))
                errors = AppendError(errors, "From address is required.");

            if(FromAddress.Contains('@') == false)
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

            foreach (var attachmentError in _attachments.Select(a => a.Validate()).Where(e => e != null))
                errors = AppendError(errors, attachmentError!);

            var totalAttachmentBytes = _attachments.Sum(a => a.SizeBytes);
            if (maxTotalAttachmentBytes > 0 && totalAttachmentBytes > maxTotalAttachmentBytes)
                errors = AppendError(errors,
                    $"Attachments total {totalAttachmentBytes:N0} bytes, exceeding the {maxTotalAttachmentBytes:N0} byte limit.");
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

    /// <summary>
    /// Creates log entries describing this message's attachments. Metadata only — attachment content
    /// is never included, so this is safe to call under any <see cref="Options.EmailLoggingMode"/>.
    /// </summary>
    /// <param name="emailLogId">The identifier of the <see cref="EmailLog"/> these attachments belong to.</param>
    /// <returns>One <see cref="EmailAttachmentLog"/> per attachment, or an empty list if there are none.</returns>
    public List<EmailAttachmentLog> ToAttachmentLogs(string emailLogId)
        => _attachments
            .Select(a => new EmailAttachmentLog
            {
                EmailLogId = emailLogId,
                FileName = a.FileName,
                ContentType = a.ResolvedContentType,
                SizeBytes = a.SizeBytes
            })
            .ToList();
}

/// <summary>
/// Represents an email recipient with an address and optional display name.
/// </summary>
/// <param name="Address">The email address of the recipient.</param>
/// <param name="DisplayName">Optional display name for the recipient.</param>
public record EmailRecipient(string Address, string? DisplayName = null);
