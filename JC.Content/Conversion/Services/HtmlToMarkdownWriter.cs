using System.Text;
using AngleSharp.Dom;
using JC.Content.Conversion.Helpers;
using JC.Content.Conversion.Models.Options;

namespace JC.Content.Conversion.Services;

/// <summary>
/// Walks an HTML document and writes the Markdown equivalent.
/// </summary>
/// <remarks>
/// Every element either maps onto Markdown syntax or is dropped, keeping what it wrapped. Nothing
/// is carried through as raw HTML — an element with no Markdown for it has its tag removed and its
/// content kept, so the result is a Markdown document rather than HTML in a <c>.md</c> file.
/// </remarks>
internal sealed class HtmlToMarkdownWriter(ContentConversionOptions options)
{
    /// <summary>Elements whose text is code or metadata rather than prose, so it goes with the tag.</summary>
    private static readonly HashSet<string> Dropped =
        ["script", "style", "noscript", "template", "head", "title", "meta", "link"];

    /// <summary>Block elements with no Markdown of their own — the tag goes, the spacing stays.</summary>
    private static readonly HashSet<string> Blocks =
    [
        "p", "div", "section", "article", "aside", "header", "footer",
        "main", "nav", "figure", "figcaption", "address", "dl", "dt", "dd"
    ];

    public void Write(INodeList nodes, ContentWriter writer)
    {
        foreach (var node in nodes)
            WriteNode(node, writer);
    }

    private void WriteNode(INode node, ContentWriter writer)
    {
        switch (node)
        {
            case IText text:
                WriteTextNode(text.Data, writer);
                return;
            case IElement element:
                WriteElement(element, writer);
                return;
        }
    }

    /// <summary>
    /// Writes text a word at a time, with every run of whitespace collapsed to one space.
    /// </summary>
    /// <remarks>
    /// HTML collapses whitespace when it renders, so the newlines and indentation between tags are
    /// not part of the content. Carrying them across would indent the Markdown — and four spaces of
    /// indentation is a code block.
    /// </remarks>
    private static void WriteTextNode(string value, ContentWriter writer)
    {
        var i = 0;

        while (i < value.Length)
        {
            if (char.IsWhiteSpace(value[i]))
            {
                while(i < value.Length && char.IsWhiteSpace(value[i]))
                    i++;

                writer.WriteSpace();
                continue;
            }

            var start = i;
            while(i < value.Length && !char.IsWhiteSpace(value[i]))
                i++;

            writer.WriteText(value[start..i]);
        }
    }

    private void WriteElement(IElement element, ContentWriter writer)
    {
        var name = element.LocalName;

        if(Dropped.Contains(name))
            return;

        switch (name)
        {
            case "h1" or "h2" or "h3" or "h4" or "h5" or "h6":
                writer.StartBlock();
                writer.Write($"{new string('#', name[1] - '0')} ");
                WriteChildren(element, writer);
                writer.EndLine();
                return;

            case "br":
                writer.HardBreak();
                return;

            case "hr":
                writer.StartBlock();
                writer.Write("---");
                writer.EndLine();
                return;

            case "strong" or "b":
                WriteWrapped(element, writer, "**");
                return;

            case "em" or "i":
                WriteWrapped(element, writer, "*");
                return;

            case "del" or "s" or "strike":
                if(options.GithubFlavoured) WriteWrapped(element, writer, "~~");
                else WriteChildren(element, writer);
                return;

            //A code element inside pre is the code block's own, and is handled with it
            case "code" when element.ParentElement?.LocalName != "pre":
                WriteInlineCode(element, writer);
                return;

            case "pre":
                WriteCodeBlock(element, writer);
                return;

            case "a":
                WriteLink(element, writer);
                return;

            case "img":
                WriteImage(element, writer);
                return;

            case "ul" or "ol":
                WriteList(element, writer);
                return;

            case "blockquote":
                WriteBlockQuote(element, writer);
                return;

            case "table":
                WriteTable(element, writer);
                return;

            default:
                if (Blocks.Contains(name))
                {
                    writer.StartBlock();
                    WriteChildren(element, writer);
                    writer.EndLine();
                    return;
                }

                //Inline or unrecognised: drop the tag, keep what it wrapped
                WriteChildren(element, writer);
                return;
        }
    }

    private void WriteChildren(IElement element, ContentWriter writer) => Write(element.ChildNodes, writer);

    private void WriteWrapped(IElement element, ContentWriter writer, string marker)
    {
        //Emphasis around nothing renders as literal markers
        if (string.IsNullOrWhiteSpace(element.TextContent))
        {
            WriteChildren(element, writer);
            return;
        }

        writer.Write(marker);
        WriteChildren(element, writer);
        writer.Write(marker);
    }

    private static void WriteInlineCode(IElement element, ContentWriter writer)
    {
        var code = element.TextContent;
        if(code.Length == 0)
            return;

        //The fence has to outrun the longest backtick run inside it, and a value starting or ending
        //in a backtick needs padding to keep the two apart
        var fence = new string('`', LongestBacktickRun(code) + 1);
        var pad = code.StartsWith('`') || code.EndsWith('`') ? " " : string.Empty;

        writer.Write($"{fence}{pad}{code}{pad}{fence}");
    }

    private static void WriteCodeBlock(IElement element, ContentWriter writer)
    {
        var code = element.QuerySelector("code");
        var text = (code ?? element).TextContent.Replace("\r\n", "\n").TrimEnd('\n');
        var fence = new string('`', Math.Max(3, LongestBacktickRun(text) + 1));

        writer.StartBlock();
        writer.WriteLine(fence + LanguageOf(code));

        foreach (var line in text.Split('\n'))
            writer.WriteLine(line);

        writer.WriteLine(fence);
    }

    /// <summary>Reads the language from the conventional <c>language-x</c> class, if it carries one.</summary>
    private static string LanguageOf(IElement? code)
    {
        var language = code?.ClassList.FirstOrDefault(c => c.StartsWith("language-", StringComparison.OrdinalIgnoreCase));

        return language is null
            ? string.Empty
            : new string(language["language-".Length..].Where(char.IsLetterOrDigit).ToArray());
    }

    private void WriteLink(IElement element, ContentWriter writer)
    {
        var href = element.GetAttribute("href");

        //An anchor with nowhere to go is not a link
        if (string.IsNullOrWhiteSpace(href))
        {
            WriteChildren(element, writer);
            return;
        }

        writer.Write("[");
        WriteChildren(element, writer);
        writer.Write($"]({FormatUrl(href)})");
    }

    private static void WriteImage(IElement element, ContentWriter writer)
    {
        var source = element.GetAttribute("src");
        if(string.IsNullOrWhiteSpace(source))
            return;

        writer.Write($"![{MarkdownEscaper.Escape(element.GetAttribute("alt"))}]({FormatUrl(source)})");
    }

    /// <summary>Wraps a destination that would otherwise end the link early.</summary>
    private static string FormatUrl(string url)
    {
        var trimmed = url.Trim();

        return trimmed.AsSpan().ContainsAny(' ', '(', ')') ? $"<{trimmed}>" : trimmed;
    }

    private void WriteList(IElement list, ContentWriter writer)
    {
        var ordered = list.LocalName == "ol";
        var number = int.TryParse(list.GetAttribute("start"), out var start) ? start : 1;

        writer.StartBlock();
        writer.EnterList();

        foreach (var item in list.Children.Where(c => c.LocalName == "li"))
        {
            var marker = ordered ? $"{number++}. " : "- ";

            writer.Write(marker);

            //Pushed after the marker so it reaches only the continuation lines, and the first block
            //child is suppressed so it starts on the marker's own line rather than below it
            writer.PushPrefix(new string(' ', marker.Length));
            writer.SuppressNextBlock();

            WriteChildren(item, writer);

            writer.EndLine();
            writer.PopPrefix();
        }

        writer.ExitList();
    }

    private void WriteBlockQuote(IElement element, ContentWriter writer)
    {
        writer.StartBlock();
        writer.PushPrefix("> ");

        WriteChildren(element, writer);

        writer.EndLine();
        writer.PopPrefix();
    }

    /// <summary>
    /// Writes a pipe table, expanding merged cells so the grid keeps its shape.
    /// </summary>
    /// <remarks>
    /// A pipe table has no merged cells and cannot nest, so a <c>colspan</c> becomes empty cells
    /// beside the content and an inner table collapses to the text of its own cells. That is as
    /// close as the syntax reaches.
    /// </remarks>
    private void WriteTable(IElement table, ContentWriter writer)
    {
        var rows = table.QuerySelectorAll("tr")
            .Where(r => r.Closest("table") == table)
            .Select(CellsOf)
            .Where(r => r.Count > 0)
            .ToList();

        if(rows.Count == 0)
            return;

        var width = rows.Max(r => r.Count);

        //Without pipe tables there is no table syntax to reach for, so the cells become text
        if (!options.GithubFlavoured)
        {
            foreach (var row in rows)
            {
                writer.StartBlock();
                writer.Write(string.Join(" ", row.Where(c => c.Length > 0)));
                writer.EndLine();
            }

            return;
        }

        writer.StartBlock();

        WriteRow(rows[0], width, writer);
        writer.WriteLine($"|{string.Concat(Enumerable.Repeat(" --- |", width))}");

        foreach (var row in rows.Skip(1))
            WriteRow(row, width, writer);
    }

    private static void WriteRow(List<string> cells, int width, ContentWriter writer)
    {
        var builder = new StringBuilder("|");

        for (var i = 0; i < width; i++)
            builder.Append(' ').Append(i < cells.Count ? cells[i] : string.Empty).Append(" |");

        writer.WriteLine(builder.ToString());
    }

    private List<string> CellsOf(IElement row)
    {
        var cells = new List<string>();

        foreach (var cell in row.Children.Where(c => c.LocalName is "td" or "th"))
        {
            cells.Add(CellText(cell));

            var span = int.TryParse(cell.GetAttribute("colspan"), out var value) ? Math.Clamp(value, 1, 64) : 1;
            for (var i = 1; i < span; i++)
                cells.Add(string.Empty);
        }

        return cells;
    }

    /// <summary>
    /// Converts a cell's contents and flattens the result to one line, so inline formatting inside
    /// it survives but its structure does not.
    /// </summary>
    private string CellText(IElement cell)
    {
        var nested = new ContentWriter();
        Write(cell.ChildNodes, nested);

        return nested.Build()
            .Replace("\r", string.Empty)
            .Replace('\n', ' ')
            .Replace("|", "\\|")
            .Trim();
    }

    private static int LongestBacktickRun(string value)
    {
        var longest = 0;
        var current = 0;

        foreach (var c in value)
        {
            current = c == '`' ? current + 1 : 0;
            longest = Math.Max(longest, current);
        }

        return longest;
    }
}
