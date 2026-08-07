using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Core.Models.Auditing;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace JC.Communication.Logging.Models.Email;

/// <summary>
/// Persisted log entry describing a file attached to an outbound email. Records metadata only —
/// attachment content is never written to the database, whatever the configured
/// <see cref="JC.Communication.Email.Models.Options.EmailLoggingMode"/>.
/// Linked to an <see cref="EmailLog"/> as a many-to-one relationship.
/// </summary>
public class EmailAttachmentLog : LogModel
{
    /// <summary>
    /// Unique identifier for the attachment log entry.
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
    /// The file name the attachment was sent under.
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string FileName { get; set; }

    /// <summary>
    /// The MIME type the attachment was sent as.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string ContentType { get; set; }

    /// <summary>
    /// The size of the attachment in bytes, before transfer encoding was applied.
    /// </summary>
    public long SizeBytes { get; set; }
}
