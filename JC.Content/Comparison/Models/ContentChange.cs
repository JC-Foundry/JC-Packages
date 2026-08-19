using JC.Content.Comparison.Enums;

namespace JC.Content.Comparison.Models;

/// <summary>
/// One run of content, and what happened to it. Runs are contiguous and in order, so concatenating
/// every <see cref="Text"/> reconstructs the original and the revised content exactly — skipping
/// <see cref="ContentChangeType.Added"/> for the one, <see cref="ContentChangeType.Removed"/> for
/// the other.
/// </summary>
public record ContentChange
{
    /// <summary>What happened to this run.</summary>
    public ContentChangeType Type { get; init; }

    /// <summary>The run as it appears in the content it belongs to.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Where the run starts in the original content, or <c>null</c> for an addition — which has no
    /// position there.
    /// </summary>
    public int? OriginalIndex { get; init; }

    /// <summary>
    /// Where the run starts in the revised content, or <c>null</c> for a removal — which has no
    /// position there.
    /// </summary>
    public int? RevisedIndex { get; init; }

    /// <summary>How many characters the run covers.</summary>
    public int Length => Text.Length;
}
