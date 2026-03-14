using System.ComponentModel.DataAnnotations;
using JC.Core.Models.Auditing;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace JC.Communication.Messaging.Models.DomainModels;

/// <summary>
/// Represents a chat thread (conversation) between two or more participants.
/// Supports both direct messages and group chats, with optional metadata and soft-delete via <see cref="AuditModel"/>.
/// </summary>
public class ChatThread : AuditModel
{
    /// <summary>Default display name assigned to direct message threads.</summary>
    public const string DirectMessageName = "Direct Message";

    /// <summary>Default display name assigned to group chat threads.</summary>
    public const string GroupChatName = "Group Chat";

    /// <summary>Gets the unique identifier for this thread.</summary>
    [Key]
    [MaxLength(36)]
    public string Id { get; private set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the display name of the thread.</summary>
    [Required]
    [MaxLength(256)]
    public string Name { get; set; }

    /// <summary>Gets or sets an optional description for the thread.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether this is the default thread for its participant set.</summary>
    public bool IsDefaultThread { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the most recent activity in this thread.</summary>
    public DateTime? LastActivityUtc { get; set; }

    /// <summary>Gets or sets whether this thread is a group chat. Persisted for use when participants are not loaded.</summary>
    public bool IsGroupThread { get; set; }

    /// <summary>Gets or sets the messages belonging to this thread.</summary>
    public ICollection<ChatMessage> Messages { get; set; }

    /// <summary>Gets or sets the participants in this thread.</summary>
    public ICollection<ChatParticipant> Participants { get; set; }

    /// <summary>Gets or sets the optional visual metadata for this thread.</summary>
    public ChatMetadata? ChatMetadata { get; set; }

    /// <summary>
    /// Determines whether this thread is a group chat. Uses the loaded <see cref="Participants"/>
    /// collection when available; otherwise falls back to the persisted <see cref="IsGroupThread"/> flag.
    /// </summary>
    /// <returns><c>true</c> if the thread has more than two participants or is flagged as a group thread.</returns>
    public bool IsGroup()
        => Participants == null!
            ? IsGroupThread
            : Participants.Count > 2;
}

/// <summary>
/// Controls how default-thread conflicts are resolved when restoring a soft-deleted thread
/// that was previously the default for its participant set.
/// </summary>
public enum DefaultThreadRestoreMode
{
    /// <summary>Prevents the restore if another default thread already exists.</summary>
    Block,

    /// <summary>Demotes the existing default thread and restores this one as the default.</summary>
    DemoteExisting,

    /// <summary>Restores this thread but removes its default status, keeping the existing default.</summary>
    DemoteRestored
}