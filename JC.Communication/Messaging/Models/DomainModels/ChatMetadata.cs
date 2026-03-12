using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Core.Models.Auditing;

namespace JC.Communication.Messaging.Models.DomainModels;

public class ChatMetadata : AuditModel
{
    [Key]
    [Required]
    [MaxLength(36)]
    public string ThreadId { get; set; }
    [ForeignKey(nameof(ThreadId))]
    public ChatThread Thread { get; set; }
    
    [MaxLength(256)]
    public string? Icon { get; set; }
    
    [MaxLength(512)]
    public string? ImgPath { get; set; }
    
    [MaxLength(8)]
    public string? ColourHex { get; set; }
    
    [MaxLength(12)]
    public string? ColourRgb { get; set; }
    
    public bool IsColourHex => !string.IsNullOrWhiteSpace(ColourHex);
    public bool IsColourRgb => !string.IsNullOrWhiteSpace(ColourRgb);
}