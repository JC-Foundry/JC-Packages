using System.Net;
using JC.Communication.Messaging.Models;
using JC.Communication.Web.Framework;
using JC.Communication.Web.Framework.Icons;
using JC.Core.Extensions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using HtmlHelper = JC.Web.UI.HTML.HtmlHelper;

namespace JC.Communication.Web.TagHelpers;

/// <summary>
/// Renders a chat message compose box with a textarea, send button, and optional reply-to preview bar.
/// When <see cref="ReplyTo"/> is set, a dismissible preview of the message being replied to is shown
/// above the input area. Posts to <see cref="Endpoint"/> with the thread ID and message content.
/// </summary>
/// <param name="html">The HTML element builder, resolved from the container.</param>
/// <param name="dictionary">The class dictionary for the configured framework.</param>
/// <param name="icons">The icon dictionary for the configured icon set.</param>
[HtmlTargetElement("chat-input", TagStructure = TagStructure.WithoutEndTag)]
public class ChatInputTagHelper(HtmlHelper html,
    ICommunicationFrameworkDictionary dictionary,
    ICommunicationIconDictionary icons)
    : TagHelper
{
    /// <summary>Gets or sets the POST endpoint URL for sending messages. Required.</summary>
    [HtmlAttributeName("endpoint")]
    public string Endpoint { get; set; } = null!;

    /// <summary>Gets or sets the thread ID to include in the form submission.</summary>
    [HtmlAttributeName("thread-id")]
    public string ThreadId { get; set; } = null!;

    /// <summary>Gets or sets the message being replied to. If null, no reply preview is shown.</summary>
    [HtmlAttributeName("reply-to")]
    public MessageModel? ReplyTo { get; set; }

    /// <summary>Gets or sets the maximum length of the reply-to preview before truncation. Defaults to 80.</summary>
    [HtmlAttributeName("reply-truncate-length")]
    public int ReplyTruncateLength { get; set; } = 80;

    /// <summary>Gets or sets the textarea placeholder text. Defaults to "Type a message...".</summary>
    [HtmlAttributeName("placeholder")]
    public string Placeholder { get; set; } = "Type a message...";

    /// <summary>Gets or sets the number of rows for the textarea. Defaults to 2.</summary>
    [HtmlAttributeName("rows")]
    public int Rows { get; set; } = 2;

    /// <summary>Gets or sets the maximum message length. Defaults to 4096.</summary>
    [HtmlAttributeName("max-length")]
    public int MaxLength { get; set; } = 4096;

    /// <summary>Gets or sets the send button text. Defaults to "Send".</summary>
    [HtmlAttributeName("button-text")]
    public string ButtonText { get; set; } = "Send";

    /// <summary>
    /// Gets or sets the send button colour. Falls back to the configured framework's default when
    /// unset, which is "primary" under Bootstrap.
    /// </summary>
    [HtmlAttributeName("button-colour")]
    public string? ButtonColour { get; set; }

    /// <summary>Gets or sets the model binding prefix for input names. Defaults to "Input".</summary>
    [HtmlAttributeName("prefix")]
    public string Prefix { get; set; } = "Input";

    /// <summary>Gets or sets whether to include an anti-forgery token. Defaults to true.</summary>
    [HtmlAttributeName("antiforgery")]
    public bool IncludeAntiforgery { get; set; } = true;

    /// <summary>
    /// Gets or sets a function that resolves a user ID to a display name.
    /// If null, the raw user ID is displayed in the reply-to preview.
    /// </summary>
    [HtmlAttributeName("user-resolver")]
    public Func<string, string>? UserResolver { get; set; }

    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException(
                "The 'endpoint' attribute is required on <chat-input>.");

        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(BuildHtml());
    }

    private string BuildHtml()
    {
        var css = dictionary.ChatInput;
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
                    content += html.CreateElement("input", string.Empty,
                        attributes: new Dictionary<string, string>
                        {
                            ["type"] = "hidden",
                            ["name"] = tokens.FormFieldName,
                            ["value"] = tokens.RequestToken
                        });
            }
        }

        // Hidden thread ID
        content += html.CreateElement("input", string.Empty,
            attributes: new Dictionary<string, string>
            {
                ["type"] = "hidden",
                ["name"] = $"{WebUtility.HtmlEncode(namePrefix)}ThreadId",
                ["value"] = WebUtility.HtmlEncode(ThreadId)
            });

        // Reply-to preview bar
        if (ReplyTo != null)
        {
            var replyName = WebUtility.HtmlEncode(ResolveName(ReplyTo.SenderUserId));
            var replyBody = WebUtility.HtmlEncode(ReplyTo.Message.Truncate(ReplyTruncateLength));

            // Hidden input for reply-to message ID
            content += html.CreateElement("input", string.Empty,
                attributes: new Dictionary<string, string>
                {
                    ["type"] = "hidden",
                    ["name"] = $"{WebUtility.HtmlEncode(namePrefix)}ReplyToMessageId",
                    ["value"] = WebUtility.HtmlEncode(ReplyTo.MessageId)
                });

            var replyContent =
                html.CreateElement("div",
                    html.CreateElement("i", string.Empty, classes: icons.Icons.Reply) + " " +
                    html.CreateElement("span", replyName, classes: css.ReplyName) + " " +
                    replyBody,
                    classes: css.ReplyText) +
                html.CreateElement("button", html.CreateElement("i", string.Empty, classes: icons.Icons.Close),
                    attributes: new Dictionary<string, string>
                    {
                        ["type"] = "button",
                        ["aria-label"] = "Cancel reply"
                    },
                    classes: css.ReplyClose);

            content += html.CreateElement("div", replyContent, classes: css.ReplyBar);
        }

        // Input row: textarea + send button
        var textarea = html.CreateElement("textarea", string.Empty,
            attributes: new Dictionary<string, string>
            {
                ["name"] = $"{WebUtility.HtmlEncode(namePrefix)}Message",
                ["placeholder"] = Placeholder,
                ["rows"] = Rows.ToString(),
                ["maxlength"] = MaxLength.ToString(),
                ["required"] = "required"
            },
            classes: css.TextArea);

        var buttonColour = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(ButtonColour) ? css.DefaultButtonColour : ButtonColour);

        var sendButton = html.CreateElement("button",
            html.CreateElement("i", string.Empty, classes: icons.Icons.Send) + " " +
            WebUtility.HtmlEncode(ButtonText),
            attributes: new Dictionary<string, string> { ["type"] = "submit" },
            classes: css.SendButton(buttonColour));

        content += html.CreateElement("div", textarea + sendButton, classes: css.InputRow);

        return html.CreateElement("form", content,
            attributes: new Dictionary<string, string>
            {
                ["method"] = "post",
                ["action"] = Endpoint
            },
            classes: css.Form);
    }

    private string ResolveName(string userId)
        => UserResolver?.Invoke(userId) ?? userId;
}
