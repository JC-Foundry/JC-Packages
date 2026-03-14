# JC.Communication: Notifications — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project with JC.Core registered
- JC.Identity registered (`IUserInfo` must be available — notifications are user-scoped)
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

## 0. Add the package

Add a project reference to `JC.Communication`:

```xml
<ProjectReference Include="path/to/JC.Communication/JC.Communication.csproj" />
```

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>();

// Register notifications with default options
builder.Services.AddNotifications<AppDbContext>();
```

### DbContext

Your `DbContext` must implement `INotificationDbContext` and apply the notification data mappings:

```csharp
public class AppDbContext : IdentityDataDbContext, INotificationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationStyle> NotificationStyles { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyNotificationMappings();
    }
}
```

### Defaults

When called with no configuration callback, `AddNotifications` registers:

| Registration | Lifetime | Description |
|-------------|----------|-------------|
| `NotificationService` | Scoped | Data layer for notification persistence and querying |
| `NotificationLogService` | Scoped | Persists read/unread event logs |
| `NotificationCache` | Scoped | In-memory cache with automatic database hydration on miss |
| `NotificationSender` | Scoped | High-level service for sending notifications |
| `INotificationManager` → `NotificationManager` | Scoped | Orchestrates state changes (read, unread, dismiss) with logging and cache sync |
| `INotificationDbContext` → `TContext` | Scoped | Your DbContext as the notification data context |
| `IRepositoryContext<Notification>` | Scoped | Repository for notification entities |
| `IRepositoryContext<NotificationStyle>` | Scoped | Repository for notification style entities |
| `IRepositoryContext<NotificationLog>` | Scoped | Repository for notification log entities |

Default option values:

| Option | Default | Description |
|--------|---------|-------------|
| `CacheDurationHours` | `24` | In-memory cache TTL in hours |
| `LoggingMode` | `All` | Both read and unread events are logged |
| `HardDeleteOnDismiss` | `false` | Dismissing soft-deletes rather than hard-deletes |

### Automatic behaviour

With no additional configuration:

- **Cache**: Notifications are cached per user with a 24-hour TTL. On cache miss, the cache hydrates from the database automatically.
- **Logging**: Every read and unread event creates a `NotificationLog` entry.
- **Dismiss**: Dismissing a notification soft-deletes it (sets `IsDeleted = true`). Related `NotificationStyle` is also deleted.
- **User scoping**: All operations are automatically scoped to the current user via `IUserInfo.UserId`.
- **Expiration**: Notifications with an `ExpiresAtUtc` in the past are excluded from queries automatically.

## 2. Full configuration

### AddNotifications — standard registration

Registers all notification services with database logging support. Your DbContext must implement both `IDataDbContext` and `INotificationDbContext`.

```csharp
builder.Services.AddNotifications<AppDbContext>(options =>
{
    options.CacheDurationHours = 24;
    options.LoggingMode = NotificationLoggingMode.All;
    options.HardDeleteOnDismiss = false;
});
```

| Type parameter | Constraint | Description |
|---------------|-----------|-------------|
| `TContext` | `DbContext, IDataDbContext, INotificationDbContext` | Your application's DbContext |

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configure` | `Action<NotificationOptions>?` | `null` | Optional callback to configure notification options |

Throws `InvalidOperationException` if `IUserInfo` is not registered or if `CacheDurationHours` is outside the valid range (1–72).

### AddNotifications with custom INotificationManager

Use the two-type-parameter overload to replace the default `NotificationManager` with a custom implementation:

```csharp
builder.Services.AddNotifications<AppDbContext, CustomNotificationManager>(options =>
{
    options.CacheDurationHours = 24;
    options.LoggingMode = NotificationLoggingMode.All;
    options.HardDeleteOnDismiss = false;
});
```

| Type parameter | Constraint | Description |
|---------------|-----------|-------------|
| `TContext` | `DbContext, IDataDbContext, INotificationDbContext` | Your application's DbContext |
| `TNotificationManager` | `class, INotificationManager` | Your custom notification manager implementation |

`TNotificationManager` is registered as the scoped `INotificationManager` implementation instead of the built-in `NotificationManager`.

### NotificationOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CacheDurationHours` | `int` | `24` | In-memory cache TTL in hours. Must be between 1 and 72. |
| `LoggingMode` | `NotificationLoggingMode` | `All` | Controls which read/unread events are persisted to `NotificationLog`. |
| `HardDeleteOnDismiss` | `bool` | `false` | When `true`, dismissing a notification hard-deletes it and its related `NotificationStyle` and `NotificationLog` entries. When `false`, soft-deletes (sets `IsDeleted = true`). |

### NotificationLoggingMode

| Member | Value | Description |
|--------|-------|-------------|
| `None` | `0` | No read/unread events are logged. |
| `ReadOnly` | `1` | Only read events are logged. |
| `UnreadOnly` | `2` | Only unread events are logged. |
| `All` | `3` | Both read and unread events are logged. |

### ApplyNotificationMappings — entity configuration

Applies all notification entity mappings to the EF Core model builder. Call this in your DbContext's `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyNotificationMappings();
}
```

Configures the following entities:

| Entity | Primary key | Max lengths | Indexes |
|--------|------------|-------------|---------|
| `Notification` | `Id` (36 chars) | `Title` 255, `Body` 8192, `UserId` 36 | `UserId` |
| `NotificationStyle` | `NotificationId` (36 chars, FK) | `CustomColourClass` 128, `CustomIconClass` 128 | — |
| `NotificationLog` | `Id` (36 chars) | `NotificationId` 36, `UserId` 36 | `NotificationId`, `UserId` |

`Notification` has an optional one-to-one relationship with `NotificationStyle`, keyed by `NotificationStyle.NotificationId`.

### INotificationDbContext — database contract

Your DbContext must implement this interface to enable notification persistence:

```csharp
public class AppDbContext : DataDbContext, INotificationDbContext
{
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationStyle> NotificationStyles { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
}
```

## 3. Apply migrations

JC.Communication notifications introduce three tables: `Notifications`, `NotificationStyles`, and `NotificationLogs`. After setting up your DbContext, generate and apply the migration:

```bash
dotnet ef migrations add AddNotifications --project YourApp
dotnet ef database update --project YourApp
```

Alternatively, apply programmatically at startup:

```bash
dotnet ef migrations add AddNotifications --project YourApp
```

```csharp
await app.Services.MigrateDatabaseAsync<AppDbContext>();
```

## 4. Verify

1. Run the application.
2. Send a notification using `NotificationSender` (e.g. from a controller or service).
3. Query the `Notifications` table — you should see the notification with the target user ID, title, body, and type.

## Next steps

- [Guide](Notifications-Guide.md) — sending notifications, managing read state, caching behaviour, batch operations, and custom styling.
- [API Reference](Notifications-API.md)
