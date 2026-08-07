using JC.Web.UI.Framework;

namespace JC.Web.UI.HTML;

/// <summary>
/// Builds HTML elements using the configured UI framework's classes, with specific methods for
/// pagination components. Registered as a singleton by <c>AddUI</c>.
/// </summary>
/// <param name="dictionary">The class dictionary for the configured framework.</param>
public class HtmlHelper(IWebFrameworkDictionary dictionary)
{
    /// <summary>
    /// Creates a new HTML tag builder for the specified tag name
    /// </summary>
    private static HtmlTagBuilder Tag(string tagName) => new(tagName);

    /// <summary>
    /// Creates a generic HTML element with optional content, state classes, custom attributes, and CSS classes.
    /// </summary>
    /// <param name="tagName">The HTML tag name (e.g. <c>"div"</c>, <c>"span"</c>).</param>
    /// <param name="content">The inner HTML content. Defaults to an empty string.</param>
    /// <param name="isActive">Whether to add the framework's active class.</param>
    /// <param name="isDisabled">Whether to add the framework's disabled class.</param>
    /// <param name="attributes">Optional dictionary of HTML attributes to add.</param>
    /// <param name="classes">Additional CSS classes to apply.</param>
    /// <returns>The rendered HTML string.</returns>
    /// <remarks>
    /// <paramref name="content"/> is inserted as raw HTML and is not encoded. Pass user-supplied
    /// text through <see cref="System.Net.WebUtility.HtmlEncode"/> or a sanitiser first.
    /// </remarks>
    public string CreateElement(string tagName, string content = "", bool isActive = false, bool isDisabled = false,
        Dictionary<string, string>? attributes = null, params string[] classes)
    {
        var builder = Tag(tagName);

        if (isActive) builder.AddClass(dictionary.State.Active);
        if (isDisabled) builder.AddClass(dictionary.State.Disabled);

        if (attributes != null)
        {
            foreach (var (key, value) in attributes)
            {
                builder.AddAttribute(key, value);
            }
        }

        foreach (var c in classes)
        {
            builder.AddClass(c);
        }

        return builder.SetRawContent(content).Build();
    }

    /// <summary>
    /// Builds a pagination list item with optional active or disabled state.
    /// </summary>
    /// <param name="content">Inner HTML content (usually a link or span)</param>
    /// <param name="isActive">Whether this is the active/current page</param>
    /// <param name="isDisabled">Whether this item is disabled</param>
    /// <returns>Complete HTML string for the list item</returns>
    /// <remarks>
    /// The three states resolve to whole class values rather than a base class plus a modifier, so
    /// a framework whose active item shares nothing with its inactive one can express that.
    /// <paramref name="isActive"/> wins when both flags are set — an item cannot meaningfully be
    /// the current page and unavailable at once.
    /// </remarks>
    public string PaginationItem(string content, bool isActive = false, bool isDisabled = false)
    {
        var classes = dictionary.Pagination;

        var itemClass = isActive
            ? classes.ActiveItem
            : isDisabled
                ? classes.DisabledItem
                : classes.Item;

        return Tag("li")
            .AddClass(itemClass)
            .SetRawContent(content)
            .Build();
    }

    /// <summary>
    /// Builds a pagination link.
    /// </summary>
    /// <param name="text">Link text</param>
    /// <param name="href">URL to navigate to</param>
    /// <param name="buttonClass">Additional CSS classes to apply to the link</param>
    /// <param name="isActive">Whether this is the active/current page (adds <c>aria-current="page"</c>)</param>
    /// <returns>Complete HTML string for the anchor tag</returns>
    public string PaginationLink(string text, string href, string? buttonClass = null, bool isActive = false)
    {
        var builder = Tag("a")
            .AddClass(dictionary.Pagination.Link)
            .AddAttribute("href", href)
            .SetRawContent(text);

        if (!string.IsNullOrWhiteSpace(buttonClass))
            builder.AddClass(buttonClass);

        if (isActive)
            builder.AddCurrentPageAttribute();

        return builder.Build();
    }

    /// <summary>
    /// The class for the list wrapping pagination items.
    /// </summary>
    public string PaginationListClass => dictionary.Pagination.List;

    /// <summary>
    /// The class for the navigation element wrapping a pagination control.
    /// </summary>
    public string PaginationNavClass => dictionary.Pagination.Nav;
}
