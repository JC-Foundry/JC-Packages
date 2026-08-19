using System.Globalization;
using DiffPlex;

namespace JC.Content.Comparison.Services;

/// <summary>
/// Splits content into the units a comparison works in.
/// </summary>
/// <remarks>
/// Every implementation must be lossless: concatenating the pieces in order has to reconstruct the
/// input exactly. <see cref="ContentComparer"/> takes each segment's text by slicing the source
/// against accumulated piece lengths, so a chunker that drops a character shifts every index after
/// it.
/// </remarks>
internal interface IContentChunker
{
    string[] Chunk(string content);
}

/// <summary>
/// One piece per character as a reader sees it, so a surrogate pair or a base letter with its
/// combining marks stays whole rather than splitting into halves that cannot render.
/// </summary>
internal sealed class CharacterChunker : IContentChunker
{
    public static readonly CharacterChunker Instance = new();

    public string[] Chunk(string content)
    {
        var pieces = new List<string>(content.Length);
        var position = 0;

        while (position < content.Length)
        {
            var length = StringInfo.GetNextTextElementLength(content.AsSpan(position));
            pieces.Add(content.Substring(position, length));
            position += length;
        }

        return [.. pieces];
    }
}

/// <summary>
/// One piece per word, carrying the whitespace that follows it.
/// </summary>
/// <remarks>
/// Trailing rather than leading, so a spacing change is reported against the word before it rather
/// than as an edit of its own. Leading whitespace has no word to attach to and becomes its own
/// piece. Punctuation is not a boundary — it rides along with the word it sits against.
/// </remarks>
internal sealed class WordChunker : IContentChunker
{
    public static readonly WordChunker Instance = new();

    public string[] Chunk(string content)
    {
        var pieces = new List<string>();
        var position = 0;

        while (position < content.Length)
        {
            var start = position;

            while (position < content.Length && !char.IsWhiteSpace(content[position]))
                position++;

            while (position < content.Length && char.IsWhiteSpace(content[position]))
                position++;

            pieces.Add(content[start..position]);
        }

        return [.. pieces];
    }
}

/// <summary>
/// One piece per line, carrying its own terminator.
/// </summary>
/// <remarks>
/// Keeping the terminator is what makes the split lossless, and it means a line-ending change
/// reports as that line changing rather than as a separate edit between every pair of lines. All
/// three conventions terminate a line, and <c>\r\n</c> counts once.
/// </remarks>
internal sealed class LineChunker : IContentChunker
{
    public static readonly LineChunker Instance = new();

    public string[] Chunk(string content)
    {
        var pieces = new List<string>();
        var start = 0;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];
            if(c != '\n' && c != '\r')
                continue;

            if(c == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                i++;

            pieces.Add(content[start..(i + 1)]);
            start = i + 1;
        }

        //Content not ending in a terminator leaves a final line behind
        if(start < content.Length)
            pieces.Add(content[start..]);

        return [.. pieces];
    }
}

/// <summary>
/// Hands one of our chunkers to DiffPlex.
/// </summary>
/// <remarks>
/// The library's own chunkers do not report where each piece came from, and its line chunker does
/// not preserve terminators — so the split is ours and DiffPlex is left to do the one thing we want
/// from it, which is the difference itself.
/// </remarks>
internal sealed class DiffPlexChunker(IContentChunker chunker) : IChunker
{
    public string[] Chunk(string str) => chunker.Chunk(str);
}
