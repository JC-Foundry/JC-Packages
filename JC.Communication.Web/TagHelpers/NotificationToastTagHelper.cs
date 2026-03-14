using System.Net;
using JC.Communication.Notifications.Helpers;
using JC.Communication.Notifications.Models;
using JC.Core.Extensions;
using JC.Web.UI.HTML;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JC.Communication.Web.TagHelpers;

/// <summary>
/// Renders a Bootstrap toast container for notification pop-ups. Accepts a list of notifications
/// to display as stacked toasts with type-based icons and colours (custom style takes precedence).
/// Ideal for displaying real-time notifications pushed via SignalR or similar.
/// </summary>
[HtmlTargetElement("notification-toast", TagStructure = TagStructure.WithoutEndTag)]
public class NotificationToastTagHelper : TagHelper
{
    /// <summary>Gets or sets the notifications to render as toasts. Required.</summary>
    [HtmlAttributeName("model")]
    public List<Notification> Model { get; set; } = null!;

    /// <summary>Gets or sets the toast container position. Defaults to "top-0 end-0" (top-right).</summary>
    [HtmlAttributeName("position")]
    public string Position { get; set; } = "top-0 end-0";

    /// <summary>Gets or sets whether toasts auto-hide. Defaults to true.</summary>
    [HtmlAttributeName("auto-hide")]
    public bool AutoHide { get; set; } = true;

    /// <summary>Gets or sets the auto-hide delay in milliseconds. Defaults to 5000.</summary>
    [HtmlAttributeName("delay")]
    public int Delay { get; set; } = 5000;

    /// <summary>Gets or sets the maximum body text length before truncation. Defaults to 120.</summary>
    [HtmlAttributeName("body-max-length")]
    public int BodyMaxLength { get; set; } = 120;

    /// <summary>Gets or sets the container ID. Defaults to "notification-toasts".</summary>
    [HtmlAttributeName("container-id")]
    public string ContainerId { get; set; } = "notification-toasts";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(BuildHtml());
    }

    private string BuildHtml()
    {
        var toasts = "";
        if (Model != null)
            toasts = string.Concat(Model.Select(BuildToast));

        var container = HtmlHelper.CreateElement("div", toasts,
            attributes: new Dictionary<string, string>
            {
                ["id"] = ContainerId,
                ["aria-live"] = "polite",
                ["aria-atomic"] = "true"
            },
            classes: $"toast-container position-fixed {WebUtility.HtmlEncode(Position)} p-3");

        // Auto-show script for all toasts in container
        var script = HtmlHelper.CreateElement("script",
            $"(function(){{document.querySelectorAll('#{WebUtility.HtmlEncode(ContainerId)} .toast')" +
            ".forEach(function(t){new bootstrap.Toast(t).show();});})()");

        return container + script;
    }

    private string BuildToast(Notification notification)
    {
        var iconClass = notification.Style?.CustomIconClass
                        ?? NotificationUIHelper.GetIconClass(notification.Type);
        var colourClass = notification.Style?.CustomColourClass
                          ?? NotificationUIHelper.GetColourClass(notification.Type);

        var title = WebUtility.HtmlEncode(notification.Title);
        var time = notification.CreatedUtc.ToRelativeTime();

        // Toast header
        var headerIcon = HtmlHelper.CreateElement("i", "",
            classes: $"bi {WebUtility.HtmlEncode(iconClass)} text-{WebUtility.HtmlEncode(colourClass)} me-2");
        var headerTitle = HtmlHelper.CreateElement("strong", title, classes: "me-auto");
        var headerTime = HtmlHelper.CreateElement("small", WebUtility.HtmlEncode(time), classes: "text-muted");
        var closeBtn = HtmlHelper.CreateElement("button", "",
            attributes: new Dictionary<string, string>
            {
                ["type"] = "button",
                ["aria-label"] = "Close",
                ["data-bs-dismiss"] = "toast"
            },
            classes: "btn-close");

        var header = HtmlHelper.CreateElement("div",
            headerIcon + headerTitle + headerTime + closeBtn,
            classes: "toast-header");

        // Toast body
        var bodyText = notification.BodyHtml ?? WebUtility.HtmlEncode(notification.Body.Truncate(BodyMaxLength));
        var body = HtmlHelper.CreateElement("div", bodyText, classes: "toast-body");

        // Wrap in link if UrlLink present
        var toastContent = header + body;
        if (!string.IsNullOrWhiteSpace(notification.UrlLink))
        {
            toastContent = HtmlHelper.CreateElement("a", toastContent,
                attributes: new Dictionary<string, string>
                {
                    ["href"] = notification.UrlLink,
                    ["style"] = "text-decoration:none;color:inherit;"
                });
        }

        var dataAttrs = new Dictionary<string, string>
        {
            ["role"] = "alert",
            ["aria-live"] = "assertive",
            ["aria-atomic"] = "true",
            ["data-bs-autohide"] = AutoHide.ToString().ToLowerInvariant(),
            ["data-bs-delay"] = Delay.ToString()
        };

        return HtmlHelper.CreateElement("div", toastContent,
            attributes: dataAttrs,
            classes: "toast");
    }
}
