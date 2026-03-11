using JC.Communication.Messaging.Models.DomainModels;

namespace JC.Communication.Messaging.Models;

public class ParticipantModel
{
    public string ThreadId { get; }
    public string UserId { get; }
    
    public bool CanSeeHistory { get; }
    public DateTime JoinedAtUtc { get; }

    public ParticipantModel(ChatParticipant participant)
    {
        ThreadId = participant.ThreadId;
        UserId = participant.UserId;
        CanSeeHistory = participant.CanSeeHistory;
        JoinedAtUtc = participant.JoinedAtUtc;
    }
}