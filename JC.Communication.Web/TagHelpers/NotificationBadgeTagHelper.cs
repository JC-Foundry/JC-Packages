using System.Net;
using JC.Communication.Notifications.Services;
using JC.Communication.Web.Framework;
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
    private readonly HtmlHelper _html;
    private readonly ICommunicationFrameworkDictionary _dictionary;
    private readonly ICommunicationIconDictionary _icons;

    /// <summary>
    /// Gets or sets the icon class. Falls back to the configured icon set's bell when unset.
    /// </summary>
    /// <remarks>
    /// Normalised against the configured icon set's base class, so under Bootstrap Icons both
    /// <c>"bi-star"</c> and <c>"bi bi-star"</c> work. A set with no base class, such as Font
    /// Awesome, takes the value as given — <c>"fa-solid fa-star"</c>.
    /// </remarks>
    [HtmlAttributeName("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the badge colour. Falls back to the configured framework's default when unset.
    /// </summary>
    [HtmlAttributeName("badge-colour")]
    public string? BadgeColour { get; set; }

    /// <summary>Gets or sets whether to hide the badge when the count is zero. Defaults to true.</summary>
    [HtmlAttributeName("hide-when-zero")]
    public bool HideWhenZero { get; set; } = true;

    public NotificationBadgeTagHelper(NotificationCache cache,
        HtmlHelper html,
        ICommunicationFrameworkDictionary dictionary,
        ICommunicationIconDictionary icons)
    {
        _cache = cache;
        _html = html;
        _dictionary = dictionary;
        _icons = icons;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var css = _dictionary.NotificationBadge;

        var unreadCount = await _cache.GetUnreadCountAsync();
        var iconHtml = _html.CreateElement("i", "", classes: WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(Icon) ? _icons.Icons.Bell : _icons.Icons.Custom(Icon)));

        if (unreadCount == 0 && HideWhenZero)
        {
            output.TagName = "span";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Content.SetHtmlContent(iconHtml);
            return;
        }

        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", css.Container);

        var badgeColour = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(BadgeColour) ? css.DefaultBadgeColour : BadgeColour);

        var countText = unreadCount > 99 ? "99+" : unreadCount.ToString();
        var badge = _html.CreateElement("span",
            countText + _html.CreateElement("span", "unread notifications", classes: css.ScreenReaderOnly),
            classes: css.Badge(badgeColour));

        output.Content.SetHtmlContent(iconHtml + badge);
    }
}
