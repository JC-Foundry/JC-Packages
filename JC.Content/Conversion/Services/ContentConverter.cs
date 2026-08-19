using System.Diagnostics.CodeAnalysis;
using JC.Content.Conversion.Enums;
using JC.Content.Conversion.Models.Options;

namespace JC.Content.Conversion.Services;

/// <summary>
/// Converts content between plain text, Markdown and HTML.
/// </summary>
/// <remarks>
/// Conversion is structural, not cosmetic: an element with no equivalent in the target format has
/// its markup removed and its content kept, so the result is a document in that format rather than
/// one format wrapped inside another.
/// <para>
/// HTML is the hub. Markdown reaches plain text through it, so plain-text output reads the same
/// whichever format it came from.
/// </para>
/// </remarks>
public class ContentConverter
{
    private readonly HtmlConverter _html;
    private readonly MarkdownConverter _markdown;
    private readonly TextConverter _text;

    public ContentConverter(ContentConversionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _html = new HtmlConverter(options);
        _markdown = new MarkdownConverter(options);
        _text = new TextConverter();
    }

    /// <summary>Converts content from one format to another, returning it unchanged where they match.</summary>
    [return: NotNullIfNotNull(nameof(content))]
    public string? Convert(string? content, ContentFormat from, ContentFormat to)
    {
        if(string.IsNullOrEmpty(content) || from == to)
            return content;

        return (from, to) switch
        {
            (ContentFormat.Html, ContentFormat.Markdown) => _html.ToMarkdown(content),
            (ContentFormat.Html, ContentFormat.PlainText) => _html.ToText(content),
            (ContentFormat.Markdown, ContentFormat.Html) => _markdown.ToHtml(content),
            (ContentFormat.Markdown, ContentFormat.PlainText) => _html.ToText(_markdown.ToHtml(content)),
            (ContentFormat.PlainText, ContentFormat.Html) => _text.ToHtml(content),
            (ContentFormat.PlainText, ContentFormat.Markdown) => _text.ToMarkdown(content),
            _ => content
        };
    }

    /// <summary>Converts HTML to Markdown, mapping what it can and dropping the tags it cannot.</summary>
    [return: NotNullIfNotNull(nameof(html))]
    public string? HtmlToMarkdown(string? html)
        => Convert(html, ContentFormat.Html, ContentFormat.Markdown);

    /// <summary>Converts HTML to its readable text, with blocks separated and list markers kept.</summary>
    [return: NotNullIfNotNull(nameof(html))]
    public string? HtmlToText(string? html)
        => Convert(html, ContentFormat.Html, ContentFormat.PlainText);

    /// <summary>Converts Markdown to HTML.</summary>
    [return: NotNullIfNotNull(nameof(markdown))]
    public string? MarkdownToHtml(string? markdown)
        => Convert(markdown, ContentFormat.Markdown, ContentFormat.Html);

    /// <summary>Converts Markdown to its readable text, via HTML.</summary>
    [return: NotNullIfNotNull(nameof(markdown))]
    public string? MarkdownToText(string? markdown)
        => Convert(markdown, ContentFormat.Markdown, ContentFormat.PlainText);

    /// <summary>Converts plain text to HTML, encoded first so its content cannot become markup.</summary>
    [return: NotNullIfNotNull(nameof(text))]
    public string? TextToHtml(string? text)
        => Convert(text, ContentFormat.PlainText, ContentFormat.Html);

    /// <summary>Escapes plain text so Markdown renders it exactly as written.</summary>
    [return: NotNullIfNotNull(nameof(text))]
    public string? TextToMarkdown(string? text)
        => Convert(text, ContentFormat.PlainText, ContentFormat.Markdown);
}
