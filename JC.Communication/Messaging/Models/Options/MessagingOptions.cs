namespace JC.Communication.Messaging.Models.Options;

public class MessagingOptions
{
    public bool ParticipantsSeeChatHistory { get; set; } = true;
    public bool PreventDuplicateChatThreads { get; set; } = true;
    public bool DisableGroups { get; set; } = false;

    public bool LogChatReads { get; set; } = true;
}