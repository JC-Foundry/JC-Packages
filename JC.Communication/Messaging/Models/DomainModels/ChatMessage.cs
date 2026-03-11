using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Core.Models.Auditing;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace JC.Communication.Messaging.Models.DomainModels;

public class ChatMessage : AuditModel
{
    [Key]
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(36)]
    public string ThreadId { get; set; }
    [ForeignKey(nameof(ThreadId))]
    public ChatThread Thread { get; set; }
    
    [Required]
    [MaxLength(8192)]
    public string Message { get; set; }
    
    [NotMapped]
    public string SenderUserId => CreatedById 
                              ?? throw new InvalidOperationException("SenderUserId cannot be null.");
    [NotMapped]
    public DateTime SentAtUtc => CreatedUtc;

    public ChatMessage()
    {
    }

    public ChatMessage(string threadId, string message)
    {
        ThreadId = threadId;
        Message = message;
    }
}