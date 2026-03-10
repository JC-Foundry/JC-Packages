using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Core.Models.Auditing;

namespace JC.Communication.Logging.Models.Email;

/// <summary>
/// Persisted log entry for email body content. Only created when <see cref="Options.EmailLoggingMode.FullLog"/> is used.
/// Linked to an <see cref="EmailLog"/> as a one-to-one relationship.
/// </summary>
public class EmailContentLog : LogModel
{
    /// <summary>
    /// Unique identifier for the content log entry.
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
    /// The plain text body of the email.
    /// </summary>
    [Required]
    public string PlainBody { get; set; }

    /// <summary>
    /// The raw HTML body of the email. Null if the HTML body was identical to the plain body.
    /// </summary>
    public string? HtmlBodyRaw { get; set; }

    /// <summary>
    /// The resolved HTML body. Returns <see cref="HtmlBodyRaw"/> if set, otherwise falls back to <see cref="PlainBody"/>.
    /// Not mapped to the database.
    /// </summary>
    [NotMapped]
    public string HtmlBody => string.IsNullOrEmpty(HtmlBodyRaw) ? PlainBody : HtmlBodyRaw;
}
