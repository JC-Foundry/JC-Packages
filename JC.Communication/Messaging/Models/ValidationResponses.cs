using JC.Communication.Messaging.Models.DomainModels;

namespace JC.Communication.Messaging.Models;

public class MessagingValidationResponse
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }

    public MessagingValidationResponse()
    {
        IsValid = true;
    }

    public MessagingValidationResponse(string errorMessage)
    {
        IsValid = false;
        ErrorMessage = errorMessage;
    }
}

public class ParticipantValidationResponse : MessagingValidationResponse
{
    public List<ChatParticipant> ValidatedParticipants { get; } = [];

    public ParticipantValidationResponse()
    {
    }
    
    public ParticipantValidationResponse(List<ChatParticipant> participant)
    {
        ValidatedParticipants = participant;
    }

    public ParticipantValidationResponse(string errorMessage)
        : base(errorMessage)
    {
    }
}

public class ChatThreadValidationResponse : MessagingValidationResponse
{
    public ChatThread? ValidatedChatThread { get; } = null;
    
    public ChatThreadValidationResponse()
    {
    }
    
    public ChatThreadValidationResponse(ChatThread chatThread)
    {
        ValidatedChatThread = chatThread;
    }
    
    public ChatThreadValidationResponse(string errorMessage)
        : base(errorMessage)
    {
    }
}