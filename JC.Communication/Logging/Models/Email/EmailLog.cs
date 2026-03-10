using System.ComponentModel.DataAnnotations;
using JC.Core.Models.Auditing;

namespace JC.Communication.Logging.Models.Email;

/// <summary>
/// Persisted log entry for an outbound email. Contains sender and subject metadata,
/// with navigation properties to recipients, content, and send results.
/// </summary>
public class EmailLog : AuditModel
{
    /// <summary>
    /// Unique identifier for the email log entry.
    /// </summary>
    [Key]
    public string Id { get; private set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The sender's email address.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string FromAddress { get; set; }

    /// <summary>
    /// The email subject line.
    /// </summary>
    [Required]
    [MaxLength(1024)]
    public string Subject { get; set; }

    /// <summary>
    /// The recipients associated with this email log entry.
    /// </summary>
    public ICollection<EmailRecipientLog> EmailRecipientLogs { get; set; }

    /// <summary>
    /// The email body content log. Only populated when <see cref="Options.EmailLoggingMode.FullLog"/> is used.
    /// </summary>
    public EmailContentLog? EmailContentLog { get; set; }

    /// <summary>
    /// The send attempt results for this email. Supports multiple entries for retry scenarios.
    /// </summary>
    public ICollection<EmailSentLog> EmailSentLogs { get; set; }
}
