# JC.Tenancy — API reference

Every public type and member in JC.Tenancy. See [Setup](Setup.md) for registration and [Guide](Guide.md) for usage.

> **Note:** Registration extensions and options classes are documented in [Setup](Setup.md), not here. That covers `AddTenancy` and `TenantOptions`, the `IServiceProvider` scope helpers on `TenantInfoExtensions` and `SeedingExtensions`, and the `ModelBuilder` extensions `ApplyTenancyMappings` and `ApplyTenantFilters`.
>
> `ITenantContext`, which `ITenantInfo` extends, is declared in JC.Core and documented in [its API reference](../JC.Core/API.md#itenantcontext).

## Models

### Tenant

**Namespace:** `JC.Tenancy.Models`

Sealed entity representing a tenant. Extends JC.Core's `AuditModel`, so creating, updating, deleting and restoring a tenant are all audited. Settings are stored as JSON in a single column and managed through the methods below.

`Tenant` does **not** implement `IMultiTenancy`, so the tenant table is never filtered by itself — reading tenants is not a cross-tenant operation.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | `Guid.NewGuid().ToString()` | get; set; | Unique identifier. Mapped with a maximum length of 36. |
| `Name` | `string` | — | get; set; | The tenant name. Marked `required`, mapped as required with a maximum length of 256, and carries a **unique** index. |
| `Description` | `string?` | `null` | get; set; | An optional description. Maximum length 10240. |
| `Domain` | `string?` | `null` | get; set; | The domain associated with the tenant. Maximum length 256, with a non-unique index. Not unique because the column is nullable, and providers disagree on how many nulls a unique index permits. |
| `MaxUsers` | `uint?` | `null` | get; set; | The maximum number of users allowed. Reported only — nothing enforces it. |
| `ExpiryDateUtc` | `DateTime?` | `null` | get; set; | When this tenant expires. Reported only — nothing enforces it. |
| `Settings` | `string` | `"[]"` | get; private set; | The JSON-serialised settings. Managed through the methods below rather than assigned directly. |

Audit properties are inherited from `AuditModel` — see the [JC.Core API reference](../JC.Core/API.md#auditmodel).

#### Methods

##### SetSettings(IEnumerable\<TenantSettings\> settings)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `settings` | `IEnumerable<TenantSettings>` | — | The settings to serialise and store. |

Replaces every setting by serialising the supplied collection into `Settings`.

---

##### SetSetting(string key, string value, bool isActive = true)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `key` | `string` | — | The setting key. |
| `value` | `string` | — | The setting value. |
| `isActive` | `bool` | `true` | Whether the setting is active. |

Deserialises the current settings, finds an entry whose key matches **exactly** — an ordinal, case-sensitive comparison — updating it where found or appending a new one, then re-serialises the whole collection.

Two consequences follow from rewriting the whole collection. Concurrent writes to different keys on the same tenant can lose one another, and a key differing only in case produces a second entry rather than updating the first, because reads match case-insensitively while this write does not.

---

##### GetSettings()

**Returns:** `List<TenantSettings>`

Deserialises the stored JSON. Returns an empty list where the value deserialises to `null`, and also where it is malformed — a `JsonException` is caught rather than propagated, because the value is consuming-application data and these are reached from property getters on the ambient tenant, where a throw would surface inside an unrelated request.

Returns every setting, active or not. `ITenantInfo.GetSettings` filters to active ones.

### TenantSettings

**Namespace:** `JC.Tenancy.Models`

Sealed class representing a single key-value setting with an active flag. Serialised into `Tenant.Settings`.

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Key` | `string?` | `null` | get; set; | The setting key. |
| `Value` | `string?` | `null` | get; set; | The setting value. |
| `IsActive` | `bool` | `false` | get; set; | Whether this setting is active. Inactive settings are invisible to `ITenantInfo`'s readers. |

### TenantValidationResponse

**Namespace:** `JC.Tenancy.Models`

The result of a tenant mutation. Every `Try*` method on `ITenantStore` that can be rejected returns one of these rather than throwing.

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `IsValid` | `bool` | get; | Whether the operation was accepted. |
| `ErrorMessage` | `string?` | get; | Why it was rejected, or `null` when valid. |
| `ValidatedTenant` | `Tenant?` | get; | The tenant the operation applied to, or `null` when invalid. |

#### Constructors

| Constructor | Result |
|-------------|--------|
| `TenantValidationResponse()` | Valid, with no tenant. |
| `TenantValidationResponse(Tenant tenant)` | Valid, carrying `tenant`. |
| `TenantValidationResponse(string errorMessage)` | Invalid, carrying the reason. |

### TenantInfo

**Namespace:** `JC.Tenancy.Models`

The default `ITenantInfo`, registered scoped by `AddTenancy`. Derives the tenant from the current user unless overridden, and resolves tenant metadata from `TenantCache` on first read.

Inject `ITenantInfo`, or `ITenantContext` from a package that cannot reference this one. Both resolve to the same scoped instance.

#### Constructor

##### TenantInfo(TenantCache cache, IUserInfo? userInfo = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cache` | `TenantCache` | — | Resolves the tenant record on first metadata read. |
| `userInfo` | `IUserInfo?` | `null` | The current user, read on **every** access rather than captured. `null` where no identity package is registered, which means the null partition. |

`userInfo` is read live and not at construction deliberately. It is populated in place by claims middleware, and this type can be built earlier in a request — authentication touches the DbContext, which resolves this — so a value read at construction would be the unpopulated one and would pin the whole request to the null partition.

#### Members

`ITenantInfo` extends JC.Core's `ITenantContext`, which declares `TenantId`, `HasTenant`, `IsOverridden`, `Name`, `Description`, `Domain`, `MaxUsers`, `ExpiryDateUtc`, `IsExpired` and both `GetSetting` overloads. Those are documented in the [JC.Core API reference](../JC.Core/API.md#itenantcontext) and behave as described there.

`TenantId` returns the override where one has been set, and `IUserInfo.TenantId` otherwise. Assigning it — including assigning `null` — sets the override and turns `IsOverridden` to `true`.

Metadata resolution is keyed on what was last resolved rather than on whether anything was, so a cached record is discarded whenever the underlying identifier changes: by an override, or by the claims middleware populating the user mid-scope.

`IsExpired` is `true` where `ExpiryDateUtc` has passed. It is reported, never enforced.

##### SetTenant(Tenant? tenant)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenant` | `Tenant?` | — | The tenant to scope to, or `null` for the null partition. |

Scopes to an already-loaded record, setting the override and seeding the resolved metadata in one step so no cache lookup occurs. For callers holding the record already — seeding, or work immediately after creating a tenant, where a freshly written tenant would otherwise wait on the cache.

##### GetSettings()

**Returns:** `IReadOnlyList<TenantSettings>`

Returns the tenant's **active** settings, or an empty collection in the null partition. Inactive settings are excluded, so deactivating one is equivalent to removing it as far as readers are concerned.

## Services

### TenantStore\<TContext\>

**Namespace:** `JC.Tenancy.Services`

The default `ITenantStore`, reading and writing tenants through the context that owns them and invalidating the cache on every write. Inject `ITenantStore`.

Generic over the owning context so it can bind the repository manager to it explicitly — a consuming application may have several contexts, and only one holds tenants.

Writing tenants through EF directly still works, and the engine makes no attempt to intercept it — but nothing invalidates the cache when you do, so those changes stay invisible until the entry expires.

| Type parameter | Constraint | Description |
|----------------|-----------|-------------|
| `TContext` | `DbContext, ITenantDbContext` | The context owning tenant storage. |

#### Methods

##### GetAsync(string tenantId, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive, CancellationToken cancellationToken = default)

**Returns:** `Task<Tenant?>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantId` | `string` | — | The tenant identifier. |
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Whether soft-deleted tenants are included. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Returns the tenant, or `null` where none matches. Read without tracking.

---

##### GetByNameAsync(string name, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive, CancellationToken cancellationToken = default)

**Returns:** `Task<Tenant?>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The tenant name. |
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Whether soft-deleted tenants are included. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Returns the tenant whose name matches, or `null`. Read without tracking.

---

##### GetByDomainAsync(string? domain, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive, CancellationToken cancellationToken = default)

**Returns:** `Task<Tenant?>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `domain` | `string?` | — | The domain to match. Nullable, and a null or empty value matches nothing. |
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Whether soft-deleted tenants are included. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Returns the tenant claiming that domain, or `null` where none does. Read without tracking.

Matched case-insensitively, by lower-casing both sides. Tenants whose own `Domain` is null or empty are excluded from the comparison, so a null or empty argument never matches the tenants that have no domain — it returns `null` rather than treating "no domain" as a value to match on.

The domain index is not unique, so more than one tenant may hold a domain; the first match is returned.

---

##### GetAllAsync(DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive, CancellationToken cancellationToken = default)

**Returns:** `Task<List<Tenant>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Whether soft-deleted tenants are included. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Every matching tenant, ordered by name and read without tracking. Unpaged — this is the overload a cross-tenant loop uses.

---

##### GetAllAsync(int pageNumber, int pageSize, DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)

**Returns:** `Task<IPagination<Tenant>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `pageNumber` | `int` | — | The 1-based page number. |
| `pageSize` | `int` | — | The maximum number of tenants per page. |
| `deletedQueryType` | `DeletedQueryType` | `OnlyActive` | Whether soft-deleted tenants are included. |

A page of tenants, ordered by name. **Takes no `CancellationToken`**, unlike every other method on this interface.

---

##### TryAddAsync(Tenant tenant, CancellationToken cancellationToken = default)

**Returns:** `Task<TenantValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenant` | `Tenant` | — | The tenant to add. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Validates, then adds. Returns the rejection where validation fails, without writing anything.

Invalidates the cache entry for the new identifier on success — cache misses are cached too, so a lookup made before this tenant existed would otherwise persist for the lifetime of the entry.

---

##### TryUpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)

**Returns:** `Task<TenantValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenant` | `Tenant` | — | The tenant to update. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Validates, then updates, then invalidates the cache entry.

---

##### TryDeleteAsync(string tenantId, CancellationToken cancellationToken = default)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantId` | `string` | — | The identifier of the tenant to delete. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Finds the **active** tenant with that identifier and soft-deletes it, invalidating the cache entry. Returns `false` where no active tenant matches.

Affects the tenant record only. Rows elsewhere carrying its identifier are left as they are — there is no cascade, because tenant-scoped data can live in contexts and databases this store has never heard of.

A deleted tenant keeps its name and domain, and the unique index still holds its row, so neither value is freed for reuse.

---

##### TryRestoreAsync(string tenantId, CancellationToken cancellationToken = default)

**Returns:** `Task<TenantValidationResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantId` | `string` | — | The identifier of the tenant to restore. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Finds the **soft-deleted** tenant with that identifier, re-validates it, restores it and invalidates the cache entry.

Returns a rejection reading "No deleted tenant was found with that identifier" where nothing matches. Re-validation can also fail: a domain freed by the delete may have been claimed since.

---

##### TrySetSettingAsync(string tenantId, string key, string value, bool isActive = true, CancellationToken cancellationToken = default)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantId` | `string` | — | The tenant identifier. |
| `key` | `string` | — | The setting key. |
| `value` | `string` | — | The setting value. |
| `isActive` | `bool` | `true` | Whether the setting is active. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Finds the **active** tenant, applies `Tenant.SetSetting`, persists and invalidates the cache entry. Returns `false` where no active tenant matches.

#### Validation

Every `Try*` method that returns a `TenantValidationResponse` validates first, and validation is identical in each case. A tenant must have a non-whitespace name. Neither its name nor, where set, its domain may be held by another tenant.

The clash check runs against **all** tenants, including soft-deleted ones, because the unique index still holds their rows — validating active-only would pass here and then fail on the constraint. Case sensitivity follows the database collation, so the check agrees with the index.

Where the clash is with a deleted tenant the message says so, telling the caller to restore or rename it, rather than reporting that something already exists which they cannot find anywhere.

### RoleTenantBypassAuthoriser

**Namespace:** `JC.Tenancy.Services`

The default `ITenantBypassAuthoriser`, granting cross-tenant access to callers holding one of the roles named in `TenantOptions.BypassRoles`. Inject `ITenantBypassAuthoriser`.

Role **names** rather than a constant, deliberately. The obvious answer is an identity package's `SystemAdmin`, but JC.Tenancy and the identity packages are siblings and neither may reference the other — and an application on a different identity authority will have its own name for the same idea. Configuring the name keeps the decision with the application that owns the role.

#### Methods

##### CanAccessAllTenants()

**Returns:** `bool`

Returns `true` where `BypassRoles` is non-empty, an `IUserInfo` resolves, and that user is in at least one of the named roles.

Denies when no roles are configured, and denies when no user is resolvable, so an application that has not considered cross-tenant access has not accidentally granted it. `IUserInfo` is resolved rather than injected, because tenancy works with no identity package registered — and no user means no bypass.

The first refusal caused by an empty `BypassRoles` logs a warning naming `AllowBypassForRole` and `AllTenantsUnsafe()`, once per process. Refusing because nothing is configured and refusing because this user lacks the role are the same answer for different reasons, and only the first is a mistake; denials from the role check itself are silent.

### TenantCache

**Namespace:** `JC.Tenancy.Services`

Resolves tenants from the owning context and keeps them in memory for a short, configurable window, so establishing tenant scope stays cheap. Registered scoped.

Backed by `IMemoryCache`. Entries are invalidated by `ITenantStore` whenever it writes; changes made outside the store are not detected and stay stale until the entry expires.

Two scopes missing the same tenant simultaneously will both load it. That is harmless, and there is no distributed invalidation across instances.

#### Methods

##### Get(string? tenantId)

**Returns:** `Tenant?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantId` | `string?` | — | The tenant identifier. |

Returns the tenant, or `null` where the identifier is null, empty, or matches nothing.

**Synchronous by necessity**: it backs `ITenantInfo`'s deferred metadata resolution, which is reached from property getters. The read is a single indexed lookup and, on all but the first call in the cache window, does not reach the database at all.

Misses are cached as well as hits, so an unknown identifier does not hit the database on every read.

The owning context is resolved on demand rather than injected, so a scope that never reads tenant metadata never pays for a `DbContext` it was never going to use. Where no `ITenantDbContext` is registered, this returns `null` rather than throwing.

Only **active** tenants are loaded, which is why a soft-deleted tenant's metadata reads as null while its identifier still scopes queries.

---

##### Invalidate(string? tenantId)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantId` | `string?` | — | The tenant identifier. |

Drops the cache entry so the next read resolves afresh. Does nothing where the identifier is null or empty.

### TenantSeeder

**Namespace:** `JC.Tenancy.Services`

Creates an application's default tenant, optionally assigning it to a user. Registered scoped.

A concrete class with no interface, deliberately. Seeding is startup work rather than a CRUD boundary, and no second implementation is in prospect — an application tenant belongs to the application, whichever authority authenticated its users, so every application seeds its own default tenant the same way.

Both overloads are idempotent: an existing tenant of the same name is reused, and a user already holding that tenant is left alone. Safe to call on every start-up.

The `IServiceProvider` extension in `SeedingExtensions` wraps this in a scope of its own and is the usual entry point — see [Setup](Setup.md#seeding-a-default-tenant).

#### Methods

##### SeedDefaultTenantAsync(string tenantName = "Default Tenant", string? description = "Default system tenant", CancellationToken cancellationToken = default)

**Returns:** `Task<Tenant?>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantName` | `string` | `"Default Tenant"` | The tenant name to find or create. |
| `description` | `string?` | `"Default system tenant"` | Applied on creation. Ignored where the tenant exists. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Looks the tenant up by name through the store. Where none exists it is created; where creation is rejected the reason is logged and `null` is returned.

---

##### SeedDefaultTenantAsync\<TUser, TUserContext\>(string userId, string tenantName = "Default Tenant", string? description = "Default system tenant", CancellationToken cancellationToken = default)

**Returns:** `Task<Tenant?>`

**Constraints:** `where TUser : class, IApplicationUser`, `where TUserContext : DbContext`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string` | — | The identifier of the user to assign the tenant to. |
| `tenantName` | `string` | `"Default Tenant"` | The tenant name to find or create. |
| `description` | `string?` | `"Default system tenant"` | Applied on creation. Ignored where the tenant exists. |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation. |

Finds or creates the tenant as above, then loads the user from `TUserContext` and assigns it.

Returns `null` where the tenant could not be created, and also where no user has that identifier — in which case the user is left untouched and the reason is logged. Where the user already holds that tenant, nothing is written, so a repeated start-up does not churn the user row.

Takes an identifier rather than an entity so the user is loaded and tracked by the context that saves it, and only the tenant column is written. Passing a detached entity would have EF mark every property modified, writing the password hash and security stamp back alongside the tenant.

## Extensions

### QueryExtensions

**Namespace:** `JC.Tenancy.Extensions`

Query extension methods for reading across tenant boundaries.

#### Methods

##### AllTenants\<T\>(this IQueryable\<T\> query, ITenantBypassAuthoriser authoriser)

**Returns:** `IQueryable<T>`

**Constraint:** `where T : class`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `query` | `IQueryable<T>` | — | The source query. |
| `authoriser` | `ITenantBypassAuthoriser` | — | The authoriser deciding whether the bypass is allowed. |

Returns the query with filters ignored where the bypass is permitted; otherwise the query unchanged.

Silently returning the filtered query when permission is refused is deliberate: a caller without the right to see other tenants still gets a working query over their own data, rather than an exception in the middle of a page they were entitled to load. The caller cannot tell from the result which happened — ask `CanAccessAllTenants()` first where it matters.

The authoriser is passed rather than resolved because this is an `IQueryable<T>` extension with no service provider to hand.

---

##### AllTenantsUnsafe\<T\>(this IQueryable\<T\> query)

**Returns:** `IQueryable<T>`

**Constraint:** `where T : class`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `query` | `IQueryable<T>` | — | The source query. |

Removes tenant filtering with no permission check, for trusted callers with no user to authorise: reconciliation jobs, maintenance tooling, migrations, and infrastructure that legitimately spans every tenant.

`IgnoreQueryFilters` is all-or-nothing, so any global filter a consuming application has added to the entity goes with the tenant one. Soft-delete is unaffected — it is applied by `FilterDeleted`, not as a global filter.

## Data

### ITenantDbContext

**Namespace:** `JC.Tenancy.Data`

Marks the one `DbContext` that owns authoritative tenant storage.

Many contexts may be tenant *filtered* — that only requires `ITenantScopedContext` — but exactly one owns the table the tenants themselves live in. Which one is a deployment decision: an identity context and a main application context are both reasonable homes, and the engine does not care as long as there is only ever one for a given tenancy domain.

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Tenants` | `DbSet<Tenant>` | get; set; | The set of tenants. |

### ITenantScopedContext

**Namespace:** `JC.Tenancy.Data`

Marks a `DbContext` that participates in tenant filtering, by exposing the tenant its queries are currently scoped to. Implement it by delegating to the scoped `ITenantInfo`.

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `CurrentTenantId` | `string?` | get; | The tenant the context's queries are scoped to, or `null` for the null partition. |

The filters bind to this property rather than closing over `ITenantInfo` directly, and that is not incidental. EF Core caches the compiled model per context type, but makes a specific allowance for a captured `DbContext` instance in a query filter, re-reading its members against the active context on every query. No such allowance exists for an arbitrary service, so a filter that captured the scoped `ITenantInfo` would bake whichever tenant happened to warm the model into every later request.

### TenantMap

**Namespace:** `JC.Tenancy.Data.DataMappings`

The `IEntityTypeConfiguration<Tenant>` applied by `ApplyTenancyMappings`. Configures the key, column lengths, a unique index on `Name`, a non-unique index on `Domain`, and JC.Core's audit mapping.

Applied for you by `ApplyTenancyMappings` — see [Setup](Setup.md#making-a-context-tenant-scoped). Apply it directly only if you are configuring the model by hand.

## Next steps

- [Setup](Setup.md) — registration, options and their defaults.
- [Guide](Guide.md) — usage, scenarios and nuances.
- [JC.Core — API reference](../JC.Core/API.md) — `IMultiTenancy` and `ITenantContext`.
