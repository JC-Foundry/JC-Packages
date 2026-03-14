using System.Net;
using JC.Communication.Messaging.Models;
using JC.Web.UI.HTML;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JC.Communication.Web.TagHelpers;

/// <summary>
/// Renders a participant list for a chat thread, showing avatars or initials for each participant.
/// When the number of participants exceeds <see cref="MaxDisplay"/>, an overflow indicator is shown.
/// </summary>
[HtmlTargetElement("chat-participants", TagStructure = TagStructure.WithoutEndTag)]
public class ChatParticipantsTagHelper : TagHelper
{
    /// <summary>Gets or sets the chat model whose participants to render. Required.</summary>
    [HtmlAttributeName("model")]
    public ChatModel Model { get; set; } = null!;

    /// <summary>Gets or sets the maximum number of participant avatars to display before showing an overflow count. Defaults to 5.</summary>
    [HtmlAttributeName("max-display")]
    public int MaxDisplay { get; set; } = 5;

    /// <summary>Gets or sets the avatar size in pixels. Defaults to 32.</summary>
    [HtmlAttributeName("avatar-size")]
    public int AvatarSize { get; set; } = 32;

    /// <summary>
    /// Gets or sets a function that resolves a user ID to a display name.
    /// Used for generating initials and tooltips. If null, the raw user ID is used.
    /// </summary>
    [HtmlAttributeName("user-resolver")]
    public Func<string, string>? UserResolver { get; set; }

    /// <summary>Gets or sets the container CSS class. Defaults to "d-flex align-items-center gap-1".</summary>
    [HtmlAttributeName("container-class")]
    public string ContainerClass { get; set; } = "d-flex align-items-center gap-1";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Model?.Participants == null || Model.Participants.Count == 0)
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
        var participants = Model.Participants;
        var visible = participants.Take(MaxDisplay).ToList();
        var overflow = participants.Count - MaxDisplay;
        var sizeStyle = $"width:{AvatarSize}px;height:{AvatarSize}px;font-size:{AvatarSize / 2.5:F0}px;";

        var avatars = string.Concat(visible.Select(p => BuildAvatar(p, sizeStyle)));

        if (overflow > 0)
        {
            avatars += HtmlHelper.CreateElement("div", $"+{overflow}",
                attributes: new Dictionary<string, string>
                {
                    ["style"] = sizeStyle,
                    ["title"] = $"{overflow} more participant{(overflow == 1 ? "" : "s")}"
                },
                classes: "rounded-circle bg-secondary-subtle d-flex align-items-center justify-content-center fw-semibold text-muted");
        }

        return HtmlHelper.CreateElement("div", avatars, classes: ContainerClass);
    }

    private string BuildAvatar(ParticipantModel participant, string sizeStyle)
    {
        var name = ResolveName(participant.UserId);
        var initials = GetInitials(name);

        return HtmlHelper.CreateElement("div",
            WebUtility.HtmlEncode(initials),
            attributes: new Dictionary<string, string>
            {
                ["style"] = sizeStyle,
                ["title"] = WebUtility.HtmlEncode(name)
            },
            classes: "rounded-circle bg-primary-subtle text-primary d-flex align-items-center justify-content-center fw-semibold");
    }

    private string ResolveName(string userId)
        => UserResolver?.Invoke(userId) ?? userId;

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => $"{parts[0][..1]}{parts[^1][..1]}".ToUpperInvariant()
        };
    }
}
