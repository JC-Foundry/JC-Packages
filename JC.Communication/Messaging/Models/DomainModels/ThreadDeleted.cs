using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Core.Models.Auditing;

namespace JC.Communication.Messaging.Models.DomainModels;

public class ThreadDeleted : AuditModel
{
    [Key]
    [MaxLength(36)]
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(36)]
    public string ThreadId { get; set; }
    [ForeignKey(nameof(ThreadId))]
    public ChatThread Thread { get; set; }
    
    [Required]
    [MaxLength(36)]
    public string UserId { get; set; }

    [NotMapped] 
    public DateTime DateDeletedUtc => CreatedUtc;
}