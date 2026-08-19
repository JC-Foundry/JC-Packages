using System.Text;
using JC.Content.Comparison.Enums;

namespace JC.Content.Comparison.Models;

/// <summary>
/// What a comparison found. Reports; it does not act — neither version is altered, and nothing here
/// decides what an application should do about a change.
/// </summary>
/// <remarks>
/// <see cref="Segments"/> is the result. <see cref="Changes"/> and <see cref="Render"/> are
/// projections over it, so a consumer that needs something neither offers can walk the segments
/// itself rather than parsing rendered text back apart.
/// </remarks>
public class ContentComparisonResult
{
    /// <summary>Two versions that were already identical.</summary>
    public static ContentComparisonResult Identical(string content, ComparisonGranularity granularity,
        bool truncated = false)
        => new()
        {
            OriginalContent = content,
            RevisedContent = content,
            Granularity = granularity,
            Truncated = truncated,
            Segments = content.Length == 0
                ? []
                :
                [
                    new ContentChange
                    {
                        Type = ContentChangeType.Unchanged,
                        Text = content,
                        OriginalIndex = 0,
                        RevisedIndex = 0
                    }
                ]
        };

    /// <summary>
    /// The original content as compared. Cut to the configured maximum where
    /// <see cref="Truncated"/>, so every index in <see cref="Segments"/> is valid against it.
    /// </summary>
    public string OriginalContent { get; init; } = string.Empty;

    /// <summary>
    /// The revised content as compared. Cut to the configured maximum where <see cref="Truncated"/>,
    /// so every index in <see cref="Segments"/> is valid against it.
    /// </summary>
    public string RevisedContent { get; init; } = string.Empty;

    /// <summary>
    /// Every run of content in order, changed or not. Adjacent runs of the same type are combined,
    /// so an unchanged paragraph is one segment rather than one per word.
    /// </summary>
    public IReadOnlyList<ContentChange> Segments { get; init; } = [];

    /// <summary>The unit the comparison ran in, whether from registration or a per-call override.</summary>
    public ComparisonGranularity Granularity { get; init; }

    /// <summary>
    /// Whether either version ran past
    /// <see cref="Options.ContentComparisonOptions.MaxContentLength"/>, so only its opening was
    /// compared.
    /// </summary>
    public bool Truncated { get; init; }

    /// <summary>Whether anything differs between the two versions.</summary>
    public bool HasChanges => Segments.Any(s => s.Type != ContentChangeType.Unchanged);

    /// <summary>The changed runs alone, in order. This is the list of where the content differs.</summary>
    public IEnumerable<ContentChange> Changes => Segments.Where(s => s.Type != ContentChangeType.Unchanged);

    
    public string Render(string addedOpen = "{+", string addedClose = "+}",
        string removedOpen = "[-", string removedClose = "-]",
        Func<string, string>? encode = null)
    {
        var builder = new StringBuilder(OriginalContent.Length + RevisedContent.Length);

        foreach (var segment in Segments)
        {
            var text = encode is null ? segment.Text : encode(segment.Text);

            switch (segment.Type)
            {
                case ContentChangeType.Added:
                    builder.Append(addedOpen).Append(text).Append(addedClose);
                    break;
                case ContentChangeType.Removed:
                    builder.Append(removedOpen).Append(text).Append(removedClose);
                    break;
                default:
                    builder.Append(text);
                    break;
            }
        }

        return builder.ToString();
    }
}
