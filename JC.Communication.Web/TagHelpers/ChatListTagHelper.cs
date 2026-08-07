using System.Net;
using JC.Communication.Logging.Models.Messaging;
using JC.Communication.Messaging.Models;
using JC.Communication.Web.Framework;
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
    private readonly HtmlHelper _html;
    private readonly ICommunicationFrameworkDictionary _dictionary;
    private readonly ICommunicationIconDictionary _icons;

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

    /// <summary>
    /// Gets or sets the container CSS class. Falls back to the configured framework's default when
    /// unset, which is "list-group" under Bootstrap.
    /// </summary>
    [HtmlAttributeName("container-class")]
    public string? ContainerClass { get; set; }

    /// <summary>
    /// Gets or sets a function that resolves a user ID to a display name.
    /// If null, the raw user ID is displayed.
    /// </summary>
    [HtmlAttributeName("user-resolver")]
    public Func<string, string>? UserResolver { get; set; }

    /// <summary>Gets or sets whether to show unread message count badges. Defaults to true.</summary>
    [HtmlAttributeName("show-unread")]
    public bool ShowUnread { get; set; } = true;

    /// <summary>
    /// Gets or sets the badge colour for unread counts. Falls back to the configured framework's
    /// default when unset, which is "primary" under Bootstrap.
    /// </summary>
    [HtmlAttributeName("unread-badge-colour")]
    public string? UnreadBadgeColour { get; set; }

    public ChatListTagHelper(IRepositoryManager repos,
        IUserInfo userInfo,
        HtmlHelper html,
        ICommunicationFrameworkDictionary dictionary,
        ICommunicationIconDictionary icons)
    {
        _repos = repos;
        _userInfo = userInfo;
        _html = html;
        _dictionary = dictionary;
        _icons = icons;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (Model == null || Model.Count == 0)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("class", _dictionary.ChatList.Empty);
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

        return _html.CreateElement("div", items,
            classes: string.IsNullOrWhiteSpace(ContainerClass)
                ? _dictionary.ChatList.Container
                : ContainerClass);
    }

    private string BuildChatItem(ChatModel chat, Dictionary<string, int> unreadCounts)
    {
        var css = _dictionary.ChatList;

        var href = string.Format(HrefFormat, WebUtility.UrlEncode(chat.ThreadId));
        var metadata = chat.ChatMetadata;
        var unread = unreadCounts.GetValueOrDefault(chat.ThreadId);

        // Avatar area
        string avatarContent;
        if (metadata?.ImgPath != null)
        {
            avatarContent = _html.CreateElement("img", "",
                attributes: new Dictionary<string, string>
                {
                    ["src"] = metadata.ImgPath,
                    ["alt"] = "",
                    ["style"] = "width:40px;height:40px;object-fit:cover;"
                },
                classes: css.AvatarImage);
        }
        else if (metadata?.Icon != null)
        {
            // A colour value rather than a class, since it is set through the style that sizes the
            // avatar — Bootstrap's own CSS variable is the framework's business, not this helper's.
            var background = metadata.Colour ?? css.AvatarIconBackground;
            var bgStyle = $"width:40px;height:40px;background-color:{WebUtility.HtmlEncode(background)};";

            avatarContent = _html.CreateElement("div",
                _html.CreateElement("i", "", classes: WebUtility.HtmlEncode(metadata.Icon)),
                attributes: new Dictionary<string, string> { ["style"] = bgStyle },
                classes: css.AvatarIcon);
        }
        else
        {
            var icon = chat.IsGroupChat ? _icons.Icons.People : _icons.Icons.Person;
            avatarContent = _html.CreateElement("div",
                _html.CreateElement("i", "", classes: icon),
                attributes: new Dictionary<string, string> { ["style"] = "width:40px;height:40px;" },
                classes: css.AvatarFallback);
        }

        var avatar = _html.CreateElement("div", avatarContent,
            attributes: new Dictionary<string, string> { ["style"] = "width:40px;height:40px;" },
            classes: css.Avatar);

        // Name + time row
        var nameAttrs = metadata?.Colour != null
            ? new Dictionary<string, string> { ["style"] = $"color:{WebUtility.HtmlEncode(metadata.Colour)};" }
            : null;
        var nameHtml = _html.CreateElement("span", WebUtility.HtmlEncode(chat.ChatName),
            attributes: nameAttrs,
            classes: css.Name);
        var timeHtml = _html.CreateElement("small", WebUtility.HtmlEncode(chat.LastActivity),
            classes: css.Time);
        var nameRow = _html.CreateElement("div", nameHtml + timeHtml, classes: css.NameRow);

        // Last message preview
        var previewHtml = "";
        var lastMessage = chat.Messages.MaxBy(m => m.SentAtUtc);
        if (lastMessage != null)
        {
            var senderName = ResolveName(lastMessage.SenderUserId);
            var preview = lastMessage.Message.Truncate(PreviewMaxLength);
            previewHtml = _html.CreateElement("div",
                _html.CreateElement("span", WebUtility.HtmlEncode(senderName) + ":", classes: css.PreviewSender) + " " +
                WebUtility.HtmlEncode(preview),
                classes: css.Preview);
        }

        var content = _html.CreateElement("div", nameRow + previewHtml, classes: css.Content);

        // Unread badge
        var badgeHtml = "";
        if (ShowUnread && unread > 0)
        {
            var badgeColour = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(UnreadBadgeColour) ? css.DefaultUnreadBadgeColour : UnreadBadgeColour);

            badgeHtml = _html.CreateElement("span",
                unread > 99 ? "99+" : unread.ToString(),
                classes: css.UnreadBadge(badgeColour));
        }

        return _html.CreateElement("a", avatar + content + badgeHtml,
            attributes: new Dictionary<string, string> { ["href"] = href },
            classes: css.Item);
    }

    private string ResolveName(string userId)
        => UserResolver?.Invoke(userId) ?? userId;
}
