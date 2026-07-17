using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Communication.Messaging.Models.DomainModels;
using JC.Core.Models.Auditing;
using Microsoft.EntityFrameworkCore;

namespace JC.Communication.Logging.Models.Messaging;

[PrimaryKey(nameof(MessageId), nameof(UserId))]
public class MessageReadLog : LogModel
{
    [Required]
    [MaxLength(36)]
    public string MessageId { get; set; }
    [ForeignKey(nameof(MessageId))]
    public ChatMessage Message { get; set; }
    
    [Required]
    public DateTime ReadAtUtc { get; set; }
    
    [Required]
    [MaxLength(36)]
    public string UserId { get; set; }
}