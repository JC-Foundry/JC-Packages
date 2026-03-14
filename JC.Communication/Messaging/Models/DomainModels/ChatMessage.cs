using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Core.Models.Auditing;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace JC.Communication.Messaging.Models.DomainModels;

/// <summary>
/// Represents a single message within a <see cref="ChatThread"/>.
/// Derives sender and timestamp from the <see cref="AuditModel"/> creation fields.
/// </summary>
public class ChatMessage : AuditModel
{
    /// <summary>Gets the unique identifier for this message.</summary>
    [Key]
    public string Id { get; private set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the ID of the thread this message belongs to.</summary>
    [Required]
    [MaxLength(36)]
    public string ThreadId { get; set; }

    /// <summary>Gets or sets the navigation property to the parent thread.</summary>
    [ForeignKey(nameof(ThreadId))]
    public ChatThread Thread { get; set; }

    /// <summary>Gets or sets the message content.</summary>
    [Required]
    [MaxLength(8192)]
    public string Message { get; set; }

    /// <summary>Gets the user ID of the message sender, derived from <see cref="BaseCreateModel.CreatedById"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the creation audit field has not been populated.</exception>
    [NotMapped]
    public string SenderUserId => CreatedById
                              ?? throw new InvalidOperationException("SenderUserId cannot be null.");

    /// <summary>Gets the UTC timestamp when the message was sent, derived from <see cref="BaseCreateModel.CreatedUtc"/>.</summary>
    [NotMapped]
    public DateTime SentAtUtc => CreatedUtc;

    /// <summary>
    /// Parameterless constructor required by EF Core.
    /// </summary>
    public ChatMessage()
    {
    }

    /// <summary>
    /// Creates a new message for the specified thread.
    /// </summary>
    /// <param name="threadId">The ID of the thread to send the message in.</param>
    /// <param name="message">The message content.</param>
    public ChatMessage(string threadId, string message)
    {
        ThreadId = threadId;
        Message = message;
    }
}