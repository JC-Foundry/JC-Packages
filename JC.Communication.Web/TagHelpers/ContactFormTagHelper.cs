using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using HtmlHelper = JC.Web.UI.HTML.HtmlHelper;

namespace JC.Communication.Web.TagHelpers;

/// <summary>
/// Renders a Bootstrap contact form with email, subject, and message fields.
/// Posts to the configured endpoint using the <see cref="Models.ContactInputModel"/> shape.
/// </summary>
[HtmlTargetElement("contact-form", TagStructure = TagStructure.WithoutEndTag)]
public class ContactFormTagHelper : TagHelper
{
    /// <summary>Gets or sets the POST endpoint URL. Required.</summary>
    [HtmlAttributeName("endpoint")]
    public string Endpoint { get; set; } = null!;

    /// <summary>Gets or sets the form heading. Defaults to "Contact Us".</summary>
    [HtmlAttributeName("heading")]
    public string Heading { get; set; } = "Contact Us";

    /// <summary>Gets or sets the submit button text. Defaults to "Send Message".</summary>
    [HtmlAttributeName("button-text")]
    public string ButtonText { get; set; } = "Send Message";

    /// <summary>Gets or sets the Bootstrap button colour class. Defaults to "primary".</summary>
    [HtmlAttributeName("button-colour")]
    public string ButtonColour { get; set; } = "primary";

    /// <summary>Gets or sets the model binding prefix for input names. Defaults to "Input".</summary>
    [HtmlAttributeName("prefix")]
    public string Prefix { get; set; } = "Input";

    /// <summary>Gets or sets the email field placeholder. Defaults to "Your email address".</summary>
    [HtmlAttributeName("email-placeholder")]
    public string EmailPlaceholder { get; set; } = "Your email address";

    /// <summary>Gets or sets the subject field placeholder. Defaults to "Subject".</summary>
    [HtmlAttributeName("subject-placeholder")]
    public string SubjectPlaceholder { get; set; } = "Subject";

    /// <summary>Gets or sets the message field placeholder. Defaults to "Your message".</summary>
    [HtmlAttributeName("message-placeholder")]
    public string MessagePlaceholder { get; set; } = "Your message";

    /// <summary>Gets or sets the number of rows for the message textarea. Defaults to 5.</summary>
    [HtmlAttributeName("message-rows")]
    public int MessageRows { get; set; } = 5;

    /// <summary>Gets or sets whether to include an anti-forgery token. Defaults to true.</summary>
    [HtmlAttributeName("antiforgery")]
    public bool IncludeAntiforgery { get; set; } = true;

    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException(
                "The 'endpoint' attribute is required on <contact-form>.");

        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(BuildHtml());
    }

    private string BuildHtml()
    {
        var namePrefix = string.IsNullOrEmpty(Prefix) ? "" : $"{Prefix}.";
        var content = "";

        // Anti-forgery token
        if (IncludeAntiforgery)
        {
            var antiforgery = ViewContext.HttpContext.RequestServices.GetService<IAntiforgery>();
            if (antiforgery != null)
            {
                var tokens = antiforgery.GetAndStoreTokens(ViewContext.HttpContext);
                if (tokens.RequestToken != null)
                    content += HtmlHelper.CreateElement("input", "",
                        attributes: new Dictionary<string, string>
                        {
                            ["type"] = "hidden",
                            ["name"] = tokens.FormFieldName,
                            ["value"] = tokens.RequestToken
                        });
            }
        }

        if (!string.IsNullOrWhiteSpace(Heading))
            content += HtmlHelper.CreateElement("h4", WebUtility.HtmlEncode(Heading), classes: "mb-3");

        // Email
        content += HtmlHelper.CreateElement("div",
            HtmlHelper.CreateElement("label", "Email",
                attributes: new Dictionary<string, string> { ["for"] = "contact-email" },
                classes: "form-label") +
            HtmlHelper.CreateElement("input", "",
                attributes: new Dictionary<string, string>
                {
                    ["type"] = "email",
                    ["id"] = "contact-email",
                    ["name"] = $"{WebUtility.HtmlEncode(namePrefix)}Email",
                    ["placeholder"] = EmailPlaceholder,
                    ["required"] = "required"
                },
                classes: "form-control"),
            classes: "mb-3");

        // Subject
        content += HtmlHelper.CreateElement("div",
            HtmlHelper.CreateElement("label", "Subject",
                attributes: new Dictionary<string, string> { ["for"] = "contact-subject" },
                classes: "form-label") +
            HtmlHelper.CreateElement("input", "",
                attributes: new Dictionary<string, string>
                {
                    ["type"] = "text",
                    ["id"] = "contact-subject",
                    ["name"] = $"{WebUtility.HtmlEncode(namePrefix)}Subject",
                    ["placeholder"] = SubjectPlaceholder,
                    ["required"] = "required"
                },
                classes: "form-control"),
            classes: "mb-3");

        // Message
        content += HtmlHelper.CreateElement("div",
            HtmlHelper.CreateElement("label", "Message",
                attributes: new Dictionary<string, string> { ["for"] = "contact-message" },
                classes: "form-label") +
            HtmlHelper.CreateElement("textarea", "",
                attributes: new Dictionary<string, string>
                {
                    ["id"] = "contact-message",
                    ["name"] = $"{WebUtility.HtmlEncode(namePrefix)}Message",
                    ["rows"] = MessageRows.ToString(),
                    ["placeholder"] = MessagePlaceholder,
                    ["required"] = "required"
                },
                classes: "form-control"),
            classes: "mb-3");

        // Submit
        content += HtmlHelper.CreateElement("button", WebUtility.HtmlEncode(ButtonText),
            attributes: new Dictionary<string, string> { ["type"] = "submit" },
            classes: $"btn btn-{WebUtility.HtmlEncode(ButtonColour)}");

        return HtmlHelper.CreateElement("form", content,
            attributes: new Dictionary<string, string>
            {
                ["method"] = "post",
                ["action"] = Endpoint
            });
    }
}
