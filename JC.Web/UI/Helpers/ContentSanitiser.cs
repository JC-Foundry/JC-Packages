using Ganss.Xss;

namespace JC.Web.UI.Helpers;

/// <summary>
/// Server-side sanitisation for HTML authored by a user — typically the output of a rich-text editor.
/// Everything outside the configured allowlist is removed: scripts, event handlers,
/// <c>javascript:</c> URLs and unknown elements.
/// </summary>
/// <remarks>
/// <para>
/// Treat this as the <b>only</b> XSS control on that content. An editor's own sanitiser and
/// paste-cleanup settings run in the browser, and the value normally reaches the server through an
/// ordinary form field — so anything holding a valid antiforgery token can post straight past them.
/// Editors that expose a source-code view make arbitrary markup an expected input, not an exotic
/// attack.
/// </para>
/// <para>
/// Sanitise on <b>write</b> rather than on render. The stored value is then trustworthy for every
/// reader, including other applications sharing the database, instead of each render site having to
/// remember — which is what keeps <c>@Html.Raw</c> honest.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Default rich-text policy
/// var clean = ContentSanitiser.SanitiseContent(model.Body);
///
/// // Comment-sized policy, reused across calls
/// var sanitiser = new ContentSanitiser(ContentSanitiserOptions.Basic());
/// var comment = sanitiser.Sanitise(model.Comment);
///
/// // Rich text, minus inline images
/// var noImages = new ContentSanitiser(o =>
/// {
///     o.AllowInlineImages = false;
///     o.AllowedTags.Remove("img");
/// });
/// </code>
/// </example>
public class ContentSanitiser
{
    private readonly ContentSanitiserOptions _options;

    /// <summary>
    /// Creates a sanitiser using <see cref="ContentSanitiserOptions.RichText"/>.
    /// </summary>
    public ContentSanitiser() : this(ContentSanitiserOptions.RichText())
    {
    }

    /// <summary>
    /// Creates a sanitiser using the supplied options.
    /// </summary>
    /// <param name="options">The allowlists to enforce. Build one from a
    /// <see cref="ContentSanitiserOptions"/> preset.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
    public ContentSanitiser(ContentSanitiserOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <summary>
    /// Creates a sanitiser from <see cref="ContentSanitiserOptions.RichText"/> with adjustments
    /// applied — the shorthand for "the usual policy, but…".
    /// </summary>
    /// <param name="configure">Receives the rich-text options to modify.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <c>null</c>.</exception>
    public ContentSanitiser(Action<ContentSanitiserOptions> configure) : this(FromRichText(configure))
    {
    }

    /// <summary>
    /// Returns <paramref name="html"/> with everything outside this instance's allowlist removed.
    /// </summary>
    /// <param name="html">The untrusted HTML to sanitise.</param>
    /// <returns>The sanitised HTML, or <c>null</c> when <paramref name="html"/> is <c>null</c>, empty
    /// or whitespace — so a visually-empty editor stores "no content" rather than stray markup, and
    /// any publish guard downstream still reads it as unpublishable.</returns>
    public string? Sanitise(string? html)
        => string.IsNullOrWhiteSpace(html) ? null : Build(_options).Sanitize(html);

    /// <summary>
    /// Sanitises <paramref name="html"/> against <see cref="ContentSanitiserOptions.RichText"/>
    /// without constructing an instance. Equivalent to <c>new ContentSanitiser().Sanitise(html)</c>.
    /// </summary>
    /// <param name="html">The untrusted HTML to sanitise.</param>
    /// <returns>The sanitised HTML, or <c>null</c> when <paramref name="html"/> is <c>null</c>, empty
    /// or whitespace.</returns>
    public static string? SanitiseContent(string? html)
        => string.IsNullOrWhiteSpace(html) ? null : Build(ContentSanitiserOptions.RichText()).Sanitize(html);

    private static ContentSanitiserOptions FromRichText(Action<ContentSanitiserOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = ContentSanitiserOptions.RichText();
        configure(options);
        return options;
    }

    // Built per call rather than cached: HtmlSanitizer documents no thread-safety guarantee, and the
    // options are mutable, so a shared instance could be reconfigured mid-sanitise. Content saves are
    // rare enough that a fresh instance costs nothing next to that certainty.
    private static HtmlSanitizer Build(ContentSanitiserOptions options)
    {
        var sanitiser = new HtmlSanitizer();

        sanitiser.AllowedTags.Clear();
        foreach (var tag in options.AllowedTags) sanitiser.AllowedTags.Add(tag);

        sanitiser.AllowedAttributes.Clear();
        foreach (var attribute in options.AllowedAttributes) sanitiser.AllowedAttributes.Add(attribute);

        sanitiser.AllowedCssProperties.Clear();
        foreach (var property in options.AllowedCssProperties) sanitiser.AllowedCssProperties.Add(property);

        sanitiser.AllowedSchemes.Clear();
        foreach (var scheme in options.AllowedSchemes) sanitiser.AllowedSchemes.Add(scheme);

        // Left empty by HtmlSanitizer means "any class"; only narrow it when the caller has asked.
        sanitiser.AllowedClasses.Clear();
        foreach (var className in options.AllowedClasses) sanitiser.AllowedClasses.Add(className);

        sanitiser.KeepChildNodes = options.KeepChildNodes;

        if (options.AllowInlineImages)
        {
            // Allowing the scheme outright would also permit data:text/html on a link, so it is
            // narrowed to image payloads on <img> below.
            sanitiser.AllowedSchemes.Add("data");
            sanitiser.FilterUrl += RestrictDataUrisToImages;
        }

        options.Configure?.Invoke(sanitiser);

        return sanitiser;
    }

    private static void RestrictDataUrisToImages(object? sender, FilterUrlEventArgs e)
    {
        if (e.SanitizedUrl is null
            || !e.SanitizedUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return;

        var isInlineImage =
            string.Equals(e.Tag?.NodeName, "img", StringComparison.OrdinalIgnoreCase)
            && e.SanitizedUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase);

        if (!isInlineImage)
            e.SanitizedUrl = null;   // drops the attribute
    }
}
