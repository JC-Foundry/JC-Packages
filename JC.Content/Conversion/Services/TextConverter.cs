using System.Net;
using System.Text;
using JC.Content.Conversion.Helpers;

namespace JC.Content.Conversion.Services;

/// <summary>Converts plain text into the marked-up formats.</summary>
/// <remarks>
/// Both directions are escaping rather than interpretation. Plain text carries no structure to
/// recover — 'Important Information' could be a heading, a title or a sentence — so anything that
/// guessed would be inventing meaning rather than converting it.
/// </remarks>
internal sealed class TextConverter
{
    /// <summary>Encodes the text, then gives it paragraphs on blank lines and breaks within them.</summary>
    /// <remarks>
    /// Encoding comes first and is not optional. Text containing <c>&lt;script&gt;</c> is text, and
    /// replacing line breaks without encoding would turn it into markup.
    /// </remarks>
    public string ToHtml(string text)
    {
        var builder = new StringBuilder();
        var paragraph = new List<string>();

        foreach (var line in Lines(text))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                Flush();
                continue;
            }

            paragraph.Add(WebUtility.HtmlEncode(line));
        }

        Flush();

        return builder.ToString().TrimEnd();

        void Flush()
        {
            if(paragraph.Count == 0)
                return;

            //Not AppendLine: Environment.NewLine would make the output differ between a Windows
            //machine and a Linux container, and everything else here normalises to '\n'
            builder.Append("<p>").AppendJoin("<br />", paragraph).Append("</p>\n");
            paragraph.Clear();
        }
    }

    /// <summary>Escapes the text so Markdown renders it exactly as written.</summary>
    public string ToMarkdown(string text)
    {
        var lines = Lines(text);
        var builder = new StringBuilder();

        for (var i = 0; i < lines.Length; i++)
        {
            var escaped = MarkdownEscaper.Escape(lines[i], atLineStart: true);
            builder.Append(escaped);

            if(i == lines.Length - 1)
                continue;

            //A lone newline is a soft break in Markdown and renders as a space, so a line followed
            //by more text needs two trailing spaces to keep the break the author typed
            if(escaped.Length > 0 && !string.IsNullOrWhiteSpace(lines[i + 1]))
                builder.Append("  ");

            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static string[] Lines(string text)
        => text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
}
