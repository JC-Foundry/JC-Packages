using JC.Web.UI.Framework;

namespace JC.Communication.Web.Framework;

/// <summary>
/// The CSS class dictionary for JC.Communication.Web's tag helpers. One implementation exists per
/// supported <see cref="UIFramework"/>, and the configured framework decides which is resolved from
/// the container.
/// </summary>
/// <remarks>
/// This contract belongs to JC.Communication.Web, not JC.Web. Adding a tag helper here therefore
/// needs no change to JC.Web and no JC.Web release — the reason
/// <see cref="IFrameworkDictionary"/> is a marker rather than a single suite-wide interface.
/// <para>
/// Register with <c>AddFrameworkDictionary</c>, which selects the implementation from the same
/// <see cref="UIFrameworkService.Framework"/> that drives every other package's dictionary, so they
/// cannot disagree about which framework is in play.
/// </para>
/// </remarks>
public interface ICommunicationFrameworkDictionary : IFrameworkDictionary
{
    /// <summary>Classes for the notification bell and dropdown.</summary>
    NotificationDropdownClasses NotificationDropdown { get; }

    /// <summary>Classes for the standalone unread count badge.</summary>
    NotificationBadgeClasses NotificationBadge { get; }

    /// <summary>Classes for notification toasts.</summary>
    NotificationToastClasses NotificationToast { get; }

    /// <summary>Classes for the chat thread view.</summary>
    MessageThreadClasses MessageThread { get; }

    /// <summary>Classes for the list of chat threads.</summary>
    ChatListClasses ChatList { get; }

    /// <summary>Classes for the message compose box.</summary>
    ChatInputClasses ChatInput { get; }

    /// <summary>Classes for the contact form.</summary>
    ContactFormClasses ContactForm { get; }

    /// <summary>Classes for the chat participant list.</summary>
    ChatParticipantsClasses ChatParticipants { get; }

    /// <summary>Classes derived from a notification's type, shared by the dropdown and the toast.</summary>
    NotificationTypeClasses NotificationTypes { get; }
}
