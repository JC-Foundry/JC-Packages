using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Communication.Email.Models.Options;
using JC.Core.Models.Auditing;
using JC.Core.Models.MultiTenancy;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace JC.Communication.Logging.Models.Email;

/// <summary>
/// Persisted log entry for an outbound email. Contains sender and subject metadata,
/// with navigation properties to recipients, content, and send results.
/// </summary>
public class EmailLog : LogModel, IMultiTenancy
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
    /// The email body content log. Only populated when <see cref="EmailLoggingMode.FullLog"/> is used.
    /// </summary>
    public EmailContentLog? EmailContentLog { get; set; }

    /// <summary>
    /// The send attempt results for this email. Supports multiple entries for retry scenarios.
    /// </summary>
    public ICollection<EmailSentLog> EmailSentLogs { get; set; }

    [MaxLength(36)]
    public string? TenantId { get; set; }
    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }
}
