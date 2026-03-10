# JC.Core — API reference

Complete reference of all public types, properties, and methods in JC.Core. See [Setup](Setup.md) for registration and [Guide](Guide.md) for usage examples.

> **Note:** Registration extensions (`IServiceCollection`, `IServiceProvider`, `IApplicationBuilder`) and options classes are documented in [Setup](Setup.md), not here.

---

# Models

## BaseCreateModel

**Namespace:** `JC.Core.Models.Auditing`

Base class providing creation audit fields. Shared by both `LogModel` and `AuditModel`. **Never extend this directly** — always use `LogModel` or `AuditModel` instead. Inheriting from `BaseCreateModel` directly bypasses the audit service's type discrimination and immutability enforcement.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `CreatedById` | `string?` | `null` | get; private set; | Identifier of the user who created this entity. |
| `CreatedUtc` | `DateTime` | `default` | get; private set; | UTC timestamp of entity creation. |

### Methods

#### FillCreated(string userId)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string` | — | The identifier of the user creating the entity. |

Sets `CreatedById` to the provided user ID and `CreatedUtc` to `DateTime.UtcNow`. Idempotent — only sets values if they haven't already been populated (`CreatedById` is null/whitespace and `CreatedUtc` is `default`). Called automatically by `RepositoryContext<T>.AddAsync` and `AddRangeAsync` for any entity extending `BaseCreateModel`.

---

## LogModel

**Namespace:** `JC.Core.Models.Auditing`

**Extends:** `BaseCreateModel`

Marker base class for immutable log entities. Has no additional properties beyond those inherited from `BaseCreateModel`. Exists for type discrimination in the audit service — entities extending `LogModel` are treated as immutable once created. The audit service skips audit entry creation on create, logs hard deletes, and throws `InvalidOperationException` on any attempt to update, soft-delete, or restore.

---

## AuditModel

**Namespace:** `JC.Core.Models.Auditing`

**Extends:** `BaseCreateModel`

Base class for auditable entities with full lifecycle support. Provides automatic population of modification, soft-delete, and restore audit fields. Creation fields are inherited from `BaseCreateModel`. All property setters are private — state is only changed through the `Fill*` methods to ensure consistency.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `LastModifiedById` | `string?` | `null` | get; private set; | Identifier of the user who last modified this entity. |
| `LastModifiedUtc` | `DateTime?` | `null` | get; private set; | UTC timestamp of the last modification. |
| `DeletedById` | `string?` | `null` | get; private set; | Identifier of the user who soft-deleted this entity. |
| `DeletedUtc` | `DateTime?` | `null` | get; private set; | UTC timestamp of soft-deletion. |
| `IsDeleted` | `bool` | `false` | get; private set; | Whether this entity is currently soft-deleted. |
| `RestoredById` | `string?` | `null` | get; private set; | Identifier of the user who restored this entity. |
| `RestoredUtc` | `DateTime?` | `null` | get; private set; | UTC timestamp of restoration. |

Inherits `CreatedById`, `CreatedUtc`, and `FillCreated` from `BaseCreateModel`.

### Methods

#### FillModified(string userId)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string` | — | The identifier of the user modifying the entity. |

Sets `LastModifiedById` to the provided user ID and `LastModifiedUtc` to `DateTime.UtcNow`. Called automatically by `RepositoryContext<T>.UpdateAsync` and `UpdateRangeAsync`.

---

#### FillDeleted(string userId)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string` | — | The identifier of the user deleting the entity. |

Sets `IsDeleted` to `true`, `DeletedById` to the provided user ID, and `DeletedUtc` to `DateTime.UtcNow`. Clears `RestoredById` and `RestoredUtc` to `null`. Called automatically by `RepositoryContext<T>.SoftDeleteAsync` and `SoftDeleteRangeAsync`.

---

#### FillRestored(string userId)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string` | — | The identifier of the user restoring the entity. |

Sets `IsDeleted` to `false`, `RestoredById` to the provided user ID, and `RestoredUtc` to `DateTime.UtcNow`. Clears `DeletedById` and `DeletedUtc` to `null`. Called automatically by `RepositoryContext<T>.RestoreAsync` and `RestoreRangeAsync`.

---

## AuditEntry

**Namespace:** `JC.Core.Models.Auditing`

Entity representing a single audit trail record capturing who performed what action, on which table, and when. Persisted automatically by `DataDbContext.SaveChangesAsync` via the change tracker.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | `Guid.NewGuid().ToString()` | get; private set; | Unique identifier for this audit entry. |
| `Action` | `AuditAction` | — | get; set; | The type of action that was performed. |
| `AuditDate` | `DateTime` | — | get; set; | UTC timestamp of when the action occurred. |
| `UserId` | `string?` | `null` | get; set; | Identifier of the user who performed the action. |
| `UserName` | `string?` | `null` | get; set; | Display name of the user who performed the action. |
| `TableName` | `string?` | `null` | get; set; | The database table name affected by the action. |
| `ActionData` | `string?` | `null` | get; set; | JSON-serialised entity data. For creates, contains all non-null property values. For updates, contains a `From`/`To` diff of modified properties. |

---

## IUserInfo

**Namespace:** `JC.Core.Models`

Read-only contract representing the current user's identity, profile, security state, and authorisation details. Populated per-request by `UserInfoMiddleware` in JC.Identity. When JC.Identity is not registered, `IUserInfo` may not be available — the repository falls back to `IUserInfo.MissingUserInfoId` for audit fields.

### Constants

| Constant | Type | Value | Description |
|----------|------|-------|-------------|
| `MissingUserInfoId` | `string` | `"<NONE>"` | Fallback user identifier used when `IUserInfo` is not resolved from DI. |

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `UserId` | `string` | get; set; | Unique identifier of the current user. |
| `Username` | `string` | get; set; | Username of the current user. |
| `Email` | `string` | get; set; | Email address of the current user. |
| `EmailConfirmed` | `bool` | get; set; | Whether the user's email has been confirmed. |
| `PhoneNumber` | `string?` | get; set; | The user's phone number, if set. |
| `PhoneNumberConfirmed` | `bool` | get; set; | Whether the user's phone number has been confirmed. |
| `TwoFactorEnabled` | `bool` | get; set; | Whether two-factor authentication is enabled. |
| `LockoutEnabled` | `bool` | get; set; | Whether lockout is enabled for the user. |
| `LockoutEnd` | `DateTime?` | get; set; | UTC timestamp when the user's lockout expires, if locked out. |
| `AccessFailedCount` | `int` | get; set; | Number of consecutive failed access attempts. |
| `TenantId` | `string?` | get; set; | Tenant identifier the user belongs to, if multi-tenancy is active. |
| `DisplayName` | `string?` | get; set; | The user's display name. |
| `LastLoginUtc` | `DateTime?` | get; set; | UTC timestamp of the user's last login. |
| `IsEnabled` | `bool` | get; set; | Whether the user account is enabled. |
| `RequiresPasswordChange` | `bool` | get; set; | Whether the user must change their password. |
| `IsSetup` | `bool` | get; set; | Whether the user info has been populated for this request. |
| `MultiTenancyEnabled` | `bool` | get; set; | Whether the current user has a tenant assigned. |
| `Roles` | `IReadOnlyList<string>` | get; set; | Role names assigned to the current user. |
| `Claims` | `IReadOnlyList<Claim>` | get; set; | All claims associated with the current user. |

### Methods

#### IsInRole(string role)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `role` | `string` | — | The role name to check. |

Determines whether the current user belongs to the specified role. Returns `true` if the user has the role; otherwise `false`.

---

## IBackgroundJob

**Namespace:** `JC.Core.Models`

Defines a background job that can be executed by hosting infrastructure. This interface lives in JC.Core so that any package can declare background jobs without depending on JC.BackgroundJobs — the consuming application wires up execution at registration time.

### Methods

#### ExecuteAsync(CancellationToken cancellationToken = default)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | Token signalling cancellation of the host or job. |

Executes the job's work. Implementations should contain only the job logic — looping, error handling, and lifecycle management are handled by the hosting infrastructure.

---

## PagedList\<T\>

**Namespace:** `JC.Core.Models.Pagination`

Default implementation of `IPagination<T>`. Wraps a page of items with pagination metadata. Implements `IReadOnlyList<T>` for direct enumeration and indexing.

### Constructor

#### PagedList(IEnumerable\<T\> items, int pageNumber, int pageSize, int totalCount)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `items` | `IEnumerable<T>` | — | The items for the current page. |
| `pageNumber` | `int` | — | The current page number (1-based). Must be ≥ 1. |
| `pageSize` | `int` | — | The maximum number of items per page. Must be ≥ 1. |
| `totalCount` | `int` | — | The total number of items across all pages. |

Throws `ArgumentOutOfRangeException` if `pageNumber` or `pageSize` is less than 1. If `pageNumber` exceeds the calculated total pages, it is clamped to the last valid page (minimum 1).

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Items` | `IReadOnlyList<T>` | get; | The items on the current page. |
| `PageNumber` | `int` | get; | The current page number (1-based, clamped to valid range). |
| `PageSize` | `int` | get; | The maximum number of items per page. |
| `TotalCount` | `int` | get; | Total number of items across all pages. |
| `TotalPages` | `int` | get; | Total number of pages, calculated as `⌈TotalCount / PageSize⌉`. |
| `HasPreviousPage` | `bool` | get; | `true` if `PageNumber > 1`. |
| `HasNextPage` | `bool` | get; | `true` if `PageNumber < TotalPages`. |
| `IsFirstPage` | `bool` | get; | `true` if there is no previous page. |
| `IsLastPage` | `bool` | get; | `true` if there is no next page. |
| `Count` | `int` | get; | The number of items on the current page (from `IReadOnlyList<T>`). |

---

## Country

**Namespace:** `JC.Core.Helpers`

Record representing a country with its ISO code and English name.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Code` | `string` | get; init; | ISO 3166-1 alpha-2 country code (e.g. `"GB"`). |
| `Name` | `string` | get; init; | English country name (e.g. `"United Kingdom"`). |

---

# Enums

## AuditAction

**Namespace:** `JC.Core.Enums`

Enum representing the type of action recorded in an audit trail entry.

| Member | Value | Description |
|--------|-------|-------------|
| `Create` | `0` | A new entity was created. |
| `Update` | `1` | An existing entity was updated. |
| `SoftDelete` | `2` | An entity was soft-deleted (`IsDeleted` set to `true`). |
| `Delete` | `3` | An entity was permanently (hard) deleted. |
| `Restore` | `4` | A soft-deleted entity was restored (`IsDeleted` set to `false`). |

---

## DeletedQueryType

**Namespace:** `JC.Core.Enums`

Enum specifying how soft-deleted records should be filtered in queries.

| Member | Value | Description |
|--------|-------|-------------|
| `All` | `0` | Include all records regardless of deletion status. |
| `OnlyActive` | `1` | Exclude soft-deleted records, returning only active records. |
| `OnlyDeleted` | `2` | Return only soft-deleted records. |

---

# Services

## RepositoryContext\<T\>

**Namespace:** `JC.Core.Services.DataRepositories`

Generic repository providing full CRUD, soft-delete, and restore operations for entity type `T`. Automatically populates creation fields for entities extending `BaseCreateModel` (both `AuditModel` and `LogModel`), full lifecycle fields for `AuditModel` entities, and falls back to reflection-based `IsDeleted` property detection for non-`AuditModel` entities. Inject via `IRepositoryContext<T>`.

**Constraint:** `T : class`

### Methods

#### AsQueryable()

**Returns:** `IQueryable<T>`

Returns the underlying EF Core `DbSet<T>` as a queryable, allowing custom query composition with LINQ. No filtering is applied — the caller is responsible for adding `Where`, `OrderBy`, and other clauses.

---

#### GetAll(Expression\<Func\<T, bool\>\> predicate)

**Returns:** `IQueryable<T>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `predicate` | `Expression<Func<T, bool>>` | — | A lambda expression to filter the entities. |

Returns a queryable filtered by the predicate. The query is not materialised — call `ToListAsync()`, `FirstOrDefaultAsync()`, or similar to execute it.

---

#### GetAll(Expression\<Func\<T, bool\>\> predicate, Func\<IQueryable\<T\>, IOrderedQueryable\<T\>\> orderBy)

**Returns:** `IQueryable<T>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `predicate` | `Expression<Func<T, bool>>` | — | A lambda expression to filter the entities. |
| `orderBy` | `Func<IQueryable<T>, IOrderedQueryable<T>>` | — | A function that applies ordering to the filtered queryable. |

Returns a queryable filtered by the predicate and ordered by the provided function. The query is not materialised.

---

#### GetAllAsync(Expression\<Func\<T, bool\>\> predicate, CancellationToken cancellationToken = default)

**Returns:** `Task<List<T>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `predicate` | `Expression<Func<T, bool>>` | — | A lambda expression to filter the entities. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Materialises the filtered queryable into a list asynchronously.

---

#### GetAllAsync(Expression\<Func\<T, bool\>\> predicate, Func\<IQueryable\<T\>, IOrderedQueryable\<T\>\> orderBy, CancellationToken cancellationToken = default)

**Returns:** `Task<List<T>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `predicate` | `Expression<Func<T, bool>>` | — | A lambda expression to filter the entities. |
| `orderBy` | `Func<IQueryable<T>, IOrderedQueryable<T>>` | — | A function that applies ordering to the filtered queryable. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Materialises the filtered and ordered queryable into a list asynchronously.

---

#### GetByIdAsync(int id, CancellationToken cancellationToken = default)

**Returns:** `Task<T?>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `int` | — | The integer primary key of the entity to retrieve. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Retrieves a single entity by its integer primary key using `FindAsync`. Returns `null` if no entity with the given ID exists.

---

#### GetByIdAsync(string id, CancellationToken cancellationToken = default)

**Returns:** `Task<T?>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `string` | — | The string primary key of the entity to retrieve. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Retrieves a single entity by its string primary key using `FindAsync`. Returns `null` if no entity with the given ID exists.

---

#### GetByIdAsync(params object[] id)

**Returns:** `Task<T?>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `id` | `params object[]` | — | The composite key values identifying the entity. |

Retrieves a single entity by a composite primary key using `FindAsync`. Returns `null` if no matching entity exists.

---

#### AddAsync(T entity, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)

**Returns:** `Task<T>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `entity` | `T` | — | The entity to add. |
| `userId` | `string?` | `null` | User identifier for audit fields. Falls back to `IUserInfo.UserId`, then `IUserInfo.MissingUserInfoId`. |
| `saveNow` | `bool` | `true` | Whether to call `SaveChangesAsync` immediately. Set to `false` to batch. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Adds a single entity to the database. If `T` extends `BaseCreateModel` (either `AuditModel` or `LogModel`), populates `CreatedById` and `CreatedUtc` before saving. Delegates to `AddRangeAsync` internally.

---

#### AddAsync(IEnumerable\<T\> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)

**Returns:** `Task<List<T>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `entities` | `IEnumerable<T>` | — | The entities to add. |
| `userId` | `string?` | `null` | User identifier for audit fields. Falls back to `IUserInfo.UserId`, then `IUserInfo.MissingUserInfoId`. |
| `saveNow` | `bool` | `true` | Whether to call `SaveChangesAsync` immediately. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Adds multiple entities to the database. Delegates to `AddRangeAsync` internally. Behaves identically to `AddRangeAsync`.

---

#### AddRangeAsync(IEnumerable\<T\> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)

**Returns:** `Task<List<T>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `entities` | `IEnumerable<T>` | — | The entities to add. |
| `userId` | `string?` | `null` | User identifier for audit fields. Falls back to `IUserInfo.UserId`, then `IUserInfo.MissingUserInfoId`. |
| `saveNow` | `bool` | `true` | Whether to call `SaveChangesAsync` immediately. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Adds a collection of entities to the database. If `T` extends `BaseCreateModel` (either `AuditModel` or `LogModel`), iterates each entity and calls `FillCreated` with the resolved user ID before adding. Logs and rethrows any exceptions.

---

#### UpdateAsync(T entity, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)

**Returns:** `Task<T>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `entity` | `T` | — | The entity to update. |
| `userId` | `string?` | `null` | User identifier for audit fields. Falls back to `IUserInfo.UserId`, then `IUserInfo.MissingUserInfoId`. |
| `saveNow` | `bool` | `true` | Whether to call `SaveChangesAsync` immediately. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Updates a single entity in the database. If `T` extends `AuditModel`, populates `LastModifiedById` and `LastModifiedUtc` before saving. Delegates to `UpdateRangeAsync` internally.

---

#### UpdateAsync(IEnumerable\<T\> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)

**Returns:** `Task<List<T>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `entities` | `IEnumerable<T>` | — | The entities to update. |
| `userId` | `string?` | `null` | User identifier for audit fields. Falls back to `IUserInfo.UserId`, then `IUserInfo.MissingUserInfoId`. |
| `saveNow` | `bool` | `true` | Whether to call `SaveChangesAsync` immediately. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Updates multiple entities. Delegates to `UpdateRangeAsync` internally.

---

#### UpdateRangeAsync(IEnumerable\<T\> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)

**Returns:** `Task<List<T>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `entities` | `IEnumerable<T>` | — | The entities to update. |
| `userId` | `string?` | `null` | User identifier for audit fields. Falls back to `IUserInfo.UserId`, then `IUserInfo.MissingUserInfoId`. |
| `saveNow` | `bool` | `true` | Whether to call `SaveChangesAsync` immediately. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Updates a collection of entities. If `T` extends `AuditModel`, iterates each entity and calls `FillModified` with the resolved user ID before updating. Logs and rethrows any exceptions.

---

#### SoftDeleteAsync(T entity, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)

**Returns:** `Task<T>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `entity` | `T` | — | The entity to soft-delete. |
| `userId` | `string?` | `null` | User identifier for audit fields. Falls back to `IUserInfo.UserId`, then `IUserInfo.MissingUserInfoId`. |
| `saveNow` | `bool` | `true` | Whether to call `SaveChangesAsync` immediately. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Soft-deletes a single entity. If `T` extends `AuditModel`, calls `FillDeleted` which sets `IsDeleted = true`, populates `DeletedById` and `DeletedUtc`, and clears `RestoredById` and `RestoredUtc`. If `T` does not extend `AuditModel` but has an `IsDeleted` property, sets it to `true` via reflection. Delegates to `SoftDeleteRangeAsync` internally.

---

#### SoftDeleteAsync(IEnumerable\<T\> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)

**Returns:** `Task<List<T>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `entities` | `IEnumerable<T>` | — | The entities to soft-delete. |
| `userId` | `string?` | `null` | User identifier for audit fields. |
| `saveNow` | `bool` | `true` | Whether to call `SaveChangesAsync` immediately. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Soft-deletes multiple entities. Delegates to `SoftDeleteRangeAsync` internally.

---

#### SoftDeleteRangeAsync(IEnumerable\<T\> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)

**Returns:** `Task<List<T>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `entities` | `IEnumerable<T>` | — | The entities to soft-delete. |
| `userId` | `string?` | `null` | User identifier for audit fields. |
| `saveNow` | `bool` | `true` | Whether to call `SaveChangesAsync` immediately. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Soft-deletes a collection of entities. For `AuditModel` entities, calls `FillDeleted` on each. For non-`AuditModel` entities with an `IsDeleted` property, sets it to `true` via reflection. Marks all entities as updated and persists if `saveNow` is `true`. Logs and rethrows any exceptions.

---

#### RestoreAsync(T entity, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)

**Returns:** `Task<T>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `entity` | `T` | — | The entity to restore. |
| `userId` | `string?` | `null` | User identifier for audit fields. |
| `saveNow` | `bool` | `true` | Whether to call `SaveChangesAsync` immediately. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Restores a single soft-deleted entity. If `T` extends `AuditModel`, calls `FillRestored` which sets `IsDeleted = false`, populates `RestoredById` and `RestoredUtc`, and clears `DeletedById` and `DeletedUtc`. If `T` does not extend `AuditModel` but has an `IsDeleted` property, sets it to `false` via reflection. Delegates to `RestoreRangeAsync` internally.

---

#### RestoreAsync(IEnumerable\<T\> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)

**Returns:** `Task<List<T>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `entities` | `IEnumerable<T>` | — | The entities to restore. |
| `userId` | `string?` | `null` | User identifier for audit fields. |
| `saveNow` | `bool` | `true` | Whether to call `SaveChangesAsync` immediately. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Restores multiple soft-deleted entities. Delegates to `RestoreRangeAsync` internally.

---

#### RestoreRangeAsync(IEnumerable\<T\> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)

**Returns:** `Task<List<T>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `entities` | `IEnumerable<T>` | — | The entities to restore. |
| `userId` | `string?` | `null` | User identifier for audit fields. |
| `saveNow` | `bool` | `true` | Whether to call `SaveChangesAsync` immediately. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Restores a collection of soft-deleted entities. For `AuditModel` entities, calls `FillRestored` on each. For non-`AuditModel` entities with an `IsDeleted` property, sets it to `false` via reflection. Marks all entities as updated and persists if `saveNow` is `true`. Logs and rethrows any exceptions.

---

#### DeleteAsync(T entity, bool saveNow = true, CancellationToken cancellationToken = default)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `entity` | `T` | — | The entity to permanently delete. |
| `saveNow` | `bool` | `true` | Whether to call `SaveChangesAsync` immediately. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Permanently removes a single entity from the database. This is a hard delete — the entity is removed from the `DbSet`, not soft-deleted. Delegates to `DeleteRangeAsync` internally. Returns `true` on success; logs and rethrows on failure.

---

#### DeleteAsync(IEnumerable\<T\> entities, bool saveNow = true, CancellationToken cancellationToken = default)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `entities` | `IEnumerable<T>` | — | The entities to permanently delete. |
| `saveNow` | `bool` | `true` | Whether to call `SaveChangesAsync` immediately. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Permanently removes multiple entities. Delegates to `DeleteRangeAsync` internally.

---

#### DeleteRangeAsync(IEnumerable\<T\> entities, bool saveNow = true, CancellationToken cancellationToken = default)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `entities` | `IEnumerable<T>` | — | The entities to permanently delete. |
| `saveNow` | `bool` | `true` | Whether to call `SaveChangesAsync` immediately. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Permanently removes a collection of entities from the database using `RemoveRange`. Returns `true` on success; logs and rethrows on failure.

---

## RepositoryManager

**Namespace:** `JC.Core.Services.DataRepositories`

Unit of work implementation providing thread-safe repository caching and transaction management. Repositories are cached in a `ConcurrentDictionary` and resolved from DI on first access. Implements `IDisposable` and `IAsyncDisposable` for transaction cleanup. Inject via `IRepositoryManager`.

### Methods

#### GetRepository\<T\>()

**Returns:** `IRepositoryContext<T>`

**Constraint:** `T : class`

Retrieves (or creates and caches) the repository context for the specified entity type. On first call for a given `T`, resolves `IRepositoryContext<T>` from the service provider. Subsequent calls return the cached instance.

---

#### BeginTransactionAsync(CancellationToken cancellationToken = default)

**Returns:** `Task<IDbContextTransaction>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Begins a new database transaction on the underlying `DbContext`. Stores a reference to the transaction for use by `CommitTransactionAsync` and `RollbackTransactionAsync`.

---

#### CommitTransactionAsync(CancellationToken cancellationToken = default)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Calls `SaveChangesAsync` on the underlying `DbContext`, then commits the current transaction and disposes it. Throws `InvalidOperationException` if no transaction has been started.

---

#### RollbackTransactionAsync(CancellationToken cancellationToken = default)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Rolls back the current transaction and disposes it, discarding all pending changes. Throws `InvalidOperationException` if no transaction has been started.

---

#### SaveChangesAsync(CancellationToken cancellationToken = default)

**Returns:** `Task<int>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Persists all pending changes to the database without committing or rolling back a transaction. Returns the number of state entries written. Use this when batching operations with `saveNow: false` outside of an explicit transaction.

---

## AuditCleanupJob

**Namespace:** `JC.Core.Services`

**Implements:** `IBackgroundJob`

Deletes audit entries older than the configured retention period. Respects minimum retention records (globally or per table) and processes deletions in configurable chunks.

The job queries all `AuditEntry` records with an `AuditDate` before the cutoff (`DateTime.UtcNow` minus `AuditRetentionMonths`), ordered by `AuditDate` descending. If `CleanupChunkingValue` is greater than zero, the result set is truncated to that size. If `MinimumRetentionRecords` is set, the job ensures that many entries are retained — either per table (when `RetentionRecordsPerTable` is `true`, grouping by `TableName`) or globally. Entries beyond the retention minimum are hard-deleted via `IRepositoryContext<AuditEntry>.DeleteRangeAsync`.

Controlled by `CoreBackgroundJobOptions.RegisterAuditCleanupJob` — if `false`, `ExecuteAsync` returns immediately. See [Setup](Setup.md#configurecorebackgroundjobs--background-job-options) for configuration.

---

## SoftDeleteCleanupJob

**Namespace:** `JC.Core.Services`

**Implements:** `IBackgroundJob`

Hard-deletes soft-deleted entities that have exceeded the configured retention period. Automatically discovers all soft-deletable entity types registered in the `DbContext` model.

On execution, the job inspects `DbContext.Model.GetEntityTypes()` and identifies types that either extend `AuditModel` (which has `IsDeleted` built in) or have their own `bool IsDeleted` property (detected via reflection). For each qualifying type not in the `SoftDeleteRetentionBlacklist`, it invokes a generic cleanup method via reflection (`MakeGenericMethod`).

For `AuditModel` entities, the cleanup filters by `IsDeleted == true` and `DeletedUtc < cutoff` directly in the database query. For non-`AuditModel` entities, it loads all records and filters in memory (EF Core cannot translate reflection-based property access). Matching entities are removed via `DbSet<T>.RemoveRange` and persisted with `SaveChangesAsync`.

Controlled by `CoreBackgroundJobOptions.RegisterSoftDeleteCleanupJob` — if `false`, `ExecuteAsync` returns immediately. Errors for individual entity types are logged and do not halt processing of remaining types. See [Setup](Setup.md#configurecorebackgroundjobs--background-job-options) for configuration.

---

# Helpers

## PaginationHelper

**Namespace:** `JC.Core.Helpers`

Static helper methods for paginating collections and queryables with page validation and skip/take logic. Used internally by `PaginationExtensions` but available for direct use.

### Methods

#### PaginateList\<T\>(IEnumerable\<T\> items, int pageNumber, int pageSize)

**Returns:** `PagedList<T>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `items` | `IEnumerable<T>` | — | The source collection. Materialised to a `List<T>` internally. |
| `pageNumber` | `int` | — | The requested page number (1-based). Clamped to the valid range. |
| `pageSize` | `int` | — | The number of items per page. |

Materialises the collection, validates and clamps the page number to the valid range, applies skip/take, and returns a `PagedList<T>`.

---

#### PaginateQueryable\<T\>(IQueryable\<T\> items, int pageNumber, int pageSize, int totalCount)

**Returns:** `IQueryable<T>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `items` | `IQueryable<T>` | — | The source queryable. |
| `pageNumber` | `int` | — | The requested page number (1-based). Clamped to the valid range. |
| `pageSize` | `int` | — | The number of items per page. |
| `totalCount` | `int` | — | The pre-computed total count of items, used for page validation without executing an additional query. |

Validates and clamps the page number, then applies `Skip` and `Take` to the queryable. Returns the queryable with pagination applied — the caller is responsible for materialising it.

---

## ColourHelper

**Namespace:** `JC.Core.Helpers`

Static helper for colour manipulation using hexadecimal colour strings in `"#RRGGBB"` format.

### Methods

#### HoverColour(string col)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `col` | `string` | — | A hex colour string in `"#RRGGBB"` format. |

Generates a lightened hover variant of the given colour by blending each RGB channel 40% towards white. Returns the result as a `"#RRGGBB"` hex string.

---

#### FontColour(string col)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `col` | `string` | — | A hex colour string in `"#RRGGBB"` format. |

Calculates the relative luminance of the background colour using the formula `0.2126R + 0.7152G + 0.0722B` (with normalised RGB values). Returns `"#000000"` (black) if the luminance exceeds 0.5, or `"#ffffff"` (white) otherwise. Use this to ensure readable text on coloured backgrounds.

---

## CountryHelper

**Namespace:** `JC.Core.Helpers`

Static helper for retrieving country names and ISO codes derived from .NET's built-in culture/region data. Results are cached after the first call.

### Methods

#### GetCountries(ILogger? logger = null)

**Returns:** `IReadOnlyList<Country>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `logger` | `ILogger?` | `null` | Optional logger for warnings when a culture fails to resolve to a `RegionInfo`. |

Returns all countries derived from `CultureInfo.GetCultures(CultureTypes.SpecificCultures)`, deduplicated by ISO code and sorted alphabetically by name. Results are cached in a static field after the first invocation.

---

#### GetCountriesDictionary()

**Returns:** `Dictionary<string, string>`

Returns all countries as a dictionary mapping ISO 3166-1 alpha-2 codes to country names. Calls `GetCountries` internally.

---

#### GetCountryName(string code)

**Returns:** `string?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `code` | `string` | — | An ISO 3166-1 alpha-2 country code (e.g. `"GB"`). |

Returns the English country name for the given code, or `null` if no match is found. Comparison is case-insensitive.

---

#### GetCountryCode(string name)

**Returns:** `string?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The English country name (e.g. `"United Kingdom"`). |

Returns the ISO 3166-1 alpha-2 code for the given country name, or `null` if no match is found. Comparison is case-insensitive.

---

## ConstHelper

**Namespace:** `JC.Core.Helpers`

Static reflection-based helper for discovering constant fields on a type.

### Methods

#### GetAllConsts\<T\>()

**Returns:** `Dictionary<string, object>`

Returns all `const` fields declared on `T` (including inherited fields) as a dictionary mapping field names to their values. Inspects public, non-public, and static fields with `FlattenHierarchy` to include constants from base classes.

---

# Extensions

## QueryExtensions

**Namespace:** `JC.Core.Extensions`

Static extension methods for filtering `AuditModel` queryables by soft-delete status.

### Methods

#### FilterDeleted\<T\>(this IQueryable\<T\> query, DeletedQueryType deletedQueryType)

**Returns:** `IQueryable<T>`

**Constraint:** `T : AuditModel`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `query` | `IQueryable<T>` | — | The source queryable to filter. |
| `deletedQueryType` | `DeletedQueryType` | — | The deletion filter to apply. |

Applies a `Where` clause based on the `deletedQueryType`: `OnlyActive` returns entities where `IsDeleted` is `false`, `OnlyDeleted` returns entities where `IsDeleted` is `true`, and `All` returns all entities regardless of deletion status. Only available on queryables of types extending `AuditModel`.

---

## PaginationExtensions

**Namespace:** `JC.Core.Extensions`

Static extension methods for paginating `IEnumerable<T>` and `IQueryable<T>` collections into `PagedList<T>`.

### Methods

#### ToPagedList\<T\>(this IEnumerable\<T\> source, int pageNumber, int pageSize)

**Returns:** `PagedList<T>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `source` | `IEnumerable<T>` | — | The source in-memory collection. |
| `pageNumber` | `int` | — | The requested page number (1-based). |
| `pageSize` | `int` | — | The number of items per page. |

Materialises the entire collection, then applies skip/take logic to return the requested page. Suitable for in-memory collections only.

---

#### ToPagedListAsync\<T\>(this IQueryable\<T\> source, int pageNumber, int pageSize)

**Returns:** `Task<PagedList<T>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `source` | `IQueryable<T>` | — | The source EF Core queryable. |
| `pageNumber` | `int` | — | The requested page number (1-based). |
| `pageSize` | `int` | — | The number of items per page. |

Executes two database queries: one `CountAsync` for the total count, and one with `Skip`/`Take` for the page data. Returns a `PagedList<T>` with the results and metadata.

---

#### ToPagedList\<T\>(this IQueryable\<T\> source, int pageNumber, int pageSize)

**Returns:** `PagedList<T>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `source` | `IQueryable<T>` | — | The source EF Core queryable. |
| `pageNumber` | `int` | — | The requested page number (1-based). |
| `pageSize` | `int` | — | The number of items per page. |

Synchronous version of `ToPagedListAsync`. Executes two database queries: one `Count` for the total count, and one with `Skip`/`Take` for the page data.

---

## StringExtensions

**Namespace:** `JC.Core.Extensions`

Static extension methods for common string operations.

### Methods

#### Truncate(this string value, int maxLength, string suffix = "...")

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `value` | `string` | — | The string to truncate. |
| `maxLength` | `int` | — | The maximum total length of the returned string, including the suffix. |
| `suffix` | `string` | `"..."` | The suffix to append when truncation occurs. |

Returns the original string unchanged if it is shorter than or equal to `maxLength`. If `maxLength` is less than or equal to the suffix length, returns the suffix truncated to `maxLength`. Otherwise, returns the first `maxLength - suffix.Length` characters followed by the suffix.

---

#### ToSlug(this string value)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `value` | `string` | — | The string to convert into a URL-friendly slug. |

Lowercases the input, replaces all non-alphanumeric characters (except hyphens) with hyphens, collapses consecutive hyphens into a single hyphen, and trims leading/trailing hyphens. Returns an empty string if the input is null or whitespace.

---

#### ToTitleCase(this string value, CultureInfo? culture = null)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `value` | `string` | — | The string to convert to title case. |
| `culture` | `CultureInfo?` | `null` | The culture whose casing rules are used. Defaults to `CultureInfo.CurrentCulture` when `null`. |

Converts the input to lowercase first, then applies the culture's `TextInfo.ToTitleCase` rules, capitalising the first letter of each word. Returns the input unchanged if null or whitespace.

---

#### Mask(this string value, int visibleChars)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `value` | `string` | — | The string to mask. |
| `visibleChars` | `int` | — | The number of leading characters to keep visible. Clamped to 0 if negative. |

Keeps the first `visibleChars` characters visible and replaces the remainder with asterisks. Returns the original string if it is null, empty, or shorter than `visibleChars`.

---

## DateTimeExtensions

**Namespace:** `JC.Core.Extensions`

Static extension methods for common `DateTime` operations.

### Methods

#### ToRelativeTime(this DateTime dateTime)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `dateTime` | `DateTime` | — | The date and time to express as relative time. |

Compares the input to `DateTime.UtcNow` and returns a human-readable relative time string. Handles both past and future dates. Past dates produce strings like "just now", "5 minutes ago", "yesterday", "3 weeks ago", "1 year ago". Future dates produce "tomorrow", "in 5 minutes", "in 3 days". The "just now" label is used for differences under 60 seconds in either direction.

---

#### ToFriendlyDate(this DateTime dateTime, CultureInfo? culture = null)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `dateTime` | `DateTime` | — | The date to format. |
| `culture` | `CultureInfo?` | `null` | The culture to use for formatting. Defaults to `CultureInfo.CurrentCulture` when `null`. |

Formats the date using the pattern `"dddd d MMMM yyyy"`, producing output like "Monday 5 March 2026".

---

#### Age(this DateTime dateOfBirth)

**Returns:** `int`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `dateOfBirth` | `DateTime` | — | The date of birth. |

Calculates a person's age in whole years from their date of birth relative to `DateTime.Today`. Correctly accounts for whether this year's birthday has already occurred.

---

## EnumExtensions

**Namespace:** `JC.Core.Extensions`

Static extension methods for enum operations.

### Methods

#### GetAllOptions\<T\>(this T _)

**Returns:** `List<(string Name, int Value)>`

**Constraint:** `T : struct, Enum`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `_` | `T` | — | An instance of the enum type (can be `default`). Used only for type inference. |

Returns all members of the enum type as a list of tuples containing the member name and its integer value.

---

#### ToDisplayName(this Enum value)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `value` | `Enum` | — | The enum value to convert. |

Converts an enum value's name to a human-readable string. Replaces underscores with spaces, inserts spaces before uppercase letters in PascalCase (handling acronyms like "XML" correctly), and capitalises the first letter of each word. Supports PascalCase (e.g. `InProgress` → "In Progress"), SCREAMING_CASE (e.g. `PENDING_APPROVAL` → "Pending Approval"), and acronym prefixes (e.g. `XMLParser` → "XML Parser").

---

#### GetDescription(this Enum value)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `value` | `Enum` | — | The enum value to describe. |

Returns the value of the `[Description]` attribute on the enum member, if present. Falls back to `ToDisplayName` if no `DescriptionAttribute` is found or the field cannot be resolved.

---

#### TryParse\<T\>(string? value, T defaultValue = default)

**Returns:** `T`

**Constraint:** `T : struct, Enum`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `value` | `string?` | — | The string to parse into the enum type. |
| `defaultValue` | `T` | `default` | The fallback value if parsing fails. |

Attempts to parse the string into the specified enum type using case-insensitive matching. Returns `defaultValue` if the input is null, whitespace, or does not match any member. This is a static method, not an extension method.

---

# Data

## DataDbContext

**Namespace:** `JC.Core.Data`

EF Core `DbContext` implementation for the core data model. Extends `DbContext` and implements `IDataDbContext`. Overrides `SaveChangesAsync` to automatically create audit trail entries via the change tracker before and after saving.

On `SaveChangesAsync`, the context inspects the change tracker for added, modified, and deleted entities. Non-create changes are logged immediately. Create entries are deferred until after `base.SaveChangesAsync` completes so that database-generated IDs are available, then logged in a second save pass. `LogModel` entities skip create audit (the log is its own record), log hard deletes, and throw `InvalidOperationException` on update/soft-delete/restore. `AuditEntry` entities skip both create and hard delete audit, and throw on update/soft-delete/restore.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `AuditEntries` | `DbSet<AuditEntry>` | get; set; | The set of audit trail records. |
