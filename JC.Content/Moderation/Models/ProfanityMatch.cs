using JC.Content.Moderation.Enums;

namespace JC.Content.Moderation.Models;

/// <summary>
/// One term found in the content. Reported whether or not it counted towards the block decision —
/// an application tuning its configuration needs to see what was rejected as well as what stuck.
/// </summary>
public record ProfanityMatch
{
    /// <summary>The term that matched.</summary>
    public string TermId { get; init; } = string.Empty;

    /// <summary>The spelling that matched, as it appears in the original content.</summary>
    public string MatchedText { get; init; } = string.Empty;

    /// <summary>
    /// The matched text with surrounding characters, for judging a false positive without going back
    /// to the source. Width is set at registration.
    /// </summary>
    public string Context { get; init; } = string.Empty;

    /// <summary>Where the match starts in the original content.</summary>
    public int Index { get; init; }

    /// <summary>How many characters of the original content the match covers.</summary>
    public int Length { get; init; }

    public ProfanitySeverity Severity { get; init; }
    public ProfanityCategory Category { get; init; }
    public ProfanityTermSource Source { get; init; }

    /// <summary>Confidence band, derived from <see cref="ConfidenceScore"/>.</summary>
    public ProfanityConfidence Confidence { get; init; }

    /// <summary>Confidence as a percentage. The band loses detail an application tuning thresholds needs.</summary>
    public int ConfidenceScore { get; init; }

    /// <summary>What the matcher had to do to the text to find this.</summary>
    public ProfanityTransformation Transformations { get; init; }

    /// <summary>
    /// Whether this match met the level's thresholds and so contributed to the block decision.
    /// A reported match that did not count is either allowed, or below the level's floors.
    /// </summary>
    public bool Counted { get; init; }

    /// <summary>
    /// Whether an allowlist entry or a term exception suppressed this match. Reported at zero
    /// confidence rather than dropped, so an application can see its allowlist working.
    /// </summary>
    public bool Allowed { get; init; }

    /// <summary>
    /// Whether a longer overlapping match superseded this one — 'nigger' inside 'sand nigger'. Kept
    /// for visibility, never counted.
    /// </summary>
    public bool Superseded { get; init; }
}
