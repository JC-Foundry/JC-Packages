using System.Net;
using JC.Communication.Notifications.Services;
using JC.Web.UI.HTML;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JC.Communication.Web.TagHelpers;

/// <summary>
/// Renders a lightweight unread notification count badge. Use this as a simpler
/// alternative to <see cref="NotificationDropdownTagHelper"/> when only the count is needed.
/// </summary>
[HtmlTargetElement("notification-badge", TagStructure = TagStructure.WithoutEndTag)]
public class NotificationBadgeTagHelper : TagHelper
{
    private readonly NotificationCache _cache;

    /// <summary>Gets or sets the Bootstrap icon class. Defaults to "bi-bell".</summary>
    [HtmlAttributeName("icon")]
    public string Icon { get; set; } = "bi-bell";

    /// <summary>Gets or sets the Bootstrap badge colour class. Defaults to "danger".</summary>
    [HtmlAttributeName("badge-colour")]
    public string BadgeColour { get; set; } = "danger";

    /// <summary>Gets or sets whether to hide the badge when the count is zero. Defaults to true.</summary>
    [HtmlAttributeName("hide-when-zero")]
    public bool HideWhenZero { get; set; } = true;

    public NotificationBadgeTagHelper(NotificationCache cache)
    {
        _cache = cache;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var unreadCount = await _cache.GetUnreadCountAsync();
        var iconHtml = HtmlHelper.CreateElement("i", "", classes: $"bi {WebUtility.HtmlEncode(Icon)}");

        if (unreadCount == 0 && HideWhenZero)
        {
            output.TagName = "span";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Content.SetHtmlContent(iconHtml);
            return;
        }

        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "position-relative");

        var countText = unreadCount > 99 ? "99+" : unreadCount.ToString();
        var badge = HtmlHelper.CreateElement("span",
            countText + HtmlHelper.CreateElement("span", "unread notifications", classes: "visually-hidden"),
            classes: $"position-absolute top-0 start-100 translate-middle badge rounded-pill bg-{WebUtility.HtmlEncode(BadgeColour)}");

        output.Content.SetHtmlContent(iconHtml + badge);
    }
}
