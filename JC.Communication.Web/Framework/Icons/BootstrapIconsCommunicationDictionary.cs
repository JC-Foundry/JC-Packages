using JC.Communication.Notifications.Models;
using JC.Web.UI.Framework;

namespace JC.Communication.Web.Framework.Icons;

/// <summary>
/// Bootstrap Icons classes for JC.Communication.Web's tag helpers. Selected when
/// <see cref="IconFramework.Bootstrap"/> is configured, which is the default.
/// </summary>
/// <remarks>
/// Every value here is the markup these tag helpers emitted before icon classes were made
/// configurable, so this dictionary reproduces the previous output exactly. The glyph values carry
/// the <c>bi</c> base class because a value is the whole class attribute — the notification-type
/// icons were previously supplied without it by <c>NotificationUIHelper</c>, with the base class
/// added by whichever tag helper rendered them.
/// </remarks>
public sealed class BootstrapIconsCommunicationDictionary : ICommunicationIconDictionary
{
    /// <inheritdoc />
    public CommunicationIcons Icons { get; } = new()
    {
        BaseClass = "bi",
        Bell = "bi bi-bell",
        Reply = "bi bi-reply",
        Send = "bi bi-send",
        Close = "bi bi-x",
        Person = "bi bi-person",
        People = "bi bi-people",
        NotificationTypes = new Dictionary<NotificationType, string>
        {
            [NotificationType.Message] = "bi bi-chat-left-text",
            [NotificationType.Info] = "bi bi-info-circle",
            [NotificationType.Success] = "bi bi-check-circle",
            [NotificationType.Warning] = "bi bi-exclamation-triangle",
            [NotificationType.Error] = "bi bi-x-circle",
            [NotificationType.System] = "bi bi-cpu",
            [NotificationType.Task] = "bi bi-list-check"
        },

        // NotificationUIHelper returned "" for an unrecognised type, so the icon element rendered
        // with the base class alone. Kept as empty rather than "bi" — an icon-less element has no
        // reason to carry an icon library's base class.
        NotificationFallback = ""
    };
}
