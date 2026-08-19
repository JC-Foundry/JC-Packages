using AngleSharp.Dom;
using JC.Content.Conversion.Helpers;
using JC.Content.Conversion.Models.Options;

namespace JC.Content.Conversion.Services;

/// <summary>
/// Walks an HTML document and writes its readable text.
/// </summary>
/// <remarks>
/// Not tag stripping — that runs blocks together, since <c>&lt;p&gt;One&lt;/p&gt;&lt;p&gt;Two&lt;/p&gt;</c>
/// holds no whitespace between the two words. Block elements separate, list items keep a marker,
/// and entities are already decoded by the parser.
/// </remarks>
internal sealed class HtmlToTextWriter(ContentConversionOptions options)
{
    private static readonly HashSet<string> Dropped =
        ["script", "style", "noscript", "template", "head", "title", "meta", "link"];

    private static readonly HashSet<string> Blocks =
    [
        "p", "div", "section", "article", "aside", "header", "footer", "main", "nav",
        "figure", "figcaption", "address", "dl", "dt", "dd", "blockquote",
        "h1", "h2", "h3", "h4", "h5", "h6"
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
            case "br":
                writer.EndLine();
                return;

            case "hr":
                writer.StartBlock();
                writer.WriteLine("---");
                return;

            case "pre":
                WriteCodeBlock(element, writer);
                return;

            case "a":
                WriteLink(element, writer);
                return;

            case "img":
                //Only the alternative text is readable; the image itself has no plain-text form
                writer.WriteText(element.GetAttribute("alt"));
                return;

            case "ul" or "ol":
                WriteList(element, writer);
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

                WriteChildren(element, writer);
                return;
        }
    }

    private void WriteChildren(IElement element, ContentWriter writer) => Write(element.ChildNodes, writer);

    private static void WriteCodeBlock(IElement element, ContentWriter writer)
    {
        var text = element.TextContent.Replace("\r\n", "\n").TrimEnd('\n');

        writer.StartBlock();

        //Whitespace is significant here, so the lines go across as they are
        foreach (var line in text.Split('\n'))
            writer.WriteLine(line);
    }

    private void WriteLink(IElement element, ContentWriter writer)
    {
        WriteChildren(element, writer);

        if(!options.IncludeLinkUrlsInText)
            return;

        var href = element.GetAttribute("href");
        if(!string.IsNullOrWhiteSpace(href))
            writer.Write($" ({href.Trim()})");
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
            writer.PushPrefix(new string(' ', marker.Length));
            writer.SuppressNextBlock();

            WriteChildren(item, writer);

            writer.EndLine();
            writer.PopPrefix();
        }

        writer.ExitList();
    }

    /// <summary>Writes each row on its own line, cells separated by a tab.</summary>
    private void WriteTable(IElement table, ContentWriter writer)
    {
        var rows = table.QuerySelectorAll("tr").Where(r => r.Closest("table") == table);

        writer.StartBlock();

        foreach (var row in rows)
        {
            var cells = row.Children
                .Where(c => c.LocalName is "td" or "th")
                .Select(CellText)
                .Where(c => c.Length > 0);

            writer.WriteLine(string.Join('\t', cells));
        }
    }

    private string CellText(IElement cell)
    {
        var nested = new ContentWriter(escape: false);
        Write(cell.ChildNodes, nested);

        return nested.Build().Replace("\r", string.Empty).Replace('\n', ' ').Trim();
    }
}
