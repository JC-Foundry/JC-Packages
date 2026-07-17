# JC.FileStorage — Guide

Covers folder registration, saving, reading and deleting files, how file names and extensions are stored, tenant scoping, cross-tenant access, and using multiple DbContexts. See [Setup](Setup.md) for registration.

JC.FileStorage is deliberately small. It is **not** a document management package — there is no archiving, no versioning, and no nested folder structure. Folders provide a single level of separation, and files are addressed by folder and name within a tenant.

## Folders

### Registering folders

Folders live in a singleton `FolderRegistry` populated at startup. A folder must be registered before any file can be saved into or read from it:

```csharp
var app = builder.Build();

// No-tenant folders
app.AddFolders(true, "invoices", "reports");

// Tenant-scoped folders
app.AddFolders(true,
    new FolderModel("invoices", "tenant-a"),
    new FolderModel("invoices", "tenant-b"));
```

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
public class FolderService(FolderRegistry folders, IUserInfo userInfo)
{
    public IReadOnlyList<string> AvailableFolders()
        => folders.GetFolderNames(userInfo.TenantId);

    public bool FolderExists(string name)
        => folders.TryGetFolder(name, userInfo.TenantId, out _);
}
```

`GetFolderNames` returns an empty list rather than throwing when a tenant has no folders.

### Size and type limits

A folder can cap file size and restrict extensions. Leave either `null` to inherit the registry default:

```csharp
var registry = app.Services.GetRequiredService<FolderRegistry>();
registry.DefaultMaxBytes = 5 * 1024 * 1024;                  // applies to folders with no MaxBytes
registry.DefaultAllowedExtensions = [".pdf", ".png"];        // applies to folders with no AllowedExtensions

app.AddFolders(true,
    new FolderModel("invoices", null, 10 * 1024 * 1024, [".pdf"]),  // its own limits
    new FolderModel("scratch"));                                     // inherits both defaults
```

`StorageService` enforces these itself, so a rejected file never reaches disk or the database whichever entry point is used — including a background job calling `TrySaveFile` directly. `TrySaveFile` returns `false` and logs a warning; use `FolderRegistry.ValidateFile` first if you need to know why:

```csharp
var check = registry.ValidateFile(folder, ".pdf", content.Length);
if (!check.Result)
    return BadRequest(check.ErrorMessage);   // check.Error tells the reasons apart
```

**Nuance:** the ceiling is 10GB (`FolderModel.MaxAllowedBytes`) — a folder or default above it throws `ArgumentOutOfRangeException`. And `FolderModel.BlockedExtensions` (`.exe`, `.bat`, `.ps1`, `.dll`, `.js` and about fifty more) is checked **before** any allow-list, so it always wins. Trying to allow one throws:

```csharp
new FolderModel("danger", null, null, [".exe"]);   // ArgumentException
```

**This is a usability guard, not a security control.** Only the extension is checked, so `evil.exe` renamed to `evil.pdf` passes. Real verification means inspecting file content.

## Saving files

### Basic usage

```csharp
public class InvoiceService(StorageService storage, FolderRegistry folders, IUserInfo userInfo)
{
    public async Task<bool> StoreAsync(byte[] pdf)
    {
        folders.TryGetFolder("invoices", userInfo.TenantId, out var folder);
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

`StorageService`'s scoped methods take the tenant from `IUserInfo.TenantId`. There is no tenant parameter on them, so they cannot reach another tenant's files:

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

A `null` tenant is **not** a shared or global scope. It is a scope of its own, isolated exactly like any named tenant — a tenant-a user cannot see no-tenant files, and vice versa. JC.Core's query filter treats it that way: when the current tenant is null it matches `TenantId == null`, otherwise it matches the tenant exactly.

Applications without JC.Identity have no `IUserInfo`, so every file lands in the no-tenant scope and stays there. That is the normal single-tenant configuration.

In a multi-tenant application, the no-tenant scope is effectively reachable only by a system administrator through a cross-tenant call.

### Isolation depends on JC.Identity

The global query filter that enforces isolation is applied by `IdentityDataDbContext`. If your `DbContext` does not extend it, `SavedFile` is not filtered, and tenancy is not enforced at the database level. See [Setup](Setup.md#multi-tenancy).

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

> **JC.FileStorage performs no authorisation check on these methods.** It cannot — the `SystemAdmin` role lives in JC.Identity, which this package does not reference. Any caller that can reach a `*ForTenant` method can reach any tenant's files. **The consuming application is responsible for authorising every call**, typically by checking `IUserInfo.IsInRole(SystemRoles.SystemAdmin)` before invoking one.

A correct call site gates first:

```csharp
public class AdminFileService(StorageService storage, IUserInfo userInfo)
{
    public async Task<GetFileByteResponse> GetForTenantAsync(string tenantId, FolderModel folder, string fileName)
    {
        // JC.FileStorage will not do this for you
        if (!userInfo.IsInRole(SystemRoles.SystemAdmin))
            throw new UnauthorizedAccessException();

        return await storage.GetSavedFileBytesForTenant(tenantId, folder, fileName);
    }
}
```

**Never bind the tenant argument straight from user input.** A `tenantId` taken from a route or query string and passed through unchecked hands every tenant's files to any caller.

The scoped methods are the safe default and delegate to these, passing `IUserInfo.TenantId`. Because the tenant then matches the caller's own, the filter bypass never engages.

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
| `css-class` | `form-text` | Classes on the wrapper. |

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