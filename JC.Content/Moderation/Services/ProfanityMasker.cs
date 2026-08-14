using System.Text;
using JC.Content.Moderation.Enums;
using JC.Content.Moderation.Models;

namespace JC.Content.Moderation.Services;

/// <summary>
/// Rewrites the matches <see cref="ProfanityModerator"/> found — masked, removed or replaced with a
/// tag. Only matches that met the level's floors are touched.
/// </summary>
public class ProfanityMasker
{
    private readonly ProfanityModerator _moderator;
    public const string CategoryTag = "{category}";
    public const string SeverityTag = "{severity}";
    public const string GenericTagValue = "Removed";
    public const string GenericTag = $"[{GenericTagValue}]";

    public ProfanityMasker(ProfanityModerator moderator)
    {
        ArgumentNullException.ThrowIfNull(moderator);
        _moderator = moderator;
    }

    /// <summary>
    /// Replaces each match with a run of <paramref name="maskChar"/>.
    /// </summary>
    /// <param name="content">The content to examine and rewrite.</param>
    /// <param name="maskChar">The character to fill the match with.</param>
    /// <param name="preserveLength">
    /// Whether the run matches the length of the text it replaces. Off by default, so the run is a
    /// fixed <paramref name="cappedMaskLength"/> and the original length is not disclosed.
    /// </param>
    /// <param name="cappedMaskLength">
    /// The longest run to write, or <c>null</c> for no cap — which with
    /// <paramref name="preserveLength"/> off leaves the run the length of the match.
    /// </param>
    /// <param name="level">Overrides the level set at registration.</param>
    public ProfanityModerationMaskResult AnalyseAndMask(string? content, char maskChar = '*',
        bool preserveLength = false, ushort? cappedMaskLength = 4, ProfanityLevel? level = null)
    {
        var (moderationResult, maskResult) = Analyse(content, level);
        if (maskResult != null)
            return maskResult;

        return Rewrite(moderationResult, content!, collapseGap: false,
            match => new string(maskChar, MaskLength(match, preserveLength, cappedMaskLength)));
    }

    /// <summary>
    /// Removes each match. Where the removal leaves whitespace either side, one is dropped.
    /// </summary>
    /// <param name="content">The content to examine and rewrite.</param>
    /// <param name="level">Overrides the level set at registration.</param>
    public ProfanityModerationMaskResult AnalyseAndRemove(string? content, ProfanityLevel? level = null)
    {
        var (moderationResult, maskResult) = Analyse(content, level);
        if (maskResult != null)
            return maskResult;

        return Rewrite(moderationResult, content!, collapseGap: true, _ => string.Empty);
    }

    /// <summary>
    /// Replaces each match with <paramref name="tagFormat"/>, substituting
    /// <see cref="CategoryTag"/> and <see cref="SeverityTag"/> where they appear. A format naming
    /// neither is used verbatim, so <c>"[Profanity]"</c> replaces every match with that text.
    /// </summary>
    /// <param name="content">The content to examine and rewrite.</param>
    /// <param name="tagFormat">
    /// The replacement template — <c>"[{severity}]"</c>, <c>"[{category}:{severity}]"</c> or any
    /// literal. A match with no category substitutes <see cref="GenericTagValue"/>.
    /// </param>
    /// <param name="level">Overrides the level set at registration.</param>
    /// <exception cref="ArgumentException"><paramref name="tagFormat"/> is null or empty.</exception>
    public ProfanityModerationMaskResult AnalyseAndTag(string? content, string tagFormat = GenericTag,
        ProfanityLevel? level = null)
    {
        if (string.IsNullOrEmpty(tagFormat))
            throw new ArgumentException("A tag format is needed. Use AnalyseAndRemove to strip matches instead.",
                nameof(tagFormat));

        var (moderationResult, maskResult) = Analyse(content, level);
        if (maskResult != null)
            return maskResult;

        return Rewrite(moderationResult, content!, collapseGap: false, match => Tag(tagFormat, match));
    }

    private (ProfanityModerationResult ModerationResult, ProfanityModerationMaskResult? MaskResult) Analyse(string? content,
        ProfanityLevel? level)
    {
        var moderationResult = _moderator.Analyse(content, level);
        if (moderationResult.ShouldBlock)
            return (moderationResult, null);

        //Cut even when nothing was found: the tail was never scanned, so returning it would hand back
        //content this class cannot vouch for
        var updated = moderationResult.Truncated
            ? content?[..moderationResult.ScannedLength]
            : content;

        return (moderationResult, new ProfanityModerationMaskResult(moderationResult, updated, content));
    }

    /// <summary>
    /// Walks the counted matches in order, replacing each. Those are non-overlapping and already
    /// sorted, so one left-to-right pass covers them without indices shifting under it.
    /// </summary>
    private static ProfanityModerationMaskResult Rewrite(ProfanityModerationResult result, string content,
        bool collapseGap, Func<ProfanityMatch, string> replacement)
    {
        var scanned = content[..result.ScannedLength];
        var builder = new StringBuilder(scanned.Length);
        var replaced = 0;
        var position = 0;

        foreach (var match in result.CountedMatches.OrderBy(m => m.Index))
        {
            builder.Append(scanned, position, match.Index - position);
            var text = replacement(match);
            builder.Append(text);
            position = match.Index + match.Length;
            replaced++;

            if (!collapseGap || text.Length > 0)
                continue;

            var following = position < scanned.Length && char.IsWhiteSpace(scanned[position]);
            var preceding = builder.Length > 0 && char.IsWhiteSpace(builder[^1]);

            //Whitespace either side of the seam collapses to one. At either end of the content there
            //is no seam, so what would be left is a leading or trailing space instead
            if (following && (preceding || builder.Length == 0))
                position++;
            else if (preceding && position >= scanned.Length)
                builder.Length--;
        }

        builder.Append(scanned, position, scanned.Length - position);

        return new ProfanityModerationMaskResult(result, builder.ToString(), content, replaced);
    }

    private static int MaskLength(ProfanityMatch match, bool preserveLength, ushort? cappedMaskLength)
    {
        if (cappedMaskLength is not { } cap)
            return match.Length;

        return preserveLength ? Math.Min(match.Length, cap) : cap;
    }

    private static string Tag(string tagFormat, ProfanityMatch match)
    {
        var category = match.Category == ProfanityCategory.None
            ? GenericTagValue
            : match.Category.ToString();

        return tagFormat
            .Replace(CategoryTag, category, StringComparison.OrdinalIgnoreCase)
            .Replace(SeverityTag, match.Severity.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
