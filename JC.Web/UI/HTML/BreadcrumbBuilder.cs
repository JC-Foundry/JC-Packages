using System.Net;
using System.Text;
using JC.Web.UI.Framework;

namespace JC.Web.UI.HTML;

/// <summary>
/// Fluent builder for constructing breadcrumb navigation using the configured UI framework's classes.
/// The last item is always rendered as the active page.
/// </summary>
/// <remarks>
/// Holds per-use state, so unlike <see cref="AlertHelper"/> and <see cref="HtmlHelper"/> this is
/// constructed per trail rather than resolved as a singleton. Inject
/// <see cref="IWebFrameworkDictionary"/> and pass it in.
/// </remarks>
/// <example>
/// <code>
/// var html = new BreadcrumbBuilder(dictionary)
///     .Add("Home", "/")
///     .Add("Products", "/products")
///     .Add("Widget")
///     .Build();
/// </code>
/// </example>
/// <param name="dictionary">The class dictionary for the configured framework.</param>
public class BreadcrumbBuilder(IWebFrameworkDictionary dictionary)
{
    private readonly IWebFrameworkDictionary _dictionary = dictionary;
    private readonly List<(string Label, string? Url)> _items = new();

    /// <summary>
    /// Adds a breadcrumb item. The last item added will be rendered as the active (current) page.
    /// </summary>
    /// <param name="label">The display text for the breadcrumb item.</param>
    /// <param name="url">The URL to link to. If <c>null</c>, the item renders as plain text.</param>
    /// <returns>The builder instance for chaining.</returns>
    public BreadcrumbBuilder Add(string label, string? url = null)
    {
        _items.Add((label, url));
        return this;
    }

    /// <summary>
    /// Builds and returns the complete breadcrumb HTML as a Bootstrap 5 nav component.
    /// </summary>
    /// <returns>The rendered HTML string, or an empty string if no items have been added.</returns>
    public string Build()
    {
        if (_items.Count == 0)
            return string.Empty;

        var classes = _dictionary.Breadcrumb;

        var sb = new StringBuilder();
        sb.Append("<nav aria-label=\"breadcrumb\"");

        // Bootstrap styles the list rather than the nav, so the nav class is usually empty —
        // emit the attribute only when a framework actually supplies one.
        if (!string.IsNullOrWhiteSpace(classes.Nav))
            sb.Append(" class=\"").Append(classes.Nav).Append('"');

        sb.Append("><ol class=\"").Append(classes.List).Append("\">");

        for (var i = 0; i < _items.Count; i++)
        {
            var (label, url) = _items[i];
            var isLast = i == _items.Count - 1;
            var encodedLabel = WebUtility.HtmlEncode(label);

            if (isLast)
            {
                sb.Append("<li class=\"").Append(classes.ActiveItem).Append("\" aria-current=\"page\">")
                    .Append(encodedLabel)
                    .Append("</li>");
            }
            else
            {
                sb.Append("<li class=\"").Append(classes.Item).Append("\">");

                if (url != null)
                    sb.Append("<a href=\"").Append(WebUtility.HtmlEncode(url)).Append("\">").Append(encodedLabel).Append("</a>");
                else
                    sb.Append(encodedLabel);

                sb.Append("</li>");
            }
        }

        sb.Append("</ol></nav>");
        return sb.ToString();
    }

    /// <summary>
    /// Implicit conversion to string — calls <see cref="Build"/>.
    /// </summary>
    public static implicit operator string(BreadcrumbBuilder builder) => builder.Build();
}
