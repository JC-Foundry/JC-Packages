using JC.Communication.Notifications.Models;
using JC.Web.UI.Framework;

namespace JC.Communication.Web.Framework;

/// <summary>
/// jc-tailwind-ui classes for JC.Communication.Web's tag helpers. Selected when
/// <see cref="UIFramework.CustomJCTailwind"/> is configured.
/// </summary>
/// <remarks>
/// The framework borrows Bootstrap's class vocabulary, so the structural values largely match
/// <see cref="BootstrapCommunicationDictionary"/>. Colour is the exception: treatments name no colour
/// and read a <c>tone-{type}</c> class, so every contextual colour composes as <c>tone-{0}</c> and a
/// custom colour the application defines a tone for works everywhere a built-in does.
/// <para>
/// Where the framework ships its own component it is used, even where that renders differently from
/// Bootstrap — participants become an overlapped <c>avatar-group</c>, and toasts use the
/// self-positioning <c>toast-host</c> rather than a placed container.
/// </para>
/// <para>
/// <b>Requirements.</b> <see cref="NotificationToast"/> needs the opt-in <c>interactive</c> layer,
/// which is where <c>toast-host</c> and <c>toast</c> live.
/// </para>
/// <para>
/// <b>Adding or changing a class here means updating <c>jc-communication.tailwind.css</c>.</b>
/// jc-tailwind-ui compiles from source through Tailwind rather than shipping finished CSS, so only
/// its authored component classes are free — every generated utility below needs declaring, exactly
/// as under plain Tailwind. That includes the theme-derived colour utilities
/// (<c>text-fg-muted</c>, <c>bg-surface-2</c>, <c>border-edge</c>): the framework's own CSS reads
/// those tokens as <c>var(--color-…)</c> in declarations, which does not cause the matching utility
/// to be generated. The arbitrary values <c>text-[var(--t-l)]</c>, <c>bg-[var(--t)]</c>,
/// <c>text-[var(--t-fg)]</c> and <c>border-[var(--accent)]</c> need it too.
/// </para>
/// </remarks>
public sealed class CustomJCTailwindCommunicationDictionary : ICommunicationFrameworkDictionary
{
    /// <inheritdoc />
    /// <remarks>
    /// The bell carries <c>dropdown-toggle</c>, which is the selector the framework's own
    /// <c>ui.js</c> binds to, so <c>initUI()</c> opens and closes this menu with no application
    /// JavaScript. Under Bootstrap the equivalent is the <c>data-bs-toggle</c> attribute the tag
    /// helper emits regardless.
    /// </remarks>
    public NotificationDropdownClasses NotificationDropdown { get; } = new()
    {
        Container = "dropdown",

        // .btn on its own is a transparent, borderless button — the framework's nearest equivalent
        // of Bootstrap's btn-link. `relative` anchors the absolutely-positioned badge.
        BellButton = "btn dropdown-toggle relative",

        // .badge is already pill-shaped, so no rounding utility is needed; the translate pair
        // reproduces Bootstrap's translate-middle.
        BadgeFormat = "absolute top-0 start-full -translate-x-1/2 -translate-y-1/2 badge badge-solid tone-{0}",
        DefaultBadgeColour = "danger",
        ScreenReaderOnly = "sr-only",
        MenuFormat = "dropdown-menu dropdown-menu-{0} p-0",
        DefaultAlign = "end",

        // .dropdown-item is a flex row, so centring is justify-center rather than text-center.
        EmptyItem = "dropdown-item justify-center text-fg-muted py-3",
        Item = "dropdown-item items-start gap-2 py-2 px-3 border-b border-edge",

        // {0} is the whole icon class from the icon dictionary, base class included — this format
        // must not add one of its own, since Font Awesome has no equivalent of "bi". The tone sets
        // the palette on the icon itself and --t-l is its light shade.
        ItemIconFormat = "{0} tone-{1} text-[var(--t-l)] mt-1",
        ItemContent = "grow overflow-hidden",
        ItemTitle = "font-semibold truncate",
        ItemBody = "text-sm text-fg-muted truncate",
        ItemTime = "text-sm text-fg-muted",
        UnreadDotFormat = "tone-{0} bg-[var(--t)] rounded-full mt-2",
        Divider = "dropdown-divider m-0",
        FooterLink = "dropdown-item justify-center py-2"
    };

    /// <inheritdoc />
    public NotificationBadgeClasses NotificationBadge { get; } = new()
    {
        Container = "relative",
        BadgeFormat = "absolute top-0 start-full -translate-x-1/2 -translate-y-1/2 badge badge-solid tone-{0}",
        DefaultBadgeColour = "danger",
        ScreenReaderOnly = "sr-only"
    };

    /// <inheritdoc />
    /// <remarks>
    /// <c>toast-host</c> fixes itself to the top right and stacks its children, so the position slot
    /// is empty by default — a caller supplying one overrides the host's own inset, since Tailwind
    /// utilities outrank the framework's component layer. The framework's toast has no header or
    /// body part, so those are composed from utilities.
    /// <para>
    /// Dismissal is not wired: the close button carries <c>data-bs-dismiss</c>, and the framework's
    /// <c>ui.js</c> listens for <c>data-dismiss</c>. Shadow it with a line of application JavaScript,
    /// or call the framework's own <c>toast()</c> instead of rendering this component.
    /// </para>
    /// </remarks>
    public NotificationToastClasses NotificationToast { get; } = new()
    {
        ContainerFormat = "toast-host {0}",
        DefaultPosition = "",
        Toast = "toast",
        Header = "flex items-center gap-2 mb-1",
        HeaderIconFormat = "{0} tone-{1} text-[var(--t-l)]",
        HeaderTitle = "me-auto font-semibold",
        HeaderTime = "text-sm text-fg-muted",
        CloseButton = "btn-close",
        Body = "text-sm text-fg-muted"
    };

    /// <inheritdoc />
    /// <remarks>
    /// A bubble takes both its fill and its text from the tone — <c>--t</c> and the readable
    /// <c>--t-fg</c> that travels with it — so the two text-colour defaults are empty and the
    /// <c>sent-text-colour</c> / <c>received-text-colour</c> attributes have no effect under this
    /// framework. That is the point of the tone carrying its own foreground: a custom colour needs
    /// no second decision about what is legible on it.
    /// </remarks>
    public MessageThreadClasses MessageThread { get; } = new()
    {
        Container = "flex flex-col gap-2 p-3",
        Header = "flex items-center gap-2 p-3 border-b border-edge",
        HeaderAvatar = "avatar",
        HeaderName = "font-semibold",
        MemberBadge = "badge badge-solid tone-secondary",
        ReplyPreview = "text-sm text-fg-muted border-l-2 border-edge ps-2 mb-1",
        ReplyName = "font-semibold",
        SenderName = "font-semibold text-sm",
        BubbleFormat = "rounded-lg px-3 py-2 tone-{0} bg-[var(--t)] text-[var(--t-fg)]",
        MessageTime = "text-sm opacity-75 text-end",
        SentAlign = "self-end",
        ReceivedAlign = "self-start",
        DefaultSentColour = "primary",

        // tone-light is the framework's pale surface tone, and its --t-fg is dark — the pairing
        // Bootstrap spells as bg-light text-dark.
        DefaultReceivedColour = "light",
        DefaultSentTextColour = "",
        DefaultReceivedTextColour = ""
    };

    /// <inheritdoc />
    /// <remarks>
    /// The three avatar variants are all <c>avatar</c>: the framework's own avatar supplies the
    /// circle, the centring and a surface background, so they differ only in what they contain.
    /// </remarks>
    public ChatListClasses ChatList { get; } = new()
    {
        Container = "list-group",
        Empty = "text-center text-fg-muted py-3",

        // .list-group-item is already a padded flex row with a gap, so nothing is added here.
        Item = "list-group-item list-group-item-action",
        Avatar = "shrink-0",
        AvatarImage = "avatar",
        AvatarIcon = "avatar",
        AvatarFallback = "avatar",

        // A colour value rather than a class — it is set through the inline style that sizes the
        // avatar. surface-2 is the framework's raised surface, matching what .avatar itself uses.
        AvatarIconBackground = "var(--color-surface-2)",
        Content = "grow overflow-hidden",
        NameRow = "flex justify-between",
        Name = "font-semibold truncate",
        Time = "text-fg-muted shrink-0 ms-2",
        Preview = "text-sm text-fg-muted truncate",
        PreviewSender = "font-medium",
        UnreadBadgeFormat = "badge badge-solid tone-{0} self-center shrink-0",
        DefaultUnreadBadgeColour = "primary"
    };

    /// <inheritdoc />
    public ChatInputClasses ChatInput { get; } = new()
    {
        Form = "p-3 border-t border-edge",

        // --accent is the app accent rather than a theme colour, so it has no generated utility.
        ReplyBar = "flex items-center border-l-2 border-[var(--accent)] ps-2 py-1 mb-2 bg-surface-2 rounded-md",
        ReplyText = "grow text-sm truncate",
        ReplyName = "font-semibold",

        // btn-close draws its own × via ::before, so the empty button renders correctly.
        ReplyClose = "btn-close ms-2",
        InputRow = "flex items-end",
        TextArea = "form-control",
        SendButtonFormat = "btn btn-solid tone-{0} ms-2 self-end",
        DefaultButtonColour = "primary"
    };

    /// <inheritdoc />
    public ContactFormClasses ContactForm { get; } = new()
    {
        Heading = "mb-3",

        // form-group carries the field spacing, so no margin utility is needed.
        Field = "form-group",
        Label = "form-label",
        Input = "form-control",
        TextArea = "form-control",
        SubmitButtonFormat = "btn btn-solid tone-{0}",
        DefaultButtonColour = "primary"
    };

    /// <inheritdoc />
    /// <remarks>
    /// These are tone names, not Bootstrap contextual names — the words coincide because the
    /// framework names its built-in types the same way, but they are consumed as <c>tone-{0}</c>.
    /// </remarks>
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
    /// <remarks>
    /// <c>avatar-group</c> overlaps its avatars and rings them against the canvas, so this renders as
    /// a stack rather than Bootstrap's spaced row. It is the component the framework ships for
    /// exactly this, and the accent tint is dropped with it — <c>avatar</c> carries the framework's
    /// own avatar surface.
    /// </remarks>
    public ChatParticipantsClasses ChatParticipants { get; } = new()
    {
        Container = "avatar-group",
        Avatar = "avatar",
        Overflow = "avatar text-fg-muted"
    };
}
