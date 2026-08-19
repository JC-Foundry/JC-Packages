using JC.Content.Helpers;

namespace JC.Content.Conversion.Models.Options;

/// <summary>Conversion settings, fixed at registration.</summary>
public class ContentConversionOptions
{
    /// <summary>
    /// Whether GitHub-flavoured Markdown is read and written — pipe tables, strikethrough and task
    /// lists. Defaults to <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Turning this off leaves no table syntax to convert an HTML table into, so one becomes its
    /// cell text instead.
    /// </remarks>
    public bool GithubFlavoured { get; set; } = true;

    /// <summary>
    /// Whether raw HTML embedded in Markdown survives a conversion to HTML. Defaults to
    /// <c>false</c>, so it is stripped.
    /// </summary>
    /// <remarks>
    /// Markdown permits raw HTML, so a document from an untrusted author can carry a script tag
    /// through unaltered. Leaving this off removes that route; turning it on means the output has
    /// to reach <see cref="ContentSanitiser"/> before anything renders it.
    /// </remarks>
    public bool AllowRawHtml { get; set; }

    /// <summary>
    /// Whether a link's destination follows its text when converting HTML to plain text — 'the docs
    /// (https://example.com)'. Defaults to <c>false</c>, which keeps the text alone.
    /// </summary>
    public bool IncludeLinkUrlsInText { get; set; }
}
