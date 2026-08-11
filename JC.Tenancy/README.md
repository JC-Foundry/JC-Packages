# JC.Tenancy

The application tenancy engine — tenant scope, EF Core query filters, a tenant store with caching, and safe and unsafe cross-tenant access. No dependency on ASP.NET Core and none on any identity package, so it behaves identically in a request, a background job and a console application.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.Tenancy/JC.Tenancy.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

## Prerequisites

- .NET 9.0 SDK
- **JC.Core**, registered with `AddCore<AppDbContext>()`
- A `DbContext` to hold the tenant table
- An identity package is optional — without one, every scope starts in the null partition until told otherwise

## Quick start

### Mark your entities

`IMultiTenancy` lives in JC.Core, so marking an entity costs no reference to this package:

```csharp
public class Order : AuditModel, IMultiTenancy
{
    public string? TenantId { get; set; }
    public required string Reference { get; set; }
}
```

### Data — `AppDbContext`

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
        modelBuilder.ApplyTenantFilters(this);   // last
    }
}
```

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();

builder.Services.AddTenancy<AppDbContext>(options =>
{
    // Nobody may query across tenants until a role is named
    options.AllowBypassForRole("SystemAdmin");
});
```

There is no middleware to register.

## Feature areas

### Tenant scope

`ITenantInfo` is registered scoped and derives its tenant from `IUserInfo` **live**, so signing a user in is enough to put their requests in the right tenant. Set it explicitly for work that has no user, or that deliberately crosses tenants:

```csharp
await using var scope = await services.CreateAsyncScopeForTenant(tenantId);

var orders = scope.ServiceProvider.GetRequiredService<IRepositoryContext<Order>>();
var theirs = await orders.AsQueryable().ToListAsync();   // filtered to that tenant
```

Also available by an already-loaded record, or by domain. All extend `IServiceProvider`, which is what keeps the package usable from any host.

Assigning a tenant — including assigning `null` — sets an override that wins for the rest of the scope. `IsOverridden` distinguishes that from a tenant merely derived from the user.

### Query filters

`ApplyTenantFilters` installs a global filter over every `IMultiTenancy` entity in the model, matching null to null so the null partition behaves as a partition rather than as "no filtering".

**Call it last.** It reads the model as it stands when called, so any tenant-scoped entity registered afterwards never receives a filter. It throws where a context holds tenant-scoped entities but cannot say which tenant is current — silently returning every tenant's rows is not an available outcome.

Many contexts may be filtered; exactly one owns tenant storage, and `AddTenancy` rejects a second registration.

Filters bind to `ITenantScopedContext.CurrentTenantId` rather than closing over `ITenantInfo`, and that is an EF Core constraint rather than a preference — EF re-reads a captured `DbContext` on every query, but would bake a captured service's value into a cached model.

### Tenant store

`ITenantStore` is the supported boundary for reading and writing tenants:

```csharp
var response = await tenants.TryAddAsync(new Tenant { Name = "Acme Ltd", Domain = "acme.example.com" });

if (!response.IsValid)
    _logger.LogWarning("Rejected: {Reason}", response.ErrorMessage);
```

Mutations return a `TenantValidationResponse` rather than throwing, enforce unique names and domains, and invalidate the cache. Deletion is soft and does not cascade — rows elsewhere keep their `TenantId`, because tenant-scoped data can live in contexts this store has never heard of.

Writing tenants through EF directly still works, but nothing invalidates the cache when you do.

### Caching

Resolved tenants are cached in `IMemoryCache` for five minutes by default, invalidated by every store write. Deliberately short: a tenant carries security and business state, so a long lifetime means a revoked or reconfigured tenant stays live far longer than anyone expects.

`TenantId` costs nothing to read — the filters consult it on every query. Everything else describes the persisted record and resolves from cache on first read, so an application that never displays tenant metadata never pays for the lookup.

### Cross-tenant access

```csharp
var everything = orders.AsQueryable().AllTenants(authoriser);   // permission-checked
var reconcile  = orders.AsQueryable().AllTenantsUnsafe();       // no check at all
```

`AllTenants` is gated by `ITenantBypassAuthoriser`, whose default matches the current user against role **names** in `TenantOptions.BypassRoles` — names rather than a constant, because this package cannot reference an identity package, and another authority will have its own word for the same idea.

It denies when no roles are configured and when no user resolves, so an application that has not considered cross-tenant access has not accidentally granted it. The first refusal for want of configuration is logged once.

Prefer looping tenants where you can: a scoped loop keeps every query filtered, so a mistake affects one tenant rather than all of them.

### Seeding

```csharp
await app.Services.SeedDefaultTenantAsync<AppUser, AppDbContext>(admin.Id);
```

Finds or creates a tenant by name and optionally assigns it to a user. Idempotent, so it is safe on every start-up. Neither this package nor the identity packages reference each other — the consuming application joins them at the call site.

## Defaults

| Default | Value |
|---------|-------|
| Current tenant | Read live from `IUserInfo.TenantId`, or the null partition where no identity package is registered |
| Entities filtered | Every `IMultiTenancy` entity in a model where `ApplyTenantFilters` is called |
| Tenant caching | Enabled, five-minute lifetime |
| Excluded entity types | None |
| Cross-tenant bypass roles | **None — every safe bypass is refused** |
| Tenant expiry, domain and user limits | Reported, never enforced |
| Middleware | None. Scope is established through a scoped factory |

## Documentation

- [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Tenancy/Setup.md) — registration, options, making a context tenant-scoped, seeding
- [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Tenancy/Guide.md) — establishing scope, the store and cache, settings, cross-tenant work
- [API Reference](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Tenancy/API.md)

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
