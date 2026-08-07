using JC.Communication.Notifications.Models;
using JC.Web.UI.Framework;

namespace JC.Communication.Web.Framework;

/// <summary>
/// Bootstrap 5 class names for JC.Communication.Web's tag helpers. Selected when
/// <see cref="UIFramework.Bootstrap"/> is configured, which is the default.
/// </summary>
/// <remarks>
/// Every value here is the markup these tag helpers emitted before class names were made
/// configurable, so this dictionary reproduces the previous output exactly.
/// </remarks>
public sealed class BootstrapCommunicationDictionary : ICommunicationFrameworkDictionary
{
    /// <inheritdoc />
    public NotificationDropdownClasses NotificationDropdown { get; } = new()
    {
        Container = "dropdown",
        BellButton = "btn btn-link position-relative",
        BadgeFormat = "position-absolute top-0 start-100 translate-middle badge rounded-pill bg-{0}",
        DefaultBadgeColour = "danger",
        ScreenReaderOnly = "visually-hidden",
        MenuFormat = "dropdown-menu dropdown-menu-{0} p-0",
        DefaultAlign = "end",
        EmptyItem = "dropdown-item text-center text-muted py-3",
        Item = "dropdown-item d-flex align-items-start gap-2 py-2 px-3 border-bottom",
        // {0} is the whole icon class from the icon dictionary, base class included — this format
        // must not add one of its own, since Font Awesome has no equivalent of "bi".
        ItemIconFormat = "{0} text-{1} mt-1",
        ItemContent = "flex-grow-1 overflow-hidden",
        ItemTitle = "fw-semibold text-truncate",
        ItemBody = "small text-muted text-truncate",
        ItemTime = "small text-muted",
        UnreadDotFormat = "bg-{0} rounded-circle mt-2",
        Divider = "dropdown-divider m-0",
        FooterLink = "dropdown-item text-center py-2"
    };

    /// <inheritdoc />
    public NotificationBadgeClasses NotificationBadge { get; } = new()
    {
        Container = "position-relative",
        BadgeFormat = "position-absolute top-0 start-100 translate-middle badge rounded-pill bg-{0}",
        DefaultBadgeColour = "danger",
        ScreenReaderOnly = "visually-hidden"
    };

    /// <inheritdoc />
    public NotificationToastClasses NotificationToast { get; } = new()
    {
        ContainerFormat = "toast-container position-fixed {0} p-3",
        DefaultPosition = "top-0 end-0",
        Toast = "toast",
        Header = "toast-header",
        HeaderIconFormat = "{0} text-{1} me-2",
        HeaderTitle = "me-auto",
        HeaderTime = "text-muted",
        CloseButton = "btn-close",
        Body = "toast-body"
    };

    /// <inheritdoc />
    public MessageThreadClasses MessageThread { get; } = new()
    {
        Container = "d-flex flex-column gap-2 p-3",
        Header = "d-flex align-items-center gap-2 p-3 border-bottom",
        HeaderAvatar = "rounded-circle",
        HeaderName = "fw-semibold",
        MemberBadge = "badge bg-secondary",
        ReplyPreview = "small text-muted border-start border-2 ps-2 mb-1",
        ReplyName = "fw-semibold",
        SenderName = "fw-semibold small",
        BubbleFormat = "rounded-3 px-3 py-2 bg-{0} text-{1}",
        MessageTime = "small opacity-75 text-end",
        SentAlign = "align-self-end",
        ReceivedAlign = "align-self-start",
        DefaultSentColour = "primary",
        DefaultReceivedColour = "light",
        DefaultSentTextColour = "white",
        DefaultReceivedTextColour = "dark"
    };

    /// <inheritdoc />
    public ChatListClasses ChatList { get; } = new()
    {
        Container = "list-group",
        Empty = "text-center text-muted py-3",
        Item = "list-group-item list-group-item-action d-flex align-items-center gap-3 py-2",
        Avatar = "flex-shrink-0",
        AvatarImage = "rounded-circle",
        AvatarIcon = "rounded-circle d-flex align-items-center justify-content-center",
        AvatarFallback = "rounded-circle d-flex align-items-center justify-content-center bg-secondary-subtle",
        AvatarIconBackground = "var(--bs-secondary-bg)",
        Content = "flex-grow-1 overflow-hidden",
        NameRow = "d-flex justify-content-between",
        Name = "fw-semibold text-truncate",
        Time = "text-muted flex-shrink-0 ms-2",
        Preview = "small text-muted text-truncate",
        PreviewSender = "fw-medium",
        UnreadBadgeFormat = "badge rounded-pill bg-{0} align-self-center flex-shrink-0",
        DefaultUnreadBadgeColour = "primary"
    };

    /// <inheritdoc />
    public ChatInputClasses ChatInput { get; } = new()
    {
        Form = "p-3 border-top",
        ReplyBar = "d-flex align-items-center border-start border-2 border-primary ps-2 py-1 mb-2 bg-light rounded",
        ReplyText = "flex-grow-1 small text-truncate",
        ReplyName = "fw-semibold",
        ReplyClose = "btn-close btn-close-sm ms-2",
        InputRow = "d-flex align-items-end",
        TextArea = "form-control",
        SendButtonFormat = "btn btn-{0} ms-2 align-self-end",
        DefaultButtonColour = "primary"
    };

    /// <inheritdoc />
    public ContactFormClasses ContactForm { get; } = new()
    {
        Heading = "mb-3",
        Field = "mb-3",
        Label = "form-label",
        Input = "form-control",
        TextArea = "form-control",
        SubmitButtonFormat = "btn btn-{0}",
        DefaultButtonColour = "primary"
    };

    /// <inheritdoc />
    public NotificationTypeClasses NotificationTypes { get; } = new()
    {
        Colours = new Dictionary<NotificationType, string>
        {
            [NotificationType.Message] = "primary",
            [NotificationType.Info] = "info",
            [NotificationType.Success] = "success",
            [NotificationType.Warning] = "warning",
            [NotificationType.Error] = "danger",
            [NotificationType.System] = "secondary",
            [NotificationType.Task] = "primary"
        },
        ColourFallback = "secondary"
    };

    /// <inheritdoc />
    public ChatParticipantsClasses ChatParticipants { get; } = new()
    {
        Container = "d-flex align-items-center gap-1",
        Avatar = "rounded-circle bg-primary-subtle text-primary d-flex align-items-center justify-content-center fw-semibold",
        Overflow = "rounded-circle bg-secondary-subtle d-flex align-items-center justify-content-center fw-semibold text-muted"
    };
}
