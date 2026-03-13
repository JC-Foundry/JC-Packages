using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Core.Models.Auditing;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace JC.Communication.Messaging.Models.DomainModels;

[PrimaryKey(nameof(ThreadId), nameof(UserId))]
public class ChatParticipant : AuditModel
{
    [Required]
    [MaxLength(36)]
    public string ThreadId { get; set; }
    [ForeignKey(nameof(ThreadId))]
    public ChatThread Thread { get; set; }

    [Required]
    [MaxLength(36)]
    public string UserId { get; set; }

    public bool CanSeeHistory { get; set; } = true;
    
    [Required]
    public DateTime JoinedAtUtc { get; set; }

    public ChatParticipant()
    {
    }

    public ChatParticipant(string threadId, string userId)
    {
        ThreadId = threadId;
        UserId = userId;
        JoinedAtUtc = DateTime.UtcNow;
    }
}