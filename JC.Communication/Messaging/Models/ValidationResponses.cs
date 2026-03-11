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

public class ParticipantValidationResponse<T> : MessagingValidationResponse
    where T : class
{
    public List<T> ValidatedParticipants { get; } = [];

    public ParticipantValidationResponse(List<T> participant)
        : base()
    {
        ValidatedParticipants = participant;
    }

    public ParticipantValidationResponse(string errorMessage)
        : base(errorMessage)
    {
    }
}