using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Communication.Email.Models;
using JC.Core.Models.Auditing;

namespace JC.Communication.Logging.Models.Email;

/// <summary>
/// Persisted log entry for an email send attempt result. Linked to an <see cref="EmailLog"/>
/// and records the success/failure status, provider, timestamp, and any error details.
/// Multiple entries per email log support retry scenarios.
/// </summary>
public class EmailSentLog : LogModel
{
    /// <summary>
    /// Unique identifier for the send result log entry.
    /// </summary>
    [Key]
    public string Id { get; private set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Foreign key to the parent <see cref="EmailLog"/>.
    /// </summary>
    public string EmailLogId { get; set; }

    /// <summary>
    /// Navigation property to the parent email log entry.
    /// </summary>
    [ForeignKey(nameof(EmailLogId))]
    public EmailLog EmailLog { get; set; }

    /// <summary>
    /// Whether the send attempt succeeded.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// The email provider that handled the send attempt.
    /// </summary>
    [Required]
    public EmailProvider Provider { get; set; }

    /// <summary>
    /// The UTC timestamp of the send attempt.
    /// </summary>
    [Required]
    public DateTime SentAtUtc { get; set; }

    /// <summary>
    /// The server response string returned by the SMTP server on successful send. Null on failure or if not available.
    /// </summary>
    public string? ServerResponse { get; set; }

    /// <summary>
    /// The error message if the send failed. Null on success.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    public EmailSentLog()
    {
    }

    /// <summary>
    /// Creates a send result log entry from an <see cref="EmailSendResult"/>.
    /// </summary>
    /// <param name="result">The send result to log.</param>
    public EmailSentLog(EmailSendResult result)
    {
        Succeeded = result.Succeeded;
        Provider = result.Provider;
        SentAtUtc = result.SentAtUtc;
        ServerResponse = result.ServerResponse;
        ErrorMessage = result.ErrorMessage;
    }

    /// <summary>
    /// Creates a send result log entry linked to a specific email log.
    /// </summary>
    /// <param name="emailLogId">The parent email log ID.</param>
    /// <param name="result">The send result to log.</param>
    public EmailSentLog(string emailLogId, EmailSendResult result)
        : this(result)
    {
        EmailLogId = emailLogId;
    }
}
