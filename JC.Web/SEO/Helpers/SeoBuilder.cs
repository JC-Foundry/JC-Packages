using System.Net;
using System.Text;
using JC.Web.SEO.Helpers.JsonLd;
using JC.Web.SEO.Models.Options;

namespace JC.Web.SEO.Helpers;

/// <summary>
/// Fluent builder for a page's SEO head content — title, description, canonical URL, robots
/// directives, Open Graph and Twitter Card tags, and JSON-LD structured data.
/// </summary>
/// <example>
/// <code>
/// var head = new SeoBuilder(options)
///     .Title("Widget")
///     .Description("A widget.")
///     .Canonical("/products/widget")
///     .Image("/img/widget.png")
///     .JsonLd(new ProductSchema { Name = "Widget" })
///     .Build();
/// </code>
/// </example>
public class SeoBuilder(SeoMetaOptions? options = null)
{
    private readonly SeoMetaOptions _options = options ?? new SeoMetaOptions();
    private readonly List<string> _jsonLd = [];

    private string? _title;
    private string? _description;
    private string? _canonical;
    private string? _image;
    private string? _openGraphType = "website";
    private string? _twitterCard;
    private bool? _index;
    private bool? _follow;

    /// <summary>Sets the page title.</summary>
    /// <param name="title">The title text.</param>
    /// <returns>The builder, so calls can be chained.</returns>
    public SeoBuilder Title(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>Sets the meta description.</summary>
    /// <param name="description">The description text.</param>
    /// <returns>The builder, so calls can be chained.</returns>
    public SeoBuilder Description(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the canonical URL. Relative values are resolved against the configured base URL —
    /// a relative canonical is widely ignored, so the absolute form matters.
    /// </summary>
    /// <param name="url">Absolute URL, or a site-relative path.</param>
    /// <returns>The builder, so calls can be chained.</returns>
    public SeoBuilder Canonical(string url)
    {
        _canonical = url;
        return this;
    }

    /// <summary>Sets the image used for Open Graph and the Twitter card.</summary>
    /// <param name="url">Absolute URL, or a site-relative path.</param>
    /// <returns>The builder, so calls can be chained.</returns>
    public SeoBuilder Image(string url)
    {
        _image = url;
        return this;
    }

    /// <summary>Sets the Open Graph object type. Defaults to <c>website</c>.</summary>
    /// <param name="type">An Open Graph type such as <c>article</c> or <c>product</c>.</param>
    /// <returns>The builder, so calls can be chained.</returns>
    public SeoBuilder OpenGraphType(string type)
    {
        _openGraphType = type;
        return this;
    }

    /// <summary>
    /// Sets the Twitter card type. When not called, a card type is inferred from whether an
    /// image is present.
    /// </summary>
    /// <param name="cardType">A card type such as <c>summary</c> or <c>summary_large_image</c>.</param>
    /// <returns>The builder, so calls can be chained.</returns>
    public SeoBuilder TwitterCard(string cardType)
    {
        _twitterCard = cardType;
        return this;
    }

    /// <summary>Sets the robots directives for this page.</summary>
    /// <param name="index">Whether the page may be indexed.</param>
    /// <param name="follow">Whether links on the page may be followed.</param>
    /// <returns>The builder, so calls can be chained.</returns>
    public SeoBuilder Robots(bool index, bool follow = true)
    {
        _index = index;
        _follow = follow;
        return this;
    }

    /// <summary>Adds a typed schema.org payload as JSON-LD.</summary>
    /// <param name="schema">The structured data to embed.</param>
    /// <returns>The builder, so calls can be chained.</returns>
    public SeoBuilder JsonLd(SchemaObject schema)
        => JsonLd((object)schema);

    /// <summary>
    /// Adds an arbitrary object as JSON-LD, for schema.org types the built-in schemas do not cover.
    /// </summary>
    /// <param name="schema">Any serialisable object.</param>
    /// <returns>The builder, so calls can be chained.</returns>
    public SeoBuilder JsonLd(object schema)
    {
        _jsonLd.Add(JsonLdSerialiser.Serialise(schema));
        return this;
    }

    /// <summary>
    /// Renders the head content.
    /// </summary>
    /// <returns>The rendered HTML, or an empty string when nothing has been set.</returns>
    public string Build()
    {
        var builder = new StringBuilder();

        var description = _description ?? _options.DefaultDescription;
        var image = ToAbsolute(_image ?? _options.DefaultImage);
        var canonical = ToAbsolute(_canonical);

        if (!string.IsNullOrWhiteSpace(_title))
            builder.Append("<title>").Append(Encode(BuildTitle(_title))).AppendLine("</title>");

        AppendNamedMeta(builder, "description", description);

        if (!string.IsNullOrWhiteSpace(canonical))
            builder.Append("<link rel=\"canonical\" href=\"").Append(Encode(canonical)).AppendLine("\">");

        AppendNamedMeta(builder, "robots", BuildRobots());

        // Open Graph
        AppendPropertyMeta(builder, "og:type", _openGraphType);
        AppendPropertyMeta(builder, "og:title", BuildTitle(_title));
        AppendPropertyMeta(builder, "og:description", description);
        AppendPropertyMeta(builder, "og:url", canonical);
        AppendPropertyMeta(builder, "og:image", image);
        AppendPropertyMeta(builder, "og:site_name", _options.SiteName);

        // Twitter card
        var card = _twitterCard ?? (string.IsNullOrWhiteSpace(image) ? "summary" : "summary_large_image");
        AppendNamedMeta(builder, "twitter:card", card);
        AppendNamedMeta(builder, "twitter:title", BuildTitle(_title));
        AppendNamedMeta(builder, "twitter:description", description);
        AppendNamedMeta(builder, "twitter:image", image);
        AppendNamedMeta(builder, "twitter:site", _options.TwitterSite);

        foreach (var payload in _jsonLd)
            builder.Append("<script type=\"application/ld+json\">").Append(payload).AppendLine("</script>");

        return builder.ToString();
    }

    private string? BuildTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return _options.SiteName;

        return string.IsNullOrEmpty(_options.TitleSuffixSeparator) || string.IsNullOrWhiteSpace(_options.SiteName)
            ? title
            : $"{title}{_options.TitleSuffixSeparator}{_options.SiteName}";
    }

    private string BuildRobots()
    {
        var index = _index ?? _options.DefaultIndex;
        var follow = _follow ?? _options.DefaultFollow;

        return $"{(index ? "index" : "noindex")},{(follow ? "follow" : "nofollow")}";
    }

    private string? ToAbsolute(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(_options.BaseUrl))
            return url;

        return Uri.TryCreate(url, UriKind.Absolute, out _)
            ? url
            : $"{_options.BaseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
    }

    private static void AppendNamedMeta(StringBuilder builder, string name, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        builder.Append("<meta name=\"").Append(name).Append("\" content=\"")
            .Append(Encode(content)).AppendLine("\">");
    }

    private static void AppendPropertyMeta(StringBuilder builder, string property, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        builder.Append("<meta property=\"").Append(property).Append("\" content=\"")
            .Append(Encode(content)).AppendLine("\">");
    }

    /// <summary>
    /// Encodes caller-supplied text. Meta content routinely carries page titles and descriptions
    /// drawn from user data, and an unencoded quote would close the attribute.
    /// </summary>
    private static string Encode(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);
}
