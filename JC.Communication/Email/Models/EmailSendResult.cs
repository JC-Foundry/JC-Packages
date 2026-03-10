namespace JC.Communication.Email.Models;

/// <summary>
/// Represents the result of an email send attempt, including success/failure status, provider, timestamps, and error details.
/// </summary>
public class EmailSendResult
{
    /// <summary>
    /// Whether the email was sent successfully. Defaults to true.
    /// </summary>
    public bool Succeeded { get; } = true;

    /// <summary>
    /// The email provider that handled the send attempt.
    /// </summary>
    public EmailProvider Provider { get; }

    /// <summary>
    /// The UTC timestamp of the send attempt. Defaults to the current UTC time.
    /// </summary>
    public DateTime SentAtUtc { get; } = DateTime.UtcNow;

    /// <summary>
    /// The server response string returned by the SMTP server on successful send. Null on failure or if not available.
    /// </summary>
    public string? ServerResponse { get; }

    /// <summary>
    /// The error message if the send failed. Null on success.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates a successful send result.
    /// </summary>
    /// <param name="provider">The email provider that handled the send.</param>
    /// <param name="messageId">Optional server response string returned by the SMTP server.</param>
    public EmailSendResult(EmailProvider provider = EmailProvider.Microsoft, string? messageId = null)
    {
        Provider = provider;
        ServerResponse = messageId;
    }

    /// <summary>
    /// Creates a successful send result with a specific timestamp.
    /// </summary>
    /// <param name="sentAtUtc">The UTC timestamp of the send.</param>
    /// <param name="provider">The email provider that handled the send.</param>
    /// <param name="messageId">Optional server response string returned by the SMTP server.</param>
    public EmailSendResult(DateTime sentAtUtc, EmailProvider provider = EmailProvider.Microsoft,
        string? messageId = null)
        : this(provider, messageId)
    {
        SentAtUtc = sentAtUtc;
    }

    /// <summary>
    /// Creates a failed send result with an error message.
    /// </summary>
    /// <param name="errorMsg">The error message describing the failure.</param>
    /// <param name="provider">The email provider that handled the send attempt.</param>
    /// <param name="messageId">Optional server response string returned by the SMTP server.</param>
    public EmailSendResult(string errorMsg, EmailProvider provider = EmailProvider.Microsoft,
        string? messageId = null)
        : this(provider, messageId)
    {
        Succeeded = false;
        ErrorMessage = errorMsg;
    }

    /// <summary>
    /// Creates a failed send result with an error message and specific timestamp.
    /// </summary>
    /// <param name="errorMsg">The error message describing the failure.</param>
    /// <param name="sentAtUtc">The UTC timestamp of the send attempt.</param>
    /// <param name="provider">The email provider that handled the send attempt.</param>
    /// <param name="messageId">Optional server response string returned by the SMTP server.</param>
    public EmailSendResult(string errorMsg, DateTime sentAtUtc,
        EmailProvider provider = EmailProvider.Microsoft, string? messageId = null)
        : this(sentAtUtc, provider, messageId)
    {
        Succeeded = false;
        ErrorMessage = errorMsg;
    }
}
