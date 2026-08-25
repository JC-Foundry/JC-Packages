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

Entity representing a single audit trail record capturing who performed what action, on which table, against which entity, and when. Persisted automatically by `DataDbContext.SaveChangesAsync` via the change tracker. Records two actors — the context-level user (`UserId`) and the actor stamped on the entity by the repository layer (`ActionUserId`) — plus the writing application (`SourceApplication`). See the [Guide](Guide.md#context-user-vs-action-user) for how they relate.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | `Guid.NewGuid().ToString()` | get; private set; | Unique identifier for this audit entry. |
| `Action` | `AuditAction` | — | get; set; | The type of action that was performed. |
| `AuditDate` | `DateTime` | — | get; set; | UTC timestamp of when the action occurred. |
| `UserId` | `string?` | `null` | get; set; | Context-level identifier of the user who performed the action — the ambient `IUserInfo.UserId` resolved by the saving context, or `IUserInfo.MissingUserInfoId` (`"<NONE>"`) when the context has no identity. |
| `UserName` | `string?` | `null` | get; set; | Display name of the context-level user who performed the action. |
| `ActionUserId` | `string?` | `null` | get; set; | Identifier stamped onto the entity's own audit field for this action (`CreatedById`, `LastModifiedById`, `DeletedById`, or `RestoredById`). This is the actor recorded by the repository/manager layer and can be more accurate than `UserId`. `null` for non-auditable entities and for actions with no stamped field (hard delete). |
| `SourceApplication` | `string?` | `null` | get; set; | Name of the application that wrote this entry, from `CoreAuditOptions.ApplicationName` (set via `AddCore(applicationName)`). `null` when the writing application did not configure one. |
| `IsActionIdPreferred` | `bool` | `false` | get; set; | Whether `ActionUserId` should be preferred over `UserId` as the true actor. `true` when `ActionUserId` is populated, is not `IUserInfo.MissingUserInfoId`, and differs from `UserId`; otherwise `false`. |
| `TableName` | `string?` | `null` | get; set; | The database table name affected by the action. |
| `EntityKey` | `string?` | `null` | get; set; | JSON-serialised primary key of the audited entity, keyed by property name (e.g. `{"Id":"abc"}` or, for composite keys, `{"ThreadId":"abc","UserId":"xyz"}`). `null` for keyless entities or if serialisation fails. |
| `ActionData` | `string?` | `null` | get; set; | JSON-serialised entity data. For creates, contains all non-null property values. For updates, contains a `From`/`To` diff of modified properties. |

Every column except `ActionData` is length-constrained: `Id` 36, `EntityKey` 512, and `UserId`, `UserName`, `ActionUserId`, `SourceApplication` and `TableName` 256 each. `ActionData` is deliberately unbounded, since an entity snapshot has no sensible ceiling.

These lengths come from `AuditEntryMapping`, not from the model's attributes — fluent configuration wins over data annotations in EF Core, so the mapping is what shapes the columns.

The audit service truncates each value to fit before writing, so an over-long table name or key never fails the save. Truncation is silent — a value longer than its column is shortened rather than rejected.

---

## IMultiTenancy

**Namespace:** `JC.Core.Models.MultiTenancy`

Contract for entities that belong to a tenant. It lives in JC.Core so that any package with domain models can mark an entity tenant-scoped without depending on the tenancy engine. Within the suite it is implemented by `SavedFile` (JC.FileStorage); other packages' entities are scoped by their owning user or are deliberately system-wide, so they do not implement it.

Nothing in JC.Core acts on the mark. JC.Tenancy's `ApplyTenantFilters` installs the global query filters, and it is the consuming application that calls it, per DbContext. **Without JC.Tenancy there is no query filter**, so implementing this interface alone enforces nothing — the column is simply carried unused.

This marks a partition, not a relationship. No foreign key is configured and there is no navigation property, because the tenant record may live in another context or another database entirely; an application whose model holds both may configure one itself.

A `null` `TenantId` is not a shared or global scope. It is a scope of its own, isolated exactly like any named tenant: the filter matches `TenantId == null` when the current tenant is null, and matches the tenant exactly otherwise.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `TenantId` | `string?` | get; set; | The tenant identifier this entity belongs to. |

---

## ITenantContext

**Namespace:** `JC.Core.Models.MultiTenancy`

The tenant the current operation is scoped to, and what is known about it. Registered scoped by JC.Tenancy, whose `ITenantInfo` extends this interface with the members that need the concrete tenant record.

It lives in JC.Core so a package can read the operational tenant for entities it has marked `IMultiTenancy` without referencing JC.Tenancy. Resolve it **optionally** — with `GetService` rather than `GetRequiredService` — because an application without tenancy registered has no implementation, which means the null partition.

This is not `IUserInfo.TenantId`. That is the tenant assigned to the current user; this is the tenant the operation is running against, and the two differ whenever a background job or an administrator deliberately works elsewhere. Data access follows this one.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `TenantId` | `string?` | get; set; | The tenant this operation is scoped to, or `null` for the null partition. Read live from the current user unless assigned; assigning overrides that for the rest of the scope, including assigning `null` to pin the null partition deliberately. |
| `HasTenant` | `bool` | get; | Whether a tenant is in scope, as opposed to the null partition. |
| `IsOverridden` | `bool` | get; | Whether the tenant was set explicitly rather than derived from the current user. |
| `Name` | `string?` | get; | The tenant's name, or `null` in the null partition. |
| `Description` | `string?` | get; | The tenant's description, if it has one. |
| `Domain` | `string?` | get; | The domain associated with the tenant, if it has one. |
| `MaxUsers` | `uint?` | get; | The maximum number of users allowed in this tenant, if one is set. |
| `ExpiryDateUtc` | `DateTime?` | get; | When this tenant expires, if an expiry is set. |
| `IsExpired` | `bool` | get; | Whether the tenant's expiry has passed. Reported, never enforced — whether an expired tenant may still be used is application policy. |

### Methods

#### GetSetting(string key)

**Returns:** `string?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `key` | `string` | — | The setting key. |

Returns the value of an active tenant setting, or `null` where the key is absent or the setting is inactive.

---

#### GetSetting\<T\>(string key, T? defaultValue = default)

**Returns:** `T?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `key` | `string` | — | The setting key. |
| `defaultValue` | `T?` | `default` | The value returned when the key is absent, inactive, or cannot be converted. |

Reads the setting and converts it to `T`. Returns `defaultValue` rather than throwing where the key is missing or the stored value cannot be converted, because a malformed setting is consuming-application data rather than a framework fault.

---

## IApplicationUser

**Namespace:** `JC.Core.Models`

How the suite stores a user. Describes **any** user record, not only the one currently signed in — an administrator loading somebody else's account holds an `IApplicationUser`, not an `IUserInfo`.

Read/write, because storage is not a one-way concern. Which store stands behind it — ASP.NET Identity, an externally supplied record, something else — is not this contract's business, which is what lets a package read a user without referencing an identity package.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Id` | `string` | get; set; | Unique identifier of the user record. |
| `UserName` | `string?` | get; set; | The username, if one is set. |
| `Email` | `string?` | get; set; | The email address, if one is set. |
| `EmailConfirmed` | `bool` | get; set; | Whether the email address has been confirmed. |
| `PhoneNumber` | `string?` | get; set; | The phone number, if one is set. |
| `PhoneNumberConfirmed` | `bool` | get; set; | Whether the phone number has been confirmed. |
| `TwoFactorEnabled` | `bool` | get; set; | Whether two-factor authentication is enabled. |
| `LockoutEnabled` | `bool` | get; set; | Whether lockout is enabled for this account. |
| `LockoutEnd` | `DateTimeOffset?` | get; set; | When the lockout ends, if the account is locked out. Note the offset type — `IUserInfo.LockoutEnd` is a plain `DateTime?`. |
| `AccessFailedCount` | `int` | get; set; | Consecutive failed access attempts. |
| `DisplayName` | `string?` | get; set; | The display name, if one is set. |
| `IsEnabled` | `bool` | get; set; | Whether the account is enabled. |
| `RequirePasswordChange` | `bool` | get; set; | Whether the user must change their password before continuing. Projected onto `IUserInfo.RequiresPasswordChange` — note the differing names. |
| `LastLoginUtc` | `DateTime?` | get; set; | When the user last logged in, if ever. |
| `RegistrationUtc` | `DateTime?` | get; set; | When the user registered. |
| `IdentityTenantId` | `string?` | get; set; | The tenant that owns the authoritative identity record. **Not** interchangeable with `IUserInfo.TenantId`, which means the tenant assigned to the user inside the consuming application. For local ASP.NET Identity the two commonly hold the same value; for an externally supplied identity they need not. |

---

## IUserInfo

**Namespace:** `JC.Core.Models`

The current user's identity, profile, security state and authorisation details — a runtime projection of whoever is executing the current operation, not a persisted record. For that, see [IApplicationUser](#iapplicationuser).

Registered scoped and populated **in place**, so constructing an instance and passing it around does not make it ambient. In a web application `UserInfoMiddleware` (JC.Identity.Shared.Web) projects the current principal onto it per request; outside one, `UserInfoExtensions` in JC.Identity.Shared establishes it explicitly. When no implementation is registered at all, the repository layer falls back to `IUserInfo.MissingUserInfoId` for audit fields.

Despite the read-only intent implied by the name, every member has a setter — the projection depends on filling an existing instance rather than replacing it.

### Constants

| Constant | Type | Value | Description |
|----------|------|-------|-------------|
| `MissingUserInfoId` | `string` | `"<NONE>"` | Fallback user identifier used when `IUserInfo` is not resolved from DI. |
| `SYSTEM_USER_ID` | `string` | `"System__ID"` | User ID assigned when no identity is present on the request. |
| `SYSTEM_USER_NAME` | `string` | `"System"` | Username assigned when no identity is present on the request. |
| `SYSTEM_USER_EMAIL` | `string` | `"<SYSTEM@EMAIL>"` | Email assigned when no identity is present on the request. |
| `UNKNOWN_USER_ID` | `string` | `"Unknown__ID"` | User ID assigned when an identity is present but not authenticated. Also the default field value on `UserInfo`. |
| `UNKNOWN_USER_NAME` | `string` | `"Unknown"` | Username assigned when an identity is present but not authenticated. Also the default field value on `UserInfo`. |
| `UNKNOWN_USER_EMAIL` | `string` | `"<UNKNOWN@EMAIL>"` | Email assigned when an identity is present but not authenticated. Also the default field value on `UserInfo`. |

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Authority` | `IdentityAuthority` | get; set; | Which authority authenticated the current user and supplied their identity. Implementations other than local ASP.NET Identity must set this explicitly. |
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
| `TenantId` | `string?` | get; set; | The tenant assigned to this user inside the consuming application. Distinct from `ITenantContext.TenantId`, which is the tenant the current operation runs against — data access follows that one, not this. |
| `DisplayName` | `string?` | get; set; | The user's display name. |
| `LastLoginUtc` | `DateTime?` | get; set; | UTC timestamp of the user's last login. |
| `RegistrationUtc` | `DateTime?` | get; set; | UTC timestamp of the user's registration. |
| `IsEnabled` | `bool` | get; set; | Whether the user account is enabled. |
| `RequiresPasswordChange` | `bool` | get; set; | Whether the user must change their password. Note the name — the persisted counterpart on `IApplicationUser` is `RequirePasswordChange`. |
| `IsSetup` | `bool` | get; set; | Whether the user info has been populated for this scope. A projection leaves an instance alone where this is already `true`. |
| `HasTenant` | `bool` | get; | Whether the user has a tenant assigned. Derived from `TenantId`, so it cannot disagree with it. |
| `Roles` | `IReadOnlyList<string>` | get; set; | Role names assigned to the current user, in the consuming application's own authorisation domain. |
| `Claims` | `IReadOnlyList<Claim>` | get; set; | All claims associated with the current user. |

### Methods

#### IsInRole(string role)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `role` | `string` | — | The role name to check. |

Determines whether the current user belongs to the specified role. Returns `true` if the user has the role; otherwise `false`.

---

#### SystemUser\<T\>()

**Returns:** `IUserInfo`

**Constraint:** `where T : IUserInfo, new()`

Static interface member constructing a new `T` populated with `SYSTEM_USER_ID`, `SYSTEM_USER_NAME` and `SYSTEM_USER_EMAIL`. Called as `IUserInfo.SystemUser<UserInfoBase>()`.

Constructs an instance; it does **not** make it ambient. `IUserInfo` is registered scoped and populated in place, so nothing that injects `IUserInfo` will observe the returned object. Use JC.Identity.Shared's scope helpers to establish an ambient identity.

---

#### UnknownUser\<T\>()

**Returns:** `IUserInfo`

**Constraint:** `where T : IUserInfo, new()`

As `SystemUser<T>`, but populated with `UNKNOWN_USER_ID`, `UNKNOWN_USER_NAME` and `UNKNOWN_USER_EMAIL`. The same caveat about ambience applies.

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

## IdentityAuthority

**Namespace:** `JC.Core.Enums`

Identifies which authority authenticated the current user and supplied their identity to the consuming application. Exposed as `IUserInfo.Authority`.

This is **not** the login method. Somebody who signs into a central portal with an external provider and is then passed through to a consuming application has an authority of `CAP` — the authority is whoever handed the application the identity, not however that party established it in the first place.

Each member also carries a `[Description]` attribute, readable through `EnumExtensions.GetDescription()`.

| Member | Value | Description |
|--------|-------|-------------|
| `None` | `0` | No authentication took place. The user info holds its system or unknown defaults, which is the expected state for unauthenticated requests and background work. The zero value deliberately, so an authority that never declares itself cannot pass as local. |
| `Local` | `1` | The application authenticated the user against its own persisted identity store. Stated by JC.Identity at registration. |
| `CAP` | `2` | The Central Admin Portal authenticated the user and supplied the identity by SSO. |
| `Custom` | `3` | An authentication mechanism the consuming application supplied itself. |

---

# Services

## RepositoryContext\<T\>

**Namespace:** `JC.Core.Services.DataRepositories`

Generic repository providing full CRUD, soft-delete, and restore operations for entity type `T`. Automatically populates creation fields for entities extending `BaseCreateModel` (both `AuditModel` and `LogModel`), full lifecycle fields for `AuditModel` entities, and falls back to reflection-based `IsDeleted` property detection for non-`AuditModel` entities. Obtained via `IRepositoryManager.GetRepository<T>()`, or `IRepositoryManager.For<TContext>().GetRepository<T>()` for a managed context.

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

Unit of work implementation providing thread-safe repository caching and transaction management. Repositories are created on first access and cached in a `ConcurrentDictionary`. Manager instances bound to other contexts via `For<T>()` are cached the same way. Implements `IDisposable` and `IAsyncDisposable` for transaction and bound-manager cleanup. Inject via `IRepositoryManager`.

### Methods

#### GetRepository\<T\>()

**Returns:** `IRepositoryContext<T>`

**Constraint:** `T : class`

Retrieves (or creates and caches) the repository context for the specified entity type, bound to this manager's `DbContext`. On first call for a given `T`, a `RepositoryContext<T>` is created; subsequent calls return the cached instance. Works for any class — no prior registration of the entity type is required.

---

#### For\<T\>()

**Returns:** `IRepositoryManager`

**Constraint:** `T : DbContext`

Returns a repository manager bound to the specified `DbContext` type, resolved from the service provider. Repositories obtained from the returned manager (via `GetRepository<T>()`) and its transactions operate against that context. The bound manager is cached per context type for the lifetime of the scope, so repeated calls for the same context return the same instance. Requesting the context this manager is already bound to returns this same manager, so it shares any in-progress transaction rather than starting a second one on the same connection. Use this to read and write additional (managed) contexts; the parameterless members continue to target the default context registered with `AddCore`.

---

#### For(Type contextType)

**Returns:** `IRepositoryManager`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `contextType` | `Type` | — | The data context type to bind to. Must derive from `DbContext`. |

Non-generic overload of `For<T>()`, for callers that only have a `Type` at runtime rather than a compile-time type argument. Behaves identically, including per-context caching — `For(typeof(PortfolioDbContext))` and `For<PortfolioDbContext>()` return the same manager instance. Throws `ArgumentException` if `contextType` does not derive from `DbContext`, and propagates the underlying `InvalidOperationException` if the context is not registered with the service provider.

---

#### BeginTransactionAsync(CancellationToken cancellationToken = default)

**Returns:** `Task<IDbContextTransaction>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Begins a database transaction on the underlying `DbContext` and stores a reference to it for use by `CommitTransactionAsync` and `RollbackTransactionAsync`. If a transaction has already been started on this manager, the existing transaction is returned rather than a new one being created.

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

## AuditCleanupJob\<TContext\>

**Namespace:** `JC.Core.Services`

**Implements:** `IBackgroundJob`

**Constraint:** `TContext : DbContext`

Deletes audit entries in `TContext`'s database older than the configured retention period. Respects minimum retention records (globally or per table) and processes deletions in configurable chunks. A non-generic `AuditCleanupJob` is also provided that targets your default context; use the generic form (e.g. `AuditCleanupJob<AppDbContext>`) to target a specific managed context.

The job resolves the `AuditEntry` repository for `TContext` via `IRepositoryManager.For<TContext>()`, then queries all `AuditEntry` records with an `AuditDate` before the cutoff (`DateTime.UtcNow` minus `AuditRetentionMonths`), ordered by `AuditDate` descending. If `AuditCleanupChunkingValue` is greater than zero, the result set is truncated to that size. If `MinimumRetentionRecords` is set, the job ensures that many entries are retained — either per table (when `RetentionRecordsPerTable` is `true`, grouping by `TableName`) or globally. Entries beyond the retention minimum are hard-deleted via `IRepositoryContext<AuditEntry>.DeleteRangeAsync`.

Controlled by `CoreBackgroundJobOptions.EnableAuditCleanupJob` — if `false`, `ExecuteAsync` returns immediately. See [Setup](Setup.md#configurecorebackgroundjobs--background-job-options) for configuration.

---

## SoftDeleteCleanupJob\<TContext\>

**Namespace:** `JC.Core.Services`

**Implements:** `IBackgroundJob`

**Constraint:** `TContext : DbContext`

Hard-deletes soft-deleted entities in `TContext` that have exceeded the configured retention period. Automatically discovers all soft-deletable entity types in the context model. A non-generic `SoftDeleteCleanupJob` (extending `SoftDeleteCleanupJob<DbContext>`) is also provided; it targets the default context registered with `AddCore`.

On execution, the job inspects `TContext.Model.GetEntityTypes()` and identifies types that either extend `AuditModel` (which has `IsDeleted` built in) or have their own `bool IsDeleted` property (detected via reflection). For each qualifying type not in the `SoftDeleteRetentionBlacklist`, it invokes a generic cleanup method via reflection (`MakeGenericMethod`).

For `AuditModel` entities, the cleanup filters by `IsDeleted == true` and `DeletedUtc < cutoff` directly in the database query. For non-`AuditModel` entities, it builds an EF-translatable expression tree to filter by `IsDeleted == true` in the database — note that this path does not filter by date, so all soft-deleted records are removed regardless of when they were deleted. Matching entities are removed via `DbSet<T>.RemoveRange` and persisted with `SaveChangesAsync`.

Controlled by `CoreBackgroundJobOptions.EnableSoftDeleteCleanupJob` — if `false`, `ExecuteAsync` returns immediately. Errors for individual entity types are logged and do not halt processing of remaining types. See [Setup](Setup.md#configurecorebackgroundjobs--background-job-options) for configuration.

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

## CoreHelpers

**Namespace:** `JC.Core.Helpers`

Static helper carrying the suite's own identifiers, and the shared display-name normalisation that `StringExtensions.ToDisplayName` and `EnumExtensions` are built on. That normalisation itself is `internal`; only the identifiers and `PackageDisplay` are callable from outside the package.

### Fields

| Field | Type | Value | Access | Description |
|-------|------|-------|--------|-------------|
| `PackageName` | `const string` | `JCP` | public | The suite's short name. |
| `PackageVersionPrefix` | `const string` | `v` | public | The prefix placed before the version in a display string. |
| `PackageVersion` | `const string` | The current suite version | public | Hand-maintained, and not derived from the assembly version. It has to be bumped by hand each release. |

### Methods

#### PackageDisplay(string introText = "Using", string? displayNameOverride = null, string? versionPrefixOverride = null)

**Returns:** `string`

**Static.**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `introText` | `string` | `"Using"` | Text placed before the package information. Trimmed before use. |
| `displayNameOverride` | `string?` | `null` | Replaces `PackageName`. Ignored when null or whitespace. |
| `versionPrefixOverride` | `string?` | `null` | Replaces `PackageVersionPrefix`. Ignored when null or whitespace. |

Builds a single line of the form `{introText} {name} {prefix}{PackageVersion}`, intended for a footer or a startup banner.

Throws `ArgumentException` when `introText` is null or whitespace. The two override parameters behave differently: a null or whitespace value falls back to the default rather than throwing, so only `introText` can fail the call.

---

# Extensions

## AuditEntryExtensions

**Namespace:** `JC.Core.Extensions`

Static extension methods on `IRepositoryManager` for querying the audit trail. Because it extends `IRepositoryManager`, the caller chooses which context's trail to query — `repos.QueryAuditEntries(...)` targets the default context, `repos.For<TContext>().QueryAuditEntries(...)` a managed one.

### AuditEntryTrailSearch

Nested record describing the primary axis of an audit trail search.

| Property | Type | Description |
|----------|------|-------------|
| `KeyIsUserId` | `bool` | When `true`, the search is scoped to a user (matched against the effective actor); when `false`, it is scoped to a table name. |
| `SearchKey` | `string` | The user identifier or table name to scope by, per `KeyIsUserId`. |

### Methods

#### QueryAuditEntries(this IRepositoryManager repos, AuditEntryTrailSearch trailSearch, string? search, AuditAction? action, string? appName)

**Returns:** `IQueryable<AuditEntry>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `repos` | `IRepositoryManager` | — | The manager whose bound context's audit trail is queried. |
| `trailSearch` | `AuditEntryTrailSearch` | — | The primary search axis — by user (effective actor) or by table name. |
| `search` | `string?` | — | Optional free-text filter. When scoped by user, matches `TableName`/`EntityKey`; when scoped by table, matches the effective actor id, `UserName`, and `EntityKey`. Case-insensitive. |
| `action` | `AuditAction?` | — | Optional filter restricting results to a single action type. |
| `appName` | `string?` | — | Optional filter restricting results to entries whose `SourceApplication` matches exactly. |

Builds an untracked `IQueryable<AuditEntry>` over the bound context's `AuditEntry` set. When `trailSearch.KeyIsUserId` is `true`, matches the *effective* actor — entries where `IsActionIdPreferred` is `true` are matched on `ActionUserId`, otherwise on `UserId`; when `false`, matches on `TableName`. The optional `appName`, `action`, and `search` filters are then applied in turn. The query is returned unmaterialised and unordered — the caller applies its own ordering and pagination (e.g. `ToPagedListAsync`).

---

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
| `maxLength` | `int` | — | The number of characters to keep from the original string before appending the suffix. The total returned length is `maxLength + suffix.Length`. |
| `suffix` | `string` | `"..."` | The suffix to append when truncation occurs. |

Returns the original string unchanged if it is shorter than or equal to `maxLength`. Otherwise, returns the first `maxLength` characters followed by the suffix.

---

#### ToSlug(this string value, bool normaliseToDisplayName = false)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `value` | `string` | — | The string to convert into a URL-friendly slug. |
| `normaliseToDisplayName` | `bool` | `false` | When `true`, the value is passed through the display name normaliser first, so word boundaries in PascalCase and underscore-separated input become hyphens. When `false`, the input is slugified as-is. |

Trims the input, lowercases it, replaces all non-alphanumeric characters (except hyphens) with hyphens, collapses consecutive hyphens into a single hyphen, and trims leading/trailing hyphens. Returns an empty string if the input is null or whitespace.

With `normaliseToDisplayName: false` (the default), PascalCase input has no word boundaries to split on, so `"MyText"` becomes `"mytext"`. With `normaliseToDisplayName: true`, the same input becomes `"my-text"`. Underscores are already treated as non-alphanumeric, so `"PENDING_APPROVAL"` becomes `"pending-approval"` either way.

The display name normaliser is invoked with `splitDigits: false`, so digits stay attached to the word they follow and `"Version2"` yields `"version2"` rather than `"version-2"`. Slugs are typically persisted in URLs, so splitting them would break existing links.

---

#### ToNormalisedSlug(this string value)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `value` | `string` | — | The string to convert into a normalised URL-friendly slug. |

Convenience alias for `ToSlug(normaliseToDisplayName: true)`. Normalises the input to a display name before slugifying, so PascalCase identifiers produce hyphen-separated slugs — `"CompletedSuccessfully"` becomes `"completed-successfully"`.

---

#### ToTitleCase(this string value, CultureInfo? culture = null)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `value` | `string` | — | The string to convert to title case. |
| `culture` | `CultureInfo?` | `null` | The culture whose casing rules are used. Defaults to `CultureInfo.CurrentCulture` when `null`. |

Converts the input to lowercase first, then applies the culture's `TextInfo.ToTitleCase` rules, capitalising the first letter of each word. Returns the input unchanged if null or whitespace.

---

#### ToDisplayName(this string value, bool splitDigits = true)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `value` | `string` | — | The string to convert into a display-friendly name. |
| `splitDigits` | `bool` | `true` | Whether a digit adjoining a letter starts a new word, so that `"Address1"` becomes `"Address 1"`. Adjacent digits are kept together either way. |

Converts an identifier-style string into human-readable text using the same normaliser as `EnumExtensions.ToDisplayName`. Underscores, hyphens, full stops and whitespace separate words; runs of them collapse to a single space and any at either end are discarded. Casing transitions open words too, and each word is capitalised. Returns an empty string if the input is null, empty or whitespace.

Letters after a word's first are only lowercased when the input contains no lowercase letter at all, which is what allows acronyms in mixed-case input to survive.

| Input | Output |
|-------|--------|
| `"MyText"` | `"My Text"` |
| `"InProgress"` | `"In Progress"` |
| `"user_first_name"` | `"User First Name"` |
| `"PENDING_APPROVAL"` | `"Pending Approval"` |
| `"hello world"` | `"Hello World"` |
| `"my-text"` | `"My Text"` |
| `"first.name"` | `"First Name"` |
| `"XMLParser"` | `"XML Parser"` |
| `"UserID"` | `"User ID"` |
| `"PDF Export"` | `"PDF Export"` |
| `"Address1"` | `"Address 1"` |
| `"Address1"` with `splitDigits: false` | `"Address1"` |

A hyphen or full stop flanked on both sides by a digit or a capital is retained rather than treated as a separator, and the token holding it is emitted verbatim — `"BT.23.9"`, `"2024-01-15"`, `"UTF-8"` and `"X-Ray"` all survive unchanged. Because the test is for capitals rather than for words, all-caps pairs such as `"PENDING-APPROVAL"` and `"CONFIG.VALUE"` are read as codes and preserved as well. Punctuation other than `_`, `-`, `.` and whitespace is never a separator, so a flags enum's `"Read, Write"` keeps its comma.

> **Intended for identifier-style input.** Every word is capitalised, so prose loses its structure — `"order of operations"` becomes `"Order Of Operations"`. Use `ToTitleCase` when the input is already readable text. Word boundaries derive from casing alone, so an unbroken uppercase run cannot be split (`"XMLEXPORT"` gives `"Xmlexport"`) and a lowercase acronym prefix cannot be detected (`"iOS App"` gives `"I OS App"`).

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

### Nested types

#### EnumOption

**Declaration:** `public readonly record struct EnumOption(string Name, int Value)`

A single enum member's name and numeric value, returned by `GetAllOptions`.

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Name` | `string` | get; init; | The enum member's name, as returned by `Enum.ToString()`. Not passed through `ToDisplayName` — apply that yourself if you need display text. |
| `Value` | `int` | get; init; | The member's underlying integer value. |

Being a `record struct`, it supports value equality, `with` expressions, and deconstruction into `(string name, int value)`. `ToString()` produces `EnumOption { Name = InProgress, Value = 1 }`.

### Methods

#### GetAllOptions\<T\>(this T _)

**Returns:** `List<EnumOption>`

**Constraint:** `T : struct, Enum`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `_` | `T` | — | An instance of the enum type (can be `default`). Used only for type inference. |

Returns all members of the enum type as a list of `EnumOption` values, each carrying the member name and its integer value, in declaration order.

---

#### ToDisplayName(this Enum value, bool splitDigits = true)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `value` | `Enum` | — | The enum value to convert. |
| `splitDigits` | `bool` | `true` | Whether a digit adjoining a letter starts a new word, so that `Version2` becomes "Version 2". Adjacent digits are kept together either way. |

Converts an enum value's name to a human-readable string. Underscore, hyphen, full stop and whitespace separators collapse to a single space, each casing transition opens a new word, and every word is capitalised. Supports PascalCase (e.g. `InProgress` → "In Progress") and SCREAMING_CASE (e.g. `PENDING_APPROVAL` → "Pending Approval").

Members whose names contain no lowercase letter have their trailing letters lowercased; mixed-case names keep their inner casing, so an acronym is preserved — `XMLParser` produces "XML Parser". An unbroken uppercase run carries no boundary to split on, so `XMLEXPORT` still produces "Xmlexport"; use a `[Description]` attribute with `GetDescription` in that case.

Shares its implementation with `StringExtensions.ToDisplayName`, which documents the full rule set including reference-code handling.

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

The context obtains the application service provider from its `DbContextOptions` and resolves the ambient `IUserInfo` and `CoreAuditOptions` from it. Each audit entry therefore records the context-level user (`UserId`/`UserName`), the entity-stamped actor (`ActionUserId`), the configured `SourceApplication`, and whether the action id should be preferred (`IsActionIdPreferred`). When no `IUserInfo` is registered, `UserId`/`UserName` fall back to `IUserInfo.MissingUserInfoId`. `AuditEntry` mapping is applied via `AuditEntryMapping.MapAuditEntry` in `OnModelCreating`.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `AuditEntries` | `DbSet<AuditEntry>` | get; set; | The set of audit trail records. |

---

## AuditEntryMapping

**Namespace:** `JC.Core.Data.DataMappings`

Static helper that applies the EF Core entity configuration for `AuditEntry` — key, column lengths, and indexes. Called from `OnModelCreating` in both `DataDbContext` and JC.Identity's `IdentityDataDbContext` so the audit table is mapped identically in every context.

### Methods

#### MapAuditEntry(EntityTypeBuilder\<AuditEntry\> builder)

**Returns:** `EntityTypeBuilder<AuditEntry>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `builder` | `EntityTypeBuilder<AuditEntry>` | — | The entity type builder for `AuditEntry`. |

Configures `AuditEntry`: `Id` as the key (max 36 characters); `UserId`, `UserName`, `ActionUserId`, `SourceApplication`, and `TableName` at max 256 characters; `EntityKey` at max 512 characters; `Action` and `AuditDate` as required; and indexes on `UserId`, `ActionUserId`, `SourceApplication`, `TableName`, `AuditDate`, and the composite `TableName, EntityKey`. Returns the same builder for chaining.

---

## AuditModelMapping\<T\>

**Namespace:** `JC.Core.Data.DataMappings`

Static generic helper applying the EF Core column configuration and indexes that every `AuditModel` entity shares. Constrained to `where T : AuditModel`.

An entity's own `IEntityTypeConfiguration<T>` calls this rather than restating the audit columns, so the create, modification, soft-delete and restore fields are mapped identically wherever they appear. Both JC.Communication and JC.FileStorage apply it to their entities this way.

### Methods

#### MapAuditModel(EntityTypeBuilder\<T\> builder)

**Returns:** `EntityTypeBuilder<T>`

**Static.**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `builder` | `EntityTypeBuilder<T>` | — | The entity type builder for the entity being configured. |

Sets each of `CreatedById`, `LastModifiedById`, `DeletedById` and `RestoredById` to a maximum of 36 characters, and each of `CreatedUtc`, `LastModifiedUtc`, `DeletedUtc` and `RestoredUtc` to precision `0`, so timestamps store whole seconds. `IsDeleted` is declared without further configuration, which maps it explicitly rather than leaving it to convention.

Adds indexes on `CreatedById`, `CreatedUtc`, `LastModifiedById`, `DeletedById`, `IsDeleted` and `RestoredById`. Returns the same builder for chaining.

Note that only `CreatedUtc` is indexed among the four timestamps: `LastModifiedUtc`, `DeletedUtc` and `RestoredUtc` are configured for precision but not indexed. An entity that queries by modification or deletion date wants its own index.

`CreatedById` and `CreatedUtc` come from `BaseCreateModel`, which `AuditModel` extends, so this covers everything `LogModelMapping<T>` does and adds the rest.

---

## LogModelMapping\<T\>

**Namespace:** `JC.Core.Data.DataMappings`

Static generic helper applying the EF Core column configuration and indexes that every `LogModel` entity shares. Constrained to `where T : LogModel`.

The counterpart to `AuditModelMapping<T>` for append-only records. `LogModel` extends `BaseCreateModel` and adds nothing, so only the creation fields exist to configure. JC.Communication applies it to its log entities.

### Methods

#### MapLogModel(EntityTypeBuilder\<T\> builder)

**Returns:** `EntityTypeBuilder<T>`

**Static.**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `builder` | `EntityTypeBuilder<T>` | — | The entity type builder for the entity being configured. |

Sets `CreatedById` to a maximum of 36 characters and `CreatedUtc` to precision `0`, then adds an index on each. Returns the same builder for chaining.
