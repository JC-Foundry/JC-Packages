using JC.Web.SEO.Helpers;
using JC.Web.SEO.Models.Options;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;

namespace JC.Web.SEO.TagHelpers;

/// <summary>
/// Renders a page's SEO head content — title, description, canonical URL, robots directives,
/// Open Graph and Twitter Card tags. Place inside <c>&lt;head&gt;</c>.
/// </summary>
/// <remarks>
/// Values not supplied fall back to the defaults configured through <c>AddSeoMeta</c>. For
/// JSON-LD structured data, use <see cref="SeoBuilder"/> directly — a tag helper attribute is a
/// poor fit for a nested object graph.
/// </remarks>
/// <example>
/// <code>
/// &lt;seo-meta title="Widget"
///           description="A widget."
///           canonical="/products/widget"
///           image="/img/widget.png" /&gt;
/// </code>
/// </example>
[HtmlTargetElement("seo-meta", TagStructure = TagStructure.WithoutEndTag)]
public class SeoMetaTagHelper(IOptions<SeoMetaOptions> options) : TagHelper
{
    private readonly SeoMetaOptions _options = options.Value;

    /// <summary>The page title.</summary>
    [HtmlAttributeName("title")]
    public string? Title { get; set; }

    /// <summary>The meta description.</summary>
    [HtmlAttributeName("description")]
    public string? Description { get; set; }

    /// <summary>The canonical URL. Relative values resolve against the configured base URL.</summary>
    [HtmlAttributeName("canonical")]
    public string? Canonical { get; set; }

    /// <summary>Image for Open Graph and the Twitter card.</summary>
    [HtmlAttributeName("image")]
    public string? Image { get; set; }

    /// <summary>The Open Graph object type, such as <c>article</c>. Defaults to <c>website</c>.</summary>
    [HtmlAttributeName("og-type")]
    public string? OpenGraphType { get; set; }

    /// <summary>The Twitter card type. Inferred from the presence of an image when omitted.</summary>
    [HtmlAttributeName("twitter-card")]
    public string? TwitterCard { get; set; }

    /// <summary>Whether the page may be indexed. Falls back to the configured default.</summary>
    [HtmlAttributeName("index")]
    public bool? Index { get; set; }

    /// <summary>Whether links may be followed. Falls back to the configured default.</summary>
    [HtmlAttributeName("follow")]
    public bool? Follow { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var builder = new SeoBuilder(_options);

        if (!string.IsNullOrWhiteSpace(Title)) builder.Title(Title);
        if (!string.IsNullOrWhiteSpace(Description)) builder.Description(Description);
        if (!string.IsNullOrWhiteSpace(Canonical)) builder.Canonical(Canonical);
        if (!string.IsNullOrWhiteSpace(Image)) builder.Image(Image);
        if (!string.IsNullOrWhiteSpace(OpenGraphType)) builder.OpenGraphType(OpenGraphType);
        if (!string.IsNullOrWhiteSpace(TwitterCard)) builder.TwitterCard(TwitterCard);

        if (Index.HasValue || Follow.HasValue)
            builder.Robots(Index ?? _options.DefaultIndex, Follow ?? _options.DefaultFollow);

        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(builder.Build());
    }
}
