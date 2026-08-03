using Ganss.Xss;

namespace JC.Web.UI.Helpers;

/// <summary>
/// The allowlists and switches a <see cref="ContentSanitiser"/> enforces. Start from one of the
/// presets — <see cref="RichText"/>, <see cref="Basic"/> or <see cref="Empty"/> — and add or remove
/// entries from there; each preset returns a fresh instance, so mutating one never affects another.
/// </summary>
/// <remarks>
/// Everything not listed is removed. Widening a list widens the attack surface, so add only what the
/// producing editor can actually emit.
/// </remarks>
public class ContentSanitiserOptions
{
    /// <summary>
    /// Element names that survive sanitisation. Everything else is stripped — subject to
    /// <see cref="KeepChildNodes"/>, which decides whether the contents survive with it.
    /// </summary>
    public HashSet<string> AllowedTags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Attribute names that survive on any allowed element. Event handlers (<c>onclick</c> and
    /// friends) are removed regardless of what is listed here.
    /// </summary>
    public HashSet<string> AllowedAttributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// CSS property names that survive inside a <c>style</c> attribute. Only consulted when
    /// <c>style</c> is present in <see cref="AllowedAttributes"/>.
    /// </summary>
    public HashSet<string> AllowedCssProperties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// URL schemes that survive in <c>href</c>, <c>src</c> and other URL attributes. An empty set
    /// strips every URL. <c>javascript:</c> is unsafe in all cases — never add it.
    /// </summary>
    public HashSet<string> AllowedSchemes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Class names that survive in a <c>class</c> attribute. Empty (the default) allows <b>all</b>
    /// classes; populate it to restrict them to a known set. Only consulted when <c>class</c> is
    /// present in <see cref="AllowedAttributes"/>.
    /// </summary>
    public HashSet<string> AllowedClasses { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether the children of a disallowed element are kept when that element is stripped. Defaults
    /// to <c>true</c>, so an unrecognised wrapper loses its tags but not the text inside it. Set to
    /// <c>false</c> to delete the whole subtree.
    /// </summary>
    public bool KeepChildNodes { get; set; } = true;

    /// <summary>
    /// Whether images inlined as <c>data:</c> URIs are kept. When <c>true</c>, the <c>data</c> scheme
    /// is allowed but narrowed to <c>data:image/*</c> on <c>&lt;img&gt;</c> elements — a
    /// <c>data:text/html</c> link, which would otherwise execute script, is dropped.
    /// </summary>
    /// <remarks>
    /// Turning this off does not remove a <c>data</c> entry you added to <see cref="AllowedSchemes"/>
    /// yourself; that stays allowed, and unnarrowed, because you asked for it.
    /// </remarks>
    public bool AllowInlineImages { get; set; }

    /// <summary>
    /// Escape hatch for anything not modelled above, run against the underlying
    /// <see cref="HtmlSanitizer"/> after every other setting has been applied — so it can override
    /// them. Use for extra <c>FilterUrl</c>/<c>RemovingAttribute</c> handlers, at-rules, or URI
    /// post-processing.
    /// </summary>
    public Action<HtmlSanitizer>? Configure { get; set; }

    /// <summary>
    /// Allows nothing. Combined with the default <see cref="KeepChildNodes"/> this reduces markup to
    /// its text, which makes it a reasonable "strip all HTML" policy as well as the starting point
    /// for an allowlist built entirely by hand.
    /// </summary>
    /// <returns>Options with every allowlist empty.</returns>
    public static ContentSanitiserOptions Empty() => new();

    /// <summary>
    /// Inline formatting, lists, quotes and links — the amount of markup a comment box or short
    /// description field needs. No images, tables, styles or classes, so the result cannot carry
    /// layout or colour into the page that renders it.
    /// </summary>
    /// <returns>Options allowing basic text formatting.</returns>
    public static ContentSanitiserOptions Basic() => new()
    {
        AllowedTags =
        {
            "p", "br",
            "strong", "b", "em", "i", "u", "s", "del", "ins",
            "blockquote", "pre", "code",
            "ol", "ul", "li",
            "a",
        },
        AllowedAttributes = { "href", "target", "rel", "title" },
        AllowedSchemes = { "http", "https", "mailto" },
    };

    /// <summary>
    /// The full output of a WYSIWYG editor — headings, tables, images and the inline styles used for
    /// font, colour and alignment. Tuned to what a Syncfusion Rich Text Editor toolbar produces, and
    /// a superset of most others.
    /// </summary>
    /// <returns>Options allowing rich-text editor markup, including inline images.</returns>
    public static ContentSanitiserOptions RichText() => new()
    {
        AllowedTags =
        {
            "p", "br", "div", "span", "hr",
            "strong", "b", "em", "i", "u", "s", "strike", "del", "ins", "sub", "sup",
            "h1", "h2", "h3", "h4", "h5", "h6",
            "blockquote", "pre", "code",
            "ol", "ul", "li",
            "a", "img", "figure", "figcaption",
            "table", "caption", "colgroup", "col", "thead", "tbody", "tfoot", "tr", "th", "td",
        },
        AllowedAttributes =
        {
            // 'class' is REQUIRED, not cosmetic: an editor's image quick-toolbar stores Align, Caption
            // and Display as theme classes (e-rte-image, e-imgleft/right/center, e-imgbreak,
            // e-rte-img-caption in Syncfusion's case) and its stylesheet is what positions them. Strip
            // it and every aligned or captioned image silently loses its layout. Table styles work the
            // same way.
            "class", "style", "title", "dir",
            "href", "target", "rel",              // links
            "src", "alt", "width", "height",      // images
            "colspan", "rowspan", "span",         // tables
        },
        AllowedCssProperties =
        {
            // Font, colour and alignment — what editors expose as inline style.
            "color", "background-color", "background", "font-size", "font-family", "text-align",
            "font-weight", "font-style", "text-decoration",

            // Dimensions. Editors write these onto images to keep them fluid (max-width:100%,
            // height:auto, and a percentage width once the author has resized one). Drop them and
            // that normalisation is undone on save.
            "width", "height", "max-width",

            // Tables, indentation and image spacing.
            "border", "border-collapse", "border-color", "border-style", "border-width",
            "padding", "margin", "margin-left", "margin-right", "float", "vertical-align",
            "list-style-type", "text-indent",
        },
        AllowedSchemes = { "http", "https", "mailto" },
        AllowInlineImages = true,
    };
}
