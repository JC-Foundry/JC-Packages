using JC.Content.Moderation.Enums;

namespace JC.Content.Moderation.Models;

/// <summary>
/// The outcome of a masking, removal or tagging pass, alongside the moderation result behind it.
/// </summary>
public class ProfanityModerationMaskResult(ProfanityModerationResult result, string? updatedContent, string? originalContent,
    int replacementCount = 0)
{
    public ProfanityModerationResult ModerationResult { get; } = result;

    /// <summary>
    /// The rewritten content. Cut to <see cref="ProfanityModerationResult.ScannedLength"/> when the
    /// content was truncated, so it never carries text this class did not examine.
    /// </summary>
    public string? UpdatedContent { get; } = updatedContent;

    /// <summary>The content as supplied, whole even where <see cref="UpdatedContent"/> was cut.</summary>
    public string? OriginalContent { get; } = originalContent;

    /// <summary>How many matches were replaced.</summary>
    public int ReplacementCount { get; } = replacementCount;

    /// <summary>Whether <see cref="UpdatedContent"/> differs from <see cref="OriginalContent"/>.</summary>
    public bool WasModified { get; } = !string.Equals(updatedContent, originalContent, StringComparison.Ordinal);

    public ProfanityModerationMaskResult(ProfanityLevel level)
        : this(ProfanityModerationResult.Clean(level), null, null)
    {
    }

    public ProfanityModerationMaskResult(ProfanityModerationResult result, string? content)
        : this(result, content, content)
    {
    }
}
