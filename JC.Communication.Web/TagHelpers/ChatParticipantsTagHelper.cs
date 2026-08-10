using System.Net;
using JC.Communication.Messaging.Models;
using JC.Communication.Web.Framework;
using JC.Web.UI.HTML;
using Microsoft.AspNetCore.Razor.TagHelpers;
// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract

namespace JC.Communication.Web.TagHelpers;

/// <summary>
/// Renders a participant list for a chat thread, showing avatars or initials for each participant.
/// When the number of participants exceeds <see cref="MaxDisplay"/>, an overflow indicator is shown.
/// </summary>
/// <param name="html">The HTML element builder, resolved from the container.</param>
/// <param name="dictionary">The class dictionary for the configured framework.</param>
[HtmlTargetElement("chat-participants", TagStructure = TagStructure.WithoutEndTag)]
public class ChatParticipantsTagHelper(HtmlHelper html, ICommunicationFrameworkDictionary dictionary)
    : TagHelper
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

    /// <summary>
    /// Gets or sets the container CSS class. Falls back to the configured framework's default when
    /// unset, which is "d-flex align-items-center gap-1" under Bootstrap.
    /// </summary>
    [HtmlAttributeName("container-class")]
    public string? ContainerClass { get; set; }

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
        var css = dictionary.ChatParticipants;

        var participants = Model.Participants;
        var visible = participants.Take(MaxDisplay).ToList();
        var overflow = participants.Count - MaxDisplay;
        var sizeStyle = $"width:{AvatarSize}px;height:{AvatarSize}px;font-size:{AvatarSize / 2.5:F0}px;";

        var avatars = string.Concat(visible.Select(p => BuildAvatar(p, sizeStyle)));

        if (overflow > 0)
        {
            avatars += html.CreateElement("div", $"+{overflow}",
                attributes: new Dictionary<string, string>
                {
                    ["style"] = sizeStyle,
                    ["title"] = $"{overflow} more participant{(overflow == 1 ? "" : "s")}"
                },
                classes: css.Overflow);
        }

        return html.CreateElement("div", avatars,
            classes: string.IsNullOrWhiteSpace(ContainerClass) ? css.Container : ContainerClass);
    }

    private string BuildAvatar(ParticipantModel participant, string sizeStyle)
    {
        var name = ResolveName(participant.UserId);
        var initials = GetInitials(name);

        return html.CreateElement("div",
            WebUtility.HtmlEncode(initials),
            attributes: new Dictionary<string, string>
            {
                ["style"] = sizeStyle,
                ["title"] = WebUtility.HtmlEncode(name)
            },
            classes: dictionary.ChatParticipants.Avatar);
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
