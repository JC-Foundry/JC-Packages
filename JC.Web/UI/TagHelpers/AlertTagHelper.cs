using JC.Web.UI.HTML;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JC.Web.UI.TagHelpers;

/// <summary>
/// Tag helper that renders an alert component using the configured UI framework's classes.
/// Usage: <c>&lt;alert type="Success" message="Saved successfully!" /&gt;</c>
/// </summary>
/// <param name="alerts">The alert renderer, resolved from the container.</param>
[HtmlTargetElement("alert", TagStructure = TagStructure.WithoutEndTag)]
public class AlertTagHelper(AlertHelper alerts) : TagHelper
{
    /// <summary>Gets or sets the alert type. Defaults to <see cref="AlertType.Info"/>.</summary>
    [HtmlAttributeName("type")]
    public AlertType Type { get; set; } = AlertType.Info;

    /// <summary>Gets or sets the alert message content.</summary>
    [HtmlAttributeName("message")]
    public string? Message { get; set; }

    /// <summary>Gets or sets whether the alert is dismissible. Defaults to <c>true</c>.</summary>
    [HtmlAttributeName("dismissible")]
    public bool Dismissible { get; set; } = true;

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Message))
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(alerts.ForType(Type, Message, Dismissible));
    }
}
