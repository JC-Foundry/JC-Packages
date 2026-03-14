using System.Net;
using JC.Communication.Notifications.Helpers;
using JC.Communication.Notifications.Models;
using JC.Communication.Notifications.Services;
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

    /// <summary>Gets or sets the Bootstrap icon class for the bell button. Defaults to "bi-bell".</summary>
    [HtmlAttributeName("icon")]
    public string Icon { get; set; } = "bi-bell";

    /// <summary>Gets or sets the Bootstrap colour class for the unread badge. Defaults to "danger".</summary>
    [HtmlAttributeName("badge-colour")]
    public string BadgeColour { get; set; } = "danger";

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

    /// <summary>Gets or sets the dropdown alignment. Defaults to "end".</summary>
    [HtmlAttributeName("align")]
    public string Align { get; set; } = "end";

    public NotificationDropdownTagHelper(NotificationCache cache)
    {
        _cache = cache;
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
        var badge = items.Count > 0
            ? HtmlHelper.CreateElement("span",
                (items.Count > 99 ? "99+" : items.Count.ToString()) +
                HtmlHelper.CreateElement("span", "unread notifications", classes: "visually-hidden"),
                attributes: new Dictionary<string, string>(),
                classes: $"position-absolute top-0 start-100 translate-middle badge rounded-pill bg-{WebUtility.HtmlEncode(BadgeColour)}")
            : "";

        var button = HtmlHelper.CreateElement("button",
            HtmlHelper.CreateElement("i", "", classes: $"bi {WebUtility.HtmlEncode(Icon)}") + badge,
            attributes: new Dictionary<string, string>
            {
                ["type"] = "button",
                ["data-bs-toggle"] = "dropdown",
                ["aria-expanded"] = "false"
            },
            classes: "btn btn-link position-relative");

        string listContent;
        if (items.Count == 0)
        {
            listContent = HtmlHelper.CreateElement("li",
                WebUtility.HtmlEncode(EmptyText),
                classes: "dropdown-item text-center text-muted py-3");
        }
        else
        {
            var notificationItems = string.Concat(items.Select(BuildNotificationItem));
            var scrollable = HtmlHelper.CreateElement("div", notificationItems,
                attributes: new Dictionary<string, string>
                {
                    ["style"] = $"max-height:{MaxHeight}px;overflow-y:auto;"
                });
            listContent = HtmlHelper.CreateElement("li", scrollable);
        }

        // View all footer
        var footer = "";
        if (!string.IsNullOrWhiteSpace(ViewAllHref))
        {
            var divider = HtmlHelper.CreateElement("li",
                HtmlHelper.CreateElement("hr", "", classes: "dropdown-divider m-0"));
            var link = HtmlHelper.CreateElement("a", "View all",
                attributes: new Dictionary<string, string> { ["href"] = ViewAllHref },
                classes: "dropdown-item text-center py-2");
            footer = divider + HtmlHelper.CreateElement("li", link);
        }

        var menu = HtmlHelper.CreateElement("ul", listContent + footer,
            attributes: new Dictionary<string, string>
            {
                ["style"] = $"width:{DropdownWidth}px;"
            },
            classes: $"dropdown-menu dropdown-menu-{WebUtility.HtmlEncode(Align)} p-0");

        return HtmlHelper.CreateElement("div", button + menu, classes: "dropdown");
    }

    private string BuildNotificationItem(Notification notification)
    {
        var iconClass = notification.Style?.CustomIconClass
                        ?? NotificationUIHelper.GetIconClass(notification.Type);
        var colourClass = notification.Style?.CustomColourClass
                          ?? NotificationUIHelper.GetColourClass(notification.Type);

        var body = WebUtility.HtmlEncode(notification.Body.Truncate(BodyMaxLength));
        var title = WebUtility.HtmlEncode(notification.Title);
        var time = notification.CreatedUtc.ToRelativeTime();

        var icon = HtmlHelper.CreateElement("i", "",
            classes: $"bi {WebUtility.HtmlEncode(iconClass)} text-{WebUtility.HtmlEncode(colourClass)} mt-1");

        var content = HtmlHelper.CreateElement("div",
            HtmlHelper.CreateElement("div", title, classes: "fw-semibold text-truncate") +
            HtmlHelper.CreateElement("div", body, classes: "small text-muted text-truncate") +
            HtmlHelper.CreateElement("div", WebUtility.HtmlEncode(time), classes: "small text-muted"),
            classes: "flex-grow-1 overflow-hidden");

        var unreadDot = HtmlHelper.CreateElement("span", "",
            attributes: new Dictionary<string, string>
            {
                ["style"] = "width:8px;height:8px;min-width:8px;"
            },
            classes: $"bg-{WebUtility.HtmlEncode(BadgeColour)} rounded-circle mt-2");

        var tag = string.IsNullOrWhiteSpace(notification.UrlLink) ? "div" : "a";
        var attrs = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(notification.UrlLink))
            attrs["href"] = notification.UrlLink;

        return HtmlHelper.CreateElement(tag, icon + content + unreadDot,
            attributes: attrs,
            classes: "dropdown-item d-flex align-items-start gap-2 py-2 px-3 border-bottom");
    }
}
