using System.Net;
using System.Text;

namespace JC.Web.UI.HTML;

/// <summary>
/// Fluent builder for constructing HTML tags with attributes, classes, and content
/// </summary>
public class HtmlTagBuilder
{
    private readonly string _tagName;
    private readonly Dictionary<string, string> _attributes = new();
    private readonly List<string> _classes = new();
    private string? _content;
    private readonly bool _selfClosing;

    private const string ActiveKey = "aria-current";

    internal HtmlTagBuilder(string tagName, bool selfClosing = false)
    {
        _tagName = tagName;
        _selfClosing = selfClosing;
    }

    /// <summary>
    /// Adds a CSS class to the tag. Multiple classes can be added by calling this method multiple times.
    /// Empty or whitespace-only class names are ignored.
    /// </summary>
    /// <param name="className">The CSS class name to add.</param>
    /// <returns>The builder instance for chaining.</returns>
    public HtmlTagBuilder AddClass(string className)
    {
        if (!string.IsNullOrWhiteSpace(className))
            _classes.Add(className);
        return this;
    }

    /// <summary>
    /// Adds or updates an HTML attribute. If the attribute already exists, its value is replaced.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value (will be HTML-encoded).</param>
    /// <returns>The builder instance for chaining.</returns>
    public HtmlTagBuilder AddAttribute(string name, string value)
    {
        _attributes[name] = value;
        return this;
    }

    /// <summary>
    /// Adds the <c>active</c> CSS class to the tag, commonly used for Bootstrap active states.
    /// </summary>
    /// <returns>The builder instance for chaining.</returns>
    public HtmlTagBuilder AddActiveAttribute()
    {
        _classes.Add("active");
        return this;
    }

    /// <summary>
    /// Adds the <c>aria-current="page"</c> attribute, indicating the current page in navigation.
    /// </summary>
    /// <returns>The builder instance for chaining.</returns>
    public HtmlTagBuilder AddCurrentPageAttribute()
    {
        _attributes[ActiveKey] = "page";
        return this;
    }

    /// <summary>
    /// Adds the <c>disabled</c> CSS class to the tag.
    /// </summary>
    /// <returns>The builder instance for chaining.</returns>
    public HtmlTagBuilder AddDisabledClass()
    {
        _classes.Add("disabled");
        return this;
    }

    /// <summary>
    /// Sets the inner text content of the tag. The content is HTML-encoded to prevent injection.
    /// Overwrites any previously set content.
    /// </summary>
    /// <param name="content">The text content (will be HTML-encoded).</param>
    /// <returns>The builder instance for chaining.</returns>
    public HtmlTagBuilder SetContent(string content)
    {
        _content = WebUtility.HtmlEncode(content);
        return this;
    }

    /// <summary>
    /// Sets the inner HTML content of the tag without encoding. Use this for nested HTML
    /// such as child elements built by other <see cref="HtmlTagBuilder"/> instances.
    /// <para>
    /// <b>Warning:</b> Content is inserted as raw HTML. Do not pass unsanitised user input.
    /// </para>
    /// </summary>
    /// <param name="rawHtml">The raw HTML content (not encoded).</param>
    /// <returns>The builder instance for chaining.</returns>
    public HtmlTagBuilder SetRawContent(string rawHtml)
    {
        _content = rawHtml;
        return this;
    }

    /// <summary>
    /// Builds and returns the complete HTML tag as a string.
    /// </summary>
    /// <returns>The rendered HTML string.</returns>
    public string Build()
    {
        var sb = new StringBuilder();
        sb.Append('<').Append(_tagName);

        // Add classes first
        if (_classes.Count > 0)
        {
            sb.Append(" class=\"").Append(string.Join(" ", _classes)).Append('"');
        }

        // Add other attributes
        foreach (var attr in _attributes)
        {
            sb.Append(' ').Append(attr.Key).Append("=\"").Append(WebUtility.HtmlEncode(attr.Value)).Append('"');
        }

        if (_selfClosing)
        {
            sb.Append(" />");
        }
        else
        {
            sb.Append('>');
            if (_content != null)
            {
                sb.Append(_content);
            }
            sb.Append("</").Append(_tagName).Append('>');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Implicit conversion to string - calls Build()
    /// </summary>
    public static implicit operator string(HtmlTagBuilder builder) => builder.Build();
}
