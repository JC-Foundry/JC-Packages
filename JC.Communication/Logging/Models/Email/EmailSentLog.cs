using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Communication.Email.Models;
using JC.Core.Models.Auditing;

namespace JC.Communication.Logging.Models.Email;

public class EmailSentLog : AuditModel
{
    [Key]
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    
    public string EmailLogId { get; set; }
    [ForeignKey(nameof(EmailLogId))]
    public EmailLog EmailLog { get; set; }
    
    public bool Succeeded { get; set; }
    
    [Required]
    public EmailProvider Provider { get; set; }
    
    [Required]
    public DateTime SentAtUtc { get; set; }
    
    public string? MessageId { get; set; }
    
    public string? ErrorMessage { get; set; }

    public EmailSentLog()
    {
    }

    public EmailSentLog(EmailSendResult result)
    {
        Succeeded = result.Succeeded;
        Provider = result.Provider;
        SentAtUtc = result.SentAtUtc;
        MessageId = result.MessageId;
        ErrorMessage = result.ErrorMessage;
    }

    public EmailSentLog(string emailLogId, EmailSendResult result)
        : this(result)
    {
        EmailLogId = emailLogId;
    }
}