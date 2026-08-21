# JC.FileStorage

Tenant-scoped file storage for .NET — bytes on disk, records in the database. Files are keyed by a generated identifier rather than the uploaded name, so a hostile file name never reaches the file system, and folder limits are enforced by the storage service itself so no caller can bypass them.

Also serves read-only static files: documents placed on disk at deploy time, registered at startup and cached in memory, with no database record and no way to write them.

No ASP.NET Core dependency: it runs unchanged from a console application, a worker service or a test host.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.FileStorage/JC.FileStorage.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

## Prerequisites

- .NET 9.0 SDK
- **JC.Core**, registered with `AddCore<AppDbContext>()`
- A `DbContext` implementing `IFileStorageDbContext`
- **JC.Tenancy** for multi-tenancy — without it every file belongs to the no-tenant scope, which is a valid single-tenant configuration
- A writable directory for `FileStorage:BasePath`, and a readable one for `FileStorage:StaticPath` if static files are enabled

## Quick start

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.AddFileStorage();
```

### Folders — after `Build()`

Folders must be registered before any file is saved or read:

```csharp
var app = builder.Build();

// throwOnFail must always be passed — it precedes a params parameter
app.Services.AddFolders(true, "invoices", "reports");
```

`AddFolders` extends `IServiceProvider`, so the same call works from any host. [JC.FileStorage.Web](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.FileStorage.Web) adds an `app.AddFolders(...)` overload on `IApplicationBuilder`.

### Data — `AppDbContext`

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyFileStorageMappings();
}
```

### Configuration — `appsettings.json`

```json
{
  "FileStorage": {
    "BasePath": "C:\\app-data\\file-storage",
    "StaticPath": "C:\\app-data\\static-files"
  }
}
```

`BasePath` is required. The directory need not exist — tenant and folder directories are created on demand — but the application account needs write access to it. `StaticPath` is only read when static files are enabled.

## Feature areas

### Saving and reading

```csharp
public class InvoiceService(StorageService storage)
{
    private static readonly FolderModel Invoices = new("invoices");

    public Task<bool> SaveAsync(string name, byte[] pdf) =>
        storage.TrySaveFile(Invoices, name, pdf, ".pdf");

    public Task<GetFileByteResponse> ReadAsync(string name) =>
        storage.GetSavedFileBytes(Invoices, name);
}
```

Text and byte overloads exist for both saving and reading. `TrySaveFile` returns `false` rather than throwing when a file is rejected or already exists — overwriting is blocked.

### Folder limits

A folder can cap size and restrict extensions, falling back to registry-wide defaults:

```csharp
app.Services.AddFolders(true,
    new FolderModel("invoices", null, 10 * 1024 * 1024, [".pdf"]),  // 10MB, PDFs only
    new FolderModel("scratch"));                                     // inherits the defaults

var registry = app.Services.GetRequiredService<FolderRegistry>();
registry.DefaultMaxBytes = 5 * 1024 * 1024;
registry.DefaultAllowedExtensions = [".pdf", ".png"];
```

`StorageService` applies these itself, so a rejected file never reaches disk or the database whichever entry point is used — including a background job calling `TrySaveFile` directly. Ask first when you need the reason:

```csharp
var check = registry.ValidateFile(folder, ".pdf", content.Length);
if (!check.Result) return BadRequest(check.ErrorMessage);
```

### Blocked extensions

Executable extensions — `.exe`, `.bat`, `.ps1` and around sixty more — are refused before any folder rule is consulted, and no configuration re-enables them.

### Tenant scoping

Folders are registered per tenant, and files are scoped to the tenant the current operation is running under, read from `ITenantContext`. The `*ForTenant` overloads reach across tenants deliberately:

```csharp
await storage.GetSavedFileBytesForTenant(tenantId, folder, fileName);
```

**These perform no authorisation check.** JC.FileStorage cannot see roles, which live in JC.Identity — any caller reaching them can reach any tenant's files, so the application must authorise every call.

### Storage layout

```
{BasePath}/{tenant}/{folder}/{savedFileId}{extension}
```

The physical name is the record's identifier, never the caller's. The original name, extension, folder and tenant live on the `SavedFile` row. Folders are a single level deep — `client/invoices` is not a path but an invalid folder name.

### Deletion

```csharp
await storage.TryDeleteFile(folder, fileName);
```

The database row is soft-deleted through JC.Core and stays auditable; the file itself is removed from disk.

### Static files

Deploy-time documents — a privacy policy, a pricing table, a configuration file. Opt in, and everything beneath `FileStorage:StaticPath` is registered at startup:

```csharp
builder.Services.AddFileStorage(useStaticFiles: true);
```

```csharp
public class PrivacyModel(StaticFileCache staticFiles) : PageModel
{
    public string? Policy { get; private set; }
    public string? Updated { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var response = await staticFiles.GetStaticFileText("privacy-policy.md", ct);
        if (response.Result)
        {
            Policy = response.FileContentText;
            Updated = response.File?.LastModified("d MMMM yyyy");
        }
    }
}
```

Subfolders come after the token — `GetStaticFileText("terms.md", ct, "legal")`. Content is cached for ten minutes by default; inject `StaticFileService` to read straight from disk.

`LastModifiedUtc` is the file's last write time on disk, taken when it is registered and again on every read that reaches the disk, so the date always describes the content returned with it.

These are deliberately not manageable: no database record, no audit, no upload, save or delete. Only registered files can be read, which is what stops a crafted name escaping the static path. The blocked-extension list does not apply — a static file was put there by whoever deployed the application.

### Multiple DbContexts

```csharp
storage.ChangeContext(typeof(ArchiveDbContext));
```

Affects every later call on that instance.

## Defaults

| Behaviour | Default |
|-----------|---------|
| `FolderRegistry` / `FilePathProvider` lifetime | Singleton |
| `StorageService` lifetime | Scoped |
| Tenant of a folder registered by name | The no-tenant scope |
| Maximum file size | None, up to a 10GB ceiling |
| Accepted file types | Any except the permanently blocked executable extensions |
| Overwrite | Blocked — `TrySaveFile` returns `false` |
| Delete | Row soft-deleted, file removed from disk |
| Folder nesting | Not supported |
| Static files | Off — `useStaticFiles` defaults to `false` |
| Static file discovery | On when static files are enabled |
| Static file caching | 10 minutes, in `IMemoryCache` |

## Documentation

- [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.FileStorage/Setup.md) — registration, folder limits, static file registration, migrations
- [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.FileStorage/Guide.md) — saving, reading, tenant scoping, cross-tenant access, static files, multiple contexts
- [API Reference](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.FileStorage/API.md)

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
