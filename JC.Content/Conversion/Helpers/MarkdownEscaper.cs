using System.Buffers;
using System.Text;

namespace JC.Content.Conversion.Helpers;

/// <summary>
/// Escapes text so Markdown renders it as written rather than reading it as syntax.
/// </summary>
/// <remarks>
/// Position matters: <c>#</c> opens a heading only at the start of a line, where <c>*</c> is
/// emphasis anywhere. Escaping the line-start set everywhere would litter ordinary prose with
/// backslashes.
/// </remarks>
internal static class MarkdownEscaper
{
    private static readonly SearchValues<char> Inline = SearchValues.Create(@"\`*_[]<>");
    private static readonly SearchValues<char> LineStart = SearchValues.Create("#>-+=|");

    public static string Escape(string? text, bool atLineStart = false)
    {
        if(string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        if(!atLineStart && !text.AsSpan().ContainsAny(Inline))
            return text;

        var builder = new StringBuilder(text.Length + 8);
        var start = 0;

        if (atLineStart)
            start = EscapeLineStart(text, builder);

        for (var i = start; i < text.Length; i++)
        {
            if(Inline.Contains(text[i]))
                builder.Append('\\');

            builder.Append(text[i]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Handles the markers that only mean something as the first thing on a line, and reports how
    /// much of the text that accounted for.
    /// </summary>
    private static int EscapeLineStart(string text, StringBuilder builder)
    {
        //An ordered-list marker is a run of digits and then '.' or ')', so the escape belongs after
        //the digits rather than before them
        var digits = 0;
        while(digits < text.Length && char.IsAsciiDigit(text[digits]))
            digits++;

        if (digits > 0 && digits < text.Length && text[digits] is '.' or ')')
        {
            builder.Append(text, 0, digits).Append('\\').Append(text[digits]);
            return digits + 1;
        }

        if (LineStart.Contains(text[0]))
        {
            builder.Append('\\').Append(text[0]);
            return 1;
        }

        return 0;
    }
}
