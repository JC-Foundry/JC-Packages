using JC.Content.Moderation.Enums;
using JC.Content.Moderation.Helpers;
using JC.Content.Moderation.Models;
using JC.Content.Moderation.Models.Options;

namespace JC.Content.Moderation.Services;

/// <summary>
/// Reports what moderation found in a piece of content.
/// </summary>
/// <remarks>
/// It reports and nothing more. The content is never altered, nothing is ever rejected on the
/// application's behalf, and <see cref="ProfanityModerationResult.ShouldBlock"/> is our reading of the
/// level in force — an application is free to ignore it and apply its own thresholds to the reported
/// severity and confidence. Masking and stripping are separate operations built on this one.
/// </remarks>
public class ProfanityModerator
{
    private readonly ProfanityTermRegistry _registry;
    private readonly ProfanityModerationOptions _options;
    private readonly Lock _lock = new();

    private ProfanityMatcher? _matcher;

    public ProfanityModerator(ProfanityTermRegistry registry, ProfanityModerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        _registry = registry;
        _options = options;
    }

    /// <summary>
    /// Examines <paramref name="content"/> and reports every term found in it.
    /// </summary>
    /// <param name="content">The content to examine. Null or whitespace comes back clean.</param>
    /// <param name="level">
    /// Overrides the level set at registration, for content this call treats differently — a username
    /// against a private message, say. Affects the block decision only; detection is identical at
    /// every level.
    /// </param>
    public ProfanityModerationResult Analyse(string? content, ProfanityLevel? level = null)
    {
        var applied = level ?? _options.Level;

        if(string.IsNullOrWhiteSpace(content))
            return ProfanityModerationResult.Clean(applied);

        //Cutting mid-word can split a term across the boundary and lose it. Accepted: the alternative
        //is scanning every candidate spelling against content of any length someone cares to submit
        var truncated = _options.MaxContentLength > 0 && content.Length > _options.MaxContentLength;
        if(truncated)
            content = content[..TruncationLength(content, _options.MaxContentLength)];

        var canonical = ProfanityCanonicaliser.Canonicalise(content);
        var matcher = GetMatcher();

        var found = matcher.Find(content, canonical, _options.MatchInsideWords, _options.ContextCharacters);
        if(found.Count == 0)
            return ProfanityModerationResult.Clean(applied, truncated, content.Length);

        var matches = found
            .Select(m => m with { Counted = Counts(m, applied) })
            .ToList();

        return Summarise(matches, applied, truncated, content.Length);
    }

    /// <summary>
    /// Where to cut, backing off a character where the limit falls between the two halves of one.
    /// Everything downstream cuts to <see cref="ProfanityModerationResult.ScannedLength"/>, so fixing
    /// the boundary here keeps a lone surrogate out of every result built from it.
    /// </summary>
    private static int TruncationLength(string content, int max)
        => char.IsHighSurrogate(content[max - 1]) ? max - 1 : max;

    /// <summary>
    /// Whether a match meets the level's floors. An allowed match never counts whatever its severity,
    /// and neither does one a longer overlapping match superseded.
    /// </summary>
    private static bool Counts(ProfanityMatch match, ProfanityLevel level)
        => match is { Allowed: false, Superseded: false }
           && ProfanityLevelPolicy.Counts(level, match.Severity, match.Confidence);

    /// <summary>
    /// Rolls the matches up. The root pair describes the worst finding and how sure we are of
    /// <em>that</em> one — a shaky severe match must not read as certain because some mild match
    /// alongside it was obvious.
    /// </summary>
    private static ProfanityModerationResult Summarise(List<ProfanityMatch> matches, ProfanityLevel level, bool truncated,
        int scannedLength)
    {
        var deciding = matches
            .Where(m => m is { Allowed: false, Superseded: false })
            .OrderByDescending(m => m.Severity)
            .ThenByDescending(m => m.ConfidenceScore)
            .FirstOrDefault();

        return new ProfanityModerationResult
        {
            ShouldBlock = matches.Any(m => m.Counted),
            Severity = deciding?.Severity ?? ProfanitySeverity.None,
            Confidence = deciding?.Confidence ?? ProfanityConfidence.None,
            ConfidenceScore = deciding?.ConfidenceScore ?? 0,
            Category = deciding?.Category ?? ProfanityCategory.None,
            Matches = matches,
            Level = level,
            Truncated = truncated,
            ScannedLength = scannedLength
        };
    }

    /// <summary>
    /// The matcher for the current term set, rebuilt when the registry has moved on. Indexing the
    /// terms is the expensive part, and the set rarely changes after startup.
    /// </summary>
    private ProfanityMatcher GetMatcher()
    {
        var version = _registry.Version;

        lock (_lock)
        {
            if(_matcher != null && _matcher.Version == version)
                return _matcher;

            _matcher = new ProfanityMatcher(_registry.GetTerms(), _registry.GetAllowed(), version);
            return _matcher;
        }
    }
}
