namespace JC.Content.Moderation.Enums;

public enum ProfanityConfidence
{
    /// <summary>0% confidence. No profanity exists</summary>
    None,
    
    /// <summary>0.01-39.99% confidence</summary>
    Low,
    
    /// <summary>40-69.99% confidence</summary>
    Medium,
    
    /// <summary>70-99.99% confidence</summary>
    High,
    
    /// <summary>100% confidence</summary>
    Certain
}