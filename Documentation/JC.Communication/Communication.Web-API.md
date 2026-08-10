# JC.Communication.Web — API reference

Complete reference for all public types in the JC.Communication.Web package. See [Setup](Communication.Web-Setup.md) for registration and [Guide](Communication.Web-Guide.md) for usage examples.

> **Note:** Registration extensions (`AddCommunicationWeb`) are documented in [Setup](Communication.Web-Setup.md), not here.

## Models

### ContactInputModel

**Namespace:** `JC.Communication.Web.Models`

Input model for the `<contact-form>` tag helper. Bind this to the form POST action to receive email, subject, and message values.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Email` | `string` | `""` | get; set; | The sender's email address. Required. Must be a valid email address. Max length 256. |
| `Subject` | `string` | `""` | get; set; | The message subject. Required. Max length 256. |
| `Message` | `string` | `""` | get; set; | The message body. Required. Max length 8192. |

All properties carry `[Required]` and `[MaxLength]` data annotations. `Email` additionally carries `[EmailAddress]`.

---

## Framework dictionary

The CSS class dictionary and its records. Every property holds a **complete** class attribute value rather than a single token, and every property defaults to `""`, so adding one to a record does not break an existing implementation.

Properties whose name ends in `Format` are `string.Format` templates read through the accessor method on the same record. See [`FrameworkClass`](../JC.Web/UI-API.md#frameworkclass) for the formatting and joining helpers.

### ICommunicationFrameworkDictionary

**Namespace:** `JC.Communication.Web.Framework`

The CSS class dictionary contract for this package's tag helpers. Extends `IFrameworkDictionary`. One implementation exists per supported `UIFramework`; the configured framework decides which is resolved from the container. Registered by `AddCommunicationWeb` via `AddFrameworkDictionary`.

#### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `NotificationDropdown` | `NotificationDropdownClasses` | get; | Classes for the notification bell and dropdown. |
| `NotificationBadge` | `NotificationBadgeClasses` | get; | Classes for the standalone unread count badge. |
| `NotificationToast` | `NotificationToastClasses` | get; | Classes for notification toasts. |
| `MessageThread` | `MessageThreadClasses` | get; | Classes for the chat thread view. |
| `ChatList` | `ChatListClasses` | get; | Classes for the list of chat threads. |
| `ChatInput` | `ChatInputClasses` | get; | Classes for the message compose box. |
| `ContactForm` | `ContactFormClasses` | get; | Classes for the contact form. |
| `ChatParticipants` | `ChatParticipantsClasses` | get; | Classes for the chat participant list. |
| `NotificationTypes` | `NotificationTypeClasses` | get; | Colours derived from a notification's type, shared by the dropdown and the toast. |

---

### NotificationDropdownClasses

**Namespace:** `JC.Communication.Web.Framework`

Sealed record holding the classes for the notification bell and its dropdown.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Container` | `string` | `""` | get; init; | The element wrapping the bell and menu. |
| `BellButton` | `string` | `""` | get; init; | The bell button. |
| `BadgeFormat` | `string` | `""` | get; init; | The unread count badge. `{0}` is the configured colour. |
| `DefaultBadgeColour` | `string` | `""` | get; init; | The badge colour when the caller specifies none. |
| `ScreenReaderOnly` | `string` | `""` | get; init; | Text available to screen readers but not shown. |
| `MenuFormat` | `string` | `""` | get; init; | The dropdown menu. `{0}` is the configured alignment. |
| `DefaultAlign` | `string` | `""` | get; init; | The alignment used when the caller specifies none. |
| `EmptyItem` | `string` | `""` | get; init; | The item shown when there are no notifications. |
| `Item` | `string` | `""` | get; init; | A single notification row. |
| `ItemIconFormat` | `string` | `""` | get; init; | A row's icon. `{0}` is the icon class, `{1}` the colour. |
| `ItemContent` | `string` | `""` | get; init; | The text column of a row. |
| `ItemTitle` | `string` | `""` | get; init; | A row's title. |
| `ItemBody` | `string` | `""` | get; init; | A row's body preview. |
| `ItemTime` | `string` | `""` | get; init; | A row's relative timestamp. |
| `UnreadDotFormat` | `string` | `""` | get; init; | The unread marker on a row. `{0}` is the configured colour. |
| `Divider` | `string` | `""` | get; init; | The rule above the footer. |
| `FooterLink` | `string` | `""` | get; init; | The "view all" footer link. |

#### Methods

##### Badge(string colour)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `colour` | `string` | — | The configured contextual colour. |

Applies `colour` to `BadgeFormat`. Returns an empty string when `BadgeFormat` is unset.

##### Menu(string align)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `align` | `string` | — | The configured alignment. |

Applies `align` to `MenuFormat`. Returns an empty string when `MenuFormat` is unset.

##### ItemIcon(string icon, string colour)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `icon` | `string` | — | The icon class from the notification or its type default. |
| `colour` | `string` | — | The colour from the notification or its type default. |

Applies both values to `ItemIconFormat`. The icon argument is the whole icon class including any base class, so the format must not add one of its own.

##### UnreadDot(string colour)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `colour` | `string` | — | The configured contextual colour. |

Applies `colour` to `UnreadDotFormat`. Returns an empty string when `UnreadDotFormat` is unset.

---

### NotificationBadgeClasses

**Namespace:** `JC.Communication.Web.Framework`

Sealed record holding the classes for the standalone unread count badge.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Container` | `string` | `""` | get; init; | The element wrapping the icon and badge. |
| `BadgeFormat` | `string` | `""` | get; init; | The count badge. `{0}` is the configured colour. |
| `DefaultBadgeColour` | `string` | `""` | get; init; | The badge colour when the caller specifies none. |
| `ScreenReaderOnly` | `string` | `""` | get; init; | Text available to screen readers but not shown. |

#### Methods

##### Badge(string colour)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `colour` | `string` | — | The configured contextual colour. |

Applies `colour` to `BadgeFormat`. Returns an empty string when `BadgeFormat` is unset.

---

### NotificationToastClasses

**Namespace:** `JC.Communication.Web.Framework`

Sealed record holding the classes for notification toasts.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `ContainerFormat` | `string` | `""` | get; init; | The fixed container holding the stack. `{0}` is the configured position. |
| `DefaultPosition` | `string` | `""` | get; init; | The position used when the caller specifies none. |
| `Toast` | `string` | `""` | get; init; | A single toast. |
| `Header` | `string` | `""` | get; init; | A toast's header. |
| `HeaderIconFormat` | `string` | `""` | get; init; | The header icon. `{0}` is the icon class, `{1}` the colour. |
| `HeaderTitle` | `string` | `""` | get; init; | The header title. |
| `HeaderTime` | `string` | `""` | get; init; | The header's relative timestamp. |
| `CloseButton` | `string` | `""` | get; init; | The dismiss button. |
| `Body` | `string` | `""` | get; init; | A toast's body. |

#### Methods

##### Container(string position)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `position` | `string` | — | The configured position. |

Applies `position` to `ContainerFormat`. Returns an empty string when `ContainerFormat` is unset.

##### HeaderIcon(string icon, string colour)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `icon` | `string` | — | The icon class from the notification or its type default. |
| `colour` | `string` | — | The colour from the notification or its type default. |

Applies both values to `HeaderIconFormat`.

---

### MessageThreadClasses

**Namespace:** `JC.Communication.Web.Framework`

Sealed record holding the classes for the chat thread view.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Container` | `string` | `""` | get; init; | The scrolling container holding the messages. |
| `Header` | `string` | `""` | get; init; | The thread header. |
| `HeaderAvatar` | `string` | `""` | get; init; | The header's avatar image. |
| `HeaderName` | `string` | `""` | get; init; | The thread name. |
| `MemberBadge` | `string` | `""` | get; init; | The member count badge shown for group chats. |
| `ReplyPreview` | `string` | `""` | get; init; | The quoted message shown above a reply. |
| `ReplyName` | `string` | `""` | get; init; | The sender name inside a reply preview. |
| `SenderName` | `string` | `""` | get; init; | The sender name shown above a received group message. |
| `BubbleFormat` | `string` | `""` | get; init; | A message bubble. `{0}` is the background colour, `{1}` the text colour. |
| `MessageTime` | `string` | `""` | get; init; | A message's relative timestamp. |
| `SentAlign` | `string` | `""` | get; init; | Positions a message sent by the current user. |
| `ReceivedAlign` | `string` | `""` | get; init; | Positions a message received from someone else. |
| `DefaultSentColour` | `string` | `""` | get; init; | The sent bubble's background colour when the caller specifies none. |
| `DefaultReceivedColour` | `string` | `""` | get; init; | The received bubble's background colour when the caller specifies none. |
| `DefaultSentTextColour` | `string` | `""` | get; init; | The sent bubble's text colour when the caller specifies none. |
| `DefaultReceivedTextColour` | `string` | `""` | get; init; | The received bubble's text colour when the caller specifies none. |

#### Methods

##### Bubble(string background, string text)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `background` | `string` | — | The bubble's background colour. |
| `text` | `string` | — | The bubble's text colour. |

Applies both values to `BubbleFormat`. A format that references only `{0}` — as `CustomJCTailwindCommunicationDictionary` does, deriving the foreground from the tone — silently ignores `text`.

---

### ChatListClasses

**Namespace:** `JC.Communication.Web.Framework`

Sealed record holding the classes for the list of chat threads.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Container` | `string` | `""` | get; init; | The list container. |
| `Empty` | `string` | `""` | get; init; | The message shown when there are no threads. |
| `Item` | `string` | `""` | get; init; | A single thread row. |
| `Avatar` | `string` | `""` | get; init; | The fixed-size avatar column. |
| `AvatarImage` | `string` | `""` | get; init; | An avatar backed by an image. |
| `AvatarIcon` | `string` | `""` | get; init; | An avatar backed by the thread's own icon. |
| `AvatarFallback` | `string` | `""` | get; init; | An avatar for a thread with neither image nor icon. |
| `AvatarIconBackground` | `string` | `""` | get; init; | The background applied to an icon avatar when the thread specifies no colour. A CSS colour **value**, not a class — it is written into the inline style that sizes the avatar. |
| `Content` | `string` | `""` | get; init; | The text column of a row. |
| `NameRow` | `string` | `""` | get; init; | The row holding the thread name and timestamp. |
| `Name` | `string` | `""` | get; init; | The thread name. |
| `Time` | `string` | `""` | get; init; | The last activity timestamp. |
| `Preview` | `string` | `""` | get; init; | The last message preview. |
| `PreviewSender` | `string` | `""` | get; init; | The sender name inside the preview. |
| `UnreadBadgeFormat` | `string` | `""` | get; init; | The unread count badge. `{0}` is the configured colour. |
| `DefaultUnreadBadgeColour` | `string` | `""` | get; init; | The unread badge colour when the caller specifies none. |

#### Methods

##### UnreadBadge(string colour)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `colour` | `string` | — | The configured contextual colour. |

Applies `colour` to `UnreadBadgeFormat`. Returns an empty string when `UnreadBadgeFormat` is unset.

---

### ChatInputClasses

**Namespace:** `JC.Communication.Web.Framework`

Sealed record holding the classes for the message compose box.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Form` | `string` | `""` | get; init; | The form wrapping the compose box. |
| `ReplyBar` | `string` | `""` | get; init; | The bar showing the message being replied to. |
| `ReplyText` | `string` | `""` | get; init; | The quoted text inside the reply bar. |
| `ReplyName` | `string` | `""` | get; init; | The sender name inside the reply bar. |
| `ReplyClose` | `string` | `""` | get; init; | The button that cancels a reply. |
| `InputRow` | `string` | `""` | get; init; | The row holding the textarea and send button. |
| `TextArea` | `string` | `""` | get; init; | The message textarea. |
| `SendButtonFormat` | `string` | `""` | get; init; | The send button. `{0}` is the configured colour. |
| `DefaultButtonColour` | `string` | `""` | get; init; | The send button colour when the caller specifies none. |

#### Methods

##### SendButton(string colour)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `colour` | `string` | — | The configured contextual colour. |

Applies `colour` to `SendButtonFormat`. Returns an empty string when `SendButtonFormat` is unset.

---

### ContactFormClasses

**Namespace:** `JC.Communication.Web.Framework`

Sealed record holding the classes for the contact form.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Heading` | `string` | `""` | get; init; | The form heading. |
| `Field` | `string` | `""` | get; init; | The wrapper around a single field. |
| `Label` | `string` | `""` | get; init; | A field label. |
| `Input` | `string` | `""` | get; init; | A single-line input. |
| `TextArea` | `string` | `""` | get; init; | The message textarea. |
| `SubmitButtonFormat` | `string` | `""` | get; init; | The submit button. `{0}` is the configured colour. |
| `DefaultButtonColour` | `string` | `""` | get; init; | The submit button colour when the caller specifies none. |

#### Methods

##### SubmitButton(string colour)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `colour` | `string` | — | The configured contextual colour. |

Applies `colour` to `SubmitButtonFormat`. Returns an empty string when `SubmitButtonFormat` is unset.

---

### ChatParticipantsClasses

**Namespace:** `JC.Communication.Web.Framework`

Sealed record holding the classes for the chat participant list.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Container` | `string` | `""` | get; init; | The element wrapping the avatars. |
| `Avatar` | `string` | `""` | get; init; | A participant avatar. |
| `Overflow` | `string` | `""` | get; init; | The avatar standing in for participants beyond the display limit. |

---

### NotificationTypeClasses

**Namespace:** `JC.Communication.Web.Framework`

Sealed record mapping a `NotificationType` to a contextual colour, shared by the dropdown and the toast. The matching icons are not here — they are selected by `IconFramework` rather than `UIFramework` and live on `CommunicationIcons`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Colours` | `IReadOnlyDictionary<NotificationType, string>` | empty | get; init; | The colour for each notification type. |
| `ColourFallback` | `string` | `""` | get; init; | The colour used for a type absent from `Colours`. |

#### Methods

##### Colour(NotificationType type)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `type` | `NotificationType` | — | The notification type. |

Returns the colour mapped to `type`, or `ColourFallback` when the dictionary does not define one.

---

### BootstrapCommunicationDictionary

**Namespace:** `JC.Communication.Web.Framework`

Sealed `ICommunicationFrameworkDictionary` implementation holding Bootstrap 5 class names. Selected when `UIFramework.Bootstrap` is configured, which is the default. Every value reproduces the markup these tag helpers emitted before class names were made configurable.

Implements every property of the interface; see `ICommunicationFrameworkDictionary` for the members.

---

### TailwindCommunicationDictionary

**Namespace:** `JC.Communication.Web.Framework`

Sealed `ICommunicationFrameworkDictionary` implementation holding Tailwind CSS classes, chosen to reproduce Bootstrap 5's appearance as closely as utilities allow. Selected when `UIFramework.Tailwind` is configured. Targets Tailwind v4 and assumes Preflight.

Contextual colours are Tailwind fragments rather than Bootstrap contextual names, and the dropdown's alignment is a placement utility such as `right-0`. Every class must reach Tailwind's scanner — see [Setup](Communication.Web-Setup.md#framework-specific-requirements).

---

### CustomJCTailwindCommunicationDictionary

**Namespace:** `JC.Communication.Web.Framework`

Sealed `ICommunicationFrameworkDictionary` implementation holding jc-tailwind-ui classes. Selected when `UIFramework.CustomJCTailwind` is configured.

Contextual colours are tone names consumed as `tone-{0}`, so any colour the application defines a tone for works, not only the framework's built-in eight. Where that framework ships its own component it is used even where the result differs from Bootstrap: participants render as an overlapped `avatar-group`, and toasts use the self-positioning `toast-host`. `MessageThreadClasses.BubbleFormat` derives its foreground from the tone, so the two text-colour values are unset and ignored.

`NotificationToastClasses` resolves to classes in that framework's opt-in interactive layer.

---

## Icon dictionary

The icon class dictionary and its record. Selected by `IconFramework`, independently of the CSS framework. Every value is a **complete** class attribute — `"bi bi-bell"`, not `"bi-bell"` — because Bootstrap Icons and Font Awesome share no base class.

### ICommunicationIconDictionary

**Namespace:** `JC.Communication.Web.Framework.Icons`

The icon class dictionary contract for this package's tag helpers. Extends `IIconDictionary`. Registered by `AddCommunicationWeb` via `AddIconDictionary`. JC.Communication.Web is the only package in the suite that registers one, because it is the only one whose components render a glyph.

#### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Icons` | `CommunicationIcons` | get; | The icons this package's tag helpers render. |

---

### CommunicationIcons

**Namespace:** `JC.Communication.Web.Framework.Icons`

Sealed record holding every icon this package renders. One flat group rather than one per component, because icons are shared — the bell appears in both the badge and the dropdown, the reply arrow in both the chat input and the message thread.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `BaseClass` | `string` | `""` | get; init; | The base class this icon set requires alongside each glyph class, empty when it needs none. Used only to normalise caller-supplied values through `Custom`. |
| `Bell` | `string` | `""` | get; init; | The notification bell. |
| `Reply` | `string` | `""` | get; init; | The reply arrow, shown against a quoted message. |
| `Send` | `string` | `""` | get; init; | The send arrow on the compose button. |
| `Close` | `string` | `""` | get; init; | The cross that cancels a reply. |
| `Person` | `string` | `""` | get; init; | The avatar stand-in for a one-to-one thread. |
| `People` | `string` | `""` | get; init; | The avatar stand-in for a group thread. |
| `NotificationTypes` | `IReadOnlyDictionary<NotificationType, string>` | empty | get; init; | The icon for each notification type. |
| `NotificationFallback` | `string` | `""` | get; init; | The icon used for a type absent from `NotificationTypes`. |

#### Methods

##### Notification(NotificationType type)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `type` | `NotificationType` | — | The notification type. |

Returns the icon mapped to `type`, or `NotificationFallback` when the dictionary does not define one.

##### Custom(string? icon)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `icon` | `string?` | — | The icon class from a tag helper attribute or a stored record. |

Prefixes `icon` with `BaseClass` unless it already carries it, delegating to `IconClass.WithBase`. Returns an empty string when `icon` is null or whitespace, and returns `icon` unchanged when `BaseClass` is empty.

This is what lets `"bi-star"` and `"bi bi-star"` both render correctly under Bootstrap Icons without a data migration. It also means a set with no base class — Font Awesome — takes caller values exactly as given, so a value written for a different icon set renders as-is and shows no glyph.

---

### BootstrapIconsCommunicationDictionary

**Namespace:** `JC.Communication.Web.Framework.Icons`

Sealed `ICommunicationIconDictionary` implementation holding Bootstrap Icons classes. Selected when `IconFramework.Bootstrap` is configured, which is the default. `BaseClass` is `"bi"`, and every glyph value carries it.

`NotificationFallback` is empty, so an icon element for an unrecognised notification type carries no icon classes at all.

---

### FontAwesomeCommunicationDictionary

**Namespace:** `JC.Communication.Web.Framework.Icons`

Sealed `ICommunicationIconDictionary` implementation holding Font Awesome 6 free class names in the solid style. Selected when `IconFramework.FontAwesome` is configured.

`BaseClass` is empty, because Font Awesome carries its style in the glyph class itself. Version 5 renamed several of these glyphs — `fa-xmark` was `fa-times`, `fa-circle-info` was `fa-info-circle`, `fa-triangle-exclamation` was `fa-exclamation-triangle`, `fa-circle-xmark` was `fa-times-circle` — so an application on version 5 needs its own dictionary rather than this one.

`NotificationFallback` is empty, matching the Bootstrap Icons dictionary.

---

## Helpers

Every tag helper in this package takes constructor dependencies from the container, so `AddCommunicationWeb` must have been called. Resolution fails when the page renders, not at build or startup.

All are self-closing (`TagStructure.WithoutEndTag`).

### NotificationDropdownTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<notification-dropdown>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a notification bell button with a dropdown list of the current user's unread notifications. Constructor dependencies: `NotificationCache`, `HtmlHelper`, `ICommunicationFrameworkDictionary`, `ICommunicationIconDictionary`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Icon` | `string?` | `null` | get; set; | Icon class for the bell. Falls back to the icon dictionary's `Bell`; a supplied value is normalised through `CommunicationIcons.Custom`. HTML attribute: `icon`. |
| `BadgeColour` | `string?` | `null` | get; set; | Colour for the unread badge and the per-item unread dot. Falls back to `DefaultBadgeColour`. HTML attribute: `badge-colour`. |
| `MaxHeight` | `int` | `350` | get; set; | Maximum height of the scrollable notification list in pixels. HTML attribute: `max-height`. |
| `DropdownWidth` | `int` | `360` | get; set; | Dropdown menu width in pixels. HTML attribute: `dropdown-width`. |
| `EmptyText` | `string` | `"No new notifications"` | get; set; | Text shown when there are no unread notifications. HTML attribute: `empty-text`. |
| `BodyMaxLength` | `int` | `80` | get; set; | Maximum notification body length before truncation. HTML attribute: `body-max-length`. |
| `ViewAllHref` | `string?` | `null` | get; set; | URL for the "View all" footer link. When null, no footer or divider is rendered. HTML attribute: `view-all-href`. |
| `Align` | `string?` | `null` | get; set; | Menu alignment. Falls back to `DefaultAlign`. HTML attribute: `align`. |

#### Methods

##### ProcessAsync(TagHelperContext context, TagHelperOutput output)

**Returns:** `Task`

Retrieves notifications from `NotificationCache.GetNotificationsAsync()`, filters to unread only, and orders by `CreatedUtc` descending.

Renders a container holding a bell button and a menu. The bell carries `type="button"`, `data-bs-toggle="dropdown"` and `aria-expanded="false"`; the count badge is rendered only when at least one unread notification exists, capped at `99+`, with a screen-reader-only "unread notifications" label appended.

Each item shows an icon, title, truncated body, relative timestamp and an unread dot. The icon comes from the notification's `NotificationStyle.CustomIconClass` when set — normalised through `CommunicationIcons.Custom` — otherwise from the icon dictionary's mapping for the notification's `Type`. The colour comes from `CustomColourClass` when set, otherwise from `NotificationTypeClasses.Colour`. The two fall back independently. Items with a `UrlLink` render as `<a>` with an `href`, otherwise as `<div>`.

The menu carries an inline `width` from `DropdownWidth`, and the scrollable region an inline `max-height` from `MaxHeight`. When `ViewAllHref` is set, a divider and a "View all" link are appended.

---

### NotificationBadgeTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<notification-badge>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a lightweight unread notification count badge. Constructor dependencies: `NotificationCache`, `HtmlHelper`, `ICommunicationFrameworkDictionary`, `ICommunicationIconDictionary`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Icon` | `string?` | `null` | get; set; | Icon class. Falls back to the icon dictionary's `Bell`; a supplied value is normalised through `CommunicationIcons.Custom`. HTML attribute: `icon`. |
| `BadgeColour` | `string?` | `null` | get; set; | Badge colour. Falls back to `DefaultBadgeColour`. HTML attribute: `badge-colour`. |
| `HideWhenZero` | `bool` | `true` | get; set; | When `true`, the badge is omitted if the unread count is zero. HTML attribute: `hide-when-zero`. |

#### Methods

##### ProcessAsync(TagHelperContext context, TagHelperOutput output)

**Returns:** `Task`

Retrieves the unread count from `NotificationCache.GetUnreadCountAsync()` and renders a `<span>` containing the icon.

When the count is zero and `HideWhenZero` is `true`, the span is emitted with the icon only and **no class attribute** — the container class is not applied on this path. Otherwise the container class is set and a badge is appended carrying the count, capped at `99+`, followed by a screen-reader-only "unread notifications" label.

---

### NotificationToastTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<notification-toast>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a fixed-position toast container for notification pop-ups. Constructor dependencies: `HtmlHelper`, `ICommunicationFrameworkDictionary`, `ICommunicationIconDictionary`, `UIFrameworkService`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Model` | `List<Notification>` | `null!` | get; set; | The notifications to render as toasts. HTML attribute: `model`. |
| `Position` | `string?` | `null` | get; set; | Container position. Falls back to `DefaultPosition`. HTML attribute: `position`. |
| `AutoHide` | `bool` | `true` | get; set; | Emitted as `data-bs-autohide`. HTML attribute: `auto-hide`. |
| `Delay` | `int` | `5000` | get; set; | Auto-hide delay in milliseconds, emitted as `data-bs-delay`. HTML attribute: `delay`. |
| `BodyMaxLength` | `int` | `120` | get; set; | Maximum body text length before truncation. HTML attribute: `body-max-length`. |
| `ContainerId` | `string` | `"notification-toasts"` | get; set; | HTML `id` for the container element. HTML attribute: `container-id`. |

#### Methods

##### Process(TagHelperContext context, TagHelperOutput output)

**Returns:** `void`

Renders a container carrying `ContainerId`, `aria-live="polite"` and `aria-atomic="true"`, holding one toast per notification. A null `Model` produces an empty container rather than suppressed output.

Each toast carries `role="alert"`, `aria-live="assertive"`, `aria-atomic="true"`, `data-bs-autohide` and `data-bs-delay`, and contains a header — icon, title, relative timestamp, and a close button carrying `data-bs-dismiss="toast"` — followed by a body. Icon and colour resolve exactly as in `NotificationDropdownTagHelper`. A notification with a `UrlLink` has its header and body wrapped in an `<a>`.

The body uses `Notification.BodyHtml` **unencoded** when set; otherwise `Body` is truncated to `BodyMaxLength` and HTML-encoded.

An auto-show script calling `new bootstrap.Toast(t).show()` is appended **only when `UIFrameworkService.Framework` is `Bootstrap`**. Under any other framework the script is omitted, because it depends on the `bootstrap` global; the markup and its `data-bs-*` attributes are still emitted for the application to drive itself.

---

### MessageThreadTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<message-thread>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a chat thread view showing messages with sender info, timestamps, and reply-to context. Constructor dependencies: `HtmlHelper`, `ICommunicationFrameworkDictionary`, `ICommunicationIconDictionary`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Model` | `ChatModel` | `null!` | get; set; | The chat model to render. HTML attribute: `model`. |
| `CurrentUserId` | `string` | `null!` | get; set; | Distinguishes sent from received messages. HTML attribute: `current-user-id`. |
| `ReplyTruncateLength` | `int` | `60` | get; set; | Maximum length of the reply-to preview before truncation. HTML attribute: `reply-truncate-length`. |
| `SentColour` | `string?` | `null` | get; set; | Background for sent bubbles. Falls back to `DefaultSentColour`. HTML attribute: `sent-colour`. |
| `ReceivedColour` | `string?` | `null` | get; set; | Background for received bubbles. Falls back to `DefaultReceivedColour`. HTML attribute: `received-colour`. |
| `SentTextColour` | `string?` | `null` | get; set; | Text colour for sent bubbles. Falls back to `DefaultSentTextColour`. HTML attribute: `sent-text-colour`. |
| `ReceivedTextColour` | `string?` | `null` | get; set; | Text colour for received bubbles. Falls back to `DefaultReceivedTextColour`. HTML attribute: `received-text-colour`. |
| `UserResolver` | `Func<string, string>?` | `null` | get; set; | Resolves a user ID to a display name. When null, the raw ID is shown. HTML attribute: `user-resolver`. |
| `ContainerClass` | `string?` | `null` | get; set; | Overrides the container class entirely. Falls back to `MessageThreadClasses.Container`. HTML attribute: `container-class`. |
| `MaxHeight` | `int` | `500` | get; set; | Maximum container height in pixels. Set to `0` for no limit. HTML attribute: `max-height`. |

#### Methods

##### Process(TagHelperContext context, TagHelperOutput output)

**Returns:** `void`

Suppresses output entirely when `Model` is null. Otherwise renders a thread header, a message container, and — when `MaxHeight` is greater than zero — an auto-scroll script.

The header shows the metadata image as a 32px element, or failing that the metadata icon at `1.25rem`, then the chat name tinted with the metadata `Colour` when present, then a member count badge for group chats.

Messages are ordered by `SentAtUtc` ascending and indexed by `MessageId` for reply lookups. Each is rendered as a bubble constrained to `max-width:75%`, aligned by `SentAlign` or `ReceivedAlign` according to whether `SenderUserId` equals `CurrentUserId`. Sender names are shown only on received messages in group chats. A reply preview is rendered only when `ReplyToMessageId` resolves against the messages in this model — a reply to a message outside the loaded set renders without a quote.

When `MaxHeight` is greater than zero, the container receives `max-height:{MaxHeight}px;overflow-y:auto;` and an `id` of `thread-{ThreadId}`, and an inline script sets `scrollTop = scrollHeight` on it.

---

### ChatListTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<chat-list>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a list of chat thread previews with optional unread message count badges. Constructor dependencies: `IRepositoryManager`, `IUserInfo`, `HtmlHelper`, `ICommunicationFrameworkDictionary`, `ICommunicationIconDictionary`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Model` | `List<ChatModel>` | `null!` | get; set; | The chat models to render. HTML attribute: `model`. |
| `HrefFormat` | `string` | `"/chat/{0}"` | get; set; | Link format. `{0}` is replaced with the URL-encoded thread ID. HTML attribute: `href-format`. |
| `PreviewMaxLength` | `int` | `50` | get; set; | Maximum last-message preview length before truncation. HTML attribute: `preview-max-length`. |
| `EmptyText` | `string` | `"No conversations"` | get; set; | Text shown when the model is null or empty. HTML attribute: `empty-text`. |
| `ContainerClass` | `string?` | `null` | get; set; | Overrides the container class entirely. Falls back to `ChatListClasses.Container`. HTML attribute: `container-class`. |
| `UserResolver` | `Func<string, string>?` | `null` | get; set; | Resolves the last message sender's ID to a display name. HTML attribute: `user-resolver`. |
| `ShowUnread` | `bool` | `true` | get; set; | Whether to compute and show unread count badges. HTML attribute: `show-unread`. |
| `UnreadBadgeColour` | `string?` | `null` | get; set; | Unread badge colour. Falls back to `DefaultUnreadBadgeColour`. HTML attribute: `unread-badge-colour`. |

#### Methods

##### ProcessAsync(TagHelperContext context, TagHelperOutput output)

**Returns:** `Task`

When `Model` is null or empty, renders a `<div>` carrying `ChatListClasses.Empty` and the HTML-encoded `EmptyText`, and returns.

Otherwise, when `ShowUnread` is `true`, computes unread counts: collects every `MessageId` across every thread in the model, queries `MessageReadLog` once for entries matching the current `IUserInfo.UserId` and those IDs, then for each thread finds the newest message with a read log by `SentAtUtc` and counts messages sent strictly after it. A thread with no read log counts all its messages as unread. Counts derive from `ChatModel.Messages` on the supplied model, not from a database query for messages.

Each thread renders as an `<a>` whose `href` comes from `HrefFormat` with the URL-encoded thread ID. The avatar falls back in three steps: the metadata `ImgPath` as a 40px image, then the metadata `Icon` on a background of the metadata `Colour` or `AvatarIconBackground`, then the icon dictionary's `People` or `Person` according to `IsGroupChat`. The content column holds the thread name — tinted with the metadata `Colour` when present — the `LastActivity` string, and a preview of the newest message prefixed with its sender's resolved name. An unread badge is appended when `ShowUnread` is `true` and the count exceeds zero, capped at `99+`.

---

### ChatInputTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<chat-input>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a chat message compose box with a textarea, send button, and optional reply-to preview bar. Constructor dependencies: `HtmlHelper`, `ICommunicationFrameworkDictionary`, `ICommunicationIconDictionary`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Endpoint` | `string` | `null!` | get; set; | POST endpoint URL. Required. HTML attribute: `endpoint`. |
| `ThreadId` | `string` | `null!` | get; set; | Thread ID included as a hidden input. HTML attribute: `thread-id`. |
| `ReplyTo` | `MessageModel?` | `null` | get; set; | The message being replied to. HTML attribute: `reply-to`. |
| `ReplyTruncateLength` | `int` | `80` | get; set; | Maximum reply preview length before truncation. HTML attribute: `reply-truncate-length`. |
| `Placeholder` | `string` | `"Type a message..."` | get; set; | Textarea placeholder. HTML attribute: `placeholder`. |
| `Rows` | `int` | `2` | get; set; | Textarea rows. HTML attribute: `rows`. |
| `MaxLength` | `int` | `4096` | get; set; | Textarea `maxlength`. HTML attribute: `max-length`. |
| `ButtonText` | `string` | `"Send"` | get; set; | Send button text, rendered beside the icon dictionary's `Send` glyph. HTML attribute: `button-text`. |
| `ButtonColour` | `string?` | `null` | get; set; | Send button colour. Falls back to `DefaultButtonColour`. HTML attribute: `button-colour`. |
| `Prefix` | `string` | `"Input"` | get; set; | Model binding prefix for input names. HTML attribute: `prefix`. |
| `IncludeAntiforgery` | `bool` | `true` | get; set; | Whether to include an anti-forgery token hidden input. HTML attribute: `antiforgery`. |
| `UserResolver` | `Func<string, string>?` | `null` | get; set; | Resolves the reply-to sender's ID to a display name. HTML attribute: `user-resolver`. |
| `ViewContext` | `ViewContext` | `null!` | get; set; | The current view context, bound automatically via `[ViewContext]`. Not bound to an HTML attribute. |

#### Methods

##### Process(TagHelperContext context, TagHelperOutput output)

**Returns:** `void`

Throws `InvalidOperationException` when `Endpoint` is null or whitespace, with the message `"The 'endpoint' attribute is required on <chat-input>."`

Renders a `<form>` with `method="post"` and `action` set to `Endpoint`. It contains, in order: an optional anti-forgery token hidden input resolved via `IAntiforgery` from `ViewContext.HttpContext.RequestServices`; a hidden input named `{Prefix}.ThreadId`; when `ReplyTo` is set, a hidden input named `{Prefix}.ReplyToMessageId` and a preview bar holding the icon dictionary's `Reply` glyph, the resolved sender name, the truncated message, and a dismiss button; then a row holding a textarea named `{Prefix}.Message` carrying `required`, `rows` and `maxlength`, and a submit button holding the `Send` glyph and `ButtonText`.

When `Prefix` is empty, the `.` separator is dropped and inputs are named `ThreadId`, `Message` and `ReplyToMessageId`.

The reply dismiss button carries no handler — cancelling a reply means re-rendering without `ReplyTo`.

---

### ChatParticipantsTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<chat-participants>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a participant list for a chat thread, showing initials in avatars. Constructor dependencies: `HtmlHelper`, `ICommunicationFrameworkDictionary`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Model` | `ChatModel` | `null!` | get; set; | The chat model whose participants to render. HTML attribute: `model`. |
| `MaxDisplay` | `int` | `5` | get; set; | Avatars shown before an overflow indicator. HTML attribute: `max-display`. |
| `AvatarSize` | `int` | `32` | get; set; | Avatar diameter in pixels. Font size is derived as `size / 2.5`, formatted with no decimal places. HTML attribute: `avatar-size`. |
| `UserResolver` | `Func<string, string>?` | `null` | get; set; | Resolves a user ID to a display name for initials and tooltips. HTML attribute: `user-resolver`. |
| `ContainerClass` | `string?` | `null` | get; set; | Overrides the container class entirely. Falls back to `ChatParticipantsClasses.Container`. HTML attribute: `container-class`. |

#### Methods

##### Process(TagHelperContext context, TagHelperOutput output)

**Returns:** `void`

Suppresses output when `Model` is null, `Model.Participants` is null, or the participant count is zero.

Otherwise renders a container holding one avatar per participant up to `MaxDisplay`. Each avatar carries an inline style setting width, height and font size from `AvatarSize`, a `title` with the resolved display name, and the participant's initials as content.

Initials are taken from the first character of the first and last space-separated parts of the resolved name and uppercased. A single-word name yields one initial; a name that splits into no parts yields `?`.

When the participant count exceeds `MaxDisplay`, an overflow element is appended showing `+{N}` with a `title` of `"{N} more participant"` or `"{N} more participants"`.

---

### ContactFormTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<contact-form>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a contact form with email, subject, and message fields, posting in the `ContactInputModel` shape. Constructor dependencies: `HtmlHelper`, `ICommunicationFrameworkDictionary`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Endpoint` | `string` | `null!` | get; set; | POST endpoint URL. Required. HTML attribute: `endpoint`. |
| `Heading` | `string` | `"Contact Us"` | get; set; | Heading rendered as an `<h4>`. When empty, no heading is rendered. HTML attribute: `heading`. |
| `ButtonText` | `string` | `"Send Message"` | get; set; | Submit button text. HTML attribute: `button-text`. |
| `ButtonColour` | `string?` | `null` | get; set; | Submit button colour. Falls back to `DefaultButtonColour`. HTML attribute: `button-colour`. |
| `Prefix` | `string` | `"Input"` | get; set; | Model binding prefix for input names. HTML attribute: `prefix`. |
| `EmailPlaceholder` | `string` | `"Your email address"` | get; set; | Email field placeholder. HTML attribute: `email-placeholder`. |
| `SubjectPlaceholder` | `string` | `"Subject"` | get; set; | Subject field placeholder. HTML attribute: `subject-placeholder`. |
| `MessagePlaceholder` | `string` | `"Your message"` | get; set; | Message textarea placeholder. HTML attribute: `message-placeholder`. |
| `MessageRows` | `int` | `5` | get; set; | Message textarea rows. HTML attribute: `message-rows`. |
| `IncludeAntiforgery` | `bool` | `true` | get; set; | Whether to include an anti-forgery token. HTML attribute: `antiforgery`. |
| `ViewContext` | `ViewContext` | `null!` | get; set; | The current view context, bound automatically via `[ViewContext]`. Not bound to an HTML attribute. |

#### Methods

##### Process(TagHelperContext context, TagHelperOutput output)

**Returns:** `void`

Throws `InvalidOperationException` when `Endpoint` is null or whitespace, with the message `"The 'endpoint' attribute is required on <contact-form>."`

Renders a `<form>` with `method="post"` and `action` set to `Endpoint`, containing: an optional anti-forgery token hidden input resolved via `IAntiforgery` from `ViewContext.HttpContext.RequestServices`; an optional `<h4>` heading; three field groups — an `email` input named `{Prefix}.Email` with id `contact-email`, a `text` input named `{Prefix}.Subject` with id `contact-subject`, and a textarea named `{Prefix}.Message` with id `contact-message` and `MessageRows` rows; and a submit button. All three inputs carry the `required` HTML attribute.

When `Prefix` is empty, the `.` separator is dropped and inputs are named `Email`, `Subject` and `Message`.

## Next steps

- [Setup](Communication.Web-Setup.md) — registration and framework selection.
- [Guide](Communication.Web-Guide.md) — usage examples and nuances.
