# JC.Core — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project with an Entity Framework Core `DbContext`
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

## 0. Add the package

Add a project reference to `JC.Core`:

```xml
<ProjectReference Include="path/to/JC.Core/JC.Core.csproj" />
```

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### DbContext

Your `DbContext` must extend `DataDbContext` to get automatic audit trail logging on `SaveChangesAsync`. `DataDbContext` already implements `IDataDbContext` for you:

```csharp
public class AppDbContext : DataDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
}
```

### Services — `Program.cs`

```csharp
// Register JC.Core services — repository pattern, audit trail, and repository manager
builder.Services.AddCore<AppDbContext>();

// Register repository contexts for each entity you want to access through the repository pattern
builder.Services.RegisterRepositoryContexts(typeof(Product), typeof(Order));
```

### Defaults

When called, `AddCore<TContext>` registers:

| Registration | Lifetime | Description |
|-------------|----------|-------------|
| `IDataDbContext` → `TContext` | Scoped | Your DbContext as the data context |
| `DbContext` → `TContext` | Scoped | Your DbContext as the base EF Core context |
| `IRepositoryManager` → `RepositoryManager` | Scoped | Unit of work with transaction support |
| `IRepositoryContext<AuditModel>` | Scoped | Repository for the built-in `AuditModel` base class |

`RegisterRepositoryContexts` adds an `IRepositoryContext<T>` / `RepositoryContext<T>` pair for each entity type, giving you typed repository access with automatic audit field population.

All registrations use `TryAddScoped` — if you've already registered a service (e.g. `DbContext`), it won't be overwritten.

### Automatic behaviour

With no additional configuration, `SaveChangesAsync` on your `DataDbContext` will:

1. Inspect the EF Core `ChangeTracker` for modified entities
2. Log audit entries for updates, deletes, soft-deletes, and restores immediately
3. Save changes to the database
4. Log audit entries for creates (deferred until after save so database-generated IDs are captured)
5. Save the create audit entries

Audit entries are written to an `AuditEntries` table with the user ID, table name, action type, timestamp, and a JSON snapshot of the entity data. If no `IUserInfo` is available (e.g. JC.Identity isn't registered), the user ID falls back to `IUserInfo.MissingUserInfoId` (`"<NONE>"`).

**Immutability enforcement:** The audit service enforces strict rules for certain entity types:

| Entity type | Create | Hard delete | Update / Soft-delete / Restore |
|-------------|--------|-------------|-------------------------------|
| `LogModel` inheritors | Skipped (the log *is* its own audit trail) | Audit entry logged | Throws `InvalidOperationException` |
| `AuditEntry` | Skipped | Skipped (housekeeping) | Throws `InvalidOperationException` |

This prevents audit-of-audit duplication and enforces that log and audit entries are immutable once written.

### Entity model hierarchy

JC.Core provides a three-level model hierarchy for entities that need automatic audit field population:

```
BaseCreateModel          — creation fields only (CreatedById, CreatedUtc)
├── LogModel             — immutable log entities (create + hard delete only)
└── AuditModel           — full lifecycle (create, update, soft-delete, restore)
```

- **`BaseCreateModel`** — the shared base class. Provides `CreatedById`, `CreatedUtc`, and `FillCreated()`. Used by the repository to populate creation fields on any entity in the hierarchy. **Never extend this directly** — always use `LogModel` or `AuditModel`. Inheriting from `BaseCreateModel` directly bypasses the audit service's type discrimination and immutability enforcement.
- **`LogModel`** — extends `BaseCreateModel`. A marker class with no extra properties. Entities extending `LogModel` are treated as immutable once created — the audit service will throw if an update, soft-delete, or restore is attempted. Only creation and hard deletion are permitted.
- **`AuditModel`** — extends `BaseCreateModel`. Adds modification, soft-delete, and restore fields with corresponding `Fill*` methods.

Entities extending `AuditModel` automatically have their audit fields populated by the repository:

| Field | Populated when | Value |
|-------|---------------|-------|
| `CreatedById` / `CreatedUtc` | Entity is added | Current user ID and UTC timestamp |
| `LastModifiedById` / `LastModifiedUtc` | Entity is updated | Current user ID and UTC timestamp |
| `DeletedById` / `DeletedUtc` / `IsDeleted` | Entity is soft-deleted | Current user ID, UTC timestamp, `true` |
| `RestoredById` / `RestoredUtc` | Entity is restored | Current user ID, UTC timestamp; clears deleted fields |

Entities extending `LogModel` have only the creation fields populated:

| Field | Populated when | Value |
|-------|---------------|-------|
| `CreatedById` / `CreatedUtc` | Entity is added | Current user ID and UTC timestamp |

## 2. Full configuration

### AddCore — service registration

```csharp
builder.Services.AddCore<AppDbContext>();
```

`AddCore` has a single generic type parameter:

| Type parameter | Constraint | Description |
|---------------|-----------|-------------|
| `TContext` | `DbContext, IDataDbContext` | Your application's DbContext. Extend `DataDbContext` which implements `IDataDbContext` for you |

There are no configuration parameters or callbacks — `AddCore` registers all services with sensible defaults. Customisation happens through entity registration and DbContext configuration.

### RegisterRepositoryContexts — entity registration

Register repository contexts for your entity types. There are two approaches:

**Multiple types at once (params array):**

```csharp
builder.Services.RegisterRepositoryContexts(typeof(Product), typeof(Order), typeof(Customer));
```

**Single type (generic):**

```csharp
builder.Services.RegisterRepositoryContext<Product>();
```

Both register a scoped `IRepositoryContext<T>` / `RepositoryContext<T>` pair for each type. Every type must be a class — passing a struct or interface throws `ArgumentException`.

You must register a repository context for every entity type you want to use with the repository pattern — whether you inject `IRepositoryContext<T>` directly or access it through `IRepositoryManager.GetRepository<T>()`. The repository manager resolves repositories from DI, so unregistered types will fail at runtime.

### DataDbContext — extending the base context

`DataDbContext` provides the two-phase audit trail on `SaveChangesAsync`. Your DbContext should extend it:

```csharp
public class AppDbContext : DataDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Configures AuditEntry indexes — always call base
        // ...your entity configuration...
    }
}
```

`base.OnModelCreating` configures the `AuditEntry` entity with:

| Configuration | Detail |
|--------------|--------|
| Primary key | `Id` — string, max 36 characters (GUID) |
| Index | `UserId` (max 256 characters) |
| Index | `TableName` (max 256 characters) |
| Index | `AuditDate` |
| Max length | `UserName` (max 256 characters) |
| Required properties | `Action`, `AuditDate` |

### AuditModel — auditable entities

Extend `AuditModel` for entities that need full lifecycle audit fields:

```csharp
public class Product : AuditModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

All audit properties have private setters — they can only be populated through the `Fill*` methods, which are called automatically by `RepositoryContext<T>`:

| Method | Defined on | Called by | Sets |
|--------|-----------|----------|------|
| `FillCreated(userId)` | `BaseCreateModel` | `AddAsync` / `AddRangeAsync` | `CreatedById`, `CreatedUtc` |
| `FillModified(userId)` | `AuditModel` | `UpdateAsync` / `UpdateRangeAsync` | `LastModifiedById`, `LastModifiedUtc` |
| `FillDeleted(userId)` | `AuditModel` | `SoftDeleteAsync` / `SoftDeleteRangeAsync` | `DeletedById`, `DeletedUtc`, `IsDeleted = true`; clears restored fields |
| `FillRestored(userId)` | `AuditModel` | `RestoreAsync` / `RestoreRangeAsync` | `RestoredById`, `RestoredUtc`, `IsDeleted = false`; clears deleted fields |

`FillCreated` is on `BaseCreateModel`, so it applies to both `AuditModel` and `LogModel` entities. It only sets values if they haven't already been populated (idempotent).

### LogModel — immutable log entities

Extend `LogModel` for entities that represent immutable log records (e.g. email logs, notification logs):

```csharp
public class EmailLog : LogModel
{
    public string Id { get; set; }
    public string Recipient { get; set; }
    public DateTime SentUtc { get; set; }
}
```

`LogModel` entities get `CreatedById` and `CreatedUtc` populated on add, just like `AuditModel`. The key difference is enforcement — the audit service throws `InvalidOperationException` if any code attempts to update, soft-delete, or restore a `LogModel` entity. Only creation and hard deletion are permitted.

### Non-model entities

Entities that don't extend `AuditModel` or `LogModel` can still use the repository pattern — they just won't get automatic audit field population. Soft-delete still works if the entity has a public `bool IsDeleted` property (detected via reflection).

### IRepositoryContext — repository operations

Every repository method that modifies data accepts two optional parameters:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string?` | `null` | Override the user ID for audit fields. When `null`, uses the current `IUserInfo.UserId` from DI |
| `saveNow` | `bool` | `true` | When `true`, calls `SaveChangesAsync` immediately. Set to `false` to batch multiple operations before saving |

Example with explicit options:

```csharp
// Batch two adds without saving, then save once
await productRepo.AddAsync(product1, saveNow: false);
await productRepo.AddAsync(product2, saveNow: false);
await repositoryManager.SaveChangesAsync();

// Override the user ID for audit purposes
await productRepo.UpdateAsync(product, userId: "system-migration");
```

### IRepositoryManager — unit of work and transactions

`IRepositoryManager` provides repository access and transaction management:

```csharp
// Get a repository at runtime
var repo = repositoryManager.GetRepository<Product>();

// Transaction support
await using var transaction = await repositoryManager.BeginTransactionAsync();
try
{
    await productRepo.AddAsync(product, saveNow: false);
    await orderRepo.AddAsync(order, saveNow: false);
    await repositoryManager.CommitTransactionAsync(); // Saves and commits
}
catch
{
    await repositoryManager.RollbackTransactionAsync();
    throw;
}
```

`CommitTransactionAsync` calls `SaveChangesAsync` before committing. `RollbackTransactionAsync` and `CommitTransactionAsync` both throw `InvalidOperationException` if no transaction has been started.

### IBackgroundJob — cross-package background jobs

JC.Core defines the `IBackgroundJob` interface so that any package can declare background jobs without depending on JC.BackgroundJobs:

```csharp
public interface IBackgroundJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
```

JC.Core only *defines* jobs — it does not execute them. The consuming application must use [JC.BackgroundJobs](../JC.BackgroundJobs/Setup.md) (hosted services or Hangfire) to register and schedule job execution. JC.Core itself provides two built-in jobs: `AuditCleanupJob` and `SoftDeleteCleanupJob`.

### ConfigureCoreBackgroundJobs — background job options

Configures `CoreBackgroundJobOptions` for the built-in cleanup jobs. Only call this if you need to override the defaults — `IOptions<CoreBackgroundJobOptions>` resolves with defaults automatically even without explicit registration.

```csharp
builder.Services.ConfigureCoreBackgroundJobs(options =>
{
    // Audit cleanup
    options.EnableAuditCleanupJob = true;
    options.AuditRetentionMonths = 6;
    options.MinimumRetentionRecords = 30;
    options.RetentionRecordsPerTable = true;
    options.AuditCleanupChunkingValue = 500;

    // Soft-delete cleanup
    options.EnableSoftDeleteCleanupJob = false;
    options.SoftDeleteRetentionMonths = 24;
    options.SetSoftDeleteRetentionBlacklist("product", "order"); // Entity names to exclude (case-insensitive)
});
```

#### `CoreBackgroundJobOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableAuditCleanupJob` | `bool` | `true` | Enables the audit cleanup job |
| `AuditRetentionMonths` | `ushort` | `6` | Audit entries older than this are eligible for cleanup |
| `MinimumRetentionRecords` | `ushort` | `30` | Minimum number of audit entries to keep regardless of age |
| `RetentionRecordsPerTable` | `bool` | `true` | When `true`, minimum retention is applied per table; when `false`, globally |
| `AuditCleanupChunkingValue` | `ushort` | `500` | Maximum records to delete per execution. `0` disables chunking |
| `EnableSoftDeleteCleanupJob` | `bool` | `false` | Enables the soft-delete cleanup job (off by default) |
| `SoftDeleteRetentionMonths` | `ushort` | `24` | Soft-deleted entities older than this are hard-deleted |
| `SoftDeleteRetentionBlacklist` | `List<string>` | `[]` | Entity type names excluded from soft-delete cleanup (case-insensitive). Set via `SetSoftDeleteRetentionBlacklist()` |

#### AuditCleanupJob

Deletes audit entries older than `AuditRetentionMonths`, respecting `MinimumRetentionRecords` (globally or per table depending on `RetentionRecordsPerTable`). Processes in chunks when `AuditCleanupChunkingValue` is set.

#### SoftDeleteCleanupJob

Discovers all entity types in the `DbContext` model that extend `AuditModel` or have a `bool IsDeleted` property. Hard-deletes soft-deleted entities older than `SoftDeleteRetentionMonths`. For `AuditModel` entities, filters by `DeletedUtc` in the database query. For non-`AuditModel` entities with `IsDeleted`, builds an expression tree so EF Core translates the filter to SQL. Respects `SoftDeleteRetentionBlacklist` to skip specific entity types.

**Important:** These jobs are not self-executing. Configuring the options here only controls their behaviour — you still need to register them as background jobs using JC.BackgroundJobs for them to actually run. Without that registration, the jobs will never be invoked. See the JC.BackgroundJobs documentation for how to register and schedule jobs:

- [JC.BackgroundJobs — Setup](../JC.BackgroundJobs/Setup.md)
- [JC.BackgroundJobs — Guide](../JC.BackgroundJobs/Guide.md)
- [JC.BackgroundJobs — API Reference](../JC.BackgroundJobs/API.md)

### MigrateDatabaseAsync — automatic migration

Apply pending EF Core migrations at startup:

```csharp
var app = builder.Build();
await app.Services.MigrateDatabaseAsync<AppDbContext>();
```

This creates an async scope, resolves the `DbContext`, and calls `Database.MigrateAsync()`. Useful for development — in production, prefer `dotnet ef database update` or a migration pipeline.

## 3. Apply migrations

JC.Core introduces the `AuditEntries` table through `DataDbContext`. After setting up your `DbContext`, generate and apply the initial migration:

```bash
dotnet ef migrations add InitialCore --project YourApp
dotnet ef database update --project YourApp
```

Alternatively, generate the migration and apply it programmatically at startup instead of running `dotnet ef database update`:

```bash
dotnet ef migrations add InitialCore --project YourApp
```

```csharp
await app.Services.MigrateDatabaseAsync<AppDbContext>();
```

## 4. Verify

1. Run the application.
2. Create or update an entity through a repository context.
3. Query the `AuditEntries` table — you should see an entry with the action type, table name, user ID, timestamp, and a JSON snapshot of the entity.

## Next steps

- [Guide](Guide.md) — repository pattern usage, soft-delete querying, pagination, and utility helpers.
- [API Reference](API.md)
