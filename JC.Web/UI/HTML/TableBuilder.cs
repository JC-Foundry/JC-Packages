using System.Net;
using System.Text;
using JC.Web.UI.Framework;

namespace JC.Web.UI.HTML;

/// <summary>
/// Fluent builder for rendering HTML tables from a collection of items, using the configured UI
/// framework's classes. Cell content is HTML-encoded to prevent XSS.
/// </summary>
/// <typeparam name="T">The type of items in the table.</typeparam>
/// <remarks>
/// Holds per-use state and is generic, so it is constructed per table rather than resolved as a
/// singleton. Inject <see cref="IWebFrameworkDictionary"/> and pass it in.
/// </remarks>
/// <example>
/// <code>
/// var html = new TableBuilder&lt;User&gt;(dictionary)
///     .AddColumn("Name", u => u.Name)
///     .AddColumn("Email", u => u.Email)
///     .AddColumn("Age", u => u.Age, cssClass: "text-end")
///     .Build(users);
/// </code>
/// </example>
/// <param name="dictionary">The class dictionary for the configured framework.</param>
public class TableBuilder<T>(IWebFrameworkDictionary dictionary)
{
    private readonly IWebFrameworkDictionary _dictionary = dictionary;
    private readonly List<Column> _columns = new();

    /// <summary>
    /// Adds a column with a string value selector.
    /// </summary>
    /// <param name="header">The column header text.</param>
    /// <param name="valueSelector">A function that extracts the cell value from each item.</param>
    /// <param name="cssClass">Optional CSS class applied to both the <c>&lt;th&gt;</c> and <c>&lt;td&gt;</c> elements.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TableBuilder<T> AddColumn(string header, Func<T, string?> valueSelector, string? cssClass = null)
    {
        _columns.Add(new Column(header, item => valueSelector(item), cssClass));
        return this;
    }

    /// <summary>
    /// Adds a column with an object value selector. The value is converted to a string via <see cref="object.ToString"/>.
    /// </summary>
    /// <param name="header">The column header text.</param>
    /// <param name="valueSelector">A function that extracts the cell value from each item.</param>
    /// <param name="cssClass">Optional CSS class applied to both the <c>&lt;th&gt;</c> and <c>&lt;td&gt;</c> elements.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TableBuilder<T> AddColumn(string header, Func<T, object?> valueSelector, string? cssClass = null)
    {
        _columns.Add(new Column(header, item => valueSelector(item)?.ToString(), cssClass));
        return this;
    }

    /// <summary>
    /// Builds and returns the complete HTML table from the provided items.
    /// </summary>
    /// <param name="items">The collection of items to render as table rows.</param>
    /// <param name="tableClass">
    /// CSS classes for the <c>&lt;table&gt;</c> element. Falls back to the configured framework's
    /// table class when null or whitespace.
    /// </param>
    /// <returns>The rendered HTML table string.</returns>
    public string Build(IEnumerable<T> items, string? tableClass = null)
    {
        var classes = _dictionary.Table;
        var css = string.IsNullOrWhiteSpace(tableClass) ? classes.Table : tableClass;

        var sb = new StringBuilder();
        Open(sb, "table", css);

        BuildHead(sb, classes);
        BuildBody(sb, classes, items);

        sb.Append("</table>");
        return sb.ToString();
    }

    /// <summary>
    /// Opens a tag, emitting a class attribute only when there is one to emit.
    /// </summary>
    /// <remarks>
    /// Bootstrap styles the table element alone and leaves the structural entries empty, so the
    /// generated markup is the bare <c>&lt;thead&gt;</c> and <c>&lt;td&gt;</c> tags it always was.
    /// A framework that has to style every part can fill them in without this builder changing.
    /// </remarks>
    private static void Open(StringBuilder sb, string tag, params string?[] classes)
    {
        var css = FrameworkClass.Join(classes);

        sb.Append('<').Append(tag);

        if (!string.IsNullOrWhiteSpace(css))
            sb.Append(" class=\"").Append(css).Append('"');

        sb.Append('>');
    }

    private void BuildHead(StringBuilder sb, TableClasses classes)
    {
        Open(sb, "thead", classes.Head);
        sb.Append("<tr>");

        foreach (var col in _columns)
        {
            Open(sb, "th", classes.HeaderCell, col.CssClass);
            sb.Append(WebUtility.HtmlEncode(col.Header)).Append("</th>");
        }

        sb.Append("</tr></thead>");
    }

    private void BuildBody(StringBuilder sb, TableClasses classes, IEnumerable<T> items)
    {
        Open(sb, "tbody", classes.Body);

        foreach (var item in items)
        {
            Open(sb, "tr", classes.Row);

            foreach (var col in _columns)
            {
                Open(sb, "td", classes.Cell, col.CssClass);
                sb.Append(WebUtility.HtmlEncode(col.ValueSelector(item) ?? string.Empty)).Append("</td>");
            }

            sb.Append("</tr>");
        }

        sb.Append("</tbody>");
    }

    private sealed record Column(string Header, Func<T, string?> ValueSelector, string? CssClass);
}
