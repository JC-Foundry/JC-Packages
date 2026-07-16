using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Core.Models.Auditing;
using JC.Core.Models.MultiTenancy;

namespace JC.Communication.Messaging.Models.DomainModels;

public class ThreadDeleted : AuditModel, IMultiTenancy
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

    [MaxLength(36)]
    public string? TenantId { get; set; }
    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }
}