using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Communication.Email.Models;
using JC.Core.Models.Auditing;

namespace JC.Communication.Logging.Models.Email;

public class EmailRecipientLog : AuditModel
{
    [Key]
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    
    public string EmailLogId { get; set; }
    [ForeignKey(nameof(EmailLogId))]
    public EmailLog EmailLog { get; set; }
    
    [Required]
    [MaxLength(256)]
    public string Address { get; set; }
    
    public string? DisplayName { get; set; }

    [Required] public RecipientLogType RecipientLogType { get; set; } = RecipientLogType.To;

    public EmailRecipientLog()
    {
    }
    
    public EmailRecipientLog(EmailRecipient recipient)
    {
        Address = recipient.Address;
        DisplayName = recipient.DisplayName;
    }
    
    public EmailRecipientLog(string emailLogId, EmailRecipient recipient)
        : this(recipient)
    {
        EmailLogId = emailLogId;
    }

    public EmailRecipientLog(EmailRecipient recipient, RecipientLogType logType)
        : this(recipient)
    {
        RecipientLogType = logType;
    }

    public EmailRecipientLog(string emailLogId, EmailRecipient recipient, RecipientLogType logType)
        : this(emailLogId, recipient)
    {
        RecipientLogType = logType;
    }
}

public enum RecipientLogType
{
    To,
    Cc,
    Bcc
}