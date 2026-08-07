using JC.Communication.Notifications.Models;
using JC.Web.UI.Framework;

namespace JC.Communication.Web.Framework;

/*
 * Icons follow the same rules as the class records — every property holds a whole class attribute
 * value, values are complete rather than compositional, and every property defaults to an empty
 * string so adding one does not break an existing dictionary.
 *
 * "Complete" matters more here than anywhere else. Bootstrap Icons needs a base class alongside the
 * glyph class ("bi bi-bell"); Font Awesome needs a style class instead ("fa-solid fa-bell") and has
 * no equivalent of "bi". Storing the finished value is the only representation that serves both, so
 * the base class never appears in a tag helper or in a class dictionary format.
 *
 * Unlike the class records, this is one flat group rather than one group per component. Icons are
 * shared — the bell appears in both the badge and the dropdown, the reply arrow in both the chat
 * input and the message thread — so grouping per component would mean defining the same glyph twice
 * and leaving the two free to drift.
 */

/// <summary>
/// The icons JC.Communication.Web's tag helpers render.
/// </summary>
public sealed record CommunicationIcons
{
    /// <summary>
    /// The base class this icon set requires alongside each glyph class, empty when it needs none.
    /// </summary>
    /// <remarks>
    /// Only used to normalise caller-supplied values through <see cref="Custom"/> — the properties
    /// below already carry it, since every value here is complete. Bootstrap Icons needs
    /// <c>"bi"</c>; Font Awesome carries its style in the glyph class itself and leaves this empty.
    /// </remarks>
    public string BaseClass { get; init; } = "";

    /// <summary>The notification bell.</summary>
    public string Bell { get; init; } = "";

    /// <summary>The reply arrow, shown against a quoted message.</summary>
    public string Reply { get; init; } = "";

    /// <summary>The send arrow on the compose button.</summary>
    public string Send { get; init; } = "";

    /// <summary>The cross that cancels a reply.</summary>
    public string Close { get; init; } = "";

    /// <summary>The avatar stand-in for a one-to-one thread.</summary>
    public string Person { get; init; } = "";

    /// <summary>The avatar stand-in for a group thread.</summary>
    public string People { get; init; } = "";

    /// <summary>
    /// The icon for each notification type. Types absent from the map fall back to
    /// <see cref="NotificationFallback"/>.
    /// </summary>
    public IReadOnlyDictionary<NotificationType, string> NotificationTypes { get; init; }
        = new Dictionary<NotificationType, string>();

    /// <summary>The icon used for a notification type absent from <see cref="NotificationTypes"/>.</summary>
    public string NotificationFallback { get; init; } = "";

    /// <summary>
    /// Returns the icon for the given notification type, or <see cref="NotificationFallback"/> when
    /// the dictionary does not define one.
    /// </summary>
    /// <param name="type">The notification type.</param>
    public string Notification(NotificationType type)
        => NotificationTypes.GetValueOrDefault(type, NotificationFallback);

    /// <summary>
    /// Normalises a caller-supplied icon class against <see cref="BaseClass"/>.
    /// </summary>
    /// <param name="icon">The icon class from a tag helper attribute or a stored record.</param>
    /// <returns>The finished class attribute value, or an empty string when there is no icon.</returns>
    /// <remarks>
    /// Values reaching a tag helper attribute or <c>NotificationStyle.CustomIconClass</c> predate
    /// this dictionary, so many are bare glyph classes like <c>"bi-star"</c>. Normalising means both
    /// forms render correctly under Bootstrap Icons without a data migration, while a set with no
    /// base class is left alone.
    /// </remarks>
    public string Custom(string? icon) => IconClass.WithBase(icon, BaseClass);
}
