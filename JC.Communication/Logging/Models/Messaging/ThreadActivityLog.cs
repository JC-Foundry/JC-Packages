using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Communication.Messaging.Models.DomainModels;
using JC.Core.Models.Auditing;

namespace JC.Communication.Logging.Models.Messaging;

public class ThreadActivityLog : LogModel
{
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(36)]
    public string ThreadId { get; set; }
    [ForeignKey(nameof(ThreadId))]
    public ChatThread Thread { get; set; }
    
    [Required]
    public DateTime ActivityTimestampUtc { get; set; }
    
    public ThreadActivityType ActivityType { get; set; }
    [MaxLength(512)]
    public string? ActivityDetails { get; set; }
}

public enum ThreadActivityType
{
    Message,
    ParticipantAdded,
    ParticipantRemoved,
}

public static class ActivityDetailsHelper
{
    public static string GetActivityDetails(ThreadActivityType activityType, List<string> participant)
        => activityType switch
        {
            ThreadActivityType.Message => $"Message from {participant.FirstOrDefault() ?? "Unknown User"}",
            ThreadActivityType.ParticipantAdded => $"Participant(s) added: {string.Join(", ", participant)}",
            ThreadActivityType.ParticipantRemoved => $"Participant(s) removed: {string.Join(", ", participant)}",
            _ => throw new ArgumentOutOfRangeException(nameof(activityType), activityType, null)
        };
}