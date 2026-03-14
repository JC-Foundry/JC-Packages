using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Core.Models.Auditing;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace JC.Communication.Messaging.Models.DomainModels;

/// <summary>
/// Represents a user's membership in a <see cref="ChatThread"/>.
/// Uses a composite primary key of (<see cref="ThreadId"/>, <see cref="UserId"/>).
/// Supports soft-delete via <see cref="AuditModel"/> for participant removal and re-join scenarios.
/// </summary>
[PrimaryKey(nameof(ThreadId), nameof(UserId))]
public class ChatParticipant : AuditModel
{
    /// <summary>Gets or sets the ID of the thread this participant belongs to.</summary>
    [Required]
    [MaxLength(36)]
    public string ThreadId { get; set; }

    /// <summary>Gets or sets the navigation property to the parent thread.</summary>
    [ForeignKey(nameof(ThreadId))]
    public ChatThread Thread { get; set; }

    /// <summary>Gets or sets the user ID of the participant.</summary>
    [Required]
    [MaxLength(36)]
    public string UserId { get; set; }

    /// <summary>Gets or sets whether this participant can see messages sent before they joined.</summary>
    public bool CanSeeHistory { get; set; } = true;

    /// <summary>Gets or sets the UTC timestamp when the participant joined (or re-joined) the thread.</summary>
    [Required]
    public DateTime JoinedAtUtc { get; set; }

    /// <summary>
    /// Parameterless constructor required by EF Core.
    /// </summary>
    public ChatParticipant()
    {
    }

    /// <summary>
    /// Creates a new participant for the specified thread.
    /// </summary>
    /// <param name="threadId">The ID of the thread to join.</param>
    /// <param name="userId">The user ID of the participant.</param>
    public ChatParticipant(string threadId, string userId)
    {
        ThreadId = threadId;
        UserId = userId;
        JoinedAtUtc = DateTime.UtcNow;
    }
}