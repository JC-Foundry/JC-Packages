namespace JC.Content.Moderation.Enums;

public enum ProfanityLevel
{
    /// <summary>Only blocks severe and high-severity profanity, with certain/high confidence.</summary>
    Minimal,
    
    /// <summary>Only blocks severe and high-severity profanity, with medium or certain/high confidence.</summary>
    Lax,
    
    /// <summary>Only blocks severe, high, and medium-severity profanity, with medium or certain/high confidence.</summary>
    Safe,
    
    /// <summary>Blocks severe, high, medium, and low-severity profanity, with medium or certain/high confidence.</summary>
    Strict,

    /// <summary>Blocks every severity including mild, with medium or certain/high confidence.</summary>
    SuperStrict
}