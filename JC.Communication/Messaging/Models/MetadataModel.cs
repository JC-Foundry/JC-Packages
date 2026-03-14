using JC.Communication.Messaging.Models.DomainModels;

namespace JC.Communication.Messaging.Models;

/// <summary>
/// Read-only projection of <see cref="ChatMetadata"/> for consumption by the UI or API layer.
/// Resolves the colour value to a single string based on the <paramref name="preferHexCode"/> preference.
/// </summary>
public class MetadataModel
{
    /// <summary>Gets the ID of the thread this metadata belongs to.</summary>
    public string ThreadId { get; }

    /// <summary>Gets the optional icon identifier.</summary>
    public string? Icon { get; }

    /// <summary>Gets the optional image path.</summary>
    public string? ImgPath { get; }

    /// <summary>Gets the resolved colour value (hex or RGB based on preference), or <c>null</c> if no colour is set.</summary>
    public string? Colour { get; }

    /// <summary>
    /// Projects a <see cref="ChatMetadata"/> entity into a read-only metadata model.
    /// </summary>
    /// <param name="metadata">The metadata entity to project.</param>
    /// <param name="preferHexCode">If <c>true</c>, returns the hex colour when available; otherwise returns the RGB value.</param>
    public MetadataModel(ChatMetadata metadata, bool preferHexCode = true)
    {
        ThreadId = metadata.ThreadId;
        Icon = metadata.Icon;
        ImgPath = metadata.ImgPath;
        Colour = metadata is { IsColourHex: false, IsColourRgb: false } 
            ? null
            : preferHexCode && metadata.IsColourHex 
                ? metadata.ColourHex 
                : metadata.ColourRgb;
    }
}