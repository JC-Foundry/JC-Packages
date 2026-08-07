using System.Net;
using JC.Communication.Notifications.Models;
using JC.Communication.Web.Framework;
using JC.Core.Extensions;
using JC.Web.UI.Framework;
using JC.Web.UI.HTML;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JC.Communication.Web.TagHelpers;

/// <summary>
/// Renders a toast container for notification pop-ups. Accepts a list of notifications to display as
/// stacked toasts with type-based icons and colours (custom style takes precedence). Ideal for
/// displaying real-time notifications pushed via SignalR or similar.
/// </summary>
/// <remarks>
/// Under Bootstrap the markup carries Bootstrap's own behavioural attributes
/// (<c>data-bs-dismiss</c>, <c>data-bs-autohide</c>, <c>data-bs-delay</c>) and an auto-show script
/// calling <c>new bootstrap.Toast(...)</c>, so no JavaScript is needed from the consuming
/// application.
/// <para>
/// Under any other framework the script is omitted — it depends on the <c>bootstrap</c> global,
/// which would otherwise throw a <c>ReferenceError</c>. The markup and its attributes are still
/// emitted, so the application supplies its own JavaScript to show and dismiss the toasts, reading
/// those attributes or its own selectors.
/// </para>
/// </remarks>
/// <param name="html">The HTML element builder, resolved from the container.</param>
/// <param name="dictionary">The class dictionary for the configured framework.</param>
/// <param name="icons">The icon dictionary for the configured icon set.</param>
/// <param name="frameworkService">The resolved framework, used to decide whether the Bootstrap auto-show script applies.</param>
[HtmlTargetElement("notification-toast", TagStructure = TagStructure.WithoutEndTag)]
public class NotificationToastTagHelper(HtmlHelper html,
    ICommunicationFrameworkDictionary dictionary,
    ICommunicationIconDictionary icons,
    UIFrameworkService frameworkService)
    : TagHelper
{
    /// <summary>Gets or sets the notifications to render as toasts. Required.</summary>
    [HtmlAttributeName("model")]
    public List<Notification> Model { get; set; } = null!;

    /// <summary>
    /// Gets or sets the toast container position. Falls back to the configured framework's default
    /// when unset, which is "top-0 end-0" (top-right) under Bootstrap.
    /// </summary>
    [HtmlAttributeName("position")]
    public string? Position { get; set; }

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
        var css = dictionary.NotificationToast;

        var toasts = "";
        if (Model != null)
            toasts = string.Concat(Model.Select(BuildToast));

        var position = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(Position) ? css.DefaultPosition : Position);

        var container = html.CreateElement("div", toasts,
            attributes: new Dictionary<string, string>
            {
                ["id"] = ContainerId,
                ["aria-live"] = "polite",
                ["aria-atomic"] = "true"
            },
            classes: css.Container(position));

        // Auto-show script for all toasts in the container. It constructs bootstrap.Toast and
        // selects on Bootstrap's own class, so it is emitted only when Bootstrap is the configured
        // framework — anywhere else it would throw a ReferenceError on a global that is not there.
        // The markup and its data-bs-* attributes are still emitted, leaving a non-Bootstrap
        // application to supply the equivalent behaviour itself.
        if (frameworkService.Framework != UIFramework.Bootstrap)
            return container;

        var script = html.CreateElement("script",
            $"(function(){{document.querySelectorAll('#{WebUtility.HtmlEncode(ContainerId)} .toast')" +
            ".forEach(function(t){new bootstrap.Toast(t).show();});})()");

        return container + script;
    }

    private string BuildToast(Notification notification)
    {
        var css = dictionary.NotificationToast;

        var iconClass = notification.Style?.CustomIconClass is { } custom
            ? icons.Icons.Custom(custom)
            : icons.Icons.Notification(notification.Type);
        var colourClass = notification.Style?.CustomColourClass
                          ?? dictionary.NotificationTypes.Colour(notification.Type);

        var title = WebUtility.HtmlEncode(notification.Title);
        var time = notification.CreatedUtc.ToRelativeTime();

        // Toast header
        var headerIcon = html.CreateElement("i", "",
            classes: css.HeaderIcon(WebUtility.HtmlEncode(iconClass), WebUtility.HtmlEncode(colourClass)));
        var headerTitle = html.CreateElement("strong", title, classes: css.HeaderTitle);
        var headerTime = html.CreateElement("small", WebUtility.HtmlEncode(time), classes: css.HeaderTime);
        var closeBtn = html.CreateElement("button", "",
            attributes: new Dictionary<string, string>
            {
                ["type"] = "button",
                ["aria-label"] = "Close",
                ["data-bs-dismiss"] = "toast"
            },
            classes: css.CloseButton);

        var header = html.CreateElement("div",
            headerIcon + headerTitle + headerTime + closeBtn,
            classes: css.Header);

        // Toast body
        var bodyText = notification.BodyHtml ?? WebUtility.HtmlEncode(notification.Body.Truncate(BodyMaxLength));
        var body = html.CreateElement("div", bodyText, classes: css.Body);

        // Wrap in link if UrlLink present
        var toastContent = header + body;
        if (!string.IsNullOrWhiteSpace(notification.UrlLink))
        {
            toastContent = html.CreateElement("a", toastContent,
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

        return html.CreateElement("div", toastContent,
            attributes: dataAttrs,
            classes: css.Toast);
    }
}
