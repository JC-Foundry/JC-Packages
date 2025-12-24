namespace JC.Web.Helpers.HTML;

/// <summary>
/// Fluent API for building HTML tags with attributes, classes, and content.
/// Provides both general-purpose tag building and specific helper methods for common patterns.
/// </summary>
public static class HtmlHelper
{
    /// <summary>
    /// Creates a new HTML tag builder for the specified tag name
    /// </summary>
    private static HtmlTagBuilder Tag(string tagName) => new(tagName);

    public static string CreateElement(string tagName, string content = "", bool isActive = false, bool isDisabled = false, 
        Dictionary<string, string>? attributes = null, params string[] classes)
    {
        var builder = Tag(tagName);
        if (isActive) builder.AddActiveAttribute();
        if (isDisabled) builder.AddDisabledClass();

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

        return builder.SetContent(content).Build();
    }
    
    /// <summary>
    /// Builds a pagination list item (&lt;li class="page-item"&gt;...&lt;/li&gt;) with optional states
    /// </summary>
    /// <param name="content">Inner HTML content (usually a link or span)</param>
    /// <param name="isActive">Whether this is the active/current page</param>
    /// <param name="isDisabled">Whether this item is disabled</param>
    /// <returns>Complete HTML string for the list item</returns>
    public static string PaginationItem(string content, bool isActive = false, bool isDisabled = false)
    {
        var builder = Tag("li").AddClass("page-item");
        if (isActive) builder.AddActiveAttribute();
        if (isDisabled) builder.AddDisabledClass();

        return builder.SetContent(content).Build();
    }

    /// <summary>
    /// Builds a pagination link (&lt;a class="page-link"&gt;...&lt;/a&gt;)
    /// </summary>
    /// <param name="text">Link text</param>
    /// <param name="href">URL to navigate to</param>
    /// <param name="buttonClass">Additional CSS classes to apply to the link</param>
    /// <param name="isActive">Whether this is the active/current page (adds aria-current="page")</param>
    /// <returns>Complete HTML string for the anchor tag</returns>
    public static string PaginationLink(string text, string href, string? buttonClass = null, bool isActive = false)
    {
        var builder = Tag("a")
            .AddClass("page-link")
            .AddAttribute("href", href)
            .SetContent(text);

        if (!string.IsNullOrWhiteSpace(buttonClass))
            builder.AddClass(buttonClass);

        if (isActive)
            builder.AddCurrentPageAttribute();

        return builder.Build();
    }
}
