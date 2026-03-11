using JC.Communication.Messaging.Models.DomainModels;

namespace JC.Communication.Messaging.Models;

public class MetadataModel
{
    public string ThreadId { get; }
    public string? Icon { get; }
    public string? ImgPath { get; }
    public string? Colour { get; }

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