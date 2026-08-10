# JC.Communication.Web — Guide

Tag helpers for rendering JC.Communication features in Razor views: notifications (dropdown, badge, toasts), messaging (thread view, chat list, compose box, participants), and a contact form. Covers how class names and icons are chosen, every tag helper with examples, and the JavaScript each component expects. See [Setup](Communication.Web-Setup.md) for registration and framework selection.

All tag helpers are self-closing (`TagStructure.WithoutEndTag`).

## How class names and icons are chosen

No class name or icon is hardcoded in a tag helper. Each is read from two dictionaries resolved from the container:

- `ICommunicationFrameworkDictionary` — CSS classes, selected by the `UIFramework` passed to `AddCommunicationWeb` (or to `AddUI` / `AddWebDefaults`, whichever ran first).
- `ICommunicationIconDictionary` — icon classes, selected by the `IconFramework`, which is an independent choice. A Tailwind application may still use Bootstrap Icons.

The practical consequence is that the components' colour and icon attributes are **nullable**. Left unset they fall back to the dictionary, which is what makes the same markup work under every framework:

```razor
@* Uses whatever the configured framework calls its danger colour *@
<notification-badge />

@* Bootstrap: bg-danger · Tailwind: bg-red-600 · jc-tailwind-ui: tone-danger *@
<notification-badge badge-colour="danger" />
```

Set a colour explicitly and you are writing that framework's vocabulary, so the value has to match the framework you registered. See [Contextual colours change meaning per framework](Communication.Web-Setup.md#contextual-colours-change-meaning-per-framework).

### Icon values are complete class attributes

An icon value is the **whole** class attribute, not a glyph suffix — `"bi bi-bell"` under Bootstrap Icons, `"fa-solid fa-bell"` under Font Awesome, which share no base class.

Caller-supplied values are normalised against the configured set's base class, so under Bootstrap Icons both forms work:

```razor
<notification-dropdown icon="bi-star" />      @* normalised to "bi bi-star" *@
<notification-dropdown icon="bi bi-star" />   @* left as-is *@
```

**Nuance:** Font Awesome has no base class, so nothing is prepended and the value is taken exactly as given. A `NotificationStyle.CustomIconClass` stored as `"bi-star"` therefore renders as `"bi-star"` under Font Awesome and shows no glyph. Stored icon values need migrating when the icon set changes.

## Notifications

### Notification dropdown

Renders a bell button with a dropdown of the current user's unread notifications, read from `NotificationCache`, filtered to unread and ordered by creation date descending.

```razor
<notification-dropdown />
```

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
| `icon` | `string?` | `null` | Icon class for the bell. Falls back to the icon dictionary's bell. |
| `badge-colour` | `string?` | `null` | Colour for the unread count badge and the per-item unread dot. Falls back to the dictionary's default, which is `danger` under Bootstrap. |
| `max-height` | `int` | `350` | Maximum height of the scrollable notification list in pixels. |
| `dropdown-width` | `int` | `360` | Dropdown menu width in pixels. |
| `empty-text` | `string` | `"No new notifications"` | Text shown when there are no unread notifications. |
| `body-max-length` | `int` | `80` | Maximum notification body length before truncation. |
| `view-all-href` | `string?` | `null` | URL for the "View all" footer link. When null, no footer or divider is rendered. |
| `align` | `string?` | `null` | Menu alignment. Falls back to the dictionary's default, which is `end` under Bootstrap. |

Each item shows an icon and colour derived from the notification's `NotificationType`. A notification's `NotificationStyle.CustomIconClass` and `CustomColourClass` override those defaults individually — set one and the other still comes from the type. Items with a `UrlLink` render as `<a>` rather than `<div>`.

The count badge is only rendered when there is at least one unread notification, and is capped at `99+`.

**Nuance:** `badge-colour` drives both the count badge and each item's unread dot, so they cannot be coloured separately.

### Notification badge

The count on its own, for when you do not need the dropdown.

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
| `icon` | `string?` | `null` | Icon class. Falls back to the icon dictionary's bell. |
| `badge-colour` | `string?` | `null` | Badge colour. Falls back to the dictionary's default, which is `danger` under Bootstrap. |
| `hide-when-zero` | `bool` | `true` | When `true`, the badge is omitted entirely if the unread count is zero. |

The count is capped at `99+`, and a visually-hidden "unread notifications" label is appended for screen readers.

**Nuance:** when the count is zero and `hide-when-zero` is `true`, the wrapping `<span>` is emitted **without** the positioning class — there is nothing to position against. Any layout you hang off that class needs to tolerate its absence, or set `hide-when-zero="false"`.

### Notification toasts

Renders a stack of toasts, one per notification. Suited to real-time notifications pushed over SignalR.

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
| `model` | `List<Notification>` | — | The notifications to render. Required. |
| `position` | `string?` | `null` | Container position. Falls back to the dictionary's default, which is `top-0 end-0` under Bootstrap. |
| `auto-hide` | `bool` | `true` | Emitted as `data-bs-autohide`. |
| `delay` | `int` | `5000` | Auto-hide delay in milliseconds, emitted as `data-bs-delay`. |
| `body-max-length` | `int` | `120` | Maximum body length before truncation. |
| `container-id` | `string` | `"notification-toasts"` | HTML `id` for the container element. |

Each toast carries a type-based icon and colour, a title, a relative timestamp and a close button. A notification with a `UrlLink` has its whole content wrapped in an `<a>`.

**Nuance:** when a notification has `BodyHtml` set, it is written into the toast **unencoded** — that is the point of the property, and it is why `BodyHtml` must never hold unsanitised user input. Without it, `Body` is truncated and HTML-encoded as normal. Sanitise on write with JC.Web's [`ContentSanitiser`](../JC.Web/UI-Guide.md#sanitising-user-html) if the value can originate from a user.

**Nuance:** `model` is the only attribute that is not null-checked into a suppressed output — a null model renders an empty container rather than nothing.

## Messaging

### Message thread

Renders a thread: a header, the messages in `SentAtUtc` order, and an auto-scroll script.

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
| `model` | `ChatModel` | — | The chat to render. Required — output is suppressed entirely when null. |
| `current-user-id` | `string` | — | Distinguishes sent from received messages. Required. |
| `sent-colour` | `string?` | `null` | Background for sent bubbles. Falls back to the dictionary's default, `primary` under Bootstrap. |
| `received-colour` | `string?` | `null` | Background for received bubbles. Falls back to `light` under Bootstrap. |
| `sent-text-colour` | `string?` | `null` | Text colour for sent bubbles. Falls back to `white` under Bootstrap. |
| `received-text-colour` | `string?` | `null` | Text colour for received bubbles. Falls back to `dark` under Bootstrap. |
| `reply-truncate-length` | `int` | `60` | Maximum length of the reply-to preview before truncation. |
| `max-height` | `int` | `500` | Maximum container height in pixels. Set to `0` for no limit. |
| `container-class` | `string?` | `null` | Overrides the container class entirely. Falls back to the dictionary's. |
| `user-resolver` | `Func<string, string>?` | `null` | Resolves a user ID to a display name. Without it, the raw ID is shown. |

The header shows the thread's metadata image or icon, the chat name (tinted when metadata carries a `Colour`), and a member count badge for group chats. Sender names appear only on received messages in group chats.

**Nuance:** under jc-tailwind-ui a bubble takes both its fill and its text colour from the tone, so `sent-text-colour` and `received-text-colour` have no effect there. The tone carries its own readable foreground, which is what lets a custom colour work without a second legibility decision.

**Nuance:** a reply preview is only rendered when the replied-to message is **in the same model**. `ReplyToMessageId` is looked up against the messages loaded into this thread, so a reply to a message outside the loaded window renders as an ordinary message with no quote.

**Nuance:** when `max-height` is greater than `0`, the container gets `overflow-y:auto` and an inline script sets `scrollTop = scrollHeight`. The container's id is `thread-{ThreadId}`. With `max-height="0"` no script is emitted and the thread grows to fit.

### Chat list

Renders thread previews with name, last message, activity time, metadata and optional unread counts.

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
| `model` | `List<ChatModel>` | — | The chats to render. Required. |
| `href-format` | `string` | `"/chat/{0}"` | Link format. `{0}` is replaced with the URL-encoded thread ID. |
| `preview-max-length` | `int` | `50` | Maximum last-message preview length before truncation. |
| `empty-text` | `string` | `"No conversations"` | Text shown when the model is null or empty. |
| `container-class` | `string?` | `null` | Overrides the container class entirely. Falls back to the dictionary's. |
| `show-unread` | `bool` | `true` | Whether to compute and show unread count badges. |
| `unread-badge-colour` | `string?` | `null` | Unread badge colour. Falls back to `primary` under Bootstrap. |
| `user-resolver` | `Func<string, string>?` | `null` | Resolves the last message sender's ID to a display name. |

The avatar falls back in three steps: the thread metadata's image, then its icon on a coloured background, then a person or people glyph from the icon dictionary depending on `IsGroupChat`.

**Nuance:** unread counts hit the database. For each thread the tag helper finds the newest message the current user has a `MessageReadLog` for, then counts messages sent after it; a thread with no read log counts as entirely unread. This runs one query across all threads in the model, but it does mean `<chat-list>` needs `IRepositoryManager` and `IUserInfo`, and that it renders asynchronously. Set `show-unread="false"` to skip the query entirely.

**Nuance:** counts are computed from `chat.Messages` on the model you pass in, not from the database. If you load threads with only the latest message per thread, every count will be at most one.

### Chat input

A compose box that posts to your endpoint as an ordinary form.

```razor
<chat-input
    endpoint="/api/messages/send"
    thread-id="@chat.ThreadId" />
```

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
| `endpoint` | `string` | — | POST endpoint. Required. |
| `thread-id` | `string` | — | Thread ID, included as a hidden input. Required. |
| `reply-to` | `MessageModel?` | `null` | The message being replied to. When set, a preview bar and a hidden `ReplyToMessageId` input are rendered. |
| `reply-truncate-length` | `int` | `80` | Maximum reply preview length before truncation. |
| `placeholder` | `string` | `"Type a message..."` | Textarea placeholder. |
| `rows` | `int` | `2` | Textarea rows. |
| `max-length` | `int` | `4096` | Textarea `maxlength`. |
| `button-text` | `string` | `"Send"` | Send button text, rendered beside the icon dictionary's send glyph. |
| `button-colour` | `string?` | `null` | Send button colour. Falls back to `primary` under Bootstrap. |
| `prefix` | `string` | `"Input"` | Model binding prefix for input names. |
| `antiforgery` | `bool` | `true` | Whether to include an anti-forgery token hidden input. |
| `user-resolver` | `Func<string, string>?` | `null` | Resolves the reply-to sender's ID to a display name. |

Inputs are named `{prefix}.ThreadId`, `{prefix}.Message` and, when replying, `{prefix}.ReplyToMessageId`. Set `prefix=""` to drop the prefix entirely and bind to top-level parameters.

**Nuance:** throws `InvalidOperationException` when `endpoint` is null or whitespace. This is a startup-visible failure only if the page renders, so exercise the view in a smoke test.

**Nuance:** the reply preview's dismiss button is markup only. It carries no handler — cancelling a reply means re-rendering without `reply-to`, which is your page's job.

### Chat participants

Avatars with initials, and an overflow count past a limit.

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
| `model` | `ChatModel` | — | The chat whose participants to render. Required — output is suppressed when null, or when there are no participants. |
| `max-display` | `int` | `5` | Avatars shown before an overflow indicator. |
| `avatar-size` | `int` | `32` | Avatar diameter in pixels. Font size is derived as `size / 2.5`. |
| `container-class` | `string?` | `null` | Overrides the container class entirely. Falls back to the dictionary's. |
| `user-resolver` | `Func<string, string>?` | `null` | Resolves a user ID to a display name for initials and tooltips. |

Initials come from the first character of the first and last space-separated parts of the resolved name, uppercased. A single-word name gives one initial; an empty name gives `?`. Each avatar carries a `title` with the full name.

**Nuance:** under jc-tailwind-ui the container is that framework's `avatar-group`, which overlaps the avatars rather than spacing them. It is the component the framework ships for this; pass `container-class` if you want the spaced row instead.

## Contact form

Renders email, subject and message fields posting to your endpoint in the `ContactInputModel` shape.

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
| `endpoint` | `string` | — | POST endpoint. Required. |
| `heading` | `string` | `"Contact Us"` | Heading rendered as an `<h4>`. Set to empty to omit it. |
| `button-text` | `string` | `"Send Message"` | Submit button text. |
| `button-colour` | `string?` | `null` | Submit button colour. Falls back to `primary` under Bootstrap. |
| `prefix` | `string` | `"Input"` | Model binding prefix for input names. |
| `email-placeholder` | `string` | `"Your email address"` | Email field placeholder. |
| `subject-placeholder` | `string` | `"Subject"` | Subject field placeholder. |
| `message-placeholder` | `string` | `"Your message"` | Message textarea placeholder. |
| `message-rows` | `int` | `5` | Message textarea rows. |
| `antiforgery` | `bool` | `true` | Whether to include an anti-forgery token. |

Inputs are named `{prefix}.Email`, `{prefix}.Subject` and `{prefix}.Message`, with ids `contact-email`, `contact-subject` and `contact-message`. All three carry the `required` HTML attribute.

Bind `ContactInputModel` in the receiving handler:

```csharp
public async Task<IActionResult> OnPostAsync(ContactInputModel input)
{
    if (!ModelState.IsValid)
        return Page();

    await email.SendAsync(
        [new EmailRecipient("support@example.com")],
        input.Subject,
        $"From {input.Email}\n\n{input.Message}");

    return RedirectToPage("ThankYou");
}
```

| Property | Type | Max length | Validation |
|----------|------|-----------|------------|
| `Email` | `string` | 256 | Required, valid email address. |
| `Subject` | `string` | 256 | Required. |
| `Message` | `string` | 8192 | Required. |

**Nuance:** throws `InvalidOperationException` when `endpoint` is null or whitespace, as `<chat-input>` does.

## JavaScript these components expect

Three components render markup that needs behaviour attached. What is supplied depends on the configured framework.

| Component | Behaviour | Under Bootstrap | Under any other framework |
|---|---|---|---|
| `<notification-dropdown>` | Opening the menu | `data-bs-toggle="dropdown"` on the bell, driven by Bootstrap's JS | You supply it, or the framework does — see below |
| `<notification-toast>` | Showing each toast | An auto-show script calling `new bootstrap.Toast(t).show()` is emitted | **No script is emitted.** You supply the equivalent |
| `<notification-toast>` | Dismissing a toast | `data-bs-dismiss="toast"` on the close button, driven by Bootstrap's JS | You supply it |

### Why the `data-bs-*` attributes stay

These attributes are emitted under every framework, not just Bootstrap. Renaming them per framework would mean shipping JavaScript for Bootstrap users, who already have working behaviour from Bootstrap's own bundle. They are a documented contract in two categories:

- **Declarative attributes** — `data-bs-toggle`, `data-bs-dismiss`, `data-bs-autohide`, `data-bs-delay`. A non-Bootstrap application shadows these with a handler of its own. They are inert markup until something reads them.
- **The auto-show script** — this is the exception, because it depends on the `bootstrap` global and would throw a `ReferenceError` without it. It is therefore **omitted** whenever the configured framework is not Bootstrap, and needs replacing rather than shadowing.

A minimal shadow for the toast, for any non-Bootstrap framework:

```html
<script type="module">
  document.querySelectorAll('[data-bs-dismiss="toast"]').forEach(btn =>
      btn.addEventListener('click', () => btn.closest('.toast')?.remove()));
</script>
```

### Under jc-tailwind-ui

The notification dropdown needs nothing: the bell carries that framework's own `dropdown-toggle` class, which is the selector its `ui.js` delegates on, so `initUI()` drives the menu.

```html
<script type="module">
  import { initUI } from "/js/ui.js";
  initUI();
</script>
```

Toasts still need the dismiss shadow above — the framework's `ui.js` listens for `data-dismiss`, not `data-bs-dismiss`. Alternatively, skip `<notification-toast>` and call the framework's own `toast()` function from your real-time handler.

## User resolver pattern

`<message-thread>`, `<chat-list>`, `<chat-participants>` and `<chat-input>` each accept a `user-resolver` — a `Func<string, string>` mapping a user ID to a display name. Without one, the raw ID is shown.

```razor
@inject IUserDisplayService userService

<message-thread
    model="@chat"
    current-user-id="@userInfo.UserId"
    user-resolver="@(id => userService.GetDisplayName(id))" />

<chat-participants
    model="@chat"
    user-resolver="@(id => userService.GetDisplayName(id))" />
```

The resolver is invoked synchronously, once per user ID encountered — including repeats. For an async lookup, or to avoid a per-message call, resolve into a dictionary first:

```razor
@{
    var nameMap = await userService.GetDisplayNamesAsync(
        chat.Participants.Select(p => p.UserId));
}

<message-thread
    model="@chat"
    current-user-id="@userInfo.UserId"
    user-resolver="@(id => nameMap.GetValueOrDefault(id, id))" />
```

## Next steps

- [Setup](Communication.Web-Setup.md) — registration, framework selection, and per-framework requirements.
- [API Reference](Communication.Web-API.md)
