# JC.Tenancy — Guide

Covers reading the current tenant, establishing scope in requests and background work, managing tenants through the store, tenant settings, and working across tenant boundaries. See [Setup](Setup.md) for registration and option defaults.

## Reading the current tenant

### Basic usage

Inject `ITenantInfo`. It is scoped, so everything in a request or job scope sees the same answer:

```csharp
public class OrderService(ITenantInfo tenant, IRepositoryContext<Order> orders)
{
    public async Task<List<Order>> GetAllAsync()
    {
        // No tenant clause needed — the query filter adds it
        return await orders.AsQueryable().ToListAsync();
    }

    public string Heading => tenant.HasTenant
        ? $"Orders for {tenant.Name}"
        : "Orders";
}
```

`TenantId` costs nothing to read — it is the value the query filters consult on every query. Everything else (`Name`, `Description`, `Domain`, `MaxUsers`, `ExpiryDateUtc`) describes the persisted record and is resolved from the cache the first time one of them is read, so an application that never displays tenant metadata never pays for the lookup.

### Reading from a package that has no tenancy reference

A package that marks its own entities `IMultiTenancy` can read the operational tenant through JC.Core alone:

```csharp
public class StorageService(IServiceProvider services)
{
    private readonly ITenantContext? _tenant = services.GetService<ITenantContext>();

    private string? CurrentTenant => _tenant?.TenantId;
}
```

Resolve it with `GetService`, not `GetRequiredService`. An application that has not registered tenancy has no implementation, and the correct reading of that is the null partition rather than a failure.

`ITenantInfo` and `ITenantContext` resolve to the **same scoped instance**, never two — two instances in one scope could hold different tenants, which is worse than not resolving at all.

### Nuances and gotchas

**`ITenantInfo.TenantId` is not `IUserInfo.TenantId`.** The user's tenant is what scope is *derived* from; the operational tenant is what queries actually follow. They diverge whenever a job or an administrator works elsewhere. Never scope a query by the user's tenant.

**A null tenant is a partition, not a wildcard.** Filters match null to null, so rows with `TenantId = NULL` are visible only in the null partition — isolated exactly like a named tenant.

**Metadata goes null for a soft-deleted tenant, and that is correct.** The cache resolves active tenants only, so after a delete `TenantId` still holds the identifier and `HasTenant` is still `true`, but `Name` and the rest read as null. Query filters compare strings and never touch the tenant table, so data access is unaffected and a restore brings everything back intact.

**`IsExpired` reports; nothing enforces it.** The same is true of `MaxUsers` and `Domain`. Whether an expired tenant may still be used is application policy, and the engine deliberately does not decide it for you.

## Establishing tenant scope

### In a background job

```csharp
public class NightlyBillingJob(IServiceProvider services, ITenantStore tenants) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        foreach (var tenant in await tenants.GetAllAsync(cancellationToken: cancellationToken))
        {
            await using var scope = await services.CreateAsyncScopeForTenant(tenant);

            var orders = scope.ServiceProvider.GetRequiredService<IRepositoryContext<Order>>();
            await BillAsync(await orders.AsQueryable().ToListAsync(cancellationToken));
        }
    }
}
```

Passing the loaded `Tenant` rather than its identifier skips the cache lookup entirely, which matters when you have the record in hand already.

Looping tenants like this is the recommended shape for cross-tenant work. Each iteration is scoped normally, so every query is filtered and nothing bypasses the safety net.

### Re-scoping the current scope

```csharp
services.SetTenantInfoForTenant("acme");
services.SetTenantInfoForTenant((string?)null);   // pin the null partition deliberately
```

Calling this inside a live request re-scopes the rest of that request. That is a deliberate cross-tenant act, not a convenience.

### Resolving by domain

```csharp
var tenant = await scope.ServiceProvider.SetTenantInfoForDomainAsync(domain, cancellationToken);

if (tenant is null)
{
    // Nothing claims that domain — decide what that means for your application
    return;
}
```

A miss leaves the scope untouched rather than pinning the null partition, because an unrecognised value is not the same act as deliberately choosing a partition. Handle the `null`.

### Nuances and gotchas

**Constructing an `ITenantInfo` achieves nothing.** It is scoped and set in place, so nothing that injects it would observe an instance you built yourself. Use the extensions.

**Order does not matter when you set both a user and a tenant.** `ITenantInfo` reads `IUserInfo.TenantId` on every access rather than capturing it, and an explicit tenant is an override that wins from whenever it was set:

```csharp
await using var scope = services.CreateAsyncScopeForUser(user, roles);
scope.ServiceProvider.SetTenantInfoForTenant("acme");   // or the other way round
```

**Assigning `null` is not the same as never assigning.** Both leave you in the null partition, but the first is an override that stops the user's tenant being consulted at all. `IsOverridden` tells the two apart.

**Re-scoping does not re-open a DbContext.** The filters read `CurrentTenantId` per query, so a context already resolved in that scope picks up the new tenant on its next query. Entities already tracked from before the change are not re-evaluated.

## Managing tenants

### Creating

```csharp
public class TenantAdminService(ITenantStore tenants)
{
    public async Task<string?> CreateAsync(string name, string? domain)
    {
        var response = await tenants.TryAddAsync(new Tenant { Name = name, Domain = domain });

        return response.IsValid
            ? response.ValidatedTenant!.Id
            : null;   // response.ErrorMessage explains why
    }
}
```

Every mutation returns a `TenantValidationResponse` rather than throwing. Check `IsValid`, then read either `ValidatedTenant` or `ErrorMessage`.

### Reading

```csharp
var byId      = await tenants.GetAsync(tenantId);
var byName    = await tenants.GetByNameAsync("Acme Ltd");
var byDomain  = await tenants.GetByDomainAsync("acme.example.com");
var all       = await tenants.GetAllAsync();
var firstPage = await tenants.GetAllAsync(pageNumber: 1, pageSize: 20);
```

Every read takes a `DeletedQueryType` and defaults to active tenants only.

`GetByNameAsync` and `GetByDomainAsync` both match case-insensitively. A null or empty `domain` matches nothing — tenants with no domain of their own are excluded from the comparison rather than treated as candidates.

### Deleting and restoring

```csharp
await tenants.TryDeleteAsync(tenantId);          // soft delete, returns bool

var restored = await tenants.TryRestoreAsync(tenantId);
if (!restored.IsValid)
    _logger.LogWarning("Could not restore: {Reason}", restored.ErrorMessage);
```

Deletion is soft and **does not cascade**. The tenant record is marked deleted; rows elsewhere keep their `TenantId`, because tenant-scoped data can live in contexts and databases this store has never heard of.

### Nuances and gotchas

**A deleted tenant keeps its name and domain.** The unique index still holds its row, so the name is never freed. That is why a restore can never clash — and why the validation message distinguishes the two cases, telling you a *deleted* tenant holds the value rather than leaving you hunting for something you cannot find.

**Validation checks against deleted tenants too.** Validating active-only would pass here and then fail on the database constraint.

**Restoring re-validates.** A domain freed by the delete may have been claimed since, so `TryRestoreAsync` can legitimately fail. Rename the other tenant, or the one being restored.

**Writing tenants through EF directly works, but nothing invalidates the cache.** Those changes stay invisible until the entry expires — five minutes by default. Going through the store is what makes invalidation a guarantee rather than a coincidence.

**`Tenant` does not implement `IMultiTenancy`**, so the tenant table is never filtered by itself. Reading tenants is not a cross-tenant operation.

## Tenant settings

### Reading

```csharp
public class BrandingService(ITenantInfo tenant)
{
    public string Colour => tenant.GetSetting("brand_colour") ?? "#0d6efd";

    public bool BetaEnabled => tenant.GetSetting("beta_features", defaultValue: false);

    public IReadOnlyList<TenantSettings> All => tenant.GetSettings();
}
```

The typed overload converts through `TypeDescriptor` and returns `defaultValue` rather than throwing where the key is missing, the setting is inactive, or the value cannot be converted.

### Writing

```csharp
await tenants.TrySetSettingAsync(tenantId, "brand_colour", "#198754");
await tenants.TrySetSettingAsync(tenantId, "beta_features", "false", isActive: false);
```

Returns `false` where no active tenant has that identifier. Invalidates the cache, so the next read sees the new value.

### Nuances and gotchas

**Only active settings are returned.** `GetSetting` and `GetSettings` filter on `IsActive`, so deactivating a setting is equivalent to removing it as far as readers are concerned — while keeping the value should you want it back.

**Keys are matched case-insensitively on read, case-sensitively on write.** `GetSetting` compares with `OrdinalIgnoreCase`; `SetSetting` matches an existing key exactly. Writing `Brand_Colour` where `brand_colour` exists therefore produces two entries, and the reader gets whichever comes first.

**Settings are JSON in one column.** A malformed value returns an empty collection rather than throwing, because the value is consuming-application data and a throw would surface inside an unrelated request.

**The whole collection is rewritten on every `SetSetting`.** Two concurrent writes to different keys on the same tenant can lose one of them.

## Working across tenants

### The safe route

```csharp
public class AuditOverviewService(IRepositoryContext<Order> orders, ITenantBypassAuthoriser authoriser)
{
    public IQueryable<Order> Everything() => orders.AsQueryable().AllTenants(authoriser);
}
```

Permitted only if the current user holds one of the roles named in `TenantOptions.BypassRoles`. If not, the query comes back filtered — the caller sees their own tenant's data rather than an exception.

**That silence is deliberate, and it means you cannot tell from the result whether the bypass applied.** Where it matters, ask first:

```csharp
if (!authoriser.CanAccessAllTenants())
    return Forbid();
```

### The unsafe route

```csharp
var everything = orders.AsQueryable().AllTenantsUnsafe();
```

No permission check at all — for trusted work with no user to authorise: reconciliation jobs, maintenance tooling, migrations.

### Nuances and gotchas

**`AllTenantsUnsafe` drops every global filter on the entity, not only the tenant one**, because `IgnoreQueryFilters` is all-or-nothing. Soft-delete is unaffected — that is applied by `FilterDeleted`, an explicit operator rather than a global filter.

**Prefer looping tenants to bypassing the filter.** A scoped loop keeps every query filtered, so a mistake affects one tenant rather than all of them. Reach for a bypass when you genuinely need one query spanning tenants — a reconciliation total, an administrative search.

**A refused bypass is warned about once, and only for missing configuration.** If nobody can ever query across tenants, check the startup log for the warning naming `AllowBypassForRole` before assuming the role check is at fault.

## How this fits with the rest of the suite

### Identity

Neither package references the other. `IUserInfo.TenantId` is where the default operational tenant comes from, read live rather than captured, so signing a user in is enough to put their requests in the right tenant.

Cross-tenant permissions are matched by role **name** rather than by a constant, which is what lets JC.Tenancy stay independent of whichever authority issued the role.

### Tenant-scoped packages

A package marks its entities `IMultiTenancy` and reads `ITenantContext` from JC.Core, taking no dependency on this package. JC.FileStorage does exactly that: `SavedFile` is tenant-scoped, and storage paths are stamped from the operational tenant.

That is why writes and reads agree. A package stamping from `IUserInfo` instead would write under the user's tenant while being filtered by the operational one — the two agree in an ordinary request and diverge in every job.

### Audit trail

Tenant records extend JC.Core's `AuditModel`, so creating, updating, deleting and restoring a tenant are all audited and attributed to the ambient `IUserInfo`.

## Next steps

- [Setup](Setup.md) — registration, options and their defaults.
- [API Reference](API.md)
- [JC.Identity — Setup](../JC.Identity/Setup.md) — deriving tenant scope from signed-in users.
