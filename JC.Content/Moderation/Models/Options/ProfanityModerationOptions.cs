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
    /// The most characters to scan, or zero for no limit. Content past the limit is neither examined
    /// nor returned, and <see cref="ProfanityModerationResult.Truncated"/> reports it.
    /// </summary>
    public int MaxContentLength { get; set; }

    internal void Validate()
    {
        if(ContextCharacters < 0)
            throw new ArgumentOutOfRangeException(nameof(ContextCharacters), ContextCharacters,
                "Context cannot be negative.");

        if(MaxContentLength < 0)
            throw new ArgumentOutOfRangeException(nameof(MaxContentLength), MaxContentLength,
                "Maximum content length cannot be negative. Use zero for no limit.");
    }
}
