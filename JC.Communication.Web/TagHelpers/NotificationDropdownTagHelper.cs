using System.Net;
using JC.Communication.Notifications.Models;
using JC.Communication.Notifications.Services;
using JC.Communication.Web.Framework;
using JC.Communication.Web.Framework.Icons;
using JC.Core.Extensions;
using JC.Web.UI.HTML;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JC.Communication.Web.TagHelpers;

/// <summary>
/// Renders a notification bell button with a dropdown list of the current user's unread notifications.
/// Notifications are retrieved from <see cref="NotificationCache"/> and ordered descending by creation date.
/// Read notifications are excluded. Custom styling on individual notifications takes precedence over type-based defaults.
/// </summary>
[HtmlTargetElement("notification-dropdown", TagStructure = TagStructure.WithoutEndTag)]
public class NotificationDropdownTagHelper : TagHelper
{
    private readonly NotificationCache _cache;
    private readonly HtmlHelper _html;
    private readonly ICommunicationFrameworkDictionary _dictionary;
    private readonly ICommunicationIconDictionary _icons;

    /// <summary>
    /// Gets or sets the icon class for the bell button. Falls back to the configured icon set's bell
    /// when unset.
    /// </summary>
    /// <remarks>
    /// Normalised against the configured icon set's base class, so under Bootstrap Icons both
    /// <c>"bi-star"</c> and <c>"bi bi-star"</c> work. A set with no base class, such as Font
    /// Awesome, takes the value as given — <c>"fa-solid fa-star"</c>.
    /// </remarks>
    [HtmlAttributeName("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the colour for the unread badge. Falls back to the configured framework's
    /// default when unset.
    /// </summary>
    [HtmlAttributeName("badge-colour")]
    public string? BadgeColour { get; set; }

    /// <summary>Gets or sets the maximum height of the scrollable notification list in pixels. Defaults to 350.</summary>
    [HtmlAttributeName("max-height")]
    public int MaxHeight { get; set; } = 350;

    /// <summary>Gets or sets the dropdown menu width in pixels. Defaults to 360.</summary>
    [HtmlAttributeName("dropdown-width")]
    public int DropdownWidth { get; set; } = 360;

    /// <summary>Gets or sets the text shown when there are no notifications. Defaults to "No new notifications".</summary>
    [HtmlAttributeName("empty-text")]
    public string EmptyText { get; set; } = "No new notifications";

    /// <summary>Gets or sets the maximum length of the notification body before truncation. Defaults to 80.</summary>
    [HtmlAttributeName("body-max-length")]
    public int BodyMaxLength { get; set; } = 80;

    /// <summary>Gets or sets a URL to link the "View all" footer to. If null, no footer is rendered.</summary>
    [HtmlAttributeName("view-all-href")]
    public string? ViewAllHref { get; set; }

    /// <summary>
    /// Gets or sets the dropdown alignment. Falls back to the configured framework's default when
    /// unset, which is "end" under Bootstrap.
    /// </summary>
    [HtmlAttributeName("align")]
    public string? Align { get; set; }

    public NotificationDropdownTagHelper(NotificationCache cache,
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
        var notifications = await _cache.GetNotificationsAsync();
        var items = notifications
            .Where(n => !n.IsRead)
            .OrderByDescending(n => n.CreatedUtc)
            .ToList();

        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(BuildHtml(items));
    }

    private string BuildHtml(List<Notification> items)
    {
        var css = _dictionary.NotificationDropdown;
        var badgeColour = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(BadgeColour) ? css.DefaultBadgeColour : BadgeColour);

        var badge = items.Count > 0
            ? _html.CreateElement("span",
                (items.Count > 99 ? "99+" : items.Count.ToString()) +
                _html.CreateElement("span", "unread notifications", classes: css.ScreenReaderOnly),
                attributes: new Dictionary<string, string>(),
                classes: css.Badge(badgeColour))
            : "";

        var bellIcon = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(Icon) ? _icons.Icons.Bell : _icons.Icons.Custom(Icon));

        var button = _html.CreateElement("button",
            _html.CreateElement("i", string.Empty, classes: bellIcon) + badge,
            attributes: new Dictionary<string, string>
            {
                ["type"] = "button",
                ["data-bs-toggle"] = "dropdown",
                ["aria-expanded"] = "false"
            },
            classes: css.BellButton);

        string listContent;
        if (items.Count == 0)
        {
            listContent = _html.CreateElement("li",
                WebUtility.HtmlEncode(EmptyText),
                classes: css.EmptyItem);
        }
        else
        {
            var notificationItems = string.Concat(items.Select(BuildNotificationItem));
            var scrollable = _html.CreateElement("div", notificationItems,
                attributes: new Dictionary<string, string>
                {
                    ["style"] = $"max-height:{MaxHeight}px;overflow-y:auto;"
                });
            listContent = _html.CreateElement("li", scrollable);
        }

        // View all footer
        var footer = "";
        if (!string.IsNullOrWhiteSpace(ViewAllHref))
        {
            var divider = _html.CreateElement("li",
                _html.CreateElement("hr", string.Empty, classes: css.Divider));
            var link = _html.CreateElement("a", "View all",
                attributes: new Dictionary<string, string> { ["href"] = ViewAllHref },
                classes: css.FooterLink);
            footer = divider + _html.CreateElement("li", link);
        }

        var align = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(Align) ? css.DefaultAlign : Align);

        var menu = _html.CreateElement("ul", listContent + footer,
            attributes: new Dictionary<string, string>
            {
                ["style"] = $"width:{DropdownWidth}px;"
            },
            classes: css.Menu(align));

        return _html.CreateElement("div", button + menu, classes: css.Container);
    }

    private string BuildNotificationItem(Notification notification)
    {
        var css = _dictionary.NotificationDropdown;

        var iconClass = notification.Style?.CustomIconClass is { } custom
            ? _icons.Icons.Custom(custom)
            : _icons.Icons.Notification(notification.Type);
        var colourClass = notification.Style?.CustomColourClass
                          ?? _dictionary.NotificationTypes.Colour(notification.Type);

        var body = WebUtility.HtmlEncode(notification.Body.Truncate(BodyMaxLength));
        var title = WebUtility.HtmlEncode(notification.Title);
        var time = notification.CreatedUtc.ToRelativeTime();

        var icon = _html.CreateElement("i", string.Empty,
            classes: css.ItemIcon(WebUtility.HtmlEncode(iconClass), WebUtility.HtmlEncode(colourClass)));

        var content = _html.CreateElement("div",
            _html.CreateElement("div", title, classes: css.ItemTitle) +
            _html.CreateElement("div", body, classes: css.ItemBody) +
            _html.CreateElement("div", WebUtility.HtmlEncode(time), classes: css.ItemTime),
            classes: css.ItemContent);

        var unreadDot = _html.CreateElement("span", string.Empty,
            attributes: new Dictionary<string, string>
            {
                ["style"] = "width:8px;height:8px;min-width:8px;"
            },
            classes: css.UnreadDot(WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(BadgeColour) ? css.DefaultBadgeColour : BadgeColour)));

        var tag = string.IsNullOrWhiteSpace(notification.UrlLink) ? "div" : "a";
        var attrs = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(notification.UrlLink))
            attrs["href"] = notification.UrlLink;

        return _html.CreateElement(tag, icon + content + unreadDot,
            attributes: attrs,
            classes: css.Item);
    }
}
