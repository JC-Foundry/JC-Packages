using JC.Communication.Notifications.Models;
using JC.Web.UI.Framework;

namespace JC.Communication.Web.Framework;

/*
 * The same rules as JC.Web's FrameworkClasses apply here — see that file for the reasoning.
 *
 * In short: every property holds a whole class attribute value rather than a single token, values
 * are complete rather than compositional, and every property defaults to an empty string so adding
 * one does not break an existing dictionary.
 *
 * Several components take a contextual colour as a tag helper attribute. Those values are stored as
 * formats, read through an accessor method, and applied with FrameworkClass.Format. The alternative
 * — the tag helper appending the colour to a base class — would put Bootstrap's naming convention
 * back into the tag helper, which is the thing this design exists to prevent.
 *
 * Icon classes are deliberately absent. An icon set is a separate library from a CSS framework, and
 * a Tailwind application may well still use Bootstrap Icons, so icons live in
 * ICommunicationIconDictionary and are selected by their own IconFramework. The colour a glyph is
 * tinted with is a CSS-framework concern and does belong here — see NotificationTypeClasses.
 */

/// <summary>
/// Classes for the notification bell and its dropdown.
/// </summary>
public sealed record NotificationDropdownClasses
{
    /// <summary>The element wrapping the bell and menu.</summary>
    public string Container { get; init; } = "";

    /// <summary>The bell button.</summary>
    public string BellButton { get; init; } = "";

    /// <summary>The unread count badge. <c>{0}</c> is the configured colour.</summary>
    public string BadgeFormat { get; init; } = "";

    /// <summary>The badge colour when the caller specifies none.</summary>
    public string DefaultBadgeColour { get; init; } = "";

    /// <summary>Text available to screen readers but not shown.</summary>
    public string ScreenReaderOnly { get; init; } = "";

    /// <summary>The dropdown menu. <c>{0}</c> is the configured alignment.</summary>
    public string MenuFormat { get; init; } = "";

    /// <summary>The alignment used when the caller specifies none.</summary>
    public string DefaultAlign { get; init; } = "";

    /// <summary>The item shown when there are no notifications.</summary>
    public string EmptyItem { get; init; } = "";

    /// <summary>A single notification row.</summary>
    public string Item { get; init; } = "";

    /// <summary>A row's icon. <c>{0}</c> is the icon class, <c>{1}</c> the colour.</summary>
    public string ItemIconFormat { get; init; } = "";

    /// <summary>The text column of a row.</summary>
    public string ItemContent { get; init; } = "";

    /// <summary>A row's title.</summary>
    public string ItemTitle { get; init; } = "";

    /// <summary>A row's body preview.</summary>
    public string ItemBody { get; init; } = "";

    /// <summary>A row's relative timestamp.</summary>
    public string ItemTime { get; init; } = "";

    /// <summary>The unread marker on a row. <c>{0}</c> is the configured colour.</summary>
    public string UnreadDotFormat { get; init; } = "";

    /// <summary>The rule above the footer.</summary>
    public string Divider { get; init; } = "";

    /// <summary>The "view all" footer link.</summary>
    public string FooterLink { get; init; } = "";

    /// <summary>Returns the badge class for the given colour.</summary>
    /// <param name="colour">The configured contextual colour.</param>
    public string Badge(string colour) => FrameworkClass.Format(BadgeFormat, colour);

    /// <summary>Returns the menu class for the given alignment.</summary>
    /// <param name="align">The configured alignment.</param>
    public string Menu(string align) => FrameworkClass.Format(MenuFormat, align);

    /// <summary>Returns a row icon class for the given icon and colour.</summary>
    /// <param name="icon">The icon class from the notification or its type default.</param>
    /// <param name="colour">The colour from the notification or its type default.</param>
    public string ItemIcon(string icon, string colour) => FrameworkClass.Format(ItemIconFormat, icon, colour);

    /// <summary>Returns the unread marker class for the given colour.</summary>
    /// <param name="colour">The configured contextual colour.</param>
    public string UnreadDot(string colour) => FrameworkClass.Format(UnreadDotFormat, colour);
}

/// <summary>
/// Classes for the standalone unread count badge.
/// </summary>
public sealed record NotificationBadgeClasses
{
    /// <summary>The element wrapping the icon and badge.</summary>
    public string Container { get; init; } = "";

    /// <summary>The count badge. <c>{0}</c> is the configured colour.</summary>
    public string BadgeFormat { get; init; } = "";

    /// <summary>The badge colour when the caller specifies none.</summary>
    public string DefaultBadgeColour { get; init; } = "";

    /// <summary>Text available to screen readers but not shown.</summary>
    public string ScreenReaderOnly { get; init; } = "";

    /// <summary>Returns the badge class for the given colour.</summary>
    /// <param name="colour">The configured contextual colour.</param>
    public string Badge(string colour) => FrameworkClass.Format(BadgeFormat, colour);
}

/// <summary>
/// Classes for notification toasts.
/// </summary>
public sealed record NotificationToastClasses
{
    /// <summary>The fixed container holding the stack. <c>{0}</c> is the configured position.</summary>
    public string ContainerFormat { get; init; } = "";

    /// <summary>The position used when the caller specifies none.</summary>
    public string DefaultPosition { get; init; } = "";

    /// <summary>A single toast.</summary>
    public string Toast { get; init; } = "";

    /// <summary>A toast's header.</summary>
    public string Header { get; init; } = "";

    /// <summary>The header icon. <c>{0}</c> is the icon class, <c>{1}</c> the colour.</summary>
    public string HeaderIconFormat { get; init; } = "";

    /// <summary>The header title.</summary>
    public string HeaderTitle { get; init; } = "";

    /// <summary>The header's relative timestamp.</summary>
    public string HeaderTime { get; init; } = "";

    /// <summary>The dismiss button.</summary>
    public string CloseButton { get; init; } = "";

    /// <summary>A toast's body.</summary>
    public string Body { get; init; } = "";

    /// <summary>Returns the container class for the given position.</summary>
    /// <param name="position">The configured position.</param>
    public string Container(string position) => FrameworkClass.Format(ContainerFormat, position);

    /// <summary>Returns a header icon class for the given icon and colour.</summary>
    /// <param name="icon">The icon class from the notification or its type default.</param>
    /// <param name="colour">The colour from the notification or its type default.</param>
    public string HeaderIcon(string icon, string colour) => FrameworkClass.Format(HeaderIconFormat, icon, colour);
}

/// <summary>
/// Classes for the chat thread view.
/// </summary>
public sealed record MessageThreadClasses
{
    /// <summary>The scrolling container holding the messages.</summary>
    public string Container { get; init; } = "";

    /// <summary>The thread header.</summary>
    public string Header { get; init; } = "";

    /// <summary>The header's avatar image.</summary>
    public string HeaderAvatar { get; init; } = "";

    /// <summary>The thread name.</summary>
    public string HeaderName { get; init; } = "";

    /// <summary>The member count badge shown for group chats.</summary>
    public string MemberBadge { get; init; } = "";

    /// <summary>The quoted message shown above a reply.</summary>
    public string ReplyPreview { get; init; } = "";

    /// <summary>The sender name inside a reply preview.</summary>
    public string ReplyName { get; init; } = "";

    /// <summary>The sender name shown above a received group message.</summary>
    public string SenderName { get; init; } = "";

    /// <summary>A message bubble. <c>{0}</c> is the background colour, <c>{1}</c> the text colour.</summary>
    public string BubbleFormat { get; init; } = "";

    /// <summary>A message's relative timestamp.</summary>
    public string MessageTime { get; init; } = "";

    /// <summary>Positions a message sent by the current user.</summary>
    public string SentAlign { get; init; } = "";

    /// <summary>Positions a message received from someone else.</summary>
    public string ReceivedAlign { get; init; } = "";

    /// <summary>The sent bubble's background colour when the caller specifies none.</summary>
    public string DefaultSentColour { get; init; } = "";

    /// <summary>The received bubble's background colour when the caller specifies none.</summary>
    public string DefaultReceivedColour { get; init; } = "";

    /// <summary>The sent bubble's text colour when the caller specifies none.</summary>
    public string DefaultSentTextColour { get; init; } = "";

    /// <summary>The received bubble's text colour when the caller specifies none.</summary>
    public string DefaultReceivedTextColour { get; init; } = "";

    /// <summary>Returns a bubble class for the given background and text colours.</summary>
    /// <param name="background">The bubble's background colour.</param>
    /// <param name="text">The bubble's text colour.</param>
    public string Bubble(string background, string text) => FrameworkClass.Format(BubbleFormat, background, text);
}

/// <summary>
/// Classes for the list of chat threads.
/// </summary>
public sealed record ChatListClasses
{
    /// <summary>The list container.</summary>
    public string Container { get; init; } = "";

    /// <summary>The message shown when there are no threads.</summary>
    public string Empty { get; init; } = "";

    /// <summary>A single thread row.</summary>
    public string Item { get; init; } = "";

    /// <summary>The fixed-size avatar column.</summary>
    public string Avatar { get; init; } = "";

    /// <summary>An avatar backed by an image.</summary>
    public string AvatarImage { get; init; } = "";

    /// <summary>An avatar backed by the thread's own icon.</summary>
    public string AvatarIcon { get; init; } = "";

    /// <summary>An avatar for a thread with neither image nor icon.</summary>
    public string AvatarFallback { get; init; } = "";

    /// <summary>
    /// The background colour applied to an icon avatar when the thread specifies none. A CSS colour
    /// value rather than a class, since it is set through the inline style that sizes the avatar.
    /// </summary>
    public string AvatarIconBackground { get; init; } = "";

    /// <summary>The text column of a row.</summary>
    public string Content { get; init; } = "";

    /// <summary>The row holding the thread name and timestamp.</summary>
    public string NameRow { get; init; } = "";

    /// <summary>The thread name.</summary>
    public string Name { get; init; } = "";

    /// <summary>The last activity timestamp.</summary>
    public string Time { get; init; } = "";

    /// <summary>The last message preview.</summary>
    public string Preview { get; init; } = "";

    /// <summary>The sender name inside the preview.</summary>
    public string PreviewSender { get; init; } = "";

    /// <summary>The unread count badge. <c>{0}</c> is the configured colour.</summary>
    public string UnreadBadgeFormat { get; init; } = "";

    /// <summary>The unread badge colour when the caller specifies none.</summary>
    public string DefaultUnreadBadgeColour { get; init; } = "";

    /// <summary>Returns the unread badge class for the given colour.</summary>
    /// <param name="colour">The configured contextual colour.</param>
    public string UnreadBadge(string colour) => FrameworkClass.Format(UnreadBadgeFormat, colour);
}

/// <summary>
/// Classes for the message compose box.
/// </summary>
public sealed record ChatInputClasses
{
    /// <summary>The form wrapping the compose box.</summary>
    public string Form { get; init; } = "";

    /// <summary>The bar showing the message being replied to.</summary>
    public string ReplyBar { get; init; } = "";

    /// <summary>The quoted text inside the reply bar.</summary>
    public string ReplyText { get; init; } = "";

    /// <summary>The sender name inside the reply bar.</summary>
    public string ReplyName { get; init; } = "";

    /// <summary>The button that cancels a reply.</summary>
    public string ReplyClose { get; init; } = "";

    /// <summary>The row holding the textarea and send button.</summary>
    public string InputRow { get; init; } = "";

    /// <summary>The message textarea.</summary>
    public string TextArea { get; init; } = "";

    /// <summary>The send button. <c>{0}</c> is the configured colour.</summary>
    public string SendButtonFormat { get; init; } = "";

    /// <summary>The send button colour when the caller specifies none.</summary>
    public string DefaultButtonColour { get; init; } = "";

    /// <summary>Returns the send button class for the given colour.</summary>
    /// <param name="colour">The configured contextual colour.</param>
    public string SendButton(string colour) => FrameworkClass.Format(SendButtonFormat, colour);
}

/// <summary>
/// Classes for the contact form.
/// </summary>
public sealed record ContactFormClasses
{
    /// <summary>The form heading.</summary>
    public string Heading { get; init; } = "";

    /// <summary>The wrapper around a single field.</summary>
    public string Field { get; init; } = "";

    /// <summary>A field label.</summary>
    public string Label { get; init; } = "";

    /// <summary>A single-line input.</summary>
    public string Input { get; init; } = "";

    /// <summary>The message textarea.</summary>
    public string TextArea { get; init; } = "";

    /// <summary>The submit button. <c>{0}</c> is the configured colour.</summary>
    public string SubmitButtonFormat { get; init; } = "";

    /// <summary>The submit button colour when the caller specifies none.</summary>
    public string DefaultButtonColour { get; init; } = "";

    /// <summary>Returns the submit button class for the given colour.</summary>
    /// <param name="colour">The configured contextual colour.</param>
    public string SubmitButton(string colour) => FrameworkClass.Format(SubmitButtonFormat, colour);
}

/// <summary>
/// Classes derived from a notification's type, shared by the dropdown and the toast.
/// </summary>
/// <remarks>
/// Previously <c>NotificationUIHelper.GetColourClass</c> in JC.Communication. That package
/// references only JC.Core, so it could never reach a dictionary — which left Bootstrap colour names
/// hardcoded in a package with no way to participate in this design. The mapping belongs here, where
/// the framework choice is known.
/// <para>
/// The matching icons are not here: they are selected by <see cref="IconFramework"/> rather than
/// <see cref="UIFramework"/> and live on <see cref="CommunicationIcons"/>.
/// </para>
/// </remarks>
public sealed record NotificationTypeClasses
{
    /// <summary>
    /// The colour for each notification type. Types absent from the map fall back to
    /// <see cref="ColourFallback"/>.
    /// </summary>
    public IReadOnlyDictionary<NotificationType, string> Colours { get; init; }
        = new Dictionary<NotificationType, string>();

    /// <summary>The colour used for a notification type absent from <see cref="Colours"/>.</summary>
    public string ColourFallback { get; init; } = "";

    /// <summary>
    /// Returns the colour for the given notification type, or <see cref="ColourFallback"/> when the
    /// dictionary does not define one.
    /// </summary>
    /// <param name="type">The notification type.</param>
    public string Colour(NotificationType type) => Colours.GetValueOrDefault(type, ColourFallback);
}

/// <summary>
/// Classes for the chat participant list.
/// </summary>
public sealed record ChatParticipantsClasses
{
    /// <summary>The element wrapping the avatars.</summary>
    public string Container { get; init; } = "";

    /// <summary>A participant avatar.</summary>
    public string Avatar { get; init; } = "";

    /// <summary>The avatar standing in for participants beyond the display limit.</summary>
    public string Overflow { get; init; } = "";
}
