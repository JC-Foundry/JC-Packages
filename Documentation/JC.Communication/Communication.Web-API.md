# JC.Communication.Web — API reference

Complete reference for all public types in the JC.Communication.Web package. See [Guide](Communication.Web-Guide.md) for usage examples.

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

## Helpers

### NotificationDropdownTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<notification-dropdown>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a notification bell button with a dropdown list of the current user's unread notifications. Requires `NotificationCache` via constructor injection.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Icon` | `string` | `"bi-bell"` | get; set; | Bootstrap icon class for the bell button. HTML attribute: `icon`. |
| `BadgeColour` | `string` | `"danger"` | get; set; | Bootstrap colour class for the unread badge. HTML attribute: `badge-colour`. |
| `MaxHeight` | `int` | `350` | get; set; | Maximum height of the scrollable notification list in pixels. HTML attribute: `max-height`. |
| `DropdownWidth` | `int` | `360` | get; set; | Dropdown menu width in pixels. HTML attribute: `dropdown-width`. |
| `EmptyText` | `string` | `"No new notifications"` | get; set; | Text shown when there are no unread notifications. HTML attribute: `empty-text`. |
| `BodyMaxLength` | `int` | `80` | get; set; | Maximum notification body length before truncation. HTML attribute: `body-max-length`. |
| `ViewAllHref` | `string?` | `null` | get; set; | URL for the "View all" footer link. If null, no footer is rendered. HTML attribute: `view-all-href`. |
| `Align` | `string` | `"end"` | get; set; | Bootstrap dropdown alignment class. HTML attribute: `align`. |

#### Methods

##### ProcessAsync(TagHelperContext context, TagHelperOutput output)

**Returns:** `Task`

Retrieves notifications from `NotificationCache.GetNotificationsAsync()`, filters to unread only (`.Where(n => !n.IsRead)`), and orders by `CreatedUtc` descending. Renders a Bootstrap dropdown with a bell button, an unread count badge (capped at `99+`), and a scrollable list of notification items. Each item shows a type-based icon and colour (custom `NotificationStyle` takes precedence via `NotificationUIHelper`), a title, a truncated body, a relative timestamp, and an unread dot. Items with a `UrlLink` are rendered as `<a>` tags. When `ViewAllHref` is set, a divider and "View all" link are appended.

---

### NotificationBadgeTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<notification-badge>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a lightweight unread notification count badge. Requires `NotificationCache` via constructor injection.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Icon` | `string` | `"bi-bell"` | get; set; | Bootstrap icon class. HTML attribute: `icon`. |
| `BadgeColour` | `string` | `"danger"` | get; set; | Bootstrap badge colour class. HTML attribute: `badge-colour`. |
| `HideWhenZero` | `bool` | `true` | get; set; | When `true`, only the icon is shown if the unread count is zero. HTML attribute: `hide-when-zero`. |

#### Methods

##### ProcessAsync(TagHelperContext context, TagHelperOutput output)

**Returns:** `Task`

Retrieves the unread count from `NotificationCache.GetUnreadCountAsync()`. Renders a `<span>` containing the icon. When the count is zero and `HideWhenZero` is `true`, only the icon is output. Otherwise, a `position-absolute translate-middle badge rounded-pill` is appended with the count (capped at `99+`) and a `visually-hidden` accessibility label.

---

### NotificationToastTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<notification-toast>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a fixed-position Bootstrap toast container for notification pop-ups.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Model` | `List<Notification>` | `null!` | get; set; | The notifications to render as toasts. HTML attribute: `model`. |
| `Position` | `string` | `"top-0 end-0"` | get; set; | Bootstrap position classes for the container. HTML attribute: `position`. |
| `AutoHide` | `bool` | `true` | get; set; | Whether toasts auto-hide after the delay. HTML attribute: `auto-hide`. |
| `Delay` | `int` | `5000` | get; set; | Auto-hide delay in milliseconds. HTML attribute: `delay`. |
| `BodyMaxLength` | `int` | `120` | get; set; | Maximum body text length before truncation. HTML attribute: `body-max-length`. |
| `ContainerId` | `string` | `"notification-toasts"` | get; set; | HTML `id` attribute for the container element. HTML attribute: `container-id`. |

#### Methods

##### Process(TagHelperContext context, TagHelperOutput output)

**Returns:** `void`

Renders a `toast-container position-fixed` div containing one Bootstrap toast per notification. Each toast has a header with a type-based icon (custom `NotificationStyle` takes precedence), title, relative timestamp, and close button. The toast body uses `BodyHtml` when available, otherwise falls back to `Body` truncated to `BodyMaxLength`. If the notification has a `UrlLink`, the entire toast content is wrapped in an `<a>` tag. Each toast carries `data-bs-autohide` and `data-bs-delay` attributes. A `<script>` block is emitted after the container that calls `new bootstrap.Toast(t).show()` on each `.toast` element within the container.

---

### MessageThreadTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<message-thread>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a chat thread view showing messages with sender info, timestamps, and reply-to context.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Model` | `ChatModel` | `null!` | get; set; | The chat model to render. HTML attribute: `model`. |
| `CurrentUserId` | `string` | `null!` | get; set; | The current user's ID, used to distinguish sent vs received messages. HTML attribute: `current-user-id`. |
| `ReplyTruncateLength` | `int` | `60` | get; set; | Maximum length of the reply-to preview before truncation. HTML attribute: `reply-truncate-length`. |
| `SentColour` | `string` | `"primary"` | get; set; | Bootstrap colour class for sent message bubbles. HTML attribute: `sent-colour`. |
| `ReceivedColour` | `string` | `"light"` | get; set; | Bootstrap colour class for received message bubbles. HTML attribute: `received-colour`. |
| `SentTextColour` | `string` | `"white"` | get; set; | Bootstrap text colour class for sent messages. HTML attribute: `sent-text-colour`. |
| `ReceivedTextColour` | `string` | `"dark"` | get; set; | Bootstrap text colour class for received messages. HTML attribute: `received-text-colour`. |
| `UserResolver` | `Func<string, string>?` | `null` | get; set; | Resolves a user ID to a display name. If null, the raw user ID is shown. HTML attribute: `user-resolver`. |
| `ContainerClass` | `string` | `"d-flex flex-column gap-2 p-3"` | get; set; | CSS class for the message container. HTML attribute: `container-class`. |
| `MaxHeight` | `int` | `500` | get; set; | Maximum height of the message container in pixels. Set to `0` for no limit. HTML attribute: `max-height`. |

#### Methods

##### Process(TagHelperContext context, TagHelperOutput output)

**Returns:** `void`

Suppresses output if `Model` is null. Otherwise renders a thread header, a message container, and an optional auto-scroll script.

The header shows the thread's metadata image (as a 32px rounded circle) or icon, the chat name (coloured if metadata has a `Colour`), and a member count badge for group chats.

Messages are ordered by `SentAtUtc` ascending and built into a dictionary keyed by `MessageId` for reply-to lookups. Each message is rendered as a bubble with `max-width:75%`, aligned to the end for sent messages and the start for received messages. Sender names are only shown for received messages in group chats. When a message has a `ReplyToMessageId` that exists in the dictionary, a border-start preview is shown with a `bi-reply` icon, the original sender's name, and the truncated message body.

When `MaxHeight` is greater than `0`, the container div receives `max-height:{MaxHeight}px;overflow-y:auto;` styling and an `id` of `thread-{ThreadId}`. An inline `<script>` is emitted that sets `scrollTop = scrollHeight` on the container element.

---

### ChatListTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<chat-list>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a list of chat thread previews with optional unread message count badges. Requires `IRepositoryManager` and `IUserInfo` via constructor injection.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Model` | `List<ChatModel>` | `null!` | get; set; | The chat models to render. HTML attribute: `model`. |
| `HrefFormat` | `string` | `"/chat/{0}"` | get; set; | URL format for thread links. `{0}` is replaced with the URL-encoded thread ID. HTML attribute: `href-format`. |
| `PreviewMaxLength` | `int` | `50` | get; set; | Maximum length of the last message preview before truncation. HTML attribute: `preview-max-length`. |
| `EmptyText` | `string` | `"No conversations"` | get; set; | Text shown when no chats exist. HTML attribute: `empty-text`. |
| `ContainerClass` | `string` | `"list-group"` | get; set; | CSS class for the container element. HTML attribute: `container-class`. |
| `UserResolver` | `Func<string, string>?` | `null` | get; set; | Resolves a user ID to a display name for the last message sender. HTML attribute: `user-resolver`. |
| `ShowUnread` | `bool` | `true` | get; set; | Whether to show unread message count badges. HTML attribute: `show-unread`. |
| `UnreadBadgeColour` | `string` | `"primary"` | get; set; | Bootstrap badge colour for unread counts. HTML attribute: `unread-badge-colour`. |

#### Methods

##### ProcessAsync(TagHelperContext context, TagHelperOutput output)

**Returns:** `Task`

If `Model` is null or empty, renders a centred muted text div with `EmptyText`. Otherwise, computes unread counts (when `ShowUnread` is `true`) and renders the chat list.

Unread counts are computed by collecting all message IDs across all threads, querying `MessageReadLog` entries for the current user matching those message IDs, then for each thread finding the latest message with a read log (by `SentAtUtc`) and counting messages sent after that point. If no read log exists for a thread, all messages are counted as unread.

Each thread is rendered as an `<a>` tag with `list-group-item list-group-item-action` classes. The layout includes an avatar area (metadata image as a 40px circle, metadata icon with colour background, or a default `bi-person`/`bi-people` icon), a content area (thread name with optional colour, last activity time, and last message preview with sender name), and an optional unread badge (capped at `99+`, `rounded-pill` styling).

---

### ChatInputTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<chat-input>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a chat message compose box with a textarea, send button, and optional reply-to preview bar.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Endpoint` | `string` | `null!` | get; set; | POST endpoint URL for sending messages. Required. HTML attribute: `endpoint`. |
| `ThreadId` | `string` | `null!` | get; set; | Thread ID included as a hidden input. HTML attribute: `thread-id`. |
| `ReplyTo` | `MessageModel?` | `null` | get; set; | The message being replied to. If set, a reply preview and hidden `ReplyToMessageId` input are included. HTML attribute: `reply-to`. |
| `ReplyTruncateLength` | `int` | `80` | get; set; | Maximum length of the reply-to preview before truncation. HTML attribute: `reply-truncate-length`. |
| `Placeholder` | `string` | `"Type a message..."` | get; set; | Textarea placeholder text. HTML attribute: `placeholder`. |
| `Rows` | `int` | `2` | get; set; | Number of rows for the textarea. HTML attribute: `rows`. |
| `MaxLength` | `int` | `4096` | get; set; | HTML `maxlength` attribute on the textarea. HTML attribute: `max-length`. |
| `ButtonText` | `string` | `"Send"` | get; set; | Send button text (shown next to a `bi-send` icon). HTML attribute: `button-text`. |
| `ButtonColour` | `string` | `"primary"` | get; set; | Bootstrap button colour class. HTML attribute: `button-colour`. |
| `Prefix` | `string` | `"Input"` | get; set; | Model binding prefix for input `name` attributes. HTML attribute: `prefix`. |
| `IncludeAntiforgery` | `bool` | `true` | get; set; | Whether to include an anti-forgery token hidden input. HTML attribute: `antiforgery`. |
| `UserResolver` | `Func<string, string>?` | `null` | get; set; | Resolves the reply-to sender's user ID to a display name. HTML attribute: `user-resolver`. |
| `ViewContext` | `ViewContext` | `null!` | get; set; | The current view context, bound automatically via `[ViewContext]`. Not bound to an HTML attribute. |

#### Methods

##### Process(TagHelperContext context, TagHelperOutput output)

**Returns:** `void`

Throws `InvalidOperationException` if `Endpoint` is null or whitespace. Renders a `<form>` with `method="post"` and `action` set to `Endpoint`, with `p-3 border-top` classes.

The form contains: an optional anti-forgery token hidden input (resolved via `IAntiforgery` from `ViewContext.HttpContext.RequestServices`), a hidden input for `{Prefix}.ThreadId`, an optional reply-to section (hidden `{Prefix}.ReplyToMessageId` input and a preview bar with `bi-reply` icon, sender name, truncated message body, and a `btn-close` dismiss button), a textarea named `{Prefix}.Message` with the `required` attribute, and a submit button with a `bi-send` icon.

---

### ChatParticipantsTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<chat-participants>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a participant list for a chat thread, showing avatars with initials in coloured circles.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Model` | `ChatModel` | `null!` | get; set; | The chat model whose participants to render. HTML attribute: `model`. |
| `MaxDisplay` | `int` | `5` | get; set; | Maximum number of avatars to display before showing the overflow count. HTML attribute: `max-display`. |
| `AvatarSize` | `int` | `32` | get; set; | Avatar circle size in pixels. Font size is calculated as `size / 2.5`. HTML attribute: `avatar-size`. |
| `UserResolver` | `Func<string, string>?` | `null` | get; set; | Resolves a user ID to a display name for initials and tooltips. HTML attribute: `user-resolver`. |
| `ContainerClass` | `string` | `"d-flex align-items-center gap-1"` | get; set; | CSS class for the container element. HTML attribute: `container-class`. |

#### Methods

##### Process(TagHelperContext context, TagHelperOutput output)

**Returns:** `void`

Suppresses output if `Model` is null, `Participants` is null, or the participant count is zero. Otherwise renders a container div with participant avatars.

Each avatar is a `rounded-circle bg-primary-subtle text-primary` div sized to `AvatarSize` pixels, containing the participant's initials and a `title` attribute with their full display name. Initials are generated by splitting the resolved display name by spaces and taking the first character of the first and last parts (single-word names produce one initial; empty names produce `"?"`). All initials are converted to uppercase.

When the participant count exceeds `MaxDisplay`, an overflow indicator is rendered as a `rounded-circle bg-secondary-subtle` div showing `+N` with a tooltip stating `"{N} more participant(s)"`.

---

### ContactFormTagHelper

**Namespace:** `JC.Communication.Web.TagHelpers`

**Tag:** `<contact-form>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a Bootstrap contact form with email, subject, and message fields. Posts to the configured endpoint using the `ContactInputModel` shape.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Endpoint` | `string` | `null!` | get; set; | POST endpoint URL. Required. HTML attribute: `endpoint`. |
| `Heading` | `string` | `"Contact Us"` | get; set; | Form heading rendered as an `<h4>`. If empty, no heading is rendered. HTML attribute: `heading`. |
| `ButtonText` | `string` | `"Send Message"` | get; set; | Submit button text. HTML attribute: `button-text`. |
| `ButtonColour` | `string` | `"primary"` | get; set; | Bootstrap button colour class. HTML attribute: `button-colour`. |
| `Prefix` | `string` | `"Input"` | get; set; | Model binding prefix for input `name` attributes. HTML attribute: `prefix`. |
| `EmailPlaceholder` | `string` | `"Your email address"` | get; set; | Email field placeholder. HTML attribute: `email-placeholder`. |
| `SubjectPlaceholder` | `string` | `"Subject"` | get; set; | Subject field placeholder. HTML attribute: `subject-placeholder`. |
| `MessagePlaceholder` | `string` | `"Your message"` | get; set; | Message textarea placeholder. HTML attribute: `message-placeholder`. |
| `MessageRows` | `int` | `5` | get; set; | Number of rows for the message textarea. HTML attribute: `message-rows`. |
| `IncludeAntiforgery` | `bool` | `true` | get; set; | Whether to include an anti-forgery token. HTML attribute: `antiforgery`. |
| `ViewContext` | `ViewContext` | `null!` | get; set; | The current view context, bound automatically via `[ViewContext]`. Not bound to an HTML attribute. |

#### Methods

##### Process(TagHelperContext context, TagHelperOutput output)

**Returns:** `void`

Throws `InvalidOperationException` if `Endpoint` is null or whitespace. Renders a `<form>` with `method="post"` and `action` set to `Endpoint`.

The form contains: an optional anti-forgery token hidden input (resolved via `IAntiforgery` from `ViewContext.HttpContext.RequestServices`), an optional `<h4>` heading, three `mb-3` form groups (email input of `type="email"` named `{Prefix}.Email`, text input named `{Prefix}.Subject`, textarea named `{Prefix}.Message` with configurable rows), and a submit button. All inputs have the `required` HTML attribute. Input IDs are `contact-email`, `contact-subject`, and `contact-message`.
