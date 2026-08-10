using System.Net;
using JC.Communication.Messaging.Models;
using JC.Communication.Web.Framework;
using JC.Communication.Web.Framework.Icons;
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
/// <param name="html">The HTML element builder, resolved from the container.</param>
/// <param name="dictionary">The class dictionary for the configured framework.</param>
/// <param name="icons">The icon dictionary for the configured icon set.</param>
[HtmlTargetElement("message-thread", TagStructure = TagStructure.WithoutEndTag)]
public class MessageThreadTagHelper(HtmlHelper html,
    ICommunicationFrameworkDictionary dictionary,
    ICommunicationIconDictionary icons)
    : TagHelper
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

    /// <summary>
    /// Gets or sets the background colour for sent messages. Falls back to the configured
    /// framework's default when unset, which is "primary" under Bootstrap.
    /// </summary>
    [HtmlAttributeName("sent-colour")]
    public string? SentColour { get; set; }

    /// <summary>
    /// Gets or sets the background colour for received messages. Falls back to the configured
    /// framework's default when unset, which is "light" under Bootstrap.
    /// </summary>
    [HtmlAttributeName("received-colour")]
    public string? ReceivedColour { get; set; }

    /// <summary>
    /// Gets or sets the text colour for sent messages. Falls back to the configured framework's
    /// default when unset, which is "white" under Bootstrap.
    /// </summary>
    [HtmlAttributeName("sent-text-colour")]
    public string? SentTextColour { get; set; }

    /// <summary>
    /// Gets or sets the text colour for received messages. Falls back to the configured framework's
    /// default when unset, which is "dark" under Bootstrap.
    /// </summary>
    [HtmlAttributeName("received-text-colour")]
    public string? ReceivedTextColour { get; set; }

    /// <summary>
    /// Gets or sets a function that resolves a user ID to a display name.
    /// If null, the raw user ID is displayed.
    /// </summary>
    [HtmlAttributeName("user-resolver")]
    public Func<string, string>? UserResolver { get; set; }

    /// <summary>
    /// Gets or sets the container CSS class. Falls back to the configured framework's default when
    /// unset, which is "d-flex flex-column gap-2 p-3" under Bootstrap.
    /// </summary>
    [HtmlAttributeName("container-class")]
    public string? ContainerClass { get; set; }

    /// <summary>Gets or sets the maximum height of the message container in pixels. Defaults to 500. Set to 0 for no limit.</summary>
    [HtmlAttributeName("max-height")]
    public int MaxHeight { get; set; } = 500;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Model == null!)
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

        var container = html.CreateElement("div", messageItems,
            attributes: new Dictionary<string, string>
            {
                ["id"] = containerId,
                ["style"] = scrollStyle
            },
            classes: string.IsNullOrWhiteSpace(ContainerClass)
                ? dictionary.MessageThread.Container
                : ContainerClass);

        // Auto-scroll script
        var script = MaxHeight > 0
            ? html.CreateElement("script",
                $"(function(){{var e=document.getElementById('{containerId}');if(e)e.scrollTop=e.scrollHeight;}})()")
            : "";

        return header + container + script;
    }

    private string BuildThreadHeader()
    {
        var css = dictionary.MessageThread;
        var metadata = Model.ChatMetadata;
        var iconHtml = "";

        if (metadata != null)
        {
            if (!string.IsNullOrWhiteSpace(metadata.ImgPath))
                iconHtml = html.CreateElement("img", string.Empty,
                    attributes: new Dictionary<string, string>
                    {
                        ["src"] = metadata.ImgPath,
                        ["alt"] = "",
                        ["style"] = "width:32px;height:32px;object-fit:cover;"
                    },
                    classes: css.HeaderAvatar);
            else if (!string.IsNullOrWhiteSpace(metadata.Icon))
                iconHtml = html.CreateElement("i", string.Empty,
                    attributes: new Dictionary<string, string>
                    {
                        ["style"] = "font-size:1.25rem;"
                    },
                    classes: WebUtility.HtmlEncode(metadata.Icon));
        }

        var nameAttrs = metadata?.Colour != null
            ? new Dictionary<string, string> { ["style"] = $"color:{WebUtility.HtmlEncode(metadata.Colour)};" }
            : null;
        var nameHtml = html.CreateElement("span", WebUtility.HtmlEncode(Model.ChatName),
            attributes: nameAttrs,
            classes: css.HeaderName);

        var membersHtml = Model.IsGroupChat
            ? html.CreateElement("span", $"{Model.Participants.Count} members", classes: css.MemberBadge)
            : "";

        return html.CreateElement("div", iconHtml + nameHtml + membersHtml, classes: css.Header);
    }

    private string BuildMessage(MessageModel message, bool isSent, Dictionary<string, MessageModel> messageMap)
    {
        var css = dictionary.MessageThread;

        var bgColour = isSent
            ? Fallback(SentColour, css.DefaultSentColour)
            : Fallback(ReceivedColour, css.DefaultReceivedColour);
        var textColour = isSent
            ? Fallback(SentTextColour, css.DefaultSentTextColour)
            : Fallback(ReceivedTextColour, css.DefaultReceivedTextColour);

        var senderName = WebUtility.HtmlEncode(ResolveName(message.SenderUserId));
        var time = message.SentAtUtc.ToRelativeTime();

        // Reply-to preview
        var replyHtml = "";
        if (!string.IsNullOrWhiteSpace(message.ReplyToMessageId)
            && messageMap.TryGetValue(message.ReplyToMessageId, out var replyTo))
        {
            var replyName = WebUtility.HtmlEncode(ResolveName(replyTo.SenderUserId));
            var replyBody = WebUtility.HtmlEncode(replyTo.Message.Truncate(ReplyTruncateLength));

            replyHtml = html.CreateElement("div",
                html.CreateElement("i", string.Empty, classes: icons.Icons.Reply) + " " +
                html.CreateElement("span", replyName, classes: css.ReplyName) + " " +
                replyBody,
                classes: css.ReplyPreview);
        }

        // Sender name (only for received messages in group chats)
        var senderHtml = !isSent && Model.IsGroupChat
            ? html.CreateElement("div", senderName, classes: css.SenderName)
            : "";

        var bubble = html.CreateElement("div",
            senderHtml +
            html.CreateElement("div", WebUtility.HtmlEncode(message.Message)) +
            html.CreateElement("div", WebUtility.HtmlEncode(time), classes: css.MessageTime),
            classes: css.Bubble(WebUtility.HtmlEncode(bgColour), WebUtility.HtmlEncode(textColour)));

        return html.CreateElement("div", replyHtml + bubble,
            attributes: new Dictionary<string, string> { ["style"] = "max-width:75%;" },
            classes: isSent ? css.SentAlign : css.ReceivedAlign);
    }

    private static string Fallback(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private string ResolveName(string userId)
        => UserResolver?.Invoke(userId) ?? userId;
}
