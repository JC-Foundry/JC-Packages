# JC.Communication.Web — Guide

Tag helpers and models for rendering JC.Communication features in Razor views. Covers notifications (dropdown, badge, toasts), messaging (thread view, chat list, compose box, participants), and the contact form. All tag helpers use Bootstrap 5 classes and are self-closing (`TagStructure.WithoutEndTag`).

## Prerequisites

Add `@addTagHelper *, JC.Communication.Web` to your `_ViewImports.cshtml` to enable the tag helpers.

Notification tag helpers require `NotificationCache` (registered via `AddNotifications`). Messaging tag helpers require `ChatModel` or `List<ChatModel>` from `ChatThreadService`. The `<chat-list>` tag helper also requires `IRepositoryManager` and `IUserInfo` (registered via JC.Core and JC.Identity).

## Notifications

### Notification dropdown

Renders a bell button with a dropdown list of the current user's unread notifications. Notifications are retrieved from `NotificationCache` and ordered by creation date descending. Read notifications are excluded.

```razor
<notification-dropdown />
```

Customise the appearance and behaviour:

```razor
<notification-dropdown
    icon="bi-bell-fill"
    badge-colour="primary"
    max-height="400"
    dropdown-width="400"
    empty-text="You're all caught up!"
    body-max-length="100"
    view-all-href="/notifications"
    align="start" />
```

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `icon` | `string` | `"bi-bell"` | Bootstrap icon class for the bell button. |
| `badge-colour` | `string` | `"danger"` | Bootstrap colour class for the unread count badge. |
| `max-height` | `int` | `350` | Maximum height of the scrollable notification list in pixels. |
| `dropdown-width` | `int` | `360` | Dropdown menu width in pixels. |
| `empty-text` | `string` | `"No new notifications"` | Text shown when there are no unread notifications. |
| `body-max-length` | `int` | `80` | Maximum notification body length before truncation. |
| `view-all-href` | `string?` | `null` | URL for the "View all" footer link. If null, no footer is rendered. |
| `align` | `string` | `"end"` | Bootstrap dropdown alignment class (`"start"` or `"end"`). |

Each notification item shows an icon and colour based on `NotificationType`. Custom styling on a notification's `NotificationStyle` takes precedence over the type-based defaults from `NotificationUIHelper`. If the notification has a `UrlLink`, the item is rendered as an `<a>` tag instead of a `<div>`.

### Notification badge

Renders a lightweight unread notification count badge — use this when you only need the count, not the full dropdown.

```razor
<notification-badge />
```

```razor
<notification-badge
    icon="bi-bell-fill"
    badge-colour="primary"
    hide-when-zero="false" />
```

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `icon` | `string` | `"bi-bell"` | Bootstrap icon class. |
| `badge-colour` | `string` | `"danger"` | Bootstrap colour class for the badge pill. |
| `hide-when-zero` | `bool` | `true` | When `true`, only the icon is shown if the unread count is zero. |

The count is capped at `99+`. The badge uses Bootstrap's `position-absolute translate-middle` positioning.

### Notification toasts

Renders a fixed-position Bootstrap toast container for notification pop-ups. Each notification becomes a toast with a type-based icon, colour, title, timestamp, and body. Ideal for real-time notifications pushed via SignalR.

```razor
<notification-toast model="@notifications" />
```

```razor
<notification-toast
    model="@notifications"
    position="bottom-0 end-0"
    auto-hide="true"
    delay="8000"
    body-max-length="200"
    container-id="my-toasts" />
```

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `model` | `List<Notification>` | — | The notifications to render as toasts. Required. |
| `position` | `string` | `"top-0 end-0"` | Bootstrap position classes for the toast container. |
| `auto-hide` | `bool` | `true` | Whether toasts auto-hide after the delay. |
| `delay` | `int` | `5000` | Auto-hide delay in milliseconds. |
| `body-max-length` | `int` | `120` | Maximum body text length before truncation. |
| `container-id` | `string` | `"notification-toasts"` | HTML `id` attribute for the container element. |

When a notification has `BodyHtml` set, it is used as the toast body content instead of the truncated plain-text `Body`. If the notification has a `UrlLink`, the entire toast is wrapped in an `<a>` tag. A `<script>` block is emitted to auto-show all toasts using `bootstrap.Toast(t).show()`.

**Nuance:** Custom `NotificationStyle` properties (`CustomIconClass`, `CustomColourClass`) take precedence over the type-based defaults from `NotificationUIHelper`.

## Messaging

### Message thread

Renders a chat thread view showing messages with sender info, timestamps, and reply-to context. Sent and received messages are styled differently and positioned on opposite sides. The container has a configurable max height with auto-scroll to the latest message.

```razor
<message-thread
    model="@chat"
    current-user-id="@userInfo.UserId" />
```

```razor
<message-thread
    model="@chat"
    current-user-id="@userInfo.UserId"
    sent-colour="primary"
    received-colour="light"
    sent-text-colour="white"
    received-text-colour="dark"
    reply-truncate-length="60"
    max-height="600"
    container-class="d-flex flex-column gap-2 p-3"
    user-resolver="@(id => userService.GetDisplayName(id))" />
```

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `model` | `ChatModel` | — | The chat model to render. Required. |
| `current-user-id` | `string` | — | The current user's ID, used to distinguish sent vs received messages. Required. |
| `sent-colour` | `string` | `"primary"` | Bootstrap colour class for sent message bubbles. |
| `received-colour` | `string` | `"light"` | Bootstrap colour class for received message bubbles. |
| `sent-text-colour` | `string` | `"white"` | Bootstrap text colour class for sent messages. |
| `received-text-colour` | `string` | `"dark"` | Bootstrap text colour class for received messages. |
| `reply-truncate-length` | `int` | `60` | Maximum length of the reply-to preview before truncation. |
| `max-height` | `int` | `500` | Maximum height of the message container in pixels. Set to `0` for no limit. |
| `container-class` | `string` | `"d-flex flex-column gap-2 p-3"` | CSS class for the message container. |
| `user-resolver` | `Func<string, string>?` | `null` | Resolves a user ID to a display name. If null, the raw user ID is shown. |

The tag helper renders a header with the thread name, metadata icon/colour, and a member count badge for group chats. Messages are ordered by `SentAtUtc` ascending. When a message has a `ReplyToMessageId`, a preview of the original message is shown with a reply arrow icon. Sender names are only shown for received messages in group chats.

**Nuance:** When `max-height` is greater than `0`, the container gets `overflow-y:auto` styling and an inline `<script>` auto-scrolls to the bottom: `e.scrollTop = e.scrollHeight`. The container ID is `thread-{ThreadId}`.

### Chat list

Renders a list of chat thread previews with thread name, last message preview, last activity time, metadata (icon/image/colour), and optional unread message count badges.

```razor
<chat-list model="@chats" />
```

```razor
<chat-list
    model="@chats"
    href-format="/messages/{0}"
    preview-max-length="50"
    empty-text="No conversations yet"
    container-class="list-group"
    show-unread="true"
    unread-badge-colour="primary"
    user-resolver="@(id => userService.GetDisplayName(id))" />
```

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `model` | `List<ChatModel>` | — | The chat models to render. Required. |
| `href-format` | `string` | `"/chat/{0}"` | URL format for thread links. `{0}` is replaced with the thread ID. |
| `preview-max-length` | `int` | `50` | Maximum length of the last message preview before truncation. |
| `empty-text` | `string` | `"No conversations"` | Text shown when no chats exist. |
| `container-class` | `string` | `"list-group"` | CSS class for the container element. |
| `show-unread` | `bool` | `true` | Whether to show unread message count badges. |
| `unread-badge-colour` | `string` | `"primary"` | Bootstrap badge colour for unread counts. |
| `user-resolver` | `Func<string, string>?` | `null` | Resolves a user ID to a display name for the last message sender. |

Each thread item is rendered as an `<a>` tag with a `list-group-item-action` class. The avatar area shows the thread's metadata image, icon (with colour background), or a default person/people icon. The unread badge is capped at `99+`.

**Nuance:** Unread counts are computed by querying `MessageReadLog` entries for the current user across all threads in the model. For each thread, the tag helper finds the latest message the user has a read log for, then counts messages with a `SentAtUtc` after that point. If no read log exists for a thread, all messages are considered unread. This requires `IRepositoryManager` and `IUserInfo` to be injected — the tag helper uses `ProcessAsync` rather than `Process` to support the async database query.

### Chat input

Renders a message compose box with a textarea, send button, and optional reply-to preview bar. Posts to the configured endpoint as a form submission.

```razor
<chat-input
    endpoint="/api/messages/send"
    thread-id="@chat.ThreadId" />
```

With reply-to:

```razor
<chat-input
    endpoint="/api/messages/send"
    thread-id="@chat.ThreadId"
    reply-to="@selectedReplyMessage"
    user-resolver="@(id => userService.GetDisplayName(id))" />
```

Full configuration:

```razor
<chat-input
    endpoint="/api/messages/send"
    thread-id="@chat.ThreadId"
    reply-to="@replyMessage"
    reply-truncate-length="80"
    placeholder="Type a message..."
    rows="2"
    max-length="4096"
    button-text="Send"
    button-colour="primary"
    prefix="Input"
    antiforgery="true"
    user-resolver="@(id => userService.GetDisplayName(id))" />
```

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `endpoint` | `string` | — | POST endpoint URL for sending messages. Required. |
| `thread-id` | `string` | — | Thread ID included as a hidden input. Required. |
| `reply-to` | `MessageModel?` | `null` | The message being replied to. If set, a dismissible reply preview is shown and a hidden `ReplyToMessageId` input is included. |
| `reply-truncate-length` | `int` | `80` | Maximum length of the reply-to preview before truncation. |
| `placeholder` | `string` | `"Type a message..."` | Textarea placeholder text. |
| `rows` | `int` | `2` | Number of rows for the textarea. |
| `max-length` | `int` | `4096` | HTML `maxlength` attribute on the textarea. |
| `button-text` | `string` | `"Send"` | Send button text (shown next to a `bi-send` icon). |
| `button-colour` | `string` | `"primary"` | Bootstrap button colour class. |
| `prefix` | `string` | `"Input"` | Model binding prefix for input `name` attributes. |
| `antiforgery` | `bool` | `true` | Whether to include an anti-forgery token hidden input. |
| `user-resolver` | `Func<string, string>?` | `null` | Resolves the reply-to sender's user ID to a display name. |

The form posts with `method="post"` and includes hidden inputs for `{Prefix}.ThreadId` and optionally `{Prefix}.ReplyToMessageId`. The message textarea uses `{Prefix}.Message` as its name. The reply-to preview bar includes a close button with `btn-close` styling.

**Nuance:** Throws `InvalidOperationException` if `endpoint` is not set.

### Chat participants

Renders a horizontal list of participant avatars showing initials in coloured circles. When the number of participants exceeds the maximum display count, an overflow indicator (`+N`) is shown.

```razor
<chat-participants model="@chat" />
```

```razor
<chat-participants
    model="@chat"
    max-display="8"
    avatar-size="40"
    container-class="d-flex align-items-center gap-2"
    user-resolver="@(id => userService.GetDisplayName(id))" />
```

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `model` | `ChatModel` | — | The chat model whose participants to render. Required. |
| `max-display` | `int` | `5` | Maximum number of avatars to display before showing the overflow count. |
| `avatar-size` | `int` | `32` | Avatar circle size in pixels. Font size is automatically calculated as `size / 2.5`. |
| `container-class` | `string` | `"d-flex align-items-center gap-1"` | CSS class for the container element. |
| `user-resolver` | `Func<string, string>?` | `null` | Resolves a user ID to a display name for initials and tooltips. |

Initials are generated by splitting the display name by spaces and taking the first character of the first and last parts. Single-word names produce one initial. If the name is empty, `"?"` is shown. Each avatar has a `title` attribute with the full display name.

## Contact form

### Contact form

Renders a Bootstrap form with email, subject, and message fields. Posts to the configured endpoint using the `ContactInputModel` shape.

```razor
<contact-form endpoint="/api/contact" />
```

```razor
<contact-form
    endpoint="/api/contact"
    heading="Get in Touch"
    button-text="Submit"
    button-colour="success"
    prefix="ContactForm"
    email-placeholder="you@example.com"
    subject-placeholder="What is this about?"
    message-placeholder="Tell us more..."
    message-rows="8"
    antiforgery="true" />
```

| Attribute | Type | Default | Description |
|-----------|------|---------|-------------|
| `endpoint` | `string` | — | POST endpoint URL. Required. |
| `heading` | `string` | `"Contact Us"` | Form heading rendered as an `<h4>`. Set to empty to hide. |
| `button-text` | `string` | `"Send Message"` | Submit button text. |
| `button-colour` | `string` | `"primary"` | Bootstrap button colour class. |
| `prefix` | `string` | `"Input"` | Model binding prefix for input `name` attributes. |
| `email-placeholder` | `string` | `"Your email address"` | Email field placeholder. |
| `subject-placeholder` | `string` | `"Subject"` | Subject field placeholder. |
| `message-placeholder` | `string` | `"Your message"` | Message textarea placeholder. |
| `message-rows` | `int` | `5` | Number of rows for the message textarea. |
| `antiforgery` | `bool` | `true` | Whether to include an anti-forgery token. |

The form inputs are named `{Prefix}.Email`, `{Prefix}.Subject`, and `{Prefix}.Message`, matching the `ContactInputModel` properties. All fields have the `required` HTML attribute set.

### ContactInputModel

Bind this model to the form POST action to receive the submitted values:

```csharp
public async Task<IActionResult> OnPostAsync(ContactInputModel input)
{
    if (!ModelState.IsValid)
        return Page();

    // Send email using input.Email, input.Subject, input.Message
    return RedirectToPage("ThankYou");
}
```

The model includes `[Required]`, `[EmailAddress]`, and `[MaxLength]` validation attributes:

| Property | Type | Max length | Validation |
|----------|------|-----------|------------|
| `Email` | `string` | 256 | Required, valid email address. |
| `Subject` | `string` | 256 | Required. |
| `Message` | `string` | 8192 | Required. |

## User resolver pattern

Several tag helpers accept a `user-resolver` attribute — a `Func<string, string>` that converts a user ID to a display name. This is used for sender names, participant initials, and tooltips. Without a resolver, the raw user ID is displayed.

```razor
@inject IUserDisplayService userService

<message-thread
    model="@chat"
    current-user-id="@userInfo.UserId"
    user-resolver="@(id => userService.GetDisplayName(id))" />

<chat-list
    model="@chats"
    user-resolver="@(id => userService.GetDisplayName(id))" />

<chat-participants
    model="@chat"
    user-resolver="@(id => userService.GetDisplayName(id))" />

<chat-input
    endpoint="/api/messages/send"
    thread-id="@chat.ThreadId"
    reply-to="@replyMessage"
    user-resolver="@(id => userService.GetDisplayName(id))" />
```

The resolver is invoked synchronously for each user ID. If you need async lookups, pre-resolve the names into a dictionary and use that:

```razor
@{
    var nameMap = await userService.GetDisplayNamesAsync(chat.Participants.Select(p => p.UserId));
}

<message-thread
    model="@chat"
    current-user-id="@userInfo.UserId"
    user-resolver="@(id => nameMap.GetValueOrDefault(id, id))" />
```
