using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Communication.Email.Models;
using JC.Core.Models.Auditing;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace JC.Communication.Logging.Models.Email;

/// <summary>
/// Persisted log entry for an email recipient. Linked to an <see cref="EmailLog"/>
/// and categorised by <see cref="RecipientLogType"/> (To, CC, or BCC).
/// </summary>
public class EmailRecipientLog : LogModel
{
    /// <summary>
    /// Unique identifier for the recipient log entry.
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
    /// The recipient's email address.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Address { get; set; }

    /// <summary>
    /// The recipient's display name, if provided.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The type of recipient (To, CC, or BCC). Defaults to <see cref="RecipientLogType.To"/>.
    /// </summary>
    [Required] public RecipientLogType RecipientLogType { get; set; } = RecipientLogType.To;

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    public EmailRecipientLog()
    {
    }

    /// <summary>
    /// Creates a recipient log entry from an <see cref="EmailRecipient"/>.
    /// </summary>
    /// <param name="recipient">The email recipient to log.</param>
    public EmailRecipientLog(EmailRecipient recipient)
    {
        Address = recipient.Address;
        DisplayName = recipient.DisplayName;
    }

    /// <summary>
    /// Creates a recipient log entry linked to a specific email log.
    /// </summary>
    /// <param name="emailLogId">The parent email log ID.</param>
    /// <param name="recipient">The email recipient to log.</param>
    public EmailRecipientLog(string emailLogId, EmailRecipient recipient)
        : this(recipient)
    {
        EmailLogId = emailLogId;
    }

    /// <summary>
    /// Creates a recipient log entry with a specific recipient type.
    /// </summary>
    /// <param name="recipient">The email recipient to log.</param>
    /// <param name="logType">The recipient type (To, CC, or BCC).</param>
    public EmailRecipientLog(EmailRecipient recipient, RecipientLogType logType)
        : this(recipient)
    {
        RecipientLogType = logType;
    }

    /// <summary>
    /// Creates a recipient log entry linked to a specific email log with a specific recipient type.
    /// </summary>
    /// <param name="emailLogId">The parent email log ID.</param>
    /// <param name="recipient">The email recipient to log.</param>
    /// <param name="logType">The recipient type (To, CC, or BCC).</param>
    public EmailRecipientLog(string emailLogId, EmailRecipient recipient, RecipientLogType logType)
        : this(emailLogId, recipient)
    {
        RecipientLogType = logType;
    }
}

/// <summary>
/// The type of email recipient for logging purposes.
/// </summary>
public enum RecipientLogType
{
    /// <summary>
    /// A primary (To) recipient.
    /// </summary>
    To,

    /// <summary>
    /// A carbon copy (CC) recipient.
    /// </summary>
    Cc,

    /// <summary>
    /// A blind carbon copy (BCC) recipient.
    /// </summary>
    Bcc
}
