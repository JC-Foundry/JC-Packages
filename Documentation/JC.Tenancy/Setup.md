# JC.Tenancy — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- JC.Core registered, with a `DbContext` to hold the tenant table
- ASP.NET Core is **not** required. Tenant scope is established through a scoped factory rather than middleware, so the engine works unchanged in a request, a background job or a console application
- An identity package is **not** required either — without one, every scope starts in the null partition until told otherwise
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

## 0. Add the package

```xml
<ProjectReference Include="path/to/JC.Tenancy/JC.Tenancy.csproj" />
```

JC.Tenancy references JC.Core and nothing else in the suite. It has no reference to any identity package, and no identity package references it — the two are joined by the consuming application.

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### Mark your entities

Any entity that belongs to a tenant implements `IMultiTenancy`, which lives in JC.Core:

```csharp
public class Order : AuditModel, IMultiTenancy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? TenantId { get; set; }
    public required string Reference { get; set; }
}
```

Marking costs nothing and requires no reference to this package. Filtering is what requires it.

### The context that owns tenant storage

One context holds the tenant table. It implements `ITenantDbContext` and applies the mapping; it will usually also be tenant-scoped itself:

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options, IUserInfo userInfo, ITenantInfo tenantInfo)
    : DataDbContext(options, userInfo), ITenantScopedContext, ITenantDbContext
{
    public string? CurrentTenantId => tenantInfo.TenantId;

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyTenancyMappings();
        modelBuilder.ApplyTenantFilters(this);
    }
}
```

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();

builder.Services.AddTenancy<AppDbContext>(options =>
{
    // Nobody may query across tenants until a role is named — see Cross-tenant access below
    options.AllowBypassForRole("SystemAdmin");
});
```

There is no middleware to register.

### Defaults

With no configuration callback, `AddTenancy` gives you:

| Default | Value |
|---------|-------|
| Current tenant | Read live from `IUserInfo.TenantId`, or the null partition where no identity package is registered |
| Entities filtered | Every `IMultiTenancy` entity in a model where `ApplyTenantFilters` is called |
| Tenant caching | Enabled |
| Cache lifetime | Five minutes |
| Excluded entity types | None |
| Cross-tenant bypass roles | **None — every safe bypass is refused** |
| Tenant expiry, domain and user limits | Reported, never enforced |

`AddTenancy` registers:

| Registration | Lifetime | Description |
|--------------|----------|-------------|
| `ITenantDbContext` → `TContext` | Scoped | Marks the context owning tenant storage |
| `ITenantInfo` and `ITenantContext` | Scoped | The operational tenant. Both resolve to the **same instance** |
| `ITenantStore` → `TenantStore<TContext>` | Scoped | The supported read/write boundary. `TryAdd`, so you may register your own |
| `ITenantBypassAuthoriser` → `RoleTenantBypassAuthoriser` | Scoped | Decides cross-tenant access. `TryAdd`, so you may register your own |
| `TenantCache` | Scoped | Resolves and caches tenant records |
| `TenantSeeder` | Scoped | Startup helper for the default tenant. `TryAdd` |
| `IMemoryCache` | — | Added if not already present |

**Registering the engine does not filter anything by itself.** Each context that should be tenant-scoped implements `ITenantScopedContext` and calls `ApplyTenantFilters` — which is what allows many contexts to be filtered while only one stores tenants.

## 2. Full configuration

### AddTenancy

**Namespace:** `JC.Tenancy.Extensions`

```csharp
builder.Services.AddTenancy<AppDbContext>(options =>
{
    options.CacheEnabled = true;
    options.CacheLifetime = TimeSpan.FromMinutes(5);
    options.AllowBypassForRole("SystemAdmin");
    options.Exclude<CountryReference>();
});
```

| Type parameter | Constraint | Description |
|----------------|-----------|-------------|
| `TContext` | `DbContext, ITenantDbContext` | The context owning the tenant table |

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configure` | `Action<TenantOptions>?` | `null` | Configures caching, exclusions and bypass roles. When `null`, the defaults above apply |

**Throws `InvalidOperationException` when called a second time**, naming the context that already owns tenant storage. Exactly one `DbContext` may own it — two would mean two disagreeing answers to which tenants exist. Other contexts participate in filtering without being registered here.

#### TenantOptions

**Namespace:** `JC.Tenancy.Models.Options`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CacheEnabled` | `bool` | `true` | Whether resolved tenants are cached. Turning it off makes every metadata read hit the store, and is intended for diagnosis |
| `CacheLifetime` | `TimeSpan` | 5 minutes | How long a resolved tenant stays cached |
| `ExcludedEntityTypes` | `HashSet<Type>` | Empty | Entity types the automatic filters leave alone, despite implementing `IMultiTenancy` |
| `BypassRoles` | `HashSet<string>` | Empty | Role names permitted to query across tenants through the safe API. Compared case-insensitively |

| Method | Returns | Description |
|--------|---------|-------------|
| `AllowBypassForRole(string role)` | `TenantOptions` | Adds a role name to `BypassRoles`. Chainable |
| `Exclude<TEntity>()` | `TenantOptions` | Adds a type to `ExcludedEntityTypes`. Chainable |

`CacheLifetime` is deliberately short. A tenant carries security and business state — expiry, domain rules, settings — so a long lifetime means a revoked or reconfigured tenant stays live far longer than anyone expects. Mutations through `ITenantStore` invalidate the entry immediately; this window only covers changes made outside it.

Every entry in `ExcludedEntityTypes` is a type whose rows cross tenants on every query. Use it for genuinely shared reference data, and be aware that nothing filters those afterwards.

### Making a context tenant-scoped

A context participates in filtering by implementing `ITenantScopedContext` and calling `ApplyTenantFilters`:

```csharp
public class ReportingDbContext(DbContextOptions<ReportingDbContext> options, IUserInfo userInfo, ITenantInfo tenantInfo)
    : DataDbContext(options, userInfo), ITenantScopedContext
{
    public string? CurrentTenantId => tenantInfo.TenantId;

    public DbSet<Report> Reports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyTenantFilters(this);
    }
}
```

Note what is absent: no `ITenantDbContext`, no `Tenants` DbSet, no `ApplyTenancyMappings`. Those belong to the one owning context only. All participating contexts share the same operational scope, because they all delegate to the same scoped `ITenantInfo`.

#### ApplyTenancyMappings(this ModelBuilder modelBuilder)

Applies the `Tenant` entity mapping — key, column lengths, a unique index on `Name` and a non-unique index on `Domain`. Call it only from the context that owns tenant storage.

`Name` is unique so a concurrent add cannot slip past the store's own check. `Domain` is not: it is nullable, and SQL Server would then permit only one tenant without a domain while MySQL would permit many, so a provider-agnostic mapping cannot express "unique except nulls".

#### ApplyTenantFilters(this ModelBuilder modelBuilder, DbContext context, TenantOptions? options = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `DbContext` | — | The context being built. Must implement `ITenantScopedContext` |
| `options` | `TenantOptions?` | `null` | Supply the same instance registered with `AddTenancy` to honour its exclusions; omit it to filter every tenant-scoped entity |

Installs a global query filter over every `IMultiTenancy` entity in the model, matching null to null so the null partition behaves as a partition rather than as "no filtering".

**Call it last.** It reads the model as it stands when called, so any tenant-scoped entity registered afterwards never receives a filter.

**Throws `InvalidOperationException`** where the model holds tenant-scoped entities but `context` does not implement `ITenantScopedContext` — without a current tenant there is nothing to filter by, and silently returning every tenant's rows is not an available outcome. It also throws where a tenant-scoped entity can never carry a filter: an owned type, or a derived type whose root is not itself tenant-scoped. EF applies a query filter only to the root of an inheritance hierarchy.

A no-op where the model holds no tenant-scoped entities, so it is safe to call from a context that may or may not end up with any.

**Filters bind to `ITenantScopedContext.CurrentTenantId` rather than to `ITenantInfo` directly**, and that is an EF Core constraint rather than a preference. EF caches the compiled model per context type, but makes a specific allowance for a captured `DbContext` in a query filter, re-reading its members against the active instance on every query. No such allowance exists for an arbitrary service, so a filter closing over the scoped `ITenantInfo` would bake whichever tenant happened to warm the model into every later request.

### Establishing tenant scope

`ITenantInfo` is registered scoped and derives its tenant live from `IUserInfo` unless set explicitly. Assigning a tenant — including assigning `null` — sets an override that wins for the rest of the scope.

**Namespace:** `JC.Tenancy.Extensions` (`TenantInfoExtensions`)

| Method | Returns | Description |
|--------|---------|-------------|
| `SetTenantInfoForTenant(string? tenantId)` | `ITenantInfo` | Scopes the current scope by identifier. `null` pins the null partition |
| `SetTenantInfoForTenant(Tenant? tenant)` | `ITenantInfo` | Scopes from an already-loaded record, skipping the cache lookup |
| `SetTenantInfoForDomainAsync(string? domain, CancellationToken)` | `Task<Tenant?>` | Resolves by domain and scopes to the result. Returns the tenant, or `null` where none matched |
| `CreateScopeForTenant(string? tenantId)` | `IServiceScope` | A new scope, already scoped |
| `CreateScopeForTenant(Tenant? tenant)` | `IServiceScope` | The same, from a loaded record |
| `CreateAsyncScopeForTenant(string? tenantId)` | `Task<AsyncServiceScope>` | For work whose scoped services implement `IAsyncDisposable` |
| `CreateAsyncScopeForTenant(Tenant? tenant)` | `Task<AsyncServiceScope>` | The same, from a loaded record |
| `CreateAsyncScopeForTenantByDomain(string? domain)` | `Task<AsyncServiceScope>` | The same, resolving by domain |

All extend `IServiceProvider`, which is what keeps this package free of any host-specific dependency.

`SetTenantInfoForDomainAsync` leaves the scope untouched where no tenant claims the domain — an unrecognised value is not the same act as deliberately choosing the null partition. Handle the `null` return.

It resolves **active tenants only**. `ITenantStore.GetByDomainAsync` can be asked for soft-deleted ones, but scoping live work to a deleted tenant is not something this extension will do for you; load it through the store and pass the record to `SetTenantInfoForTenant` if that is genuinely what you want.

Usage is covered in the [Guide](Guide.md#establishing-tenant-scope).

### Cross-tenant access

**Namespace:** `JC.Tenancy.Extensions` (`QueryExtensions`)

```csharp
public class TenantReportService(IRepositoryContext<Order> orders, ITenantBypassAuthoriser authoriser)
{
    public IQueryable<Order> AcrossAllTenants()
        => orders.AsQueryable().AllTenants(authoriser);
}
```

| Method | Description |
|--------|-------------|
| `AllTenants<T>(ITenantBypassAuthoriser authoriser)` | Removes tenant filtering **if** the authoriser permits it. Returns the query unchanged if not |
| `AllTenantsUnsafe<T>()` | Removes tenant filtering with no permission check at all |

The authoriser is passed rather than resolved because an `IQueryable<T>` extension has no service provider to hand.

`AllTenants` returns the filtered query rather than throwing when permission is refused: a caller without the right to see other tenants still gets a working query over their own data, rather than an exception in the middle of a page they were entitled to load.

#### ITenantBypassAuthoriser

The default implementation, `RoleTenantBypassAuthoriser`, matches the current user's roles against `TenantOptions.BypassRoles`. It denies when no roles are configured, and denies when no user resolves, so an application that has not considered cross-tenant access has not accidentally granted it.

Role **names** rather than a constant, because JC.Tenancy and the identity packages are siblings and neither may reference the other — and because an application on a different identity authority will have its own word for the same idea.

The first refusal caused by an empty `BypassRoles` logs a warning naming `AllowBypassForRole`, once per process. Denials from the role check itself stay silent, because those are the mechanism working.

Register your own to replace it:

```csharp
builder.Services.AddScoped<ITenantBypassAuthoriser, MyAuthoriser>();
builder.Services.AddTenancy<AppDbContext>();
```

`AddTenancy` uses `TryAdd`, so a registration made first wins.

### Seeding a default tenant

**Namespace:** `JC.Tenancy.Extensions` (`SeedingExtensions`)

```csharp
var app = builder.Build();

await app.Services.SeedDefaultTenantAsync<AppUser, AppDbContext>(
    userId: admin.Id,
    tenantName: "Default Tenant",
    description: "Default system tenant");
```

| Type parameter | Constraint | Description |
|----------------|-----------|-------------|
| `TUser` | `class, IApplicationUser` | The user entity type |
| `TUserContext` | `DbContext` | The context owning the user's table |

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string` | — | The user to assign the tenant to |
| `tenantName` | `string` | `"Default Tenant"` | The tenant name to find or create |
| `description` | `string?` | `"Default system tenant"` | Applied on creation. Ignored where the tenant already exists |
| `cancellationToken` | `CancellationToken` | `default` | A token to cancel the operation |

Runs in a scope of its own and returns the tenant, or `null` where it could not be created or no user has that identifier — the reason is logged either way.

Idempotent: an existing tenant of the same name is reused, and a user already holding that tenant is left alone. Safe to call on every start-up.

`TenantSeeder` also offers an overload creating the tenant without assigning it to anybody. Extending `IServiceProvider` rather than `IApplicationBuilder` keeps the package usable from a worker service or console host.

### Composing with an identity package

Neither package references the other, so the consuming application joins them:

```csharp
var admin = await app.ConfigureAdminAndRolesAsync<AppUser, AppRole, AppRoles>();

if (admin is not null)
    await app.Services.SeedDefaultTenantAsync<AppUser, AppDbContext>(admin.Id);
```

The identity package seeds the administrator and returns it; this package gives that administrator a tenant.

## 3. Apply migrations

JC.Tenancy introduces one table, `Tenants`, in the context you nominate through `AddTenancy<TContext>`. It carries a unique index on `Name` and a non-unique index on `Domain`.

Tenant-scoped entities need no schema change beyond the `TenantId` column their own package or application already declares. Query filters are model configuration and produce no migration.

```bash
dotnet ef migrations add AddTenancy --project YourApp
dotnet ef database update --project YourApp
```

**The unique index on `Name` fails to apply where duplicate tenant names already exist.** Deduplicate before migrating.

## 4. Verify

1. Create two tenants through `ITenantStore.TryAddAsync`, and a tenant-scoped row under each.
2. Scope to one of them with `CreateScopeForTenant` and query the entity — only that tenant's rows should come back.
3. Query again inside `AllTenantsUnsafe()` and confirm both rows appear, proving the filter rather than the data was the difference.

## Next steps

- [Guide](Guide.md) — establishing scope, the tenant store and cache, settings, and cross-tenant work.
- [API Reference](API.md)
- [JC.Identity — Setup](../JC.Identity/Setup.md) — if your tenants come from signed-in users.
