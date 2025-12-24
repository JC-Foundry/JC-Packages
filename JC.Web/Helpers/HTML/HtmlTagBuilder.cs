using System.Text;

namespace JC.Web.Helpers.HTML;

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
    public HtmlTagBuilder AddClass(string className)
    {
        if (!string.IsNullOrWhiteSpace(className))
            _classes.Add(className);
        return this;
    }

    /// <summary>
    /// Adds or updates an HTML attribute. If the attribute already exists, its value is replaced.
    /// </summary>
    public HtmlTagBuilder AddAttribute(string name, string value)
    {
        _attributes[name] = value;
        return this;
    }

    public HtmlTagBuilder AddActiveAttribute()
    {
        _classes.Add("active");
        return this;
    }

    public HtmlTagBuilder AddCurrentPageAttribute()
    {
        _attributes[ActiveKey] = "page";
        return this;
    }

    public HtmlTagBuilder AddDisabledClass()
    {
        _classes.Add("disabled");
        return this;
    }

    /// <summary>
    /// Sets the inner HTML content of the tag. Overwrites any previously set content.
    /// </summary>
    public HtmlTagBuilder SetContent(string content)
    {
        _content = content;
        return this;
    }

    /// <summary>
    /// Builds and returns the complete HTML tag as a string
    /// </summary>
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
            sb.Append(' ').Append(attr.Key).Append("=\"").Append(attr.Value).Append('"');
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
