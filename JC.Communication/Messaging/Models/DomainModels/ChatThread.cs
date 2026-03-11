using System.ComponentModel.DataAnnotations;
using JC.Core.Models.Auditing;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace JC.Communication.Messaging.Models.DomainModels;

public class ChatThread : AuditModel
{
    [Key]
    [MaxLength(36)]
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(256)]
    public string Name { get; set; }
    public string? Description { get; set; }
    
    public bool IsDefaultThread { get; set; }
    public DateTime? LastActivityUtc { get; set; }
    
    public bool IsGroupThread { get; set; }
    
    public ICollection<ChatMessage> Messages { get; set; }
    public ICollection<ChatParticipant> Participants { get; set; }
    
    public ChatMetadata? ChatMetadata { get; set; }

    public bool IsGroup()
        => Participants == null! 
            ? IsGroupThread
            : Participants.Count > 2;
}