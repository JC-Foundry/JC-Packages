using System.Text;
using JC.Core.Models.Pagination;
using JC.Web.UI.HTML;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JC.Web.UI.TagHelpers;

/// <summary>
/// Tag helper that renders Bootstrap-compatible pagination from an <see cref="IPagination{T}"/> model.
/// Usage: <c>&lt;pagination model="Model.Items" href-format="/items?page={0}" /&gt;</c>
/// </summary>
[HtmlTargetElement("pagination", TagStructure = TagStructure.WithoutEndTag)]
public class PaginationTagHelper : TagHelper
{
    /// <summary>Gets or sets the pagination model containing page metadata.</summary>
    [HtmlAttributeName("model")]
    public IPagination<object>? Model { get; set; }

    /// <summary>
    /// Gets or sets the URL format string for page links. Use <c>{0}</c> as the page number placeholder.
    /// Example: <c>/items?page={0}</c>
    /// </summary>
    [HtmlAttributeName("href-format")]
    public string HrefFormat { get; set; } = "?page={0}";

    /// <summary>
    /// Gets or sets the maximum number of page links to display before showing ellipsis.
    /// Defaults to <c>5</c>.
    /// </summary>
    [HtmlAttributeName("max-pages")]
    public int MaxVisiblePages { get; set; } = 5;

    /// <summary>Gets or sets the text for the "previous" link. Defaults to <c>&amp;laquo;</c>.</summary>
    [HtmlAttributeName("previous-text")]
    public string PreviousText { get; set; } = "&laquo;";

    /// <summary>Gets or sets the text for the "next" link. Defaults to <c>&amp;raquo;</c>.</summary>
    [HtmlAttributeName("next-text")]
    public string NextText { get; set; } = "&raquo;";

    /// <summary>Gets or sets the text for the "first page" link. Defaults to <c>First</c>.</summary>
    [HtmlAttributeName("first-text")]
    public string FirstText { get; set; } = "First";

    /// <summary>Gets or sets the text for the "last page" link. Defaults to <c>Last</c>.</summary>
    [HtmlAttributeName("last-text")]
    public string LastText { get; set; } = "Last";

    /// <summary>Gets or sets whether to show first/last page links. Defaults to <c>true</c>.</summary>
    [HtmlAttributeName("show-first-last")]
    public bool ShowFirstLast { get; set; } = true;

    /// <summary>Gets or sets additional CSS classes for the nav container.</summary>
    [HtmlAttributeName("container-class")]
    public string? ContainerClass { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Model == null || Model.TotalPages <= 1)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "nav";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("aria-label", "Page navigation");

        if (!string.IsNullOrWhiteSpace(ContainerClass))
            output.Attributes.SetAttribute("class", ContainerClass);

        var sb = new StringBuilder();
        sb.Append("<ul class=\"pagination\">");

        // First page
        if (ShowFirstLast)
            sb.Append(BuildPageItem(FirstText, 1, isDisabled: Model.IsFirstPage));

        // Previous
        sb.Append(BuildPageItem(PreviousText, Model.PageNumber - 1, isDisabled: !Model.HasPreviousPage));

        // Page numbers
        var (start, end) = CalculatePageRange();

        if (start > 1)
            sb.Append(BuildEllipsis());

        for (var i = start; i <= end; i++)
            sb.Append(BuildPageItem(i.ToString(), i, isActive: i == Model.PageNumber));

        if (end < Model.TotalPages)
            sb.Append(BuildEllipsis());

        // Next
        sb.Append(BuildPageItem(NextText, Model.PageNumber + 1, isDisabled: !Model.HasNextPage));

        // Last page
        if (ShowFirstLast)
            sb.Append(BuildPageItem(LastText, Model.TotalPages, isDisabled: Model.IsLastPage));

        sb.Append("</ul>");

        output.Content.SetHtmlContent(sb.ToString());
    }

    private string BuildPageItem(string text, int page, bool isActive = false, bool isDisabled = false)
    {
        var href = isDisabled ? "#" : string.Format(HrefFormat, page);
        var link = HtmlHelper.PaginationLink(text, href, isActive: isActive);
        return HtmlHelper.PaginationItem(link, isActive: isActive, isDisabled: isDisabled);
    }

    private static string BuildEllipsis()
    {
        var link = HtmlHelper.PaginationLink("&hellip;", "#");
        return HtmlHelper.PaginationItem(link, isDisabled: true);
    }

    private (int Start, int End) CalculatePageRange()
    {
        var totalPages = Model!.TotalPages;
        var currentPage = Model.PageNumber;

        if (totalPages <= MaxVisiblePages)
            return (1, totalPages);

        var half = MaxVisiblePages / 2;
        var start = Math.Max(1, currentPage - half);
        var end = Math.Min(totalPages, start + MaxVisiblePages - 1);

        // Adjust start if we're near the end
        if (end - start + 1 < MaxVisiblePages)
            start = Math.Max(1, end - MaxVisiblePages + 1);

        return (start, end);
    }
}