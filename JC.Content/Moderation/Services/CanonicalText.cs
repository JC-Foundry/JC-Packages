using JC.Content.Moderation.Enums;

namespace JC.Content.Moderation.Services;

/// <summary>
/// Content folded into a form terms can be matched against, with a map back to where each character
/// came from. The map is the point: folding shifts every index, so without it a match cannot report
/// a position in the original or be masked out of it.
/// </summary>
internal sealed class CanonicalText
{
    /// <summary>The folded characters. Separator runs collapse to a single space.</summary>
    public required char[] Value { get; init; }

    /// <summary>Index in the original content each folded character came from.</summary>
    public required int[] SourceIndex { get; init; }

    /// <summary>How many original characters each folded character stands for.</summary>
    public required int[] SourceLength { get; init; }

    /// <summary>What was done to each character to fold it.</summary>
    public required ProfanityTransformation[] Applied { get; init; }

    /// <summary>Which folded characters are separators rather than letters.</summary>
    public required bool[] IsSeparator { get; init; }

    /// <summary>Which folded characters were a mask standing in for a letter.</summary>
    public required bool[] IsWildcard { get; init; }

    /// <summary>Which separators stood on whitespace, and so mark a real word boundary.</summary>
    public required bool[] IsWordBreak { get; init; }

    public int Length => Value.Length;

    /// <summary>
    /// Where the match ends in the original content — the source index of its last character plus
    /// however many original characters that stood for.
    /// </summary>
    public int OriginalEnd(int canonicalEnd)
        => SourceIndex[canonicalEnd] + SourceLength[canonicalEnd];
}
