using JC.Communication.Notifications.Models;
using JC.Web.UI.Framework;

namespace JC.Communication.Web.Framework.Icons;

/// <summary>
/// Font Awesome classes for JC.Communication.Web's tag helpers. Selected when
/// <see cref="IconFramework.FontAwesome"/> is configured.
/// </summary>
/// <remarks>
/// Values are Font Awesome 6 free names, in the solid style. Version 5 renamed several of these —
/// <c>fa-xmark</c> was <c>fa-times</c>, <c>fa-circle-info</c> was <c>fa-info-circle</c>,
/// <c>fa-triangle-exclamation</c> was <c>fa-exclamation-triangle</c> and <c>fa-circle-xmark</c> was
/// <c>fa-times-circle</c> — so an application still on 5 needs its own dictionary rather than this one.
/// <para>
/// <see cref="CommunicationIcons.BaseClass"/> is empty because Font Awesome has no equivalent of
/// Bootstrap Icons' <c>bi</c>: the style is carried in the glyph class itself. That has one
/// consequence worth knowing — <see cref="CommunicationIcons.Custom"/> normalises against the base
/// class, so with none to add it returns caller-supplied values untouched. A stored
/// <c>NotificationStyle.CustomIconClass</c> written for Bootstrap Icons therefore renders as-is and
/// shows nothing; those values need migrating when the icon set changes.
/// </para>
/// </remarks>
public sealed class FontAwesomeCommunicationDictionary : ICommunicationIconDictionary
{
    /// <inheritdoc />
    public CommunicationIcons Icons { get; } = new()
    {
        // Font Awesome carries its style in the glyph class, so there is no base class to add.
        BaseClass = "",
        Bell = "fa-solid fa-bell",
        Reply = "fa-solid fa-reply",
        Send = "fa-solid fa-paper-plane",
        Close = "fa-solid fa-xmark",
        Person = "fa-solid fa-user",
        People = "fa-solid fa-users",
        NotificationTypes = new Dictionary<NotificationType, string>
        {
            [NotificationType.Message] = "fa-solid fa-comment-dots",
            [NotificationType.Info] = "fa-solid fa-circle-info",
            [NotificationType.Success] = "fa-solid fa-circle-check",
            [NotificationType.Warning] = "fa-solid fa-triangle-exclamation",
            [NotificationType.Error] = "fa-solid fa-circle-xmark",
            [NotificationType.System] = "fa-solid fa-microchip",
            [NotificationType.Task] = "fa-solid fa-list-check"
        },

        // Empty for the same reason as the Bootstrap Icons dictionary: an icon element for an
        // unrecognised type has no glyph, so it carries no icon classes at all.
        NotificationFallback = ""
    };
}
