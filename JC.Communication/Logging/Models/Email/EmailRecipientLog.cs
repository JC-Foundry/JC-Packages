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
}