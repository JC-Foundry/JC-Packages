using JC.Web.UI.HTML;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JC.Web.UI.TagHelpers;

/// <summary>
/// Tag helper that renders a Bootstrap 5 breadcrumb navigation from nested <c>&lt;crumb&gt;</c> elements.
/// The last crumb is automatically rendered as the active page.
/// </summary>
/// <example>
/// <code>
/// &lt;breadcrumb&gt;
///   &lt;crumb label="Home" href="/" /&gt;
///   &lt;crumb label="Products" href="/products" /&gt;
///   &lt;crumb label="Widget" /&gt;
/// &lt;/breadcrumb&gt;
/// </code>
/// </example>
[HtmlTargetElement("breadcrumb")]
public class BreadcrumbTagHelper : TagHelper
{
    internal const string CrumbListKey = nameof(BreadcrumbTagHelper) + ".Crumbs";

    /// <inheritdoc />
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var crumbs = new List<(string Label, string? Href)>();
        context.Items[CrumbListKey] = crumbs;

        _ = await output.GetChildContentAsync();

        if (crumbs.Count == 0)
        {
            output.SuppressOutput();
            return;
        }

        var builder = new BreadcrumbBuilder();
        foreach (var (label, href) in crumbs)
            builder.Add(label, href);

        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(builder.Build());
    }
}

/// <summary>
/// Child tag helper for <see cref="BreadcrumbTagHelper"/>. Defines a single breadcrumb item.
/// Must be nested inside a <c>&lt;breadcrumb&gt;</c> element.
/// </summary>
[HtmlTargetElement("crumb", ParentTag = "breadcrumb", TagStructure = TagStructure.WithoutEndTag)]
public class CrumbTagHelper : TagHelper
{
    /// <summary>Gets or sets the display text for this breadcrumb item.</summary>
    [HtmlAttributeName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL for this breadcrumb item. If omitted, renders as plain text.</summary>
    [HtmlAttributeName("href")]
    public string? Href { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.SuppressOutput();

        if (context.Items.TryGetValue(BreadcrumbTagHelper.CrumbListKey, out var obj)
            && obj is List<(string Label, string? Href)> crumbs)
        {
            crumbs.Add((Label, Href));
        }
    }
}
