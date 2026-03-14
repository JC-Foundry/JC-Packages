using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Core.Models.Auditing;

namespace JC.Communication.Messaging.Models.DomainModels;

/// <summary>
/// Optional visual metadata for a <see cref="ChatThread"/>, including icon, image, and colour settings.
/// Keyed by <see cref="ThreadId"/> (one-to-one with the thread). Supports soft-delete via <see cref="AuditModel"/>.
/// </summary>
public class ChatMetadata : AuditModel
{
    /// <summary>Gets or sets the ID of the thread this metadata belongs to. Also serves as the primary key.</summary>
    [Key]
    [Required]
    [MaxLength(36)]
    public string ThreadId { get; set; }

    /// <summary>Gets or sets the navigation property to the parent thread.</summary>
    [ForeignKey(nameof(ThreadId))]
    public ChatThread Thread { get; set; }

    /// <summary>Gets or sets an optional icon identifier (e.g. emoji, icon class name).</summary>
    [MaxLength(256)]
    public string? Icon { get; set; }

    /// <summary>Gets or sets an optional image path for the thread avatar.</summary>
    [MaxLength(512)]
    public string? ImgPath { get; set; }

    /// <summary>Gets or sets the thread colour in normalised hex format (e.g. "#FF00AA"). Max 7 characters.</summary>
    [MaxLength(7)]
    public string? ColourHex { get; set; }

    /// <summary>Gets or sets the thread colour in normalised RGB format (e.g. "rgb(255,0,170)"). Max 16 characters.</summary>
    [MaxLength(16)]
    public string? ColourRgb { get; set; }

    /// <summary>Gets whether a hex colour value has been set.</summary>
    public bool IsColourHex => !string.IsNullOrWhiteSpace(ColourHex);

    /// <summary>Gets whether an RGB colour value has been set.</summary>
    public bool IsColourRgb => !string.IsNullOrWhiteSpace(ColourRgb);
}