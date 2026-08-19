using DiffPlex;
using DiffPlex.Model;
using JC.Content.Comparison.Enums;
using JC.Content.Comparison.Models;
using JC.Content.Comparison.Models.Options;

namespace JC.Content.Comparison.Services;

/// <summary>
/// Reports how two versions of a piece of content differ.
/// </summary>
/// <remarks>
/// Content is compared exactly as supplied. Nothing is normalised, trimmed or case-folded on the
/// way in, so a change of line ending or of casing is a change — deciding otherwise is the caller's
/// to make before calling, or the content pipeline's.
/// </remarks>
public class ContentComparer
{
    private readonly ContentComparisonOptions _options;

    public ContentComparer(ContentComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        _options = options;
    }

    /// <summary>
    /// Compares two versions and reports every run of content, changed or not.
    /// </summary>
    /// <param name="original">The version being changed from. Null is treated as empty.</param>
    /// <param name="revised">The version being changed to. Null is treated as empty.</param>
    /// <param name="granularity">
    /// Overrides the unit set at registration, for content this call treats differently — a title
    /// against a document body, say.
    /// </param>
    public ContentComparisonResult Compare(string? original, string? revised,
        ComparisonGranularity? granularity = null)
    {
        var applied = granularity ?? _options.Granularity;

        var left = original ?? string.Empty;
        var right = revised ?? string.Empty;

        var truncated = false;
        if (_options.MaxContentLength > 0)
        {
            truncated = left.Length > _options.MaxContentLength || right.Length > _options.MaxContentLength;

            left = Truncate(left, _options.MaxContentLength);
            right = Truncate(right, _options.MaxContentLength);
        }

        //Cheap and common - two versions that never diverged should not pay to be chunked
        if(string.Equals(left, right, StringComparison.Ordinal))
            return ContentComparisonResult.Identical(left, applied, truncated);

        var diff = Differ.Instance.CreateDiffs(left, right,
            ignoreWhiteSpace: false,
            ignoreCase: false,
            new DiffPlexChunker(Chunker(applied)));

        return new ContentComparisonResult
        {
            OriginalContent = left,
            RevisedContent = right,
            Granularity = applied,
            Truncated = truncated,
            Segments = BuildSegments(left, right, diff)
        };
    }

    private static IContentChunker Chunker(ComparisonGranularity granularity)
        => granularity switch
        {
            ComparisonGranularity.Character => CharacterChunker.Instance,
            ComparisonGranularity.Line => LineChunker.Instance,
            _ => WordChunker.Instance
        };

    /// <summary>
    /// Where to cut, backing off a character where the limit falls between the two halves of one.
    /// Segments index into the cut content, so a lone surrogate left at the boundary would reach
    /// every result built from it.
    /// </summary>
    private static string Truncate(string content, int max)
    {
        if(content.Length <= max)
            return content;

        return content[..(char.IsHighSurrogate(content[max - 1]) ? max - 1 : max)];
    }

    /// <summary>
    /// Walks the difference blocks into contiguous runs. DiffPlex reports only what changed and
    /// where, so the matching runs between its blocks are implied and filled in here.
    /// </summary>
    private static List<ContentChange> BuildSegments(string original, string revised, DiffResult diff)
    {
        //Character offset of each piece, so a run's text can be sliced from the source rather than
        //rebuilt from the pieces. One entry longer than the piece list, to give the final end
        var offsetsOld = BuildOffsets(diff.PiecesOld);
        var offsetsNew = BuildOffsets(diff.PiecesNew);

        var segments = new List<ContentChange>();
        var positionOld = 0;
        var positionNew = 0;

        foreach (var block in diff.DiffBlocks)
        {
            if(block.DeleteStartA > positionOld)
                segments.Add(Unchanged(original, offsetsOld, offsetsNew,
                    positionOld, block.DeleteStartA, positionNew));

            //Taken from the block rather than counted forward: DiffPlex is authoritative about
            //where each side resumes, and the two can advance by different amounts
            positionOld = block.DeleteStartA;
            positionNew = block.InsertStartB;

            if (block.DeleteCountA > 0)
            {
                segments.Add(Removed(original, offsetsOld, positionOld, block.DeleteCountA));
                positionOld += block.DeleteCountA;
            }

            if (block.InsertCountB > 0)
            {
                segments.Add(Added(revised, offsetsNew, positionNew, block.InsertCountB));
                positionNew += block.InsertCountB;
            }
        }

        if(positionOld < diff.PiecesOld.Length)
            segments.Add(Unchanged(original, offsetsOld, offsetsNew,
                positionOld, diff.PiecesOld.Length, positionNew));

        return segments;
    }

    private static int[] BuildOffsets(string[] pieces)
    {
        var offsets = new int[pieces.Length + 1];

        for (var i = 0; i < pieces.Length; i++)
            offsets[i + 1] = offsets[i] + pieces[i].Length;

        return offsets;
    }

    private static ContentChange Unchanged(string original, int[] offsetsOld, int[] offsetsNew,
        int from, int to, int fromNew)
        => new()
        {
            Type = ContentChangeType.Unchanged,
            Text = original[offsetsOld[from]..offsetsOld[to]],
            OriginalIndex = offsetsOld[from],
            RevisedIndex = offsetsNew[fromNew]
        };

    private static ContentChange Removed(string original, int[] offsets, int from, int count)
        => new()
        {
            Type = ContentChangeType.Removed,
            Text = original[offsets[from]..offsets[from + count]],
            OriginalIndex = offsets[from]
        };

    private static ContentChange Added(string revised, int[] offsets, int from, int count)
        => new()
        {
            Type = ContentChangeType.Added,
            Text = revised[offsets[from]..offsets[from + count]],
            RevisedIndex = offsets[from]
        };
}
