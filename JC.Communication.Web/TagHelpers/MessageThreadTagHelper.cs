using System.Net;
using JC.Communication.Messaging.Models;
using JC.Core.Extensions;
using JC.Web.UI.HTML;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JC.Communication.Web.TagHelpers;

/// <summary>
/// Renders a chat thread view showing messages with sender info, timestamps, and reply-to context.
/// Messages with a <see cref="MessageModel.ReplyToMessageId"/> display a truncated preview of the
/// original message with a reply arrow. Thread metadata (icon, colour) is applied when available.
/// The container has a configurable max height and auto-scrolls to the latest message.
/// </summary>
[HtmlTargetElement("message-thread", TagStructure = TagStructure.WithoutEndTag)]
public class MessageThreadTagHelper : TagHelper
{
    /// <summary>Gets or sets the chat model to render. Required.</summary>
    [HtmlAttributeName("model")]
    public ChatModel Model { get; set; } = null!;

    /// <summary>Gets or sets the current user's ID, used to distinguish sent vs received messages.</summary>
    [HtmlAttributeName("current-user-id")]
    public string CurrentUserId { get; set; } = null!;

    /// <summary>Gets or sets the maximum length of the reply-to preview before truncation. Defaults to 60.</summary>
    [HtmlAttributeName("reply-truncate-length")]
    public int ReplyTruncateLength { get; set; } = 60;

    /// <summary>Gets or sets the Bootstrap colour class for sent messages. Defaults to "primary".</summary>
    [HtmlAttributeName("sent-colour")]
    public string SentColour { get; set; } = "primary";

    /// <summary>Gets or sets the Bootstrap colour class for received messages. Defaults to "light".</summary>
    [HtmlAttributeName("received-colour")]
    public string ReceivedColour { get; set; } = "light";

    /// <summary>Gets or sets the Bootstrap text colour class for sent messages. Defaults to "white".</summary>
    [HtmlAttributeName("sent-text-colour")]
    public string SentTextColour { get; set; } = "white";

    /// <summary>Gets or sets the Bootstrap text colour class for received messages. Defaults to "dark".</summary>
    [HtmlAttributeName("received-text-colour")]
    public string ReceivedTextColour { get; set; } = "dark";

    /// <summary>
    /// Gets or sets a function that resolves a user ID to a display name.
    /// If null, the raw user ID is displayed.
    /// </summary>
    [HtmlAttributeName("user-resolver")]
    public Func<string, string>? UserResolver { get; set; }

    /// <summary>Gets or sets the container CSS class. Defaults to "d-flex flex-column gap-2 p-3".</summary>
    [HtmlAttributeName("container-class")]
    public string ContainerClass { get; set; } = "d-flex flex-column gap-2 p-3";

    /// <summary>Gets or sets the maximum height of the message container in pixels. Defaults to 500. Set to 0 for no limit.</summary>
    [HtmlAttributeName("max-height")]
    public int MaxHeight { get; set; } = 500;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Model == null)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(BuildHtml());
    }

    private string BuildHtml()
    {
        var messages = Model.Messages.OrderBy(m => m.SentAtUtc).ToList();
        var messageMap = messages.ToDictionary(m => m.MessageId);

        var header = BuildThreadHeader();

        var messageItems = string.Concat(messages.Select(m =>
            BuildMessage(m, m.SenderUserId == CurrentUserId, messageMap)));

        var containerId = $"thread-{WebUtility.HtmlEncode(Model.ThreadId)}";
        var scrollStyle = MaxHeight > 0
            ? $"max-height:{MaxHeight}px;overflow-y:auto;"
            : "";

        var container = HtmlHelper.CreateElement("div", messageItems,
            attributes: new Dictionary<string, string>
            {
                ["id"] = containerId,
                ["style"] = scrollStyle
            },
            classes: ContainerClass);

        // Auto-scroll script
        var script = MaxHeight > 0
            ? HtmlHelper.CreateElement("script",
                $"(function(){{var e=document.getElementById('{containerId}');if(e)e.scrollTop=e.scrollHeight;}})()")
            : "";

        return header + container + script;
    }

    private string BuildThreadHeader()
    {
        var metadata = Model.ChatMetadata;
        var iconHtml = "";

        if (metadata != null)
        {
            if (!string.IsNullOrWhiteSpace(metadata.ImgPath))
                iconHtml = HtmlHelper.CreateElement("img", "",
                    attributes: new Dictionary<string, string>
                    {
                        ["src"] = metadata.ImgPath,
                        ["alt"] = "",
                        ["style"] = "width:32px;height:32px;object-fit:cover;"
                    },
                    classes: "rounded-circle");
            else if (!string.IsNullOrWhiteSpace(metadata.Icon))
                iconHtml = HtmlHelper.CreateElement("i", "",
                    attributes: new Dictionary<string, string>
                    {
                        ["style"] = "font-size:1.25rem;"
                    },
                    classes: WebUtility.HtmlEncode(metadata.Icon));
        }

        var nameAttrs = metadata?.Colour != null
            ? new Dictionary<string, string> { ["style"] = $"color:{WebUtility.HtmlEncode(metadata.Colour)};" }
            : null;
        var nameHtml = HtmlHelper.CreateElement("span", WebUtility.HtmlEncode(Model.ChatName),
            attributes: nameAttrs,
            classes: "fw-semibold");

        var membersHtml = Model.IsGroupChat
            ? HtmlHelper.CreateElement("span", $"{Model.Participants.Count} members", classes: "badge bg-secondary")
            : "";

        return HtmlHelper.CreateElement("div", iconHtml + nameHtml + membersHtml,
            classes: "d-flex align-items-center gap-2 p-3 border-bottom");
    }

    private string BuildMessage(MessageModel message, bool isSent, Dictionary<string, MessageModel> messageMap)
    {
        var bgColour = isSent ? SentColour : ReceivedColour;
        var textColour = isSent ? SentTextColour : ReceivedTextColour;
        var senderName = WebUtility.HtmlEncode(ResolveName(message.SenderUserId));
        var time = message.SentAtUtc.ToRelativeTime();

        // Reply-to preview
        var replyHtml = "";
        if (!string.IsNullOrWhiteSpace(message.ReplyToMessageId)
            && messageMap.TryGetValue(message.ReplyToMessageId, out var replyTo))
        {
            var replyName = WebUtility.HtmlEncode(ResolveName(replyTo.SenderUserId));
            var replyBody = WebUtility.HtmlEncode(replyTo.Message.Truncate(ReplyTruncateLength));

            replyHtml = HtmlHelper.CreateElement("div",
                HtmlHelper.CreateElement("i", "", classes: "bi bi-reply") + " " +
                HtmlHelper.CreateElement("span", replyName, classes: "fw-semibold") + " " +
                replyBody,
                classes: "small text-muted border-start border-2 ps-2 mb-1");
        }

        // Sender name (only for received messages in group chats)
        var senderHtml = !isSent && Model.IsGroupChat
            ? HtmlHelper.CreateElement("div", senderName, classes: "fw-semibold small")
            : "";

        var bubble = HtmlHelper.CreateElement("div",
            senderHtml +
            HtmlHelper.CreateElement("div", WebUtility.HtmlEncode(message.Message)) +
            HtmlHelper.CreateElement("div", WebUtility.HtmlEncode(time), classes: "small opacity-75 text-end"),
            classes: $"rounded-3 px-3 py-2 bg-{WebUtility.HtmlEncode(bgColour)} text-{WebUtility.HtmlEncode(textColour)}");

        return HtmlHelper.CreateElement("div", replyHtml + bubble,
            attributes: new Dictionary<string, string> { ["style"] = "max-width:75%;" },
            classes: isSent ? "align-self-end" : "align-self-start");
    }

    private string ResolveName(string userId)
        => UserResolver?.Invoke(userId) ?? userId;
}
