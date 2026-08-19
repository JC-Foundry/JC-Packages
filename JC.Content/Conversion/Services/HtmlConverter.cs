using AngleSharp.Html.Parser;
using JC.Content.Conversion.Helpers;
using JC.Content.Conversion.Models.Options;

namespace JC.Content.Conversion.Services;

/// <summary>Converts HTML into the other formats by walking its parsed document.</summary>
internal sealed class HtmlConverter(ContentConversionOptions options)
{
    private readonly HtmlToMarkdownWriter _markdown = new(options);
    private readonly HtmlToTextWriter _text = new(options);

    public string ToMarkdown(string html) => Walk(html, escape: true, _markdown.Write);

    public string ToText(string html) => Walk(html, escape: false, _text.Write);

    private static string Walk(string html, bool escape, Action<AngleSharp.Dom.INodeList, ContentWriter> write)
    {
        //Parsed per call rather than shared: the parser is cheap next to the parse itself, and this
        //service is a singleton that may be reached concurrently
        var document = new HtmlParser().ParseDocument(html);
        if(document.Body is null)
            return string.Empty;

        var writer = new ContentWriter(escape);
        write(document.Body.ChildNodes, writer);

        return writer.Build();
    }
}
