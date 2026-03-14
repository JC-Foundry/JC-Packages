# JC.Communication: Notifications — Guide

Covers sending notifications, managing read state, dismissing, batch operations, querying, caching behaviour, custom styling, and validation. See [Setup](Notifications-Setup.md) for registration.

## Sending notifications

### Basic send

Inject `NotificationSender` to send notifications to users:

```csharp
public class OrderService(NotificationSender notifications)
{
    public async Task NotifyOrderShipped(string userId, string orderId)
    {
        var response = await notifications.SendNotification(
            userId: userId,
            title: "Order Shipped",
            body: $"Your order {orderId} has been shipped.",
            type: NotificationType.Success
        );

        if (!response.IsValid)
            logger.LogWarning("Failed to send notification: {Error}", response.ErrorMessage);
    }
}
```

`SendNotification` validates the notification, persists it to the database, and adds it to the in-memory cache. It returns a `NotificationValidationResponse` — check `IsValid` to confirm the notification was created successfully. The `ValidatedNotification` property contains the persisted entity (with its generated `Id`) on success.

### With optional parameters

```csharp
var response = await notifications.SendNotification(
    userId: userId,
    title: "Payment Due",
    body: "Your invoice is due in 7 days.",
    type: NotificationType.Warning,
    htmlBody: "<p>Your invoice is due in <strong>7 days</strong>.</p>",
    link: "/invoices/INV-001",
    expiryUtc: DateTime.UtcNow.AddDays(7),
    colourClass: "text-orange",
    iconClass: "bi-credit-card"
);
```

- `htmlBody` — an HTML version of the body for rich rendering. The plain `body` is always required as a fallback.
- `link` — a URL the user can navigate to from the notification.
- `expiryUtc` — notifications past their expiry are automatically excluded from queries.
- `colourClass` and `iconClass` — override the default Bootstrap styling for this notification's type. When either is provided, a `NotificationStyle` entity is created and linked to the notification.

### With a TimeSpan expiry

```csharp
var response = await notifications.SendNotification(
    userId: userId,
    title: "Session Expiring",
    body: "Your session will expire in 30 minutes.",
    type: NotificationType.Warning,
    expiryTimespan: TimeSpan.FromMinutes(30)
);
```

The `TimeSpan` overload converts to an absolute `DateTime` internally via `DateTime.UtcNow.Add(expiryTimespan)`.

### Sending a pre-built notification

For full control, construct the `Notification` entity directly:

```csharp
var notification = new Notification
{
    Title = "New Comment",
    Body = "Someone commented on your post.",
    UserId = userId,
    Type = NotificationType.Message,
    UrlLink = $"/posts/{postId}#comments"
};

var style = new NotificationStyle
{
    NotificationId = notification.Id,
    CustomIconClass = "bi-chat-dots"
};

var response = await notifications.SendNotification(notification, style);
```

**Nuance:** The `Notification.Id` is auto-generated as a GUID on construction. When creating a `NotificationStyle`, set its `NotificationId` to the notification's `Id` before sending.

### Batch sending

Send multiple notifications in a single transaction:

```csharp
var notificationA = new Notification
{
    Title = "System Maintenance",
    Body = "Scheduled maintenance at 2:00 AM UTC.",
    UserId = userIdA,
    Type = NotificationType.System
};

var notificationB = new Notification
{
    Title = "System Maintenance",
    Body = "Scheduled maintenance at 2:00 AM UTC.",
    UserId = userIdB,
    Type = NotificationType.System
};

var (success, responses) = await notifications.SendNotifications(
    [notificationA, notificationB]
);
```

All notifications are validated upfront. If any single notification fails validation, the entire batch is rejected — no notifications are persisted. On success, each notification is added to its respective user's cache.

To include styles in a batch, pass them as the second argument. Each style's `NotificationId` must match a notification in the batch:

```csharp
var style = new NotificationStyle
{
    NotificationId = notificationA.Id,
    CustomColourClass = "text-danger"
};

var (success, responses) = await notifications.SendNotifications(
    [notificationA, notificationB],
    [style]
);
```

## Managing read state

### Marking as read

Inject `INotificationManager` to manage notification state:

```csharp
public class NotificationController(INotificationManager manager) : Controller
{
    [HttpPost("notifications/{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        var result = await manager.TryMarkAsReadAsync(id);
        return result ? Ok() : NotFound();
    }
}
```

`TryMarkAsReadAsync` validates the current user, updates the notification's `IsRead` to `true` and `ReadAtUtc` to the current UTC time, logs the read event (if logging is enabled), and updates the cache. Returns `false` if the notification doesn't exist or the user ID is invalid.

### Marking as unread

```csharp
var result = await manager.TryMarkAsUnreadAsync(id);
```

Resets `IsRead` to `false` and clears `ReadAtUtc`. Logs an unread event if logging is configured for unread events.

### Bulk read/unread

```csharp
// Mark all notifications as read for the current user
await manager.TryMarkAllAsReadAsync();

// Mark all notifications as unread for the current user
await manager.TryMarkAllAsUnreadAsync();
```

Bulk operations update all applicable notifications in a single database call, then log each state change individually. Returns `false` if no notifications were updated (e.g. all were already in the target state).

## Dismissing notifications

### Single dismiss

```csharp
var result = await manager.TryDismissAsync(id);
```

Dismissing removes a notification from the user's view. The behaviour depends on the `HardDeleteOnDismiss` option configured in [Setup](Notifications-Setup.md#notificationoptions):

- **Soft delete** (default) — sets `IsDeleted = true` on the notification and its style. The data remains in the database and can be restored. Related `NotificationLog` entries are preserved.
- **Hard delete** — permanently removes the notification, its style, and all related `NotificationLog` entries from the database.

### Dismiss all

```csharp
await manager.TryDismissAllAsync();
```

Dismisses all active, non-expired notifications for the current user. Uses the same soft/hard delete behaviour as single dismiss.

## Querying notifications

There are two ways to read notifications: the **cache** (`NotificationCache`) and **direct database queries** (`NotificationService`). Choosing the right one depends on how frequently the query runs.

### Cache — for high-frequency reads

Use `NotificationCache` for anything that runs on every request or is visible on every page — notification banners, unread count badges, nav bar widgets, layout partials. The cache hydrates lazily from the database on first access per user, then serves subsequent requests from memory without a database round-trip.

**The cache only contains active notifications** — expired and dismissed (deleted) notifications are excluded. This means every read from the cache returns only notifications the user should currently see.

```csharp
public class NotificationWidgetViewComponent(NotificationCache cache) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var notifications = await cache.GetNotificationsAsync();
        var unreadCount = await cache.GetUnreadCountAsync();

        return View(new NotificationWidgetModel
        {
            Notifications = notifications,
            UnreadCount = unreadCount
        });
    }
}
```

You can also look up a single notification from the cache:

```csharp
var notification = await cache.GetNotificationByIdAsync(id);
```

Cache results are `IEnumerable<Notification>`, so you can paginate them in memory using JC.Core's pagination extensions:

```csharp
var notifications = await cache.GetNotificationsAsync();
var paged = notifications.ToPagedList(pageNumber: 1, pageSize: 10);
```

**This is the right default choice.** If your notification UI is rendered on every page load or polled frequently, always use the cache. Direct database queries on every request will create unnecessary load.

### Direct database queries — for dedicated pages and admin scenarios

Use `NotificationService` for dedicated notification pages, admin panels, filtered queries, and any scenario where you need query options that the cache doesn't support (soft-delete filtering, expired notification queries, cross-user lookups).

```csharp
public class NotificationPageModel(NotificationService notificationService) : PageModel
{
    public List<Notification> Notifications { get; set; } = [];

    public async Task OnGetAsync()
    {
        Notifications = await notificationService.GetNotifications();
    }
}
```

By default, `GetNotifications` returns all active (non-deleted), non-expired notifications for the current user, ordered newest first, with `NotificationStyle` eagerly loaded. This is appropriate for a dedicated notifications page that the user navigates to explicitly — it only runs when the page is loaded, not on every request.

### Controlling query behaviour

```csharp
// Oldest first
var oldest = await notificationService.GetNotifications(orderByNewest: false);

// Include soft-deleted notifications
var all = await notificationService.GetNotifications(
    deletedQueryType: DeletedQueryType.All
);

// Only soft-deleted notifications (e.g. for a "dismissed" view)
var dismissed = await notificationService.GetNotifications(
    deletedQueryType: DeletedQueryType.OnlyDeleted
);

// Query for a specific user (e.g. from an admin panel)
var userNotifications = await notificationService.GetNotifications(userId: targetUserId);
```

**Nuance:** When `userId` is not provided, all queries are automatically scoped to the current user via `IUserInfo.UserId`. The `userId` parameter overrides this for admin or system scenarios.

### Paginated queries

```csharp
var page = await notificationService.GetNotifications(
    pageNumber: 1,
    pageSize: 20
);

// page.Items       — notifications for this page
// page.TotalCount  — total matching notifications
// page.TotalPages  — calculated from TotalCount / PageSize
// page.HasNextPage — true if more pages exist
```

The paginated overload accepts the same optional parameters (`orderByNewest`, `userId`, `asNoTracking`, `deletedQueryType`).

### Querying expired notifications

```csharp
// Get all expired notifications (e.g. for cleanup)
var expired = await notificationService.GetExpiredNotifications();

// Paginated expired notifications
var expiredPage = await notificationService.GetExpiredNotifications(
    pageNumber: 1,
    pageSize: 50
);
```

Expired notifications are those where `ExpiresAtUtc` is set and has passed. These are excluded from regular `GetNotifications` queries but can be retrieved explicitly for audit or cleanup purposes.

### Looking up by ID

```csharp
var notification = await notificationService.GetNotificationById(id);

if (notification is null)
    return NotFound();
```

Optionally pass `userId` to query a specific user's notifications, or `deletedQueryType` to include soft-deleted entries (defaults to `OnlyActive`).

## Caching

### Cache key and TTL

Notifications are cached per user with the key format `JC.Notifications:{UserId}`. The TTL is configured via `NotificationOptions.CacheDurationHours` (default 24 hours) — see [Setup](Notifications-Setup.md#notificationoptions) for configuration.

### How the cache stays in sync

When you use `NotificationSender` and `INotificationManager`, the cache is updated automatically:

| Operation | Cache update |
|-----------|-------------|
| `SendNotification` | Notification added to cache |
| `TryMarkAsReadAsync` | Read state updated in cache |
| `TryMarkAsUnreadAsync` | Read state updated in cache |
| `TryDismissAsync` | Notification removed from cache |
| `TryMarkAllAsReadAsync` | All cache entries marked read |
| `TryMarkAllAsUnreadAsync` | All cache entries marked unread |
| `TryDismissAllAsync` | Cache cleared for user |

**Nuance:** If you modify notifications directly through the repository (bypassing `NotificationSender` and `INotificationManager`), the cache will be stale until it expires or is explicitly invalidated.

### Manual cache invalidation

```csharp
// Force the cache to reload from the database on next access
cache.Invalidate();

// Invalidate for a specific user
cache.Invalidate(userId: targetUserId);
```

## Custom styling

### Default type-based styling

Each `NotificationType` has default Bootstrap icon and colour mappings. Use `NotificationUIHelper` to resolve them in your UI:

```csharp
@foreach (var notification in Model.Notifications)
{
    var icon = NotificationUIHelper.GetIconClass(notification.Type);
    var colour = NotificationUIHelper.GetColourClass(notification.Type);

    <div class="notification text-@colour">
        <i class="bi @icon"></i>
        <strong>@notification.Title</strong>
        <p>@notification.Body</p>
    </div>
}
```

Default mappings:

| Type | Icon class | Colour class |
|------|-----------|-------------|
| `Message` | `bi-chat-left-text` | `primary` |
| `Info` | `bi-info-circle` | `info` |
| `Success` | `bi-check-circle` | `success` |
| `Warning` | `bi-exclamation-triangle` | `warning` |
| `Error` | `bi-x-circle` | `danger` |
| `System` | `bi-cpu` | `secondary` |
| `Task` | `bi-list-check` | `primary` |

### Per-notification style overrides

When a notification has a `NotificationStyle` attached, use its values instead of the defaults:

```csharp
var icon = notification.Style?.CustomIconClass
    ?? NotificationUIHelper.GetIconClass(notification.Type);

var colour = notification.Style?.CustomColourClass
    ?? NotificationUIHelper.GetColourClass(notification.Type);
```

A `NotificationStyle` is created automatically when you pass `colourClass` or `iconClass` to `NotificationSender.SendNotification`. You can also create one manually — at least one of `CustomColourClass` or `CustomIconClass` must be set (validation rejects styles with both empty).

## Soft-delete and restore

### Restoring dismissed notifications

If `HardDeleteOnDismiss` is `false` (the default), dismissed notifications can be restored:

```csharp
// Restore a single notification
var result = await notificationService.TryRestoreNotification(notificationId);

// Restore all dismissed notifications for the current user
var result = await notificationService.TryRestoreAllNotifications();

// Restore for a specific user
var result = await notificationService.TryRestoreAllNotifications(userId: targetUserId);
```

Restoring sets `IsDeleted = false` and populates the restore audit fields (`RestoredById`, `RestoredUtc`). Both the notification and its associated `NotificationStyle` are restored together in a transaction.

**Nuance:** `TryRestoreNotification` returns `true` if the notification is already active (not deleted). It only performs a restore operation on notifications that are actually soft-deleted.

## Validation

### User ID validation

All notification operations validate the target user ID. The following are rejected:

- `null` or whitespace
- `IUserInfo.UNKNOWN_USER_ID` or `IUserInfo.SYSTEM_USER_ID` — prevents notifications targeted at system or placeholder users
- Non-GUID strings — user IDs must be valid GUIDs. This also rejects `IUserInfo.MissingUserInfoId` (`"<NONE>"`), which is not a valid GUID

This validation runs automatically through `NotificationSender` and `INotificationManager`. You can also call it directly:

```csharp
if (!NotificationValidator.ValidateUserId(userId))
{
    // Invalid user — don't attempt to send
}
```

### Notification validation

New notifications are validated before persistence:

- `Title` — required, maximum 255 characters
- `Body` — required, maximum 8192 characters
- `IsRead` — must be `false` (new notifications always start unread)
- `ReadAtUtc` — must be `null`
- `UserId` — must pass user ID validation

If a `NotificationStyle` is attached, at least one of `CustomColourClass` or `CustomIconClass` must be non-empty.

Validation errors are returned in `NotificationValidationResponse.ErrorMessage` — they are never thrown as exceptions.

## Notification logging

### How logging works

When database logging is enabled (via `AddNotifications`), read and unread state changes create `NotificationLog` entries. Each log records the notification ID, the user who performed the action, a timestamp, and whether the event was a read or unread action.

Both `LogReadAsync` and `LogUnreadAsync` accept an optional `userId` parameter that defaults to the current user (`IUserInfo.UserId`). Pass a different user ID to record who actually performed the action — for example, when an admin reads a notification on behalf of a user but wants the log to reflect their own identity for audit purposes.

The logging mode controls which events are persisted — see [Setup](Notifications-Setup.md#notificationloggingmode) for configuration.

| Mode | Read events | Unread events |
|------|------------|---------------|
| `All` | Logged | Logged |
| `ReadOnly` | Logged | Skipped |
| `UnreadOnly` | Skipped | Logged |
| `None` | Skipped | Skipped |

**Nuance:** Logging is fire-and-forget — if a log entry fails to persist, the error is logged but the notification operation itself is not affected. This prevents logging failures from blocking user-facing operations.

### What is not logged

Notification creation, deletion, expiration, and restoration are not captured by `NotificationLog`. These operations are covered by JC.Core's audit trail via `AuditModel` — the notification and style entities inherit full audit fields (`CreatedById`, `LastModifiedById`, `DeletedById`, etc.).

## Custom INotificationManager

The built-in `NotificationManager` orchestrates persistence, logging, and caching. If you need different behaviour (e.g. SignalR push, external webhook), implement `INotificationManager`:

```csharp
public class SignalRNotificationManager(
    NotificationService notificationService,
    NotificationLogService logService,
    NotificationCache cache,
    IHubContext<NotificationHub> hub,
    IOptions<NotificationOptions> options,
    IUserInfo userInfo) : INotificationManager
{
    public async Task<bool> TryMarkAsReadAsync(string id, string? userId = null)
    {
        if (!NotificationValidator.ValidateUserId(userId ?? userInfo.UserId))
            return false;

        var result = await notificationService.MarkNotificationAsRead(id);
        if (!result) return false;

        await logService.LogReadAsync(id);
        await cache.UpdateReadStateAsync(id, isRead: true);

        // Push update to connected client
        await hub.Clients.User(userId ?? userInfo.UserId)
            .SendAsync("NotificationRead", id);

        return true;
    }

    // ...implement remaining INotificationManager methods
}
```

Register your implementation in [Setup](Notifications-Setup.md#addnotifications-with-custom-inotificationmanager):

```csharp
builder.Services.AddNotifications<AppDbContext, SignalRNotificationManager>();
```
