using JC.Content.Moderation.Enums;
using JC.Content.Moderation.Services;

namespace JC.Content.Models;

public sealed class ProfanitySettings
{
    public enum ProfanityMaskType
    {
        Mask,
        Remove,
        Tag
    }
    
    public ProfanityLevel? LevelOverride { get; private set; }
    public ProfanityMaskType MaskType { get; private set; }
    public char? MaskChar { get; private set; }
    public ushort? CappedMaskLength { get; private set; }
    public string? TagFormat { get; private set; }

    public ProfanitySettings(ProfanityMaskType maskType = ProfanityMaskType.Mask, 
        ProfanityLevel? levelOverride = null)
    {
        switch (maskType)
        {
            case ProfanityMaskType.Remove:
                SetToRemove();
                break;
            case ProfanityMaskType.Tag:
                SetToTag();
                break;
            case ProfanityMaskType.Mask:
            default:
                SetToMask();
                break;
        }
        
        ChangeProfanityLevel(levelOverride);
    }
    
    public void ChangeProfanityLevel(ProfanityLevel? level)
    {
        LevelOverride = level;
    }

    public void SetToMask(char maskChar = '*', ushort? cappedMaskLength = 4)
    {
        MaskType = ProfanityMaskType.Mask;
        MaskChar = maskChar;
        CappedMaskLength = cappedMaskLength;
        
        TagFormat = null;
    }

    public void SetToRemove()
    {
        MaskType = ProfanityMaskType.Remove;
        
        MaskChar = null;
        CappedMaskLength = null;
        TagFormat = null;
    }

    public void SetToTag(string tagFormat = ProfanityMasker.GenericTag)
    {
        MaskType = ProfanityMaskType.Tag;
        TagFormat = tagFormat;
        
        MaskChar = null;
        CappedMaskLength = null;
    }
}