# JC.Core

The foundation every other JC package builds on. A repository and unit-of-work layer over EF Core with multi-DbContext support, an audit trail written automatically on `SaveChanges`, soft-delete, pagination — and the contracts the rest of the suite shares.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.Core/JC.Core.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

## Prerequisites

- .NET 9.0 SDK
- A `DbContext` implementing `IDataDbContext` — extend `DataDbContext`, or `IdentityDataDbContext` when using [JC.Identity](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.Identity)
- A database provider: [JC.SqlServer](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.SqlServer) or [JC.MySql](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.MySql)

## Quick start

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.AddSqlServerDatabase<AppDbContext>(builder.Configuration, migrationsAssembly: "MyApp");
```

Pass an application name when several applications share a database, and every audit row it writes carries it:

```csharp
builder.Services.AddCore<AppDbContext>(applicationName: "AdminPortal");
```

### Configuration — `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MyApp;Trusted_Connection=true;"
  }
}
```

## Feature areas

### Repositories

Inject `IRepositoryManager` and ask it for a repository per entity type — there is no per-entity registration:

```csharp
public class ProductService(IRepositoryManager repos)
{
    private IRepositoryContext<Product> Products => repos.GetRepository<Product>();

    public Task<Product> CreateAsync(Product product) => Products.AddAsync(product);

    public Task<Product?> GetAsync(int id) => Products.GetByIdAsync(id);
}
```

Every write stamps the audit fields from `IUserInfo` — the signed-in user where JC.Identity is registered, a fallback identifier where it is not.

### Batching and transactions

Every method saves immediately unless told otherwise:

```csharp
await Products.AddAsync(first, saveNow: false);
await Products.AddAsync(second, saveNow: false);
await repos.SaveChangesAsync();          // one round trip

await repos.BeginTransactionAsync();
try
{
    await Products.UpdateAsync(product);
    await repos.CommitTransactionAsync();
}
catch
{
    await repos.RollbackTransactionAsync();
    throw;
}
```

### Multiple DbContexts

Reach another context through `For<T>()`. Each gets its own manager, cached, with its own transaction:

```csharp
var reporting = repos.For<ReportingDbContext>();
var snapshots = reporting.GetRepository<Snapshot>();
```

A transaction opened on one manager does not span the contexts reached through `For<T>()` — they are separate connections and commit separately.

### Soft-delete

```csharp
await Products.SoftDeleteAsync(product);   // sets IsDeleted, DeletedById, DeletedUtc
await Products.RestoreAsync(product);      // clears them, stamps RestoredById/Utc

var active = Products.AsQueryable().FilterDeleted(DeletedQueryType.OnlyActive).ToList();
var binned = Products.AsQueryable().FilterDeleted(DeletedQueryType.OnlyDeleted).ToList();
```

`DeleteAsync` is the hard delete, and remains available where a row genuinely must go.

### Pagination

```csharp
var page = await Products.AsQueryable()
    .Where(p => !p.IsDeleted)
    .OrderBy(p => p.Name)
    .ToPagedListAsync(pageNumber: 1, pageSize: 20);
```

`PagedList<T>` carries the items, total count, page count and navigation flags. A page number beyond the last adjusts to the final page rather than returning nothing.

### Audit trail

Entities extending `AuditModel` are tracked automatically on `SaveChangesAsync` — no call sites to remember. Each `AuditEntry` records the action, the table and entity, both the context user and the acting user, and the writing application.

### Shared contracts

Several types live here purely so packages can cooperate without depending on each other:

| Contract | Implemented by | Used by |
|----------|----------------|---------|
| `IUserInfo` | JC.Identity | Audit attribution, tenant scoping, notifications, messaging, file storage |
| `IBackgroundJob` | Your job classes | JC.BackgroundJobs, and the cleanup jobs across the suite |
| `IMultiTenancy` / `Tenant` | Your entities | JC.Identity's global query filters, JC.FileStorage |

That is why a package can declare a background job or a tenant-scoped entity without referencing JC.BackgroundJobs or JC.Identity at all.

### Retention jobs

`AuditCleanupJob` and `SoftDeleteCleanupJob` implement `IBackgroundJob`, in both ambient and `<TContext>` forms, so a single host can clean several databases:

```csharp
builder.Services.ConfigureCoreBackgroundJobs(o => o.EnableAuditCleanupJob = true);
builder.Services.AddHangfireJob<AuditCleanupJob<AppDbContext>>(o => o.Cron = "0 3 * * *");
```

### Helpers

Extensions for strings (slugs, display names, truncation, masking), enums (display names, descriptions), dates (relative time), plus colour and country helpers.

## Defaults

| Behaviour | Default |
|-----------|---------|
| `IRepositoryManager` / `IDataDbContext` / `DbContext` lifetime | Scoped, registered with `TryAdd` so an earlier registration wins |
| Save behaviour | Immediate — pass `saveNow: false` to batch |
| Audit trail | On for entities extending `AuditModel` |
| `AuditEntry.SourceApplication` | `null` unless `applicationName` is passed to `AddCore` |
| Repository registration per entity | None required — obtained from `IRepositoryManager` at runtime |

## Documentation

- [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Core/Setup.md) — registration, multi-context, audit and job options
- [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Core/Guide.md) — repository usage, soft-delete, pagination, audit behaviour, helpers
- [API Reference](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Core/API.md)
- [Database Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Core/Database-Setup.md) — provider registration and connection strings

## Versioning

Major and minor versions are shared across the whole suite. JC.Core is the exception to package-specific patches: a patch here bumps every package that depends on it. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
