using JC.Communication.Messaging.Models.DomainModels;

namespace JC.Communication.Messaging.Models;

public class ChatModel
{
    public string ThreadId { get; }
    public string ChatName { get; }
    public string? ChatDescription { get; }
    
    public bool IsDefaultThread { get; }
    public string LastActivity { get; }
    
    public bool IsGroupChat { get; }

    public List<MessageModel> Messages { get; internal set; } = [];
    public List<ParticipantModel> Participants { get; } = [];
    public MetadataModel? ChatMetadata { get; }

    public ChatModel(ChatThread thread, string dateFormat = "g", bool preferHexCode = true)
    {
        ThreadId = thread.Id;
        ChatName = thread.Name;
        ChatDescription = thread.Description;
        IsDefaultThread = thread.IsDefaultThread;
        LastActivity = thread.LastActivityUtc?.ToLocalTime().ToString(dateFormat) ?? "Never";
        IsGroupChat = thread.IsGroup();
        
        if(thread.ChatMetadata != null)
            ChatMetadata = new MetadataModel(thread.ChatMetadata, preferHexCode);
        
        if(thread.Messages != null!) 
            Messages = thread.Messages.Select(m => new MessageModel(m))
                .ToList();
        
        if(thread.Participants != null!) 
            Participants = thread.Participants.Select(p => new ParticipantModel(p))
                .ToList();
    }
}