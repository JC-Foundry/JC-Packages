using JC.Communication.Messaging.Models.DomainModels;

namespace JC.Communication.Messaging.Models;

/// <summary>
/// Read-only projection of a <see cref="ChatThread"/> for consumption by the UI or API layer.
/// Includes flattened messages, participants, and metadata. Soft-deleted children are excluded during construction.
/// </summary>
public class ChatModel
{
    /// <summary>Gets the unique identifier of the thread.</summary>
    public string ThreadId { get; }

    /// <summary>Gets the display name of the chat.</summary>
    public string ChatName { get; }

    /// <summary>Gets the optional description of the chat.</summary>
    public string? ChatDescription { get; }

    /// <summary>Gets whether this is the default thread for its participant set.</summary>
    public bool IsDefaultThread { get; }

    /// <summary>Gets a formatted string representing the last activity time, or "Never" if no activity has occurred.</summary>
    public string LastActivity { get; }

    /// <summary>Gets whether this chat is a group chat (more than two participants).</summary>
    public bool IsGroupChat { get; }

    /// <summary>Gets or sets the messages in this chat, excluding soft-deleted entries.</summary>
    public List<MessageModel> Messages { get; internal set; } = [];

    /// <summary>Gets the active participants in this chat.</summary>
    public List<ParticipantModel> Participants { get; } = [];

    /// <summary>Gets the visual metadata for this chat, or <c>null</c> if none exists or it has been soft-deleted.</summary>
    public MetadataModel? ChatMetadata { get; }

    /// <summary>
    /// Projects a <see cref="ChatThread"/> entity into a read-only chat model.
    /// Excludes soft-deleted messages, participants, and metadata.
    /// </summary>
    /// <param name="thread">The thread entity to project.</param>
    /// <param name="dateFormat">The format string used to display the last activity date.</param>
    /// <param name="preferHexCode">If <c>true</c>, colour values prefer hex over RGB in the metadata model.</param>
    public ChatModel(ChatThread thread, string dateFormat = "g", bool preferHexCode = true)
    {
        ThreadId = thread.Id;
        ChatName = thread.Name;
        ChatDescription = thread.Description;
        IsDefaultThread = thread.IsDefaultThread;
        LastActivity = thread.LastActivityUtc?.ToLocalTime().ToString(dateFormat) ?? "Never";
        IsGroupChat = thread.IsGroup();
        
        if(thread.ChatMetadata != null)
            ChatMetadata = thread.ChatMetadata.IsDeleted 
                ? null 
                : new MetadataModel(thread.ChatMetadata, preferHexCode);
        
        if(thread.Messages != null!) 
            Messages = thread.Messages.Where(m => !m.IsDeleted)
                .Select(m => new MessageModel(m))
                .ToList();
        
        if(thread.Participants != null!) 
            Participants = thread.Participants.Where(p => !p.IsDeleted)
                .Select(p => new ParticipantModel(p))
                .ToList();
    }
}