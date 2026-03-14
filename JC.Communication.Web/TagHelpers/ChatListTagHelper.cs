using System.Net;
using JC.Communication.Logging.Models.Messaging;
using JC.Communication.Messaging.Models;
using JC.Core.Extensions;
using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using JC.Web.UI.HTML;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;

namespace JC.Communication.Web.TagHelpers;

/// <summary>
/// Renders a list of chat thread previews, showing the thread name, last message preview,
/// last activity time, metadata (icon/image/colour), and optional unread message count.
/// When <see cref="ShowUnread"/> is <c>true</c>, the tag helper queries the current user's
/// latest <see cref="MessageReadLog"/> per thread and counts messages received after that point.
/// </summary>
[HtmlTargetElement("chat-list", TagStructure = TagStructure.WithoutEndTag)]
public class ChatListTagHelper : TagHelper
{
    private readonly IRepositoryManager _repos;
    private readonly IUserInfo _userInfo;

    /// <summary>Gets or sets the list of chat models to render. Required.</summary>
    [HtmlAttributeName("model")]
    public List<ChatModel> Model { get; set; } = null!;

    /// <summary>Gets or sets the URL format for thread links. Use {0} as a placeholder for the thread ID. Defaults to "/chat/{0}".</summary>
    [HtmlAttributeName("href-format")]
    public string HrefFormat { get; set; } = "/chat/{0}";

    /// <summary>Gets or sets the maximum length of the message preview before truncation. Defaults to 50.</summary>
    [HtmlAttributeName("preview-max-length")]
    public int PreviewMaxLength { get; set; } = 50;

    /// <summary>Gets or sets the text shown when no chats exist. Defaults to "No conversations".</summary>
    [HtmlAttributeName("empty-text")]
    public string EmptyText { get; set; } = "No conversations";

    /// <summary>Gets or sets the container CSS class. Defaults to "list-group".</summary>
    [HtmlAttributeName("container-class")]
    public string ContainerClass { get; set; } = "list-group";

    /// <summary>
    /// Gets or sets a function that resolves a user ID to a display name.
    /// If null, the raw user ID is displayed.
    /// </summary>
    [HtmlAttributeName("user-resolver")]
    public Func<string, string>? UserResolver { get; set; }

    /// <summary>Gets or sets whether to show unread message count badges. Defaults to true.</summary>
    [HtmlAttributeName("show-unread")]
    public bool ShowUnread { get; set; } = true;

    /// <summary>Gets or sets the Bootstrap badge colour for unread counts. Defaults to "primary".</summary>
    [HtmlAttributeName("unread-badge-colour")]
    public string UnreadBadgeColour { get; set; } = "primary";

    public ChatListTagHelper(IRepositoryManager repos, IUserInfo userInfo)
    {
        _repos = repos;
        _userInfo = userInfo;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (Model == null || Model.Count == 0)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("class", "text-center text-muted py-3");
            output.Content.SetHtmlContent(WebUtility.HtmlEncode(EmptyText));
            return;
        }

        var unreadCounts = ShowUnread
            ? await GetUnreadCountsAsync()
            : new Dictionary<string, int>();

        output.TagName = null;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(BuildHtml(unreadCounts));
    }

    private async Task<Dictionary<string, int>> GetUnreadCountsAsync()
    {
        var userId = _userInfo.UserId;

        // Collect all message IDs across all threads to query read logs
        var allMessageIds = Model
            .SelectMany(c => c.Messages.Select(m => m.MessageId))
            .ToHashSet();

        // Get all read logs for this user for messages in these threads
        var readLogs = await _repos.GetRepository<MessageReadLog>()
            .AsQueryable()
            .Where(r => r.UserId == userId && allMessageIds.Contains(r.MessageId))
            .ToListAsync();

        var readMessageIds = readLogs.Select(r => r.MessageId).ToHashSet();

        var result = new Dictionary<string, int>();
        foreach (var chat in Model)
        {
            // Find the latest message in this thread that the user has a read log for
            var lastReadMessage = chat.Messages
                .Where(m => readMessageIds.Contains(m.MessageId))
                .MaxBy(m => m.SentAtUtc);

            if (lastReadMessage == null)
            {
                // No read logs — all messages are unread
                result[chat.ThreadId] = chat.Messages.Count;
            }
            else
            {
                // Count messages that arrived after the last-read message
                result[chat.ThreadId] = chat.Messages
                    .Count(m => m.SentAtUtc > lastReadMessage.SentAtUtc);
            }
        }

        return result;
    }

    private string BuildHtml(Dictionary<string, int> unreadCounts)
    {
        var items = string.Concat(Model.Select(c => BuildChatItem(c, unreadCounts)));
        return HtmlHelper.CreateElement("div", items, classes: ContainerClass);
    }

    private string BuildChatItem(ChatModel chat, Dictionary<string, int> unreadCounts)
    {
        var href = string.Format(HrefFormat, WebUtility.UrlEncode(chat.ThreadId));
        var metadata = chat.ChatMetadata;
        var unread = unreadCounts.GetValueOrDefault(chat.ThreadId);

        // Avatar area
        string avatarContent;
        if (metadata?.ImgPath != null)
        {
            avatarContent = HtmlHelper.CreateElement("img", "",
                attributes: new Dictionary<string, string>
                {
                    ["src"] = metadata.ImgPath,
                    ["alt"] = "",
                    ["style"] = "width:40px;height:40px;object-fit:cover;"
                },
                classes: "rounded-circle");
        }
        else if (metadata?.Icon != null)
        {
            var bgStyle = metadata.Colour != null
                ? $"width:40px;height:40px;background-color:{WebUtility.HtmlEncode(metadata.Colour)};"
                : "width:40px;height:40px;background-color:var(--bs-secondary-bg);";

            avatarContent = HtmlHelper.CreateElement("div",
                HtmlHelper.CreateElement("i", "", classes: WebUtility.HtmlEncode(metadata.Icon)),
                attributes: new Dictionary<string, string> { ["style"] = bgStyle },
                classes: "rounded-circle d-flex align-items-center justify-content-center");
        }
        else
        {
            var icon = chat.IsGroupChat ? "bi-people" : "bi-person";
            avatarContent = HtmlHelper.CreateElement("div",
                HtmlHelper.CreateElement("i", "", classes: $"bi {icon}"),
                attributes: new Dictionary<string, string> { ["style"] = "width:40px;height:40px;" },
                classes: "rounded-circle d-flex align-items-center justify-content-center bg-secondary-subtle");
        }

        var avatar = HtmlHelper.CreateElement("div", avatarContent,
            attributes: new Dictionary<string, string> { ["style"] = "width:40px;height:40px;" },
            classes: "flex-shrink-0");

        // Name + time row
        var nameAttrs = metadata?.Colour != null
            ? new Dictionary<string, string> { ["style"] = $"color:{WebUtility.HtmlEncode(metadata.Colour)};" }
            : null;
        var nameHtml = HtmlHelper.CreateElement("span", WebUtility.HtmlEncode(chat.ChatName),
            attributes: nameAttrs,
            classes: "fw-semibold text-truncate");
        var timeHtml = HtmlHelper.CreateElement("small", WebUtility.HtmlEncode(chat.LastActivity),
            classes: "text-muted flex-shrink-0 ms-2");
        var nameRow = HtmlHelper.CreateElement("div", nameHtml + timeHtml,
            classes: "d-flex justify-content-between");

        // Last message preview
        var previewHtml = "";
        var lastMessage = chat.Messages.MaxBy(m => m.SentAtUtc);
        if (lastMessage != null)
        {
            var senderName = ResolveName(lastMessage.SenderUserId);
            var preview = lastMessage.Message.Truncate(PreviewMaxLength);
            previewHtml = HtmlHelper.CreateElement("div",
                HtmlHelper.CreateElement("span", WebUtility.HtmlEncode(senderName) + ":", classes: "fw-medium") + " " +
                WebUtility.HtmlEncode(preview),
                classes: "small text-muted text-truncate");
        }

        var content = HtmlHelper.CreateElement("div", nameRow + previewHtml,
            classes: "flex-grow-1 overflow-hidden");

        // Unread badge
        var badgeHtml = "";
        if (ShowUnread && unread > 0)
        {
            badgeHtml = HtmlHelper.CreateElement("span",
                unread > 99 ? "99+" : unread.ToString(),
                classes: $"badge rounded-pill bg-{WebUtility.HtmlEncode(UnreadBadgeColour)} align-self-center flex-shrink-0");
        }

        return HtmlHelper.CreateElement("a", avatar + content + badgeHtml,
            attributes: new Dictionary<string, string> { ["href"] = href },
            classes: "list-group-item list-group-item-action d-flex align-items-center gap-3 py-2");
    }

    private string ResolveName(string userId)
        => UserResolver?.Invoke(userId) ?? userId;
}
