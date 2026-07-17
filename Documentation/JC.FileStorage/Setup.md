# JC.FileStorage — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project with JC.Core registered
- A writable directory on the host for file storage
- JC.Identity is **optional** — it is only required for multi-tenancy. Without it, every file belongs to the no-tenant scope
- JC.FileStorage.Web is **optional** — only needed for `IFormFile` handling and the upload constraints tag helper. It brings in JC.Web
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

## 0. Add the package

Add a project reference to `JC.FileStorage`:

```xml
<ProjectReference Include="path/to/JC.FileStorage/JC.FileStorage.csproj" />
```

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### DbContext

Your `DbContext` must implement `IFileStorageDbContext` and apply the file storage data mappings:

```csharp
public class AppDbContext : DataDbContext, IFileStorageDbContext
{
    public AppDbContext(DbContextOptions options) : base(options) { }

    public DbSet<SavedFile> SavedFiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyFileStorageMappings();
    }
}
```

For a multi-tenant application, extend `IdentityDataDbContext<TUser, TRole>` instead of `DataDbContext` — see [Multi-tenancy](#multi-tenancy) below.

### Services — `Program.cs`

```csharp
// JC.Core must be registered first — JC.FileStorage resolves its repositories through IRepositoryManager
builder.Services.AddCore<AppDbContext>();

// Registers the folder registry, path provider, and storage service
builder.Services.AddFileStorage();
```

### Folders — `Program.cs`

Folders must be registered before any file is saved or read. Register them after `app.Build()`:

```csharp
var app = builder.Build();

// throwOnFail must always be passed — see AddFolders below
app.AddFolders(true, "invoices", "reports");
```

### Configuration — `appsettings.json`

`FileStorage:BasePath` is required. `FilePathProvider` throws `InvalidOperationException` if it is missing:

```json
{
  "FileStorage": {
    "BasePath": "C:\\app-data\\file-storage"
  }
}
```

### Defaults

When registered as above:

| Default | Value |
|---------|-------|
| `FolderRegistry` lifetime | Singleton — folders are registered once at startup and shared across requests |
| `FilePathProvider` lifetime | Singleton |
| `StorageService` lifetime | Scoped |
| Tenant of a folder registered by name | The no-tenant scope (`FolderModel.NullTenantName`, the literal `NO__TENANT`) |
| Tenant of a saved file | `IUserInfo.TenantId`, or the no-tenant scope if JC.Identity is not registered |
| Overwrite behaviour | Blocked — `TrySaveFile` returns `false` if the file already exists |
| Maximum file size | None — no limit until you set one, subject to the 10GB ceiling |
| Accepted file types | Any, except the permanently blocked executable extensions |
| Physical file name | The `SavedFile.Id` (a GUID) plus the extension — never the caller's file name |
| Physical layout | `{BasePath}/{tenant}/{folder}/{savedFileId}{extension}` |
| Delete behaviour | The row is soft-deleted; the file is permanently removed from disk |
| Folder nesting | Not supported — folders are a single level of separation |

## 2. Full configuration

### AddFileStorage — service registration

Takes no parameters and no options callback.

```csharp
builder.Services.AddFileStorage();
```

Registers the following, each with `TryAdd` semantics so a prior registration of the same type wins:

| Service | Lifetime | Purpose |
|---------|----------|---------|
| `FolderRegistry` | Singleton | Holds the registered folders, keyed by tenant |
| `FilePathProvider` | Singleton | Resolves physical paths and creates directories |
| `StorageService` | Scoped | The entry point consuming applications use |

`StorageService` resolves `IUserInfo` optionally through the service provider. If JC.Identity is registered, the current user's tenant scopes every call. If it is not, `IUserInfo` is absent and every call operates in the no-tenant scope.

### AddFolders — folder registration

An `IApplicationBuilder` extension with two overloads — one taking folder names, one taking `FolderModel` instances.

```csharp
var app = builder.Build();

// Names — each folder is registered in the no-tenant scope
app.AddFolders(true, "invoices", "reports");

// FolderModel — required for tenant-scoped folders
app.AddFolders(true,
    new FolderModel("invoices", "tenant-a"),
    new FolderModel("invoices", "tenant-b"));
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `throwOnFail` | `bool` | `true` | When `true`, a folder that fails to register throws `InvalidOperationException`. When `false`, the failure is skipped silently and the remaining folders are still registered. |
| `folderNames` | `params IEnumerable<string>` | — | Folder names, each registered in the no-tenant scope. |
| `folders` | `params IEnumerable<FolderModel>` | — | Folder models, each registered against its own tenant. |

**`throwOnFail` must always be passed.** Because it precedes a `params` parameter, its default can never be used — `app.AddFolders("invoices")` fails to compile with `CS1503`. Calling `app.AddFolders(true)` with no folders is also a compile error (`CS0121`), as the two overloads are ambiguous with an empty `params`.

Registration fails when a folder of the same name already exists **for that tenant** (compared case-insensitively). The same name under a different tenant is not a conflict:

```csharp
// Both succeed — same name, different tenants
app.AddFolders(true,
    new FolderModel("invoices", "tenant-a"),
    new FolderModel("invoices", "tenant-b"));

// The second is a duplicate and throws with throwOnFail: true
app.AddFolders(true, "reports", "REPORTS");
```

Folders are held in a singleton registry, so registration happens once at startup and applies for the lifetime of the application. A tenant created after startup has no folders until the application registers them.

### Folder limits — size and accepted types

A folder can declare a maximum file size and the extensions it accepts. Both are optional, and both are enforced by `StorageService` itself, so no caller can store a file a folder forbids.

```csharp
app.AddFolders(true,
    // Limits declared on the folder: 10MB, PDFs only
    new FolderModel("invoices", null, 10 * 1024 * 1024, [".pdf"]),

    // No limits declared — falls back to the registry defaults below
    new FolderModel("scratch"));
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The folder name. |
| `tenantId` | `string?` | — | The owning tenant. `null` for the no-tenant scope. |
| `maxBytes` | `long?` | — | Maximum file size in bytes, or `null` to use `FolderRegistry.DefaultMaxBytes`. Must be greater than zero and no more than `FolderModel.MaxAllowedBytes`. |
| `allowedExtensions` | `IEnumerable<string>?` | — | Accepted extensions, or `null` to use `FolderRegistry.DefaultAllowedExtensions`. Normalised to lower case with a leading dot, so `PDF`, `.pdf` and `.PDF` are the same thing. |

Limits are a four-argument constructor rather than optional parameters: `new FolderModel("x", null)` would otherwise be ambiguous between `tenantId` and `maxBytes`. Pass `null` for the tenant on a no-tenant folder.

### Registry defaults and the blocked list

`FolderRegistry` holds the fallback used by folders that declare no limits of their own. Both are editable at any point, and both default to `null` — meaning no size limit, and any type that is not blocked.

```csharp
var registry = app.Services.GetRequiredService<FolderRegistry>();

registry.DefaultMaxBytes = 5 * 1024 * 1024;              // 5MB
registry.DefaultAllowedExtensions = [".pdf", ".png", ".csv"];
```

| Member | Type | Default | Description |
|--------|------|---------|-------------|
| `DefaultMaxBytes` | `long?` | `null` | Size limit for folders with no `MaxBytes`. `null` means no limit. Throws `ArgumentOutOfRangeException` if set to zero, a negative, or above the ceiling. |
| `DefaultAllowedExtensions` | `IReadOnlyList<string>?` | `null` | Accepted extensions for folders with no `AllowedExtensions`. `null` means any non-blocked type. Throws `ArgumentException` if empty or if it names a blocked extension. |

A folder's own value always wins; the default applies only where the folder left it `null`. `ResolveMaxBytes` and `ResolveAllowedExtensions` return whichever is in force.

**The 10GB ceiling.** `FolderModel.MaxAllowedBytes` is a hard limit of 10GB (`10737418240` bytes). Neither a folder nor `DefaultMaxBytes` can be set above it — both throw `ArgumentOutOfRangeException`.

**Blocked extensions cannot be re-enabled.** `FolderModel.BlockedExtensions` lists around sixty executable and script extensions — `.exe`, `.bat`, `.cmd`, `.ps1`, `.sh`, `.dll`, `.msi`, `.vbs`, `.js`, `.jar`, `.lnk` and similar — that can never be stored. The list is checked **before** any allow-list, so it wins over one:

```csharp
// Throws ArgumentException — a blocked extension cannot be allowed
new FolderModel("danger", null, null, [".exe"]);
registry.DefaultAllowedExtensions = [".exe"];
```

Even with no limits configured anywhere, a `.exe` is refused. Use `FolderModel.IsBlockedExtension(ext)` to test one.

> **`AllowedExtensions` is a usability guard, not a security control.** It compares the extension only, so renaming `evil.exe` to `evil.pdf` passes. Verifying a file really is what it claims means inspecting its content. Treat the blocked list as a safety net against obvious mistakes, not as protection against a determined uploader.

### ApplyFileStorageMappings — entity configuration

Applies the `SavedFile` entity mapping to the EF Core model builder. Call this in your `DbContext`'s `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyFileStorageMappings();
}
```

This configures the key, all column lengths, the relationship to `Tenant`, a composite index over `TenantId`, `FolderName` and `FileName`, and the inherited `AuditModel` columns and indexes.

### IFileStorageDbContext — database contract

A marker interface exposing the `SavedFile` table. Implement it on your application's `DbContext`:

```csharp
public class AppDbContext : DataDbContext, IFileStorageDbContext
{
    public DbSet<SavedFile> SavedFiles { get; set; } = null!;
}
```

| Property | Type | Description |
|----------|------|-------------|
| `SavedFiles` | `DbSet<SavedFile>` | The saved file records table. |

### FileStorage:BasePath — storage location

The root directory under which all files are written. Required.

```json
{
  "FileStorage": {
    "BasePath": "/var/lib/myapp/file-storage"
  }
}
```

`FilePathProvider` reads this key in its constructor and throws `InvalidOperationException` if it is missing or empty. Because `FilePathProvider` is a singleton, this surfaces the first time it is resolved rather than at startup.

The directory does not need to exist — `FilePathProvider` creates each tenant and folder directory on demand. The account running the application needs write access to the base path.

### Multi-tenancy

Tenant isolation is provided by JC.Identity, not by JC.FileStorage. `SavedFile` implements `IMultiTenancy`, and the global query filter that scopes it to the current tenant is applied by `IdentityDataDbContext`. To get tenant-isolated file storage, your `DbContext` must extend it:

```csharp
public class AppDbContext : IdentityDataDbContext<AppUser, AppRole>, IFileStorageDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options, IUserInfo userInfo)
        : base(options, userInfo) { }

    public DbSet<SavedFile> SavedFiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyFileStorageMappings();
    }
}
```

Without JC.Identity there is no `IUserInfo` and no query filter, so every file belongs to the no-tenant scope. This is a valid configuration for single-tenant applications — see the [Guide](Guide.md) for how the tenant scopes behave.

### JC.FileStorage.Web — ASP.NET Core integration

An optional companion package for web applications. It adds `IFormFile` handling, MIME type inference, and a tag helper for showing a folder's limits — nothing else. JC.FileStorage has no ASP.NET dependency of its own and is fully usable without this package, from background jobs and console applications.

Add a project reference to `JC.FileStorage.Web`, which brings in JC.FileStorage and JC.Web:

```xml
<ProjectReference Include="path/to/JC.FileStorage.Web/JC.FileStorage.Web.csproj" />
```

Register with `AddFileStorageWeb`, which calls `AddFileStorage` for you:

```csharp
builder.Services.AddCore<AppDbContext>();

// Registers WebStorageService, plus everything AddFileStorage registers
builder.Services.AddFileStorageWeb();
```

| Service | Lifetime | Purpose |
|---------|----------|---------|
| `WebStorageService` | Scoped | Wraps `StorageService` for `IFormFile` uploads and file downloads |
| `FolderRegistry`, `FilePathProvider`, `StorageService` | As above | Registered by the `AddFileStorage` call inside |

`StorageService` stays registered and injectable. `WebStorageService` covers uploads, downloads and validation only — inject `StorageService` directly for anything else.

To use the tag helper, add it to `_ViewImports.cshtml`:

```csharp
@addTagHelper *, JC.FileStorage.Web
```

Then it can show a folder's limits beneath a file input:

```html
<input type="file" name="file" class="form-control" />
<upload-constraints folder="invoices" />
```

Which renders, for a folder accepting PDFs and CSVs up to 1MB:

```html
<div class="form-text">Accepted types: .pdf, .csv &middot; Maximum size: 1 MB</div>
```

The text is read from the same `FolderRegistry` values the server enforces, so it cannot drift from them. See the [Guide](Guide.md#web-applications) for the full attribute list and the upload and download flows.

## 3. Apply migrations

JC.FileStorage introduces a `SavedFiles` table. Generate and apply a migration once the mappings are applied:

```bash
dotnet ef migrations add AddFileStorage --project YourApp
dotnet ef database update --project YourApp
```

`SavedFile` has a required relationship to `Tenant`, so the `Tenant` entity is pulled into your model whether or not you use JC.Identity. The table name differs between the two cases:

| Setup | Tenant table name |
|-------|-------------------|
| `IdentityDataDbContext` (JC.Identity) | `Tenants` — named after its `DbSet<Tenant> Tenants` property |
| `DataDbContext` (JC.Core only) | `Tenant` — named after the entity type, as no `DbSet` declares it |

If you are not using JC.Identity you will get a `Tenant` table you never populate. `SavedFile.TenantId` stays `null` for every row, so the relationship is never exercised.

Deleting a tenant row sets `TenantId` to `null` on its files rather than deleting them, so their records survive in the no-tenant scope.

## 4. Verify

1. Run the application and save a file through `StorageService.TrySaveFile` — it should return `true`.
2. Check `{BasePath}/NO__TENANT/{folder}/` (or `{BasePath}/{tenantId}/{folder}/` for a tenanted user) — it should contain one file named with a GUID and your extension.
3. Query the `SavedFiles` table — it should hold one row with `FileName` stored **without** its extension and `Extension` stored separately.

## Next steps

- [Guide](Guide.md) — saving, reading and deleting files, folder and tenant scoping, cross-tenant access, and multiple DbContexts.
- [API Reference](API.md)