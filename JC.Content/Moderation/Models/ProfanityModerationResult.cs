using JC.Content.Moderation.Enums;

namespace JC.Content.Moderation.Models;

/// <summary>
/// What moderation found. Reports; it does not act — nothing here blocks anything or alters the
/// content. <see cref="ShouldBlock"/> is our reading of the configured level, and an application is
/// free to ignore it and apply its own thresholds to <see cref="Matches"/>.
/// </summary>
public class ProfanityModerationResult
{
    /// <summary>Content with nothing found in it.</summary>
    public static ProfanityModerationResult Clean(ProfanityLevel level, bool truncated = false, int scannedLength = 0)
        => new() { Level = level, Truncated = truncated, ScannedLength = scannedLength };

    /// <summary>
    /// Whether the content breaches the level applied. False when nothing was found, when everything
    /// found was allowed, or when nothing met the level's floors.
    /// </summary>
    public bool ShouldBlock { get; init; }

    /// <summary>
    /// The worst severity found, whether or not it met the level's floors.
    /// <see cref="ProfanitySeverity.None"/> means nothing was found, or everything found was allowed.
    /// </summary>
    public ProfanitySeverity Severity { get; init; }

    /// <summary>
    /// Confidence in the <see cref="Severity"/> finding, not the highest confidence anywhere. A
    /// severe match we are unsure of must not read as certain because some mild match was obvious.
    /// </summary>
    public ProfanityConfidence Confidence { get; init; }

    /// <summary>The percentage behind <see cref="Confidence"/>.</summary>
    public int ConfidenceScore { get; init; }

    /// <summary>The category of the finding that set <see cref="Severity"/>.</summary>
    public ProfanityCategory Category { get; init; }

    /// <summary>
    /// Everything found, counted or not — allowed matches, low-confidence matches and superseded
    /// overlaps included. This is the tuning surface: an application sees what was rejected and why.
    /// </summary>
    public IReadOnlyList<ProfanityMatch> Matches { get; init; } = [];

    /// <summary>The level applied, whether from registration or a per-call override.</summary>
    public ProfanityLevel Level { get; init; }

    /// <summary>
    /// Whether the content ran past <see cref="Options.ProfanityModerationOptions.MaxContentLength"/>,
    /// so only its opening was scanned.
    /// </summary>
    public bool Truncated { get; init; }

    /// <summary>How many characters were examined. Shorter than the content when <see cref="Truncated"/>.</summary>
    public int ScannedLength { get; init; }

    /// <summary>Whether anything was found at all, regardless of whether it counted.</summary>
    public bool HasMatches => Matches.Count > 0;

    /// <summary>The matches that met the level's floors and drove <see cref="ShouldBlock"/>.</summary>
    public IEnumerable<ProfanityMatch> CountedMatches => Matches.Where(m => m.Counted);
}
