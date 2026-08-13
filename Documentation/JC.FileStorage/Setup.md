# JC.FileStorage — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project with JC.Core registered
- A writable directory on the host for file storage
- JC.Tenancy is **optional** — it is only required for tenant isolation. Without it, every file belongs to the no-tenant scope
- JC.FileStorage.Web is **optional** — only needed for `IFormFile` handling and the upload constraints tag helper. It brings in JC.Web
- Static files are **opt-in** and need a second directory, `FileStorage:StaticPath`, holding files put there at deploy time. They need no database table
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

For a tenant-isolated application, the same context also declares itself tenant-scoped — see [Multi-tenancy](#multi-tenancy) below.

### Services — `Program.cs`

```csharp
// JC.Core must be registered first — JC.FileStorage resolves its repositories through IRepositoryManager
builder.Services.AddCore<AppDbContext>();

// Registers the folder registry, path provider and storage service.
// useStaticFiles is opt-in: it adds the static file registry, reader and cache,
// and requires FileStorage:StaticPath. Omit it if you only store managed files.
builder.Services.AddFileStorage(useStaticFiles: true);
```

### Folders — `Program.cs`

Folders must be registered before any file is saved or read. Register them once the service provider exists:

```csharp
var app = builder.Build();

// throwOnFail must always be passed — see AddFolders below
app.Services.AddFolders(true, "invoices", "reports");
```

`AddFolders` extends `IServiceProvider`, not a host-specific builder, so the same call works from a worker service or a test host. Applications that also reference JC.FileStorage.Web can use the `app.AddFolders(...)` overload it adds on `IApplicationBuilder` instead — see [JC.FileStorage.Web](#jcfilestorageweb--aspnet-core-integration).

### Configuration — `appsettings.json`

`FileStorage:BasePath` is required. `FilePathProvider` throws `InvalidOperationException` if it is missing. `FileStorage:StaticPath` is only read when static files are enabled:

```json
{
  "FileStorage": {
    "BasePath": "C:\\app-data\\file-storage",
    "StaticPath": "C:\\app-data\\static-files"
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
| Static files | Off — `useStaticFiles` defaults to `false`, so nothing reads `FileStorage:StaticPath` |
| Static file discovery | On when static files are enabled — every file beneath the static path is registered at startup |
| Static file caching | 10 minutes, content held in `IMemoryCache` |
| Tenant of a folder registered by name | The no-tenant scope (`FolderModel.NullTenantName`, the literal `NO__TENANT`) |
| Tenant of a saved file | `ITenantContext.TenantId` — the tenant the operation is scoped to, or the no-tenant scope if JC.Tenancy is not registered |
| Overwrite behaviour | Blocked — `TrySaveFile` returns `false` if the file already exists |
| Maximum file size | None — no limit until you set one, subject to the 10GB ceiling |
| Accepted file types | Any, except the permanently blocked executable extensions |
| Physical file name | The `SavedFile.Id` (a GUID) plus the extension — never the caller's file name |
| Physical layout | `{BasePath}/{tenant}/{folder}/{savedFileId}{extension}` |
| Delete behaviour | The row is soft-deleted; the file is permanently removed from disk |
| Folder nesting | Not supported — folders are a single level of separation |

## 2. Full configuration

### AddFileStorage — service registration

Takes three parameters and no options callback. All three concern static files; the managed file services are always registered.

```csharp
builder.Services.AddFileStorage(
    useStaticFiles: false,
    autoDiscoverStaticFiles: true,
    staticFileCacheDurationMinutes: 10);
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `useStaticFiles` | `bool` | `false` | Registers the static file services. When `false`, the method returns after the three managed file services and nothing reads `FileStorage:StaticPath`. |
| `autoDiscoverStaticFiles` | `bool` | `true` | Walks the static path at startup and registers every file beneath it. Ignored when `useStaticFiles` is `false`. Set to `false` to register files by hand through `AddStaticFiles`. |
| `staticFileCacheDurationMinutes` | `int` | `10` | How long `StaticFileCache` holds a file's content. `0` disables caching so every read goes to disk. A negative value throws `ArgumentOutOfRangeException`. Ignored when `useStaticFiles` is `false`. |

Registers the following, each with `TryAdd` semantics so a prior registration of the same type wins:

| Service | Lifetime | Registered | Purpose |
|---------|----------|------------|---------|
| `FolderRegistry` | Singleton | Always | Holds the registered folders, keyed by tenant |
| `FilePathProvider` | Singleton | Always | Resolves physical paths and creates directories |
| `StorageService` | Scoped | Always | The entry point consuming applications use for managed files |
| `StaticFileRegistry` | Singleton | `useStaticFiles` | Holds the registered static files, keyed by their path beneath the static root |
| `StaticFileService` | Singleton | `useStaticFiles` | Reads a registered static file from disk |
| `StaticFileCache` | Singleton | `useStaticFiles` | Holds static file content in memory. The type most applications inject |

Enabling static files also calls `AddMemoryCache()`, since `StaticFileCache` resolves `IMemoryCache` as required and a worker or console host has none by default.

**`StaticFileRegistry` is built by a factory that runs discovery during construction.** Because it is a singleton, that happens the first time something resolves it rather than at startup, so a missing `FileStorage:StaticPath` surfaces as an `InvalidOperationException` on first use.

`StorageService` resolves `ITenantContext` optionally through the service provider. If JC.Tenancy is registered, the tenant the operation is scoped to stamps every write and scopes every read. If it is not, `ITenantContext` is absent and every call operates in the no-tenant scope.

It reads the **operational** tenant rather than the user's own, deliberately. `SavedFile` is filtered by the operational tenant, so writes have to be stamped from the same source or the two disagree — a background job scoped to a tenant would otherwise write files it could not then see.

### AddFolders — folder registration

An `IServiceProvider` extension with two overloads — one taking folder names, one taking `FolderModel` instances. It extends the service provider rather than a host-specific builder so this package stays free of any ASP.NET Core dependency.

```csharp
var app = builder.Build();

// Names — each folder is registered in the no-tenant scope
app.Services.AddFolders(true, "invoices", "reports");

// FolderModel — required for tenant-scoped folders
app.Services.AddFolders(true,
    new FolderModel("invoices", "tenant-a"),
    new FolderModel("invoices", "tenant-b"));
```

Outside ASP.NET Core, call it on whatever provider the host built:

```csharp
using var host = Host.CreateApplicationBuilder(args).Build();
host.Services.AddFolders(true, "invoices", "reports");
```

JC.FileStorage.Web adds an `IApplicationBuilder` overload that forwards to this one, so `app.AddFolders(...)` remains available to applications referencing that package.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `throwOnFail` | `bool` | `true` | When `true`, a folder that fails to register throws `InvalidOperationException`. When `false`, the failure is skipped silently and the remaining folders are still registered. |
| `folderNames` | `params IEnumerable<string>` | — | Folder names, each registered in the no-tenant scope. |
| `folders` | `params IEnumerable<FolderModel>` | — | Folder models, each registered against its own tenant. |

**`throwOnFail` must always be passed.** Because it precedes a `params` parameter, its default can never be used — `app.Services.AddFolders("invoices")` fails to compile with `CS1503`. Calling `app.Services.AddFolders(true)` with no folders is also a compile error (`CS0121`), as the two overloads are ambiguous with an empty `params`.

Registration fails when a folder of the same name already exists **for that tenant** (compared case-insensitively). The same name under a different tenant is not a conflict:

```csharp
// Both succeed — same name, different tenants
app.Services.AddFolders(true,
    new FolderModel("invoices", "tenant-a"),
    new FolderModel("invoices", "tenant-b"));

// The second is a duplicate and throws with throwOnFail: true
app.Services.AddFolders(true, "reports", "REPORTS");
```

Folders are held in a singleton registry, so registration happens once at startup and applies for the lifetime of the application. A tenant created after startup has no folders until the application registers them.

### Folder limits — size and accepted types

A folder can declare a maximum file size and the extensions it accepts. Both are optional, and both are enforced by `StorageService` itself, so no caller can store a file a folder forbids.

```csharp
app.Services.AddFolders(true,
    // Limits declared on the folder: 10MB, PDFs only
    new FolderModel("invoices", null, 10 * 1024 * 1024, [".pdf"]),

    // No limits declared — falls back to the registry defaults below
    new FolderModel("scratch"));
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The folder name. |
| `tenantId` | `string?` | — | The owning tenant. `null` for the no-tenant scope. |
| `maxBytes` | `long?` | — | Maximum file size in bytes, or `null` to use `FolderRegistry.DefaultMaxBytes`. Must be greater than zero and no more than `ValidationHelper.MaxAllowedBytes`. |
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

**The 10GB ceiling.** `ValidationHelper.MaxAllowedBytes` is a hard limit of 10GB (`10737418240` bytes). Neither a folder nor `DefaultMaxBytes` can be set above it — both throw `ArgumentOutOfRangeException`.

**Blocked extensions cannot be re-enabled.** `ValidationHelper.BlockedExtensions` lists around sixty executable and script extensions — `.exe`, `.bat`, `.cmd`, `.ps1`, `.sh`, `.dll`, `.msi`, `.vbs`, `.js`, `.jar`, `.lnk` and similar — that can never be stored. The list is checked **before** any allow-list, so it wins over one:

```csharp
// Throws ArgumentException — a blocked extension cannot be allowed
new FolderModel("danger", null, null, [".exe"]);
registry.DefaultAllowedExtensions = [".exe"];
```

Even with no limits configured anywhere, a `.exe` is refused. Use `ValidationHelper.IsBlockedExtension(ext)` to test one.

> **`AllowedExtensions` is a usability guard, not a security control.** It compares the extension only, so renaming `evil.exe` to `evil.pdf` passes. Verifying a file really is what it claims means inspecting its content. Treat the blocked list as a safety net against obvious mistakes, not as protection against a determined uploader.

**The blocked list applies to managed files only.** Static files are put in place at deploy time by a developer or a build step rather than uploaded, so nothing about them is untrusted and no extension check is applied to them — see [Static files](#addstaticfiles--static-file-registration).

### AddStaticFiles — static file registration

Static files are read-only files placed beneath `FileStorage:StaticPath` at deploy time — a privacy policy, a pricing table, a configuration document. There is no database record, no audit trail, no upload, no save and no delete. The only operation is reading one.

They are only registered if `AddFileStorage(useStaticFiles: true)` was called. With `autoDiscoverStaticFiles` left at its default of `true`, every file beneath the static path is registered when `StaticFileRegistry` is first resolved, and nothing else is needed:

```text
C:\app-data\static-files\
  privacy-policy.md
  legal\
    terms.md
  config\
    v2\
      pricing.json
```

Turn discovery off to register files by hand instead. `AddStaticFiles` is an `IServiceProvider` extension, matching `AddFolders`:

```csharp
builder.Services.AddFileStorage(useStaticFiles: true, autoDiscoverStaticFiles: false);

var app = builder.Build();

// throwOnFail must always be passed — see the note below
app.Services.AddStaticFiles(true,
    new StaticFile("privacy-policy.md"),
    new StaticFile("terms.md", "legal"),
    new StaticFile("pricing.json", "config", "v2"));
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `throwOnFail` | `bool` | `true` | When `true`, a file that fails to register throws `InvalidOperationException`. When `false`, the failure is skipped and the remaining files are still registered. |
| `files` | `params IEnumerable<StaticFile>` | — | The files to register. Each carries its name, extension and any subfolders. |

**`throwOnFail` must always be passed**, for the same reason as on `AddFolders` — it precedes a `params` parameter, so its default can never be used.

`StaticFile` takes the file name, then any subfolders relative to the static path:

| Constructor | Result |
|-------------|--------|
| `new StaticFile("privacy-policy.md")` | `{StaticPath}/privacy-policy.md` |
| `new StaticFile("terms.md", "legal")` | `{StaticPath}/legal/terms.md` |
| `new StaticFile("pricing.json", "config", "v2")` | `{StaticPath}/config/v2/pricing.json` |

The name must carry an extension — `new StaticFile("privacy-policy")` throws `ArgumentException`. Directory components in the name are stripped, so a name cannot escape the static root.

Registration and discovery can be combined: leave `autoDiscoverStaticFiles` on and call `AddStaticFiles` as well. Discovery runs first, inside the singleton factory, so a hand-registered file that discovery already found returns `false` and, with `throwOnFail: true`, throws.

Files are keyed by their path beneath the static root, compared case-insensitively, so `legal/terms.md` and `docs/terms.md` are different files while `Legal/Terms.md` and `legal/terms.md` are the same one. Discovery throws `InvalidOperationException` if two files collide — on a case-sensitive file system, `Terms.md` and `TERMS.md` in one directory is the case that does it.

**No extension check applies to static files.** The blocked list exists because an uploaded file is untrusted; a static file was put there by whoever deployed the application, so the same reasoning does not hold. If a static file is served to a browser, the application decides what it is willing to serve.

### ApplyFileStorageMappings — entity configuration

Applies the `SavedFile` entity mapping to the EF Core model builder. Call this in your `DbContext`'s `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyFileStorageMappings();
}
```

This configures the key, all column lengths, a composite index over `TenantId`, `FolderName` and `FileName`, and the inherited `AuditModel` columns and indexes. `TenantId` is a plain column with no foreign key — see [Apply migrations](#3-apply-migrations).

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

### FileStorage:StaticPath — static file location

The root directory holding static files. Read only when `AddFileStorage(useStaticFiles: true)` was called, and required in that case.

```json
{
  "FileStorage": {
    "BasePath": "/var/lib/myapp/file-storage",
    "StaticPath": "/var/lib/myapp/static-files"
  }
}
```

`FilePathProvider` reads the key in its constructor but does not validate it there — `GetStaticPath` throws `InvalidOperationException` when it is missing or empty. That happens the first time `StaticFileRegistry` is resolved, not at startup.

Point it somewhere separate from `BasePath`. Nothing enforces that, but the managed store writes tenant directories beneath its own root, and mixing the two makes the layout hard to reason about.

The directory is created if it does not exist, which means a mistyped path produces an empty directory and zero registered files rather than an error. The application account only needs read access to it.

### Multi-tenancy

Tenant isolation is provided by [JC.Tenancy](../JC.Tenancy/Setup.md), not by JC.FileStorage. `SavedFile` implements `IMultiTenancy`, which marks it tenant-scoped and nothing more — the global query filter that acts on the mark is installed by `ApplyTenantFilters`, which the consuming application calls per `DbContext`.

To get tenant-isolated file storage, declare your context tenant-scoped:

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options, IUserInfo userInfo, ITenantInfo tenantInfo)
    : DataDbContext(options, userInfo), IFileStorageDbContext, ITenantScopedContext
{
    public string? CurrentTenantId => tenantInfo.TenantId;

    public DbSet<SavedFile> SavedFiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyFileStorageMappings();
        modelBuilder.ApplyTenantFilters(this);   // last
    }
}
```

Register the engine alongside JC.Core, nominating whichever context owns the tenant table:

```csharp
builder.Services.AddTenancy<AppDbContext>();
```

`ApplyTenantFilters` must be called **last**, after the file storage mappings. It reads the model as it stands when called.

Without JC.Tenancy there is no query filter, so every file belongs to the no-tenant scope. This is a valid configuration for single-tenant applications — see the [Guide](Guide.md) for how the tenant scopes behave.

An identity package is not required for either configuration. Where one is registered, JC.Tenancy derives the operational tenant from the signed-in user by default; where none is, tenant scope is established explicitly or stays in the null partition.

### JC.FileStorage.Web — ASP.NET Core integration

An optional companion package for web applications. It adds `IFormFile` handling, MIME type inference, a tag helper for showing a folder's limits, and an `IApplicationBuilder` overload of `AddFolders` — nothing else.

JC.FileStorage carries **no ASP.NET Core dependency at all**: no framework reference, and every type it exposes works from a console application, a worker service or a test host. Everything ASP.NET-specific lives here, which is why the `app.AddFolders(...)` form is in this package while the `IServiceProvider` form it forwards to is in the base one.

Add a project reference to `JC.FileStorage.Web`, which brings in JC.FileStorage and JC.Web:

```xml
<ProjectReference Include="path/to/JC.FileStorage.Web/JC.FileStorage.Web.csproj" />
```

Register with `AddFileStorageWeb`, which calls `AddFileStorage` and JC.Web's `AddUI` for you:

```csharp
builder.Services.AddCore<AppDbContext>();

// Registers WebStorageService and the UI services the tag helper resolves,
// plus everything AddFileStorage registers
builder.Services.AddFileStorageWeb(
    framework: UIFramework.Bootstrap,
    iconFramework: IconFramework.Bootstrap);
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `framework` | `UIFramework` | `Bootstrap` | The CSS framework the tag helper renders classes for. Selects `BootstrapFileStorageDictionary`, `TailwindFileStorageDictionary` or `CustomJCTailwindFileStorageDictionary` |
| `iconFramework` | `IconFramework` | `Bootstrap` | Passed through to `AddUI`. This package registers no icon dictionary of its own — its tag helper renders no glyphs — so this only matters for packages layered above it |

| Service | Lifetime | Purpose |
|---------|----------|---------|
| `WebStorageService` | Scoped | Wraps `StorageService` for `IFormFile` uploads and file downloads |
| `IFileStorageFrameworkDictionary` | Singleton | The class dictionary for the configured framework |
| `UIFrameworkService`, `AlertHelper`, `HtmlHelper` | Singleton | Registered by the `AddUI` call inside |
| `FolderRegistry`, `FilePathProvider`, `StorageService` | As above | Registered by the `AddFileStorage` call inside |

`StorageService` stays registered and injectable. `WebStorageService` covers uploads, downloads and validation only — inject `StorageService` directly for anything else.

**`AddUI` registers through `TryAdd`, so the first call wins.** An application that has already called `AddWebDefaults` or `AddUI` keeps the framework it chose there, and the arguments here are ignored. Pass the framework to whichever call runs first, or pass the same value to both.

**Under either Tailwind framework, import the shipped safelists.** Tailwind generates utilities by scanning source files, and these class names live in compiled assemblies it never reads — without the imports the help text renders with a valid class name and no CSS behind it. Both files ship in their `.nupkg`, so they reach you either way you consume the suite:

```css
/* Project reference */
@import "../path/to/JC.Web/UI/jc-web.tailwind.css";
@import "../path/to/JC.FileStorage.Web/jc-filestorage.tailwind.css";

/* Package reference — under the global packages folder */
@import "<nuget-root>/jc.web/<version>/contentFiles/any/any/jc-web.tailwind.css";
@import "<nuget-root>/jc.filestorage.web/<version>/contentFiles/any/any/jc-filestorage.tailwind.css";
```

`<nuget-root>` is `%USERPROFILE%\.nuget\packages` on Windows and `~/.nuget/packages` elsewhere. On NuGet, prefer copying both files into your own `Styles` folder — the package path carries the version number, so every upgrade breaks the import until you edit it.

Under `CustomJCTailwind` this package needs nothing of its own — its only value is jc-tailwind-ui's `form-text`, an authored CSS rule in that framework's bundle rather than a generated utility — but JC.Web's safelist is still required for anything else you use from it.

To use the tag helper, add it to `_ViewImports.cshtml`:

```csharp
@addTagHelper *, JC.FileStorage.Web
```

Then it can show a folder's limits beneath a file input:

```html
<input type="file" name="file" class="form-control" />
<upload-constraints folder="invoices" />
```

Which renders, for a folder accepting PDFs and CSVs up to 1MB, under Bootstrap:

```html
<div class="form-text">Accepted types: .pdf, .csv &middot; Maximum size: 1 MB</div>
```

The wrapper's class comes from the configured framework's dictionary — `form-text` under Bootstrap and jc-tailwind-ui, which both define it, and `mt-1 text-sm text-gray-500` under Tailwind. The text itself is read from the same `FolderRegistry` values the server enforces, so it cannot drift from them. See the [Guide](Guide.md#web-applications) for the full attribute list and the upload and download flows.

## 3. Apply migrations

JC.FileStorage introduces a `SavedFiles` table. Generate and apply a migration once the mappings are applied:

```bash
dotnet ef migrations add AddFileStorage --project YourApp
dotnet ef database update --project YourApp
```

`SavedFile.TenantId` is a plain 36-character column with no foreign key and no navigation property. `IMultiTenancy` marks a partition rather than a relationship, so no `Tenant` entity is pulled into your model and no constraint is created — the tenant record may live in another context or another database entirely.

That has a consequence worth knowing: **deleting a tenant does not touch its files.** There is no cascade and nothing sets `TenantId` back to `null`. `ITenantStore.TryDeleteAsync` soft-deletes the tenant record only, and its files keep pointing at the identifier — which is what allows a restore to bring everything back intact.

Adding JC.Tenancy introduces its own `Tenants` table in whichever context you nominate through `AddTenancy<TContext>`, and that is a separate migration concern — see [JC.Tenancy — Setup](../JC.Tenancy/Setup.md#3-apply-migrations).

## 4. Verify

1. Run the application and save a file through `StorageService.TrySaveFile` — it should return `true`.
2. Check `{BasePath}/NO__TENANT/{folder}/` (or `{BasePath}/{tenantId}/{folder}/` for a tenanted user) — it should contain one file named with a GUID and your extension.
3. Query the `SavedFiles` table — it should hold one row with `FileName` stored **without** its extension and `Extension` stored separately.

With static files enabled, put a `test.txt` in `{StaticPath}` and read it through `StaticFileCache.GetStaticFileText("test.txt")` — `Result` should be `true` and `FileContentText` should hold its contents.

## Next steps

- [Guide](Guide.md) — saving, reading and deleting files, folder and tenant scoping, cross-tenant access, and multiple DbContexts.
- [API Reference](API.md)