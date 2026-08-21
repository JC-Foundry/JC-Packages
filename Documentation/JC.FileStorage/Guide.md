# JC.FileStorage — Guide

Covers folder registration, saving, reading and deleting files, how file names and extensions are stored, tenant scoping, cross-tenant access, static files, and using multiple DbContexts. See [Setup](Setup.md) for registration.

JC.FileStorage is deliberately small. It is **not** a document management package — there is no archiving, no versioning, and no nested folder structure. Folders provide a single level of separation, and files are addressed by folder and name within a tenant.

The package holds two separate things. **Managed files** are uploaded at runtime, backed by a `SavedFile` row, tenant-scoped and audited — everything up to [Cross-tenant access](#cross-tenant-access) concerns them. **[Static files](#static-files)** are put in place at deploy time and only ever read: no record, no tenancy, no audit, no writing of any kind.

## Folders

### Registering folders

Folders live in a singleton `FolderRegistry` populated at startup. A folder must be registered before any file can be saved into or read from it:

```csharp
var app = builder.Build();

// No-tenant folders
app.Services.AddFolders(true, "invoices", "reports");

// Tenant-scoped folders
app.Services.AddFolders(true,
    new FolderModel("invoices", "tenant-a"),
    new FolderModel("invoices", "tenant-b"));
```

`AddFolders` extends `IServiceProvider`, so the same call registers folders from a worker service or a test host as readily as from a web application. Referencing JC.FileStorage.Web adds an `app.AddFolders(...)` overload on `IApplicationBuilder` that forwards to it.

Passing an unregistered folder to any `StorageService` method throws `ArgumentException` from `FilePathProvider.GetPath`.

**Nuance:** folders are registered per tenant, and the registry is fixed at startup. A tenant created while the application is running has no folders until they are registered. If tenants are dynamic, register their folders when the tenant is created by resolving `FolderRegistry` and calling `TryAddFolder` directly.

### Folder names

`FolderModel` rejects names containing `/`, `\`, `.` or `?`, and names longer than 256 characters:

```csharp
new FolderModel("invoices");        // fine
new FolderModel("2024.invoices");   // ArgumentException — contains '.'
new FolderModel("client/invoices"); // ArgumentException — contains '/'
```

There is no nesting. `client/invoices` is not a path — it is an invalid folder name.

### Inspecting the registry

```csharp
public class FolderService(FolderRegistry folders, ITenantContext? tenant = null)
{
    public IReadOnlyList<string> AvailableFolders()
        => folders.GetFolderNames(tenant?.TenantId);

    public bool FolderExists(string name)
        => folders.TryGetFolder(name, tenant?.TenantId, out _);
}
```

`GetFolderNames` returns an empty list rather than throwing when a tenant has no folders.

**Resolve folders by the operational tenant, not the user's.** `ITenantContext.TenantId` is what `StorageService` stamps and filters by; `IUserInfo.TenantId` is the tenant assigned to the signed-in user, and the two differ whenever a background job or an administrator works elsewhere. Look a folder up by the wrong one and the save throws, because the folder's tenant will not match the file's.

`ITenantContext` is optional — resolve it with `GetService` or an optional constructor parameter, since an application without JC.Tenancy has no implementation and belongs in the no-tenant scope.

### Size and type limits

A folder can cap file size and restrict extensions. Leave either `null` to inherit the registry default:

```csharp
var registry = app.Services.GetRequiredService<FolderRegistry>();
registry.DefaultMaxBytes = 5 * 1024 * 1024;                  // applies to folders with no MaxBytes
registry.DefaultAllowedExtensions = [".pdf", ".png"];        // applies to folders with no AllowedExtensions

app.Services.AddFolders(true,
    new FolderModel("invoices", null, 10 * 1024 * 1024, [".pdf"]),  // its own limits
    new FolderModel("scratch"));                                     // inherits both defaults
```

`StorageService` enforces these itself, so a rejected file never reaches disk or the database whichever entry point is used — including a background job calling `TrySaveFile` directly. `TrySaveFile` returns `false` and logs a warning; use `FolderRegistry.ValidateFile` first if you need to know why:

```csharp
var check = registry.ValidateFile(folder, ".pdf", content.Length);
if (!check.Result)
    return BadRequest(check.ErrorMessage);   // check.Error tells the reasons apart
```

**Nuance:** the ceiling is 10GB (`ValidationHelper.MaxAllowedBytes`) — a folder or default above it throws `ArgumentOutOfRangeException`. And `ValidationHelper.BlockedExtensions` (`.exe`, `.bat`, `.ps1`, `.dll`, `.js` and about fifty more) is checked **before** any allow-list, so it always wins. Trying to allow one throws:

```csharp
new FolderModel("danger", null, null, [".exe"]);   // ArgumentException
```

**This is a usability guard, not a security control.** Only the extension is checked, so `evil.exe` renamed to `evil.pdf` passes. Real verification means inspecting file content.

## Saving files

### Basic usage

```csharp
public class InvoiceService(StorageService storage, FolderRegistry folders, ITenantContext? tenant = null)
{
    public async Task<bool> StoreAsync(byte[] pdf)
    {
        folders.TryGetFolder("invoices", tenant?.TenantId, out var folder);
        return await storage.TrySaveFile(folder!, "invoice-001.pdf", pdf, "pdf");
    }
}
```

`TrySaveFile` returns `true` on success and `false` on failure — it does not throw for IO or database errors. Failures are logged through `ILogger<StorageService>`. It **does** throw `ArgumentException` if the folder's tenant does not match the caller's tenant, or if the file name is invalid.

### Saving text

There is a text overload that encodes as UTF-8 and delegates to the byte overload:

```csharp
await storage.TrySaveFile(folder, "report.csv", csvText, "csv");
```

### Overwriting

Overwriting is blocked by default. Pass `blockOverwrite: false` to replace an existing file:

```csharp
// Returns false if "invoice-001" already exists in this folder
await storage.TrySaveFile(folder, "invoice-001.pdf", pdf, "pdf");

// Replaces the existing file and its record
await storage.TrySaveFile(folder, "invoice-001.pdf", pdf, "pdf", blockOverwrite: false);
```

An overwrite reuses the existing `SavedFile` record — the `Id`, and therefore the physical file name, stays the same. If the extension changes, the file is written to the new path and the old one is deleted after the transaction commits.

**Nuance:** overwrite detection only considers active records. A file that has been deleted (and so soft-deleted) does not block a save of the same name — a new record with a new `Id` is created instead.

### File names and extensions

`SavedFile` stores the name and the extension in separate columns. The name is stored **without** its extension:

| Call | `FileName` | `Extension` |
|------|-----------|-------------|
| `TrySaveFile(folder, "invoice-001.pdf", …, "pdf")` | `invoice-001` | `.pdf` |
| `TrySaveFile(folder, "invoice-001", …, "pdf")` | `invoice-001` | `.pdf` |
| `TrySaveFile(folder, "my.report.v2.pdf", …, "pdf")` | `my.report.v2` | `.pdf` |
| `TrySaveFile(folder, "archive.tar.gz", …, "gz")` | `archive.tar` | `.gz` |

**An extension on the file name always wins.** The `ext` parameter is only a fallback for names that carry none, so `TrySaveFile(folder, "invoice.pdf", …, "txt")` stores `.pdf` and ignores `"txt"`. The file on disk is always built from the stored extension, so the record and the file cannot disagree.

Directory components are stripped, so `"sub/dir/invoice.pdf"` stores as `invoice`.

Two name shapes throw `ArgumentException`:

```csharp
await storage.TrySaveFile(folder, ".gitignore", content, null!); // nothing left once the extension is removed
await storage.TrySaveFile(folder, "invoice", content, null!);    // no extension on the name and none supplied
```

### What ends up on disk

Files are written to `{BasePath}/{tenant}/{folder}/{savedFileId}{extension}`. The caller's file name never appears on disk — the physical name is the record's GUID:

```text
C:\app-data\file-storage\
  NO__TENANT\
    invoices\
      3f2504e0-4f89-11d3-9a0c-0305e82c3301.pdf
  tenant-a\
    invoices\
      7c9e6679-7425-40de-944b-e07fc1f90ae7.pdf
```

This means the caller's file name is only ever a lookup key, so it is never used to build a path and cannot be used for traversal.

## Reading files

### Reading bytes

```csharp
var response = await storage.GetSavedFileBytes(folder, "invoice-001.pdf");
if (!response.Result)
    return NotFound(response.ErrorMessage);

return File(response.FileContent!, "application/pdf", "invoice-001.pdf");
```

### Reading text

```csharp
var response = await storage.GetSavedFileText(folder, "report.csv");
if (response.Result)
    Process(response.FileContentText!);
```

Both return a response object rather than throwing. `Result` indicates success; on success `File` holds the `SavedFile` record and the content property holds the data; on failure `ErrorMessage` explains why.

**Nuance:** `File`, `FileContent` and `FileContentText` are all nullable even when `Result` is `true`, so the compiler cannot narrow them for you — check `Result` first, then use `!`.

`ErrorMessage` is one of two values:

| Message | Cause |
|---------|-------|
| `File not found.` | No active record matched, **or** a record matched but no file exists at its path |
| `Error reading file.` | The record and file both exist but reading threw — the exception is logged |

### Looking a file up by name

The name you pass is normalised the same way it was stored, so either form works:

```csharp
await storage.GetSavedFileBytes(folder, "invoice-001.pdf"); // extension ignored for the lookup
await storage.GetSavedFileBytes(folder, "invoice-001");     // identical
```

The lookup is case-insensitive on both folder and file name.

## Deleting files

### Basic usage

```csharp
var deleted = await storage.TryDeleteFile(folder, "invoice-001.pdf");
```

Returns `false` if no active record matched, and `true` once both sides are done.

### What deletion means

Deletion is asymmetric by design:

- **The file is permanently deleted** from disk. There is no archive and no recycle bin.
- **The record is only soft-deleted**, so an audit can still show the file was there, when it went, and who removed it. `DeletedById` and `DeletedUtc` are populated from the current user.

**There is no restore path in this package.** A consuming application *can* restore the record through `IRepositoryContext<SavedFile>.RestoreAsync`, but the file is gone — all it recovers is metadata. Reading a restored record returns `File not found.`, which is expected rather than a fault.

Soft-deleted records are eventually removed for good by JC.Core's `SoftDeleteCleanupJob`.

**Nuance:** the file delete happens inside the database transaction. If the file cannot be deleted — most commonly because a reader is holding it open — the record is rolled back and `TryDeleteFile` returns `false`, leaving both sides unchanged so the call can be retried.

## Multi-tenancy

### How a file gets its tenant

`StorageService`'s scoped methods take the tenant from `ITenantContext.TenantId` — the tenant the current operation is scoped to, not the tenant assigned to the signed-in user. There is no tenant parameter on them, so they cannot reach another tenant's files:

```csharp
// Saves into the current user's tenant. Nothing here can cross a tenant boundary.
await storage.TrySaveFile(folder, "invoice-001.pdf", pdf, "pdf");
```

The folder must belong to the same tenant as the caller, or `ArgumentException` is thrown before anything is read or written:

```csharp
// A tenant-a user
await storage.TrySaveFile(new FolderModel("invoices", "tenant-a"), …); // fine
await storage.TrySaveFile(new FolderModel("invoices", "tenant-b"), …); // ArgumentException
await storage.TrySaveFile(new FolderModel("invoices"), …);             // ArgumentException — no-tenant folder
```

### The no-tenant scope

A `null` tenant is **not** a shared or global scope. It is a scope of its own, isolated exactly like any named tenant — a tenant-a user cannot see no-tenant files, and vice versa. JC.Tenancy's query filter treats it that way: when the current tenant is null it matches `TenantId == null`, otherwise it matches the tenant exactly.

Applications without JC.Tenancy have no `ITenantContext`, so every file lands in the no-tenant scope and stays there. That is the normal single-tenant configuration.

In a multi-tenant application, the no-tenant scope is effectively reachable only through a cross-tenant call.

**Because the scope is operational rather than per-user, a background job scoped to a tenant writes and reads that tenant's files with no user involved.** Establish it the usual way:

```csharp
await using var scope = await services.CreateAsyncScopeForTenant(tenantId);

var storage = scope.ServiceProvider.GetRequiredService<StorageService>();
await storage.TrySaveFile(folder, "invoice-001.pdf", pdf, "pdf");   // lands in tenantId
```

### Isolation depends on JC.Tenancy

The global query filter that enforces isolation is installed by `ApplyTenantFilters`, which the consuming application calls from its own `OnModelCreating`. If your `DbContext` does not call it, `SavedFile` is not filtered and tenancy is not enforced at the database level — `IMultiTenancy` alone enforces nothing. See [Setup](Setup.md#multi-tenancy).

## Cross-tenant access

### The ForTenant methods

Every operation has a `*ForTenant` counterpart that takes the tenant as its first argument:

```csharp
await storage.GetSavedFileBytesForTenant("tenant-b", folder, "invoice-001.pdf");
await storage.GetSavedFileTextForTenant("tenant-b", folder, "report.csv");
await storage.TrySaveFileForTenant("tenant-b", folder, "invoice-001.pdf", pdf, "pdf");
await storage.TryDeleteFileForTenant("tenant-b", folder, "invoice-001.pdf");
```

When the tenant passed differs from the caller's own, these **bypass the global tenant query filter** via `IgnoreQueryFilters()` and scope the query to the tenant given instead.

> **JC.FileStorage performs no authorisation check on these methods.** It cannot — it references only JC.Core, so it can see neither an identity package's roles nor JC.Tenancy's bypass authoriser. Any caller that can reach a `*ForTenant` method can reach any tenant's files. **The consuming application is responsible for authorising every call.**

A correct call site gates first. Where JC.Tenancy is registered, its authoriser is the natural gate — it already knows which roles the application nominated:

```csharp
public class AdminFileService(StorageService storage, ITenantBypassAuthoriser authoriser)
{
    public async Task<GetFileByteResponse> GetForTenantAsync(string tenantId, FolderModel folder, string fileName)
    {
        // JC.FileStorage will not do this for you
        if (!authoriser.CanAccessAllTenants())
            throw new UnauthorizedAccessException();

        return await storage.GetSavedFileBytesForTenant(tenantId, folder, fileName);
    }
}
```

Checking a role directly — `userInfo.IsInRole(SystemRoles.SystemAdmin)` — works equally well where the application knows its own authority. The authoriser is preferable only because the role names are then configured in one place.

**Never bind the tenant argument straight from user input.** A `tenantId` taken from a route or query string and passed through unchecked hands every tenant's files to any caller.

The scoped methods are the safe default and delegate to these, passing `ITenantContext.TenantId`. Because the tenant then matches the operational scope, the filter bypass never engages.

### Reaching the no-tenant scope

`null` addresses the no-tenant scope explicitly, and needs a no-tenant folder to match:

```csharp
// As a tenant-a system administrator, read a no-tenant file
await storage.GetSavedFileBytesForTenant(null, new FolderModel("invoices"), "invoice-001.pdf");
```

The folder and the tenant must always agree, in both directions:

```csharp
await storage.GetSavedFileBytesForTenant(null, new FolderModel("invoices", "tenant-b"), …); // ArgumentException
await storage.GetSavedFileBytesForTenant("tenant-b", new FolderModel("invoices"), …);       // ArgumentException
```

## Static files

Static files are files put beneath `FileStorage:StaticPath` at deploy time — a privacy policy, a pricing table, a configuration document. They are deliberately not manageable: there is no `SavedFile` row, no audit trail, no tenancy, and no upload, save, overwrite or delete. The only operation is reading one.

They are opt-in. See [Setup](Setup.md#addstaticfiles--static-file-registration) for enabling them and for how files get registered.

### Basic usage

`StaticFileCache` is the type to inject — it holds content in memory, so a file read on every page render reaches the disk once per cache window:

```csharp
public class PrivacyModel(StaticFileCache staticFiles) : PageModel
{
    public string? Policy { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var response = await staticFiles.GetStaticFileText("privacy-policy.md", ct);
        if (response.Result)
            Policy = response.FileContentText;
    }
}
```

Like the managed file reads, this returns a response object rather than throwing. `Result` says whether the file was read; on success `File` holds the `StaticFile` and `FileContentText` the content; on failure `ErrorMessage` explains why.

### Files in subfolders

Subfolders are given after the cancellation token, relative to the static path:

```csharp
// {StaticPath}/legal/terms.md
var terms = await staticFiles.GetStaticFileText("terms.md", ct, "legal");

// {StaticPath}/config/v2/pricing.json
var pricing = await staticFiles.GetStaticFileText("pricing.json", ct, "config", "v2");
```

**Nuance:** the token has a default but can never be omitted when subfolders are given, because it precedes a `params` parameter. `GetStaticFileText("terms.md", "legal")` does not compile — pass `default` if you have no token to hand.

### Reading bytes

```csharp
var response = await staticFiles.GetStaticFileBytes("logo.png", ct);
if (!response.Result)
    return NotFound(response.ErrorMessage);

return File(response.FileContent!, "image/png");
```

**Nuance:** a cached response is handed to every caller as the same instance, and `FileContent` is a `byte[]`. Writing into that array changes what every later caller sees until the entry expires. Copy it before mutating.

### When a file last changed

`StaticFile.LastModifiedUtc` carries the file's last write time from disk, so a page can show when a document was last updated alongside the document itself:

```csharp
var response = await staticFiles.GetStaticFileText("privacy-policy.md", ct);
if (response.Result)
{
    Policy = response.FileContentText;
    Updated = response.File?.LastModified("d MMMM yyyy");   // "14 August 2026"
}
```

`LastModified(format)` formats the timestamp in local time, returning null when there is none — so a view can bind it and render nothing rather than guard first.

The timestamp is captured when the file is registered, and again immediately after every read that reaches the disk, so the date describes the content being handed back. Through the cache that holds: a cache hit returns the date captured by the read that filled the entry, not a newer one.

**Local means the server's time zone**, and the format is applied under whatever `CultureInfo.CurrentCulture` is at the time. Read `LastModifiedUtc` directly and convert it yourself where the viewer's zone or a fixed culture matters:

```csharp
var utc = response.File?.LastModifiedUtc;
Updated = utc is null
    ? null
    : TimeZoneInfo.ConvertTimeFromUtc(utc.Value, userTimeZone)
        .ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
```

Where only the date needs to be current — the content is being served from the cache and the file is edited in place — refresh it without reading the content:

```csharp
public class PolicyDate(StaticFileRegistry registry, FilePathProvider paths)
{
    public string? LastUpdated()
    {
        if (!registry.TryGetStaticFile("privacy-policy.md", out var file) || file is null)
            return null;

        file.RefreshLastModified(paths);
        return file.LastModified("d MMMM yyyy");
    }
}
```

`LastModifiedUtc` has an internal setter and `RefreshLastModified` always reads this file's own path, so the value can only ever be a real timestamp taken from that file — there is no way to set an arbitrary date on one.

### Bypassing the cache

Inject `StaticFileService` where a read should always reach the disk — reloading a configuration document that an operator edits in place, for instance:

```csharp
public class PricingReloader(StaticFileService staticFiles)
{
    public Task<GetStaticFileTextResponse> ReadAsync(CancellationToken ct)
        => staticFiles.GetStaticFileText("pricing.json", ct, "config", "v2");
}
```

The two types expose the same method names, arguments and responses, so swapping one for the other changes only what you inject. Setting `staticFileCacheDurationMinutes: 0` achieves the same thing globally — `StaticFileCache` then passes every call straight through.

### Registering a file at runtime

Discovery runs once, when `StaticFileRegistry` is first resolved. A file that appears in the directory afterwards is not registered, and reading it returns not-found. Register it through the registry when that matters:

```csharp
public class StaticFileAdmin(StaticFileRegistry registry)
{
    public bool Register(string fileName)
        => registry.TryAddStaticFile(fileName);

    public bool IsRegistered(string fileName)
        => registry.TryGetStaticFile(fileName, out _);
}
```

`TryAddStaticFile` returns `false` if a file is already registered under the same key, and `TryGetStaticFile` gives the registered `StaticFile` — including the casing it was discovered with, which is what the read then uses to build the path.

**Nuance:** the two are not symmetric on a bad name. `TryGetStaticFile("privacy-policy", out _)` is a miss and returns `false`, but `TryAddStaticFile("privacy-policy")` throws `ArgumentException` — the `StaticFile` is built in the argument expression, outside the guard that makes the lookup safe. Registering a name that came from outside the application means validating it first, or catching.

### Nuances and gotchas

**Only registered files can be read.** The registry is the gate: `StaticFileService` resolves the name through it before touching the disk, so a name that was never registered returns `"Static file not found"` without a file system call. That is also what stops a crafted name reaching outside the static path.

**A name without an extension is a miss, not an error.** `GetStaticFileText("privacy-policy")` returns not-found — `StaticFile` cannot be built without an extension, and the registry treats that as a failed lookup, logging at debug level. Nothing is thrown.

**Registration does not mean the file is still there.** The registry records what discovery found. If the file is later removed from disk, the lookup still succeeds and the read then returns `"Static file not found"`.

**Names are matched case-insensitively, but the path is not.** The registry key is lower-cased, so `Privacy-Policy.md` finds the file discovered as `privacy-policy.md`. The path used to read it comes from the registered `StaticFile`, which keeps the casing it was discovered with — so the read works on a case-sensitive file system too.

**Failed reads are not cached.** A file locked during a deployment copy returns `"Error reading file."`, and that response is not held, so the next call retries rather than being stuck with the failure for the rest of the cache window.

**Dot files work.** `.gitignore` and similar are registered with an empty name and the whole thing as the extension, and read back under their full name.

**The `StaticFile` you get back is shared.** The registry holds one instance per file and hands that same object to every caller, including inside cached responses. `LastModifiedUtc` is the only part of it that changes, so a read or a `RefreshLastModified` updates the date for every holder — usually what you want, but it does mean the value can move under a caller holding a reference.

**No extension check applies.** The blocked list guards uploads, which are untrusted. A static file was put there by whoever deployed the application, so nothing about it is filtered — including `.exe` and `.ps1`, which discovery will register and the service will read. What an application does with those bytes is its own decision.

## Web applications

Everything above works in a web application as-is. The optional **JC.FileStorage.Web** package adds only what needs ASP.NET: `IFormFile` handling, MIME inference, and a tag helper. See [Setup](Setup.md#jcfilestorageweb--aspnet-core-integration) for registration.

### Uploading an IFormFile

```csharp
public class InvoiceUploadModel(WebStorageService storage, FolderRegistry folders) : PageModel
{
    [BindProperty] public IFormFile? Upload { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        folders.TryGetFolder("invoices", User.GetTenantId(), out var folder);

        var result = await storage.TryUploadFile(folder!, Upload);
        if (!result.Result)
        {
            ModelState.AddModelError(nameof(Upload), result.ErrorMessage!);
            return Page();
        }

        return RedirectToPage("Index");
    }
}
```

`TryUploadFile` reads the name, extension and bytes from the upload, then delegates to `StorageService`. `ErrorMessage` is safe to show a user, and `ValidationError` lets you branch on the reason (`TooLarge`, `ExtensionNotAllowed`, `BlockedExtension`) without parsing text.

**Nuance:** validation runs against `IFormFile.Length` *before* the stream is read, so an oversized upload is rejected without being buffered into memory. It is a fail-fast convenience, not the gate — `StorageService` enforces the same rules regardless, so injecting it directly instead is safe, just less efficient.

### Downloading a file

```csharp
var file = await storage.GetFileForDownload(folder, "invoice-001.pdf");
if (!file.Result)
    return NotFound(file.ErrorMessage);

return File(file.Content!, file.ContentType!, file.DownloadName!);
```

`ContentType` comes from the stored extension (`.pdf` gives `application/pdf`, falling back to `application/octet-stream`). `DownloadName` rejoins `FileName` and `Extension` — the name on disk is a GUID, so it is never suitable to serve.

### Showing a folder's limits

```html
<input type="file" name="Upload" class="form-control" />
<upload-constraints folder="invoices" />
```

```html
<div class="form-text">Accepted types: .pdf, .csv &middot; Maximum size: 1 MB</div>
```

The text is read from the registry, so it always matches what the server enforces. Where a folder has no type restriction it reads "Any type except executable files", and where it has no size limit that half is omitted.

| Attribute | Default | Description |
|-----------|---------|-------------|
| `folder` | — | Required. The folder name. |
| `tenant-id` | current user's tenant | The tenant owning the folder. |
| `show-types` / `show-size` | `true` | Show each half. Both off suppresses the element. |
| `types-label` / `size-label` | "Accepted types" / "Maximum size" | Leading labels. |
| `any-type-text` | "Any type except executable files" | Shown when no type restriction applies. |
| `css-class` | `null` | Classes on the wrapper. Falls back to the configured framework's dictionary, which is `form-text` under Bootstrap. |

**Nuance:** the tag helper throws if the folder is not registered *for that tenant*. Folders are per-tenant, so a page shared across tenants needs the folder registered for every one of them.

### Working with IFormFile directly

`FormFileHelper` is static, and useful outside `WebStorageService`:

```csharp
FormFileHelper.GetFileName(file);        // "report.pdf" — strips any client path
FormFileHelper.GetExtension(file);       // ".pdf" — lower-cased, leading dot
await FormFileHelper.GetBytesAsync(file);
FormFileHelper.GetContentType(".pdf");   // "application/pdf"
FormFileHelper.FormatFileSize(1572864);  // "1.5 MB"
```

**Nuance:** always use `GetFileName` rather than `IFormFile.FileName` — browsers have historically sent full client paths, and the raw value is not safe to use as a name.

## Multiple DbContexts

`StorageService` resolves its repositories through `IRepositoryManager`, which binds to the ambient context registered by `AddCore<TContext>`. To target a different context, call `ChangeContext`:

```csharp
public class ArchiveFileService(StorageService storage)
{
    public async Task<bool> SaveToArchiveAsync(FolderModel folder, byte[] content)
    {
        storage.ChangeContext(typeof(ArchiveDbContext));
        return await storage.TrySaveFile(folder, "snapshot.bin", content, "bin");
    }
}
```

**Nuance:** `ChangeContext` rebinds the service instance for the rest of its lifetime, not just the next call. `StorageService` is scoped, so a call made early in a request changes which database every later call in that request writes to. Each context also carries its own transaction — a transaction started on one does not span another. If a request needs two contexts, resolve two scopes rather than switching back and forth.

The target context must implement `IFileStorageDbContext` and apply the file storage mappings, exactly like the default one.