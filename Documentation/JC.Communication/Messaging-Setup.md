# JC.Communication: Messaging — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project with JC.Core registered
- JC.Identity registered (`IUserInfo` must be available — messaging is user-scoped)
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

// Register messaging with default options
builder.Services.AddMessaging<AppDbContext>();

// Optional: override background job defaults
builder.Services.ConfigureMessagingBackgroundJobs(options =>
{
    options.ActivityLogRetentionMonths = 6;
    options.ReadLogRetentionMonths = 6;
});
```

### DbContext

Your `DbContext` must implement `IMessagingDbContext` and apply the messaging data mappings:

```csharp
public class AppDbContext : IdentityDataDbContext, IMessagingDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ChatThread> ChatThreads { get; set; }
    public DbSet<ThreadDeleted> DeletedThreads { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ChatParticipant> ChatParticipants { get; set; }
    public DbSet<ChatMetadata> ChatMetadata { get; set; }
    public DbSet<ThreadActivityLog> ThreadActivityLogs { get; set; }
    public DbSet<MessageReadLog> MessageReadLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyMessagingMappings();
    }
}
```

### Defaults

When called with no configuration callback, `AddMessaging` registers:

| Registration | Lifetime | Description |
|---|---|---|
| `ChatThreadService` | Scoped | Thread creation, querying, deletion (per-user and for-all), and restore |
| `ChatMessageService` | Scoped | Sending messages, editing, soft-delete, and reply-to |
| `ChatParticipantService` | Scoped | Adding, removing, and managing thread participants |
| `ChatMetadataService` | Scoped | Thread visual metadata (icon, image, colour) |
| `MessagingValidationService` | Scoped | Internal validation shared across messaging services |
| `MessagingLogService` | Scoped | Thread activity logging and message read tracking |
| `IMessagingDbContext` → `TContext` | Scoped | Your DbContext as the messaging data context |
| `IRepositoryContext<T>` (×7) | Scoped | Repositories for ChatThread, ChatMessage, ChatParticipant, ChatMetadata, ThreadDeleted, ThreadActivityLog, MessageReadLog |

Default option values:

| Option | Default | Description |
|---|---|---|
| `MaxMessageLength` | `10000` | Maximum permitted message length in characters |
| `ParticipantsSeeChatHistory` | `true` | New participants can see messages sent before they joined |
| `PreventDuplicateChatThreads` | `true` | Each participant set can only have a single default thread |
| `ImmutableDirectMessageParticipants` | `true` | Participants cannot be added to or removed from a DM |
| `DisableGroups` | `false` | Whether group chats (more than two participants) are disabled |
| `LogChatReads` | `true` | Whether message read events are logged |
| `ThreadActivityLoggingMode` | `All` | All thread activity types (message, participant added, participant removed) are logged |

### Automatic behaviour

With no additional configuration:

- **User scoping**: All operations are automatically scoped to the current user via `IUserInfo.UserId`.
- **Soft-delete**: Threads, messages, participants, and metadata support soft-delete and restore via `AuditModel`.
- **Per-user thread deletion**: Users can delete threads for themselves without affecting other participants. A `ThreadDeleted` record is created per user; soft-deleting that record restores the thread for that user.
- **Duplicate prevention**: Creating a thread for a participant set that already has a default thread is blocked.
- **DM immutability**: Direct message threads lock their participant list — no additions or removals.
- **Read tracking**: Loading a thread logs that the current user has read up to the most recent message. Only the latest message is tracked to avoid log churn from cleanup jobs.
- **Activity logging**: Message sends, participant additions, and participant removals are logged to `ThreadActivityLog`.

## 2. Full configuration

### AddMessaging — standard registration

Registers all messaging services with database support. Your DbContext must implement both `IDataDbContext` and `IMessagingDbContext`. Requires `IUserInfo` to be registered (typically via JC.Identity).

```csharp
builder.Services.AddMessaging<AppDbContext>(options =>
{
    options.MaxMessageLength = 10000;
    options.ParticipantsSeeChatHistory = true;
    options.PreventDuplicateChatThreads = true;
    options.ImmutableDirectMessageParticipants = true;
    options.DisableGroups = false;
    options.LogChatReads = true;
    options.ThreadActivityLoggingMode = ThreadActivityLoggingMode.All;
});
```

| Type parameter | Constraint | Description |
|---|---|---|
| `TContext` | `DbContext, IDataDbContext, IMessagingDbContext` | Your application's DbContext |

| Parameter | Type | Default | Description |
|---|---|---|---|
| `configure` | `Action<MessagingOptions>?` | `null` | Optional callback to configure messaging options |

Throws `InvalidOperationException` if `IUserInfo` is not registered.

### MessagingOptions

| Property | Type | Default | Description |
|---|---|---|---|
| `MaxMessageLength` | `ushort` | `10000` | Maximum permitted message length in characters |
| `ParticipantsSeeChatHistory` | `bool` | `true` | When `true`, new participants can see messages sent before they joined. When `false`, messages prior to `JoinedAtUtc` are hidden |
| `PreventDuplicateChatThreads` | `bool` | `true` | When `true`, each participant set can only have a single default thread and no additional threads |
| `ImmutableDirectMessageParticipants` | `bool` | `true` | When `true`, participants cannot be added to or removed from direct message threads (two participants). Group threads are unaffected |
| `DisableGroups` | `bool` | `false` | When `true`, thread creation with more than two participants is rejected |
| `LogChatReads` | `bool` | `true` | When `true`, loading a thread creates a `MessageReadLog` entry for the latest message. When `false`, no read logs are created |
| `ThreadActivityLoggingMode` | `ThreadActivityLoggingMode` | `All` | Controls which thread activity types are persisted to `ThreadActivityLog` |

### ThreadActivityLoggingMode

Flags enum — values can be combined.

| Member | Value | Description |
|---|---|---|
| `None` | `0` | No thread activity is logged |
| `Message` | `1` | Message send events are logged |
| `ParticipantAdded` | `2` | Participant addition events are logged |
| `ParticipantRemoved` | `4` | Participant removal events are logged |
| `All` | `7` | All thread activity types are logged |

```csharp
// Log only message events and participant additions
options.ThreadActivityLoggingMode = ThreadActivityLoggingMode.Message | ThreadActivityLoggingMode.ParticipantAdded;
```

### ConfigureMessagingBackgroundJobs — cleanup job options

Configures `MessagingBackgroundJobOptions` for the `ActivityLogCleanupJob` and `ReadLogCleanupJob`. Only needs to be called if overriding defaults — jobs use default values automatically if this is not called.

```csharp
builder.Services.ConfigureMessagingBackgroundJobs(options =>
{
    // Activity log cleanup
    options.EnableActivityLogCleanupJob = true;
    options.ActivityLogRetentionMonths = 6;
    options.ActivityLogMinimumRetentionRecords = 30;
    options.ActivityLogCleanupChunkingValue = 500;

    // Read log cleanup
    options.EnableReadLogCleanupJob = true;
    options.ReadLogRetentionMonths = 6;
    options.ReadLogMinimumRetentionRecords = 30;
    options.ReadLogCleanupChunkingValue = 500;
    options.KeepMostRecentReadLog = true;
});
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `configure` | `Action<MessagingBackgroundJobOptions>` | — | Action to configure background job options |

### MessagingBackgroundJobOptions

#### Activity log cleanup

| Property | Type | Default | Description |
|---|---|---|---|
| `EnableActivityLogCleanupJob` | `bool` | `true` | Whether the activity log cleanup job runs |
| `ActivityLogRetentionMonths` | `ushort` | `6` | Logs older than this many months are eligible for deletion |
| `ActivityLogMinimumRetentionRecords` | `ushort` | `30` | Minimum number of records to always retain per thread, regardless of age |
| `ActivityLogCleanupChunkingValue` | `ushort` | `500` | Maximum number of records deleted per job execution |

#### Read log cleanup

| Property | Type | Default | Description |
|---|---|---|---|
| `EnableReadLogCleanupJob` | `bool` | `true` | Whether the read log cleanup job runs |
| `ReadLogRetentionMonths` | `ushort` | `6` | Logs older than this many months are eligible for deletion |
| `ReadLogMinimumRetentionRecords` | `ushort` | `30` | Minimum number of records to always retain, regardless of age |
| `ReadLogCleanupChunkingValue` | `ushort` | `500` | Maximum number of records deleted per job execution |
| `KeepMostRecentReadLog` | `bool` | `true` | When `true`, the most recent read log per user per message is always retained regardless of retention period. Takes precedence over `ReadLogRetentionMonths` |

### ApplyMessagingMappings — entity configuration

Applies all messaging entity mappings to the EF Core model builder. Call this in your DbContext's `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyMessagingMappings();
}
```

Configures the following entities:

| Entity | Primary key | Relationships | Max lengths |
|---|---|---|---|
| `ChatThread` | `Id` (36) | HasMany Messages, Participants, UserThreadDeletions; HasOne ChatMetadata | `Name` 256, `Description` 1024 |
| `ChatMessage` | `Id` (36) | BelongsTo Thread (FK `ThreadId`); self-referencing ReplyToMessage (FK `ReplyToMessageId`, restrict delete) | `Message` 8192, `ThreadId` 36, `ReplyToMessageId` 36 |
| `ChatParticipant` | Composite (`ThreadId`, `UserId`) | BelongsTo Thread (FK `ThreadId`) | `ThreadId` 36, `UserId` 36 |
| `ChatMetadata` | `ThreadId` (36, FK) | One-to-one with ChatThread | `Icon` 256, `ImgPath` 512, `ColourHex` 7, `ColourRgb` 16 |
| `ThreadDeleted` | `Id` (36) | BelongsTo Thread (FK `ThreadId`) | `ThreadId` 36, `UserId` 36 |
| `ThreadActivityLog` | `Id` (36) | BelongsTo Thread (FK `ThreadId`) | `ActivityDetails` 512, `ThreadId` 36 |
| `MessageReadLog` | Composite (`MessageId`, `UserId`) | BelongsTo ChatMessage (FK `MessageId`) | `MessageId` 36, `UserId` 36 |

All domain entities (`ChatThread`, `ChatMessage`, `ChatParticipant`, `ChatMetadata`, `ThreadDeleted`) extend `AuditModel` — audit fields are configured via `AuditModelMapping`. Both log entities (`ThreadActivityLog`, `MessageReadLog`) extend `LogModel` — creation fields are configured via `LogModelMapping`.

### IMessagingDbContext — database contract

Your DbContext must implement this interface to enable messaging persistence:

```csharp
public class AppDbContext : DataDbContext, IMessagingDbContext
{
    public DbSet<ChatThread> ChatThreads { get; set; }
    public DbSet<ThreadDeleted> DeletedThreads { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ChatParticipant> ChatParticipants { get; set; }
    public DbSet<ChatMetadata> ChatMetadata { get; set; }
    public DbSet<ThreadActivityLog> ThreadActivityLogs { get; set; }
    public DbSet<MessageReadLog> MessageReadLogs { get; set; }
}
```

## 3. Apply migrations

JC.Communication messaging introduces seven tables: `ChatThreads`, `ChatMessages`, `ChatParticipants`, `ChatMetadata`, `DeletedThreads`, `ThreadActivityLogs`, and `MessageReadLogs`. After setting up your DbContext, generate and apply the migration:

```bash
dotnet ef migrations add AddMessaging --project YourApp
dotnet ef database update --project YourApp
```

Alternatively, apply programmatically at startup:

```bash
dotnet ef migrations add AddMessaging --project YourApp
```

```csharp
await app.Services.MigrateDatabaseAsync<AppDbContext>();
```

## 4. Verify

1. Run the application.
2. Create a thread using `ChatThreadService` (e.g. from a controller or service) with at least two participant user IDs.
3. Query the `ChatThreads` table — you should see the thread with its name, participants, and the current user's audit trail.

## Next steps

- [Guide](Messaging-Guide.md) — creating threads, sending messages, reply-to, per-user deletion, group chat management, metadata, and read tracking.
- [API Reference](Messaging-API.md)
