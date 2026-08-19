using JC.Content.Moderation.Enums;

namespace JC.Content.Moderation.Models.Options;


/// <summary>
/// Moderation settings, fixed at registration. <see cref="Level"/> is the only one a call can
/// override.
/// </summary>
public class ProfanityModerationOptions
{
    /// <summary>
    /// The level applied when a call does not name one. Governs the block decision only — detection
    /// and reporting are the same at every level.
    /// </summary>
    public ProfanityLevel Level { get; set; } = ProfanityLevel.Safe;

    /// <summary>
    /// Characters of surrounding text kept either side of a match, for judging a false positive from
    /// a log entry. Zero reports the matched text alone.
    /// </summary>
    public int ContextCharacters { get; set; } = 5;

    /// <summary>
    /// Whether to look for terms inside longer words. Those matches never block — they are capped in
    /// the Low band — but they are reported, which is how deliberate padding shows up. Turning this
    /// off drops them entirely and makes matching cheaper.
    /// </summary>
    public bool MatchInsideWords { get; set; } = true;

    /// <summary>
    /// Whether to step over whitespace to reach a term — 'f u c k'. Off by default, since the same
    /// step joins words that were never one: 'Ann, Al' reaches 'anal'. Capped in Low either way.
    /// </summary>
    public bool MatchAcrossWordBreaks { get; set; }

    /// <summary>
    /// The most characters to scan, or zero for no limit. Content past the limit is neither examined
    /// nor returned, and <see cref="ProfanityModerationResult.Truncated"/> reports it.
    /// </summary>
    public int MaxContentLength { get; set; }
    
    /// <summary>
    /// The minimum confidence score for medium confidence matches.
    /// Medium confidence band is this value to the value of <see cref="HighConfidenceMinimum"/>.
    /// Low confidence band is 0.01 to this value (0 is none/no confidence).
    /// </summary>
    public ushort MediumConfidenceMinimum { get; set; } = 40;
    
    /// <summary>
    /// The minimum confidence score for high confidence matches.
    /// High confidence band is this value to 99.99% (100 is certain).
    /// </summary>
    public ushort HighConfidenceMinimum { get; set; } = 70;

    internal void Validate()
    {
        if(ContextCharacters < 0)
            throw new ArgumentOutOfRangeException(nameof(ContextCharacters), ContextCharacters,
                "Context cannot be negative.");

        if(MaxContentLength < 0)
            throw new ArgumentOutOfRangeException(nameof(MaxContentLength), MaxContentLength,
                "Maximum content length cannot be negative. Use zero for no limit.");

        //Zero is reserved for no confidence, and the band below this one is where every structurally
        //unreliable match is held - a floor of zero would leave it nowhere to sit
        if(MediumConfidenceMinimum == 0)
            throw new ArgumentOutOfRangeException(nameof(MediumConfidenceMinimum), MediumConfidenceMinimum,
                "The medium confidence floor must be above zero.");

        if(HighConfidenceMinimum <= MediumConfidenceMinimum)
            throw new ArgumentOutOfRangeException(nameof(HighConfidenceMinimum), HighConfidenceMinimum,
                $"The high confidence floor must be above {nameof(MediumConfidenceMinimum)}.");

        if(HighConfidenceMinimum > 100)
            throw new ArgumentOutOfRangeException(nameof(HighConfidenceMinimum), HighConfidenceMinimum,
                "A confidence floor cannot exceed 100.");
    }
}
