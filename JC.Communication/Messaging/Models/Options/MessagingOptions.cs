namespace JC.Communication.Messaging.Models.Options;

public class MessagingOptions
{
    public bool ParticipantsSeeChatHistory { get; set; } = true;
    public bool PreventDuplicateChatThreads { get; set; } = true;           //All chats are default and can never have a non-default chat
    public bool ImmutableDirectMessageParticipants { get; set; } = true;    //DMs can never be turned into a group chat
    public bool DisableGroups { get; set; } = false;

    public bool LogChatReads { get; set; } = true;
}