# JC.Communication: Notifications — API reference

Complete reference of all public types, properties, and methods in the JC.Communication notification system. See [Setup](Notifications-Setup.md) for registration and [Guide](Notifications-Guide.md) for usage examples.

> **Note:** Registration extensions (`IServiceCollection`, `IServiceProvider`, `IApplicationBuilder`) and options classes are documented in [Setup](Notifications-Setup.md), not here.

---

# Models

## Notification

**Namespace:** `JC.Communication.Notifications.Models`

Entity representing an in-app notification targeted at a specific user. Extends `AuditModel` for full audit trail support — inherits all audit properties (`CreatedById`, `CreatedUtc`, `LastModifiedById`, `LastModifiedUtc`, `DeletedById`, `DeletedUtc`, `IsDeleted`, `RestoredById`, `RestoredUtc`). See the [JC.Core API reference](../JC.Core/API.md#auditmodel).

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | `Guid.NewGuid().ToString()` | get; private set; | Unique identifier. Max 36 characters. |
| `Title` | `string` | — | get; set; | Notification title. Marked `required`. Max 255 characters. |
| `Body` | `string` | — | get; set; | Plain text body. Marked `required`. Max 8192 characters. |
| `BodyHtml` | `string?` | `null` | get; set; | Optional HTML body for rich rendering. |
| `UserId` | `string` | — | get; set; | Target user ID. Marked `required`. Max 36 characters. |
| `Type` | `NotificationType` | `Info` | get; set; | The notification type, used for default styling. |
| `IsRead` | `bool` | `false` | get; private set; | Whether the notification has been read. Managed by `Read()` and `Unread()`. |
| `ReadAtUtc` | `DateTime?` | `null` | get; private set; | UTC timestamp when the notification was read. Managed by `Read()` and `Unread()`. |
| `ExpiresAtUtc` | `DateTime?` | `null` | get; set; | Optional UTC expiration time. Expired notifications are excluded from queries. |
| `UrlLink` | `string?` | `null` | get; set; | Optional URL link for the notification. |
| `Style` | `NotificationStyle?` | `null` | get; set; | Optional custom styling. Navigation property. |

### Methods

#### Read()

**Returns:** `void`

Sets `IsRead` to `true` and `ReadAtUtc` to `DateTime.UtcNow`.

---

#### Unread()

**Returns:** `void`

Sets `IsRead` to `false` and `ReadAtUtc` to `null`.

---

## NotificationStyle

**Namespace:** `JC.Communication.Notifications.Models`

Optional custom UI styling for a notification, stored as a separate entity to avoid null column bloat. Extends `AuditModel`. Linked one-to-one with `Notification` via `NotificationId`. At least one of `CustomColourClass` or `CustomIconClass` must be set.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `NotificationId` | `string` | — | get; set; | Primary key and foreign key to `Notification`. Max 36 characters. |
| `Notification` | `Notification` | — | get; set; | Navigation property to the parent notification. |
| `CustomColourClass` | `string?` | `null` | get; set; | CSS colour class override. Max 128 characters. |
| `CustomIconClass` | `string?` | `null` | get; set; | CSS icon class override. Max 128 characters. |

---

## NotificationLog

**Namespace:** `JC.Communication.Logging.Models.Notifications`

Audit record for notification read/unread events. Extends `LogModel` — immutable once created. See the [JC.Core API reference](../JC.Core/API.md#logmodel).

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | `Guid.NewGuid().ToString()` | get; private set; | Unique log entry identifier. |
| `NotificationId` | `string` | — | get; set; | Foreign key to the notification. Marked `required`. Max 36 characters. |
| `Notification` | `Notification` | — | get; set; | Navigation property to the notification. |
| `Timestamp` | `DateTime` | — | get; | Read-only property returning `CreatedUtc`. |
| `UserId` | `string` | — | get; set; | The user who performed the read/unread action. Marked `required`. Max 36 characters. |
| `IsRead` | `bool` | `true` | get; set; | `true` for a read event, `false` for an unread event. |

---

## NotificationValidationResponse

**Namespace:** `JC.Communication.Notifications.Helpers`

The result of a notification validation operation.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `IsValid` | `bool` | — | get; | Whether validation passed. |
| `ValidatedNotification` | `Notification?` | — | get; | The validated notification entity, or `null` if validation failed. If a `NotificationStyle` was passed during validation, it is included on the notification's `Style` property. |
| `ErrorMessage` | `string?` | — | get; | Validation error message, or `null` if validation passed. |

### Constructors

#### NotificationValidationResponse(Notification notification)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `notification` | `Notification` | — | The validated notification. |

Creates a successful validation response with `IsValid = true`.

---

#### NotificationValidationResponse(string errorMessage)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `errorMessage` | `string` | — | The validation error message. |

Creates a failed validation response with `IsValid = false`.

---

# Enums

## NotificationType

**Namespace:** `JC.Communication.Notifications.Models`

Defines notification types used for default styling and semantic meaning.

| Member | Value | Description |
|--------|-------|-------------|
| `Message` | `0` | Direct message notification. |
| `Info` | `1` | Informational notification. |
| `Success` | `2` | Success notification. |
| `Warning` | `3` | Warning notification. |
| `Error` | `4` | Error notification. |
| `System` | `5` | System-level notification. |
| `Task` | `6` | Task-related notification. |

---

## NotificationLoggingMode

**Namespace:** `JC.Communication.Notifications.Models.Options`

Controls which notification read/unread events are persisted by `NotificationLogService`.

| Member | Value | Description |
|--------|-------|-------------|
| `None` | `0` | No events are logged. Required when using `AddNotifications` (without logging). |
| `ReadOnly` | `1` | Only read events are logged. |
| `UnreadOnly` | `2` | Only unread events are logged. |
| `All` | `3` | Both read and unread events are logged. |

---

# Services

## NotificationSender

**Namespace:** `JC.Communication.Notifications.Services`

High-level service for sending notifications with convenience methods. Handles validation, persistence, and cache synchronisation. Inject via `NotificationSender`.

### Methods

#### SendNotification(string userId, string title, string body, NotificationType type, string? htmlBody = null, string? link = null, DateTime? expiryUtc = null, string? colourClass = null, string? iconClass = null)

**Returns:** `Task<NotificationValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string` | — | Target user ID. Must be a valid GUID. |
| `title` | `string` | — | Notification title. Max 255 characters. |
| `body` | `string` | — | Plain text body. Max 8192 characters. |
| `type` | `NotificationType` | — | The notification type. |
| `htmlBody` | `string?` | `null` | Optional HTML body. |
| `link` | `string?` | `null` | Optional URL link. |
| `expiryUtc` | `DateTime?` | `null` | Optional UTC expiration time. |
| `colourClass` | `string?` | `null` | Optional CSS colour class override. |
| `iconClass` | `string?` | `null` | Optional CSS icon class override. |

Creates a `Notification` entity from the parameters. If `colourClass` or `iconClass` is provided, also creates a `NotificationStyle`. Validates the user ID, persists via `NotificationService.TryAddNotification`, and adds to the cache on success. Returns a validation response.

---

#### SendNotification(string userId, string title, string body, NotificationType type, TimeSpan expiryTimespan, string? htmlBody = null, string? link = null, string? colourClass = null, string? iconClass = null)

**Returns:** `Task<NotificationValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string` | — | Target user ID. |
| `title` | `string` | — | Notification title. |
| `body` | `string` | — | Plain text body. |
| `type` | `NotificationType` | — | The notification type. |
| `expiryTimespan` | `TimeSpan` | — | Expiry as a duration from now. Converted to `DateTime.UtcNow.Add(expiryTimespan)`. |
| `htmlBody` | `string?` | `null` | Optional HTML body. |
| `link` | `string?` | `null` | Optional URL link. |
| `colourClass` | `string?` | `null` | Optional CSS colour class override. |
| `iconClass` | `string?` | `null` | Optional CSS icon class override. |

Convenience overload that converts the `TimeSpan` to an absolute `DateTime` and delegates to the other `SendNotification` overload.

---

#### SendNotification(Notification notification, NotificationStyle? style = null)

**Returns:** `Task<NotificationValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `notification` | `Notification` | — | The pre-built notification entity. |
| `style` | `NotificationStyle?` | `null` | Optional pre-built style entity. |

Validates the notification's user ID, persists via `NotificationService.TryAddNotification`, and adds to the cache on success. Returns a validation response with an error message if the user ID is invalid or persistence fails.

---

#### SendNotifications(IEnumerable\<Notification\> notifications, params IEnumerable\<NotificationStyle\> styles)

**Returns:** `Task<(bool Result, List<NotificationValidationResponse> Responses)>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `notifications` | `IEnumerable<Notification>` | — | The notifications to send. |
| `styles` | `params IEnumerable<NotificationStyle>` | — | Optional styles to associate. Each style's `NotificationId` must match a notification in the batch. |

Validates all user IDs upfront — returns early if any are invalid. Matches styles to notifications by `NotificationId` and returns an error if a style references a notification not in the batch. Persists all notifications in a single transaction via `NotificationService.TryAddNotificationBatch`. On success, adds each notification to its respective user's cache.

---

## INotificationManager / NotificationManager

**Namespace:** `JC.Communication.Notifications.Services`

Orchestrates notification state changes (read, unread, dismiss) with persistence, event logging, and cache synchronisation. The default `NotificationManager` is registered as `INotificationManager`. Inject via `INotificationManager`.

### Methods

#### TryMarkAsReadAsync(string id, string? userId = null)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `string` | — | The notification ID. |
| `userId` | `string?` | `null` | Optional user ID for cross-user operations. The default `NotificationManager` throws `InvalidOperationException` if provided — implement a custom `INotificationManager` to enable cross-user access. |

Validates the current user ID, marks the notification as read via `NotificationService.MarkNotificationAsRead`, logs the read event via `NotificationLogService.LogReadAsync` (if logging mode permits), and updates the cache. Returns `true` on success; `false` if the user ID is invalid or the notification was not found.

---

#### TryMarkAsUnreadAsync(string id, string? userId = null)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `string` | — | The notification ID. |
| `userId` | `string?` | `null` | Optional user ID for cross-user operations. The default `NotificationManager` throws `InvalidOperationException` if provided. |

Validates the current user ID, marks the notification as unread via `NotificationService.UnmarkNotificationAsRead`, logs the unread event (if logging mode permits), and updates the cache. Returns `true` on success; `false` if the user ID is invalid or the notification was not found.

---

#### TryDismissAsync(string id, string? userId = null)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `string` | — | The notification ID. |
| `userId` | `string?` | `null` | Optional user ID for cross-user operations. The default `NotificationManager` throws `InvalidOperationException` if provided. |

Validates the current user ID, then deletes the notification via `NotificationService.TryDeleteNotification`. Uses soft delete or hard delete based on `NotificationOptions.HardDeleteOnDismiss`. Removes the notification from the cache on success. Returns `true` on success; `false` if the user ID is invalid or the notification was not found.

---

#### TryMarkAllAsReadAsync(string? userId = null)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string?` | `null` | Optional user ID for cross-user operations. The default `NotificationManager` throws `InvalidOperationException` if provided. |

Validates the current user ID, marks all unread, non-expired, active notifications as read for the current user, logs a read event for each updated notification individually, and updates the cache. Returns `true` if at least one notification was updated; `false` if the user ID is invalid or no notifications were updated.

---

#### TryMarkAllAsUnreadAsync(string? userId = null)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string?` | `null` | Optional user ID for cross-user operations. The default `NotificationManager` throws `InvalidOperationException` if provided. |

Validates the current user ID, marks all read, non-expired, active notifications as unread for the current user, logs an unread event for each updated notification individually, and updates the cache. Returns `true` if at least one notification was updated; `false` if the user ID is invalid or no notifications were updated.

---

#### TryDismissAllAsync(string? userId = null)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string?` | `null` | Optional user ID for cross-user operations. The default `NotificationManager` throws `InvalidOperationException` if provided. |

Validates the current user ID, deletes all active, non-expired notifications for the current user (soft or hard based on `NotificationOptions.HardDeleteOnDismiss`), and clears the user's cache. Returns `true` on success; `false` if the user ID is invalid or no notifications existed.

---

## NotificationService

**Namespace:** `JC.Communication.Notifications.Services`

Data layer service for notification persistence and querying. Handles CRUD operations, expiration filtering, and soft/hard deletion with transaction support. Inject via `NotificationService`.

### Methods

#### GetNotifications(bool orderByNewest = true, bool asNoTracking = true, string? userId = null, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)

**Returns:** `Task<List<Notification>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `orderByNewest` | `bool` | `true` | When `true`, orders by `CreatedUtc` descending; when `false`, ascending. |
| `asNoTracking` | `bool` | `true` | When `true`, uses no-tracking queries for read-only access. |
| `userId` | `string?` | `null` | Override user ID. When `null`, uses the current `IUserInfo.UserId`. |
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Controls soft-delete filtering. |

Returns all non-expired notifications for the user with `NotificationStyle` eagerly loaded. Returns an empty list if the user ID is invalid.

---

#### GetNotifications(int pageNumber, int pageSize, bool orderByNewest = true, string? userId = null, bool asNoTracking = true, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)

**Returns:** `Task<IPagination<Notification>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `pageNumber` | `int` | — | The 1-based page number. |
| `pageSize` | `int` | — | The number of items per page. |
| `orderByNewest` | `bool` | `true` | Ordering direction. |
| `userId` | `string?` | `null` | Override user ID. |
| `asNoTracking` | `bool` | `true` | No-tracking mode. |
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Soft-delete filtering. |

Paginated version of `GetNotifications`. Returns an `IPagination<Notification>` with page metadata.

---

#### GetExpiredNotifications(bool orderByNewest = true, bool asNoTracking = true, string? userId = null, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)

**Returns:** `Task<List<Notification>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `orderByNewest` | `bool` | `true` | Ordering direction. |
| `asNoTracking` | `bool` | `true` | No-tracking mode. |
| `userId` | `string?` | `null` | Override user ID. |
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Soft-delete filtering. |

Returns only expired notifications (where `ExpiresAtUtc` is set and has passed). These are excluded from regular `GetNotifications` queries.

---

#### GetExpiredNotifications(int pageNumber, int pageSize, bool orderByNewest = true, bool asNoTracking = true, string? userId = null, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)

**Returns:** `Task<IPagination<Notification>>`

Paginated version of `GetExpiredNotifications`.

---

#### GetNotificationById(string id, string? userId = null, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)

**Returns:** `Task<Notification?>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `string` | — | The notification ID. |
| `userId` | `string?` | `null` | Override user ID. |
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Soft-delete filtering. |

Returns a single tracked notification with `NotificationStyle` eagerly loaded, or `null` if not found or the user ID is invalid. Returns a tracked entity (not no-tracking) for use in subsequent update operations.

---

#### MarkNotificationAsRead(string id)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `string` | — | The notification ID. |

Fetches the notification as a tracked entity, calls `Notification.Read()`, and persists the change. Returns `true` on success; `false` if the notification was not found.

---

#### UnmarkNotificationAsRead(string id)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `string` | — | The notification ID. |

Fetches the notification as a tracked entity, calls `Notification.Unread()`, and persists the change. Returns `true` on success; `false` if the notification was not found.

---

#### MarkAllNotificationsAsRead(string? userId = null)

**Returns:** `Task<List<string>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string?` | `null` | Override user ID. |

Marks all unread, non-expired, active notifications as read for the user. Calls `Read()` on each and persists as a batch via `UpdateRangeAsync`. Returns the list of updated notification IDs. Returns an empty list if the user ID is invalid.

---

#### UnmarkAllNotificationsAsRead(string? userId = null)

**Returns:** `Task<List<string>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string?` | `null` | Override user ID. |

Marks all read, non-expired, active notifications as unread for the user. Returns the list of updated notification IDs.

---

#### TryAddNotification(Notification notification, NotificationStyle? style = null)

**Returns:** `Task<NotificationValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `notification` | `Notification` | — | The notification to persist. |
| `style` | `NotificationStyle?` | `null` | Optional style to persist alongside the notification. |

Validates the notification via `NotificationValidator.Validate`. On success, persists the notification and optional style in a database transaction. Returns a validation response with the persisted entity or an error message. Rolls back and logs the error if the transaction fails.

---

#### TryAddNotificationBatch(IEnumerable\<Notification\> notifications)

**Returns:** `Task<(bool Result, List<NotificationValidationResponse> Responses)>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `notifications` | `IEnumerable<Notification>` | — | The notifications to persist. Styles should be set on each notification's `Style` property before calling. |

Validates all notifications upfront. If any fail, returns early with all validation responses and `Result = false`. On success, extracts styles from the notification entities and persists everything in a single transaction. Returns `Result = true` with the list of validation responses.

---

#### TryDeleteNotification(string notificationId, bool softDelete = true)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `notificationId` | `string` | — | The notification ID. |
| `softDelete` | `bool` | `true` | When `true`, soft-deletes. When `false`, hard-deletes the notification, its style, and all related `NotificationLog` entries. |

Deletes the notification and its associated style in a transaction. For hard deletes, also deletes all related `NotificationLog` entries first to satisfy foreign key constraints. Returns `true` on success; `false` if the notification was not found or the transaction failed.

---

#### TryDeleteAllNotifications(bool softDelete = true, string? userId = null)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `softDelete` | `bool` | `true` | Soft or hard delete. |
| `userId` | `string?` | `null` | Override user ID. |

Deletes all active, non-expired notifications for the user in a transaction. For hard deletes, deletes all related `NotificationLog` entries first. Returns `true` on success; `false` if no notifications existed or the transaction failed.

---

#### TryRestoreNotification(string notificationId)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `notificationId` | `string` | — | The notification ID. |

Restores a soft-deleted notification and its associated style in a transaction. Returns `true` if the notification was restored or was already active. Returns `false` if the notification was not found or the transaction failed.

---

#### TryRestoreAllNotifications(string? userId = null)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string?` | `null` | Override user ID. |

Restores all soft-deleted notifications and their styles for the user in a transaction. Returns `true` if notifications were restored or none were deleted. Returns `false` if the user ID is invalid or the transaction failed.

---

## NotificationCache

**Namespace:** `JC.Communication.Notifications.Services`

In-memory cache layer for user notifications with configurable TTL. Hydrates from the database on cache miss. Mutation methods on this class update the cache only — `NotificationSender` and `NotificationManager` call these methods internally after persisting to the database. Inject via `NotificationCache`.

Cache key format: `JC.Notifications:{UserId}`

### Methods

#### GetNotificationsAsync(string? userId = null)

**Returns:** `Task<List<Notification>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string?` | `null` | Override user ID. |

Returns cached notifications for the user. On cache miss, hydrates from `NotificationService.GetNotifications` and sets the cache with the configured TTL. Returns an empty list if the user ID is invalid.

---

#### GetUnreadCountAsync(string? userId = null)

**Returns:** `Task<int>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string?` | `null` | Override user ID. |

Returns the count of unread notifications from the cache.

---

#### GetNotificationByIdAsync(string id, string? userId = null)

**Returns:** `Task<Notification?>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `string` | — | The notification ID. |
| `userId` | `string?` | `null` | Override user ID. |

Returns a single notification from the cache by ID, or `null` if not found.

---

#### AddNotificationAsync(Notification notification)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `notification` | `Notification` | — | The notification to add. |

Inserts the notification at the beginning of the cached list (newest first) for the notification's `UserId`.

---

#### UpdateReadStateAsync(string id, bool isRead, string? userId = null)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `string` | — | The notification ID. |
| `isRead` | `bool` | — | The new read state. |
| `userId` | `string?` | `null` | Override user ID. |

Updates the read state of a single notification in the cache. Calls `Read()` or `Unread()` on the cached entity. No-ops if the notification is not in the cache.

---

#### RemoveNotificationAsync(string id, string? userId = null)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `string` | — | The notification ID. |
| `userId` | `string?` | `null` | Override user ID. |

Removes a single notification from the cache. No-ops if the notification is not in the cache.

---

#### MarkAllAsReadAsync(string? userId = null)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string?` | `null` | Override user ID. |

Marks all unread notifications in the cache as read.

---

#### MarkAllAsUnreadAsync(string? userId = null)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string?` | `null` | Override user ID. |

Marks all read notifications in the cache as unread.

---

#### RemoveAllNotificationsAsync(string? userId = null)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string?` | `null` | Override user ID. |

Replaces the cached list with an empty list for the user. Preserves the cache entry to avoid database hydration on next access.

---

#### Invalidate(string? userId = null)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string?` | `null` | Override user ID. |

Completely removes the cache entry for the user, forcing a fresh database load on next access.

---

## NotificationLogService

**Namespace:** `JC.Communication.Logging.Services`

Handles persistence of notification read/unread event logs. Respects the configured `NotificationLoggingMode`. Inject via `NotificationLogService`.

### Methods

#### LogReadAsync(string notificationId, string? userId = null, CancellationToken cancellationToken = default)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `notificationId` | `string` | — | The notification ID to log. |
| `userId` | `string?` | `null` | Override user ID. When `null`, uses `IUserInfo.UserId`. |
| `cancellationToken` | `CancellationToken` | `default` | Cancellation token. |

Creates a `NotificationLog` entry with `IsRead = true`. Only logs if `LoggingMode` is `ReadOnly` or `All`. Catches and logs exceptions without rethrowing — logging failures do not propagate to the caller.

---

#### LogUnreadAsync(string notificationId, string? userId = null, CancellationToken cancellationToken = default)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `notificationId` | `string` | — | The notification ID to log. |
| `userId` | `string?` | `null` | Override user ID. |
| `cancellationToken` | `CancellationToken` | `default` | Cancellation token. |

Creates a `NotificationLog` entry with `IsRead = false`. Only logs if `LoggingMode` is `UnreadOnly` or `All`. Catches and logs exceptions without rethrowing.

---

# Helpers

## NotificationValidator

**Namespace:** `JC.Communication.Notifications.Helpers`

Static validation logic for notifications and styles.

### Methods

#### ValidateUserId(string? userId)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string?` | — | The user ID to validate. |

Returns `true` if the user ID is not null or whitespace, is not `UNKNOWN_USER_ID` or `SYSTEM_USER_ID` (case-insensitive comparison), and is a valid GUID.

---

#### Validate(Notification notification)

**Returns:** `NotificationValidationResponse`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `notification` | `Notification` | — | The notification to validate. |

Validates the notification's properties. If `notification.Style` is set, also validates the style. Returns a success response with the validated notification, or a failure response with error messages.

Validation rules:
- `Title` — required, max 255 characters
- `Body` — required, max 8192 characters
- `IsRead` — must be `false`
- `ReadAtUtc` — must be `null`
- `UserId` — must pass `ValidateUserId`

---

#### Validate(Notification notification, NotificationStyle style)

**Returns:** `NotificationValidationResponse`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `notification` | `Notification` | — | The notification to validate. |
| `style` | `NotificationStyle` | — | The style to validate. |

Validates both the notification and the style. The style must have at least one of `CustomColourClass` or `CustomIconClass` set. Returns a combined error message if either fails validation.

---

## NotificationUIHelper

**Namespace:** `JC.Communication.Notifications.Helpers`

Static helper providing default Bootstrap icon and colour class mappings for each `NotificationType`.

### Methods

#### GetIconClass(NotificationType type)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `type` | `NotificationType` | — | The notification type. |

Returns the default Bootstrap icon CSS class for the type:

| Type | Icon class |
|------|-----------|
| `Message` | `bi-chat-left-text` |
| `Info` | `bi-info-circle` |
| `Success` | `bi-check-circle` |
| `Warning` | `bi-exclamation-triangle` |
| `Error` | `bi-x-circle` |
| `System` | `bi-cpu` |
| `Task` | `bi-list-check` |
| Unknown | `""` |

---

#### GetColourClass(NotificationType type)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `type` | `NotificationType` | — | The notification type. |

Returns the default Bootstrap colour CSS class for the type:

| Type | Colour class |
|------|-------------|
| `Message` | `primary` |
| `Info` | `info` |
| `Success` | `success` |
| `Warning` | `warning` |
| `Error` | `danger` |
| `System` | `secondary` |
| `Task` | `primary` |
| Unknown | `secondary` |

---

# Data

## INotificationDbContext

**Namespace:** `JC.Communication.Notifications.Data`

Contract for the notification data context, exposing entity sets for notification persistence.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Notifications` | `DbSet<Notification>` | get; set; | The set of notifications. |
| `NotificationStyles` | `DbSet<NotificationStyle>` | get; set; | The set of notification styles. |
| `NotificationLogs` | `DbSet<NotificationLog>` | get; set; | The set of notification read/unread event logs. |
