using JC.Communication.Messaging.Models.DomainModels;

namespace JC.Communication.Messaging.Models;

/// <summary>
/// Read-only projection of a <see cref="ChatParticipant"/> for consumption by the UI or API layer.
/// </summary>
public class ParticipantModel
{
    /// <summary>Gets the ID of the thread this participant belongs to.</summary>
    public string ThreadId { get; }

    /// <summary>Gets the user ID of the participant.</summary>
    public string UserId { get; }

    /// <summary>Gets whether this participant can see messages sent before they joined.</summary>
    public bool CanSeeHistory { get; }

    /// <summary>Gets the UTC timestamp when the participant joined the thread.</summary>
    public DateTime JoinedAtUtc { get; }

    /// <summary>
    /// Projects a <see cref="ChatParticipant"/> entity into a read-only participant model.
    /// </summary>
    /// <param name="participant">The participant entity to project.</param>
    public ParticipantModel(ChatParticipant participant)
    {
        ThreadId = participant.ThreadId;
        UserId = participant.UserId;
        CanSeeHistory = participant.CanSeeHistory;
        JoinedAtUtc = participant.JoinedAtUtc;
    }
}