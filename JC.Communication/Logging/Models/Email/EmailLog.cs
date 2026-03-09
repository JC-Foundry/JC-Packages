using System.ComponentModel.DataAnnotations;
using JC.Core.Models.Auditing;

namespace JC.Communication.Logging.Models.Email;

public class EmailLog : AuditModel
{
    [Key]
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(256)]
    public string FromAddress { get; set; }
    
    public string? Cc { get; set; }
    public string? Bcc { get; set; }

    [Required]
    [MaxLength(1024)]
    public string Subject { get; set; }
    
    public ICollection<EmailRecipientLog> EmailRecipientLogs { get; set; }
    public ICollection<EmailContentLog> EmailContentLogs { get; set; }
    public ICollection<EmailSentLog> EmailSentLogs { get; set; }
}