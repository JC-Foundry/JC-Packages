# JC.FileStorage — API reference

Complete reference of all public types, properties, and methods in JC.FileStorage. See [Setup](Setup.md) for registration and [Guide](Guide.md) for usage examples.

> **Note:** Registration extensions (`IServiceCollection`, `IServiceProvider`, `IApplicationBuilder`) and the `ModelBuilder` mapping extension are documented in [Setup](Setup.md), not here.

---

# Models

## SavedFile

**Namespace:** `JC.FileStorage.Models`

Entity representing a stored file. Extends `AuditModel` (JC.Core) for full audit trail and soft-delete support, and implements `IMultiTenancy` so it is scoped by the tenant query filter, where the consuming application has installed one through JC.Tenancy's `ApplyTenantFilters`. See the JC.Core API reference for inherited members.

The file name and its extension are held in separate columns, and the physical file on disk is named after `Id`, not `FileName`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | New GUID | get; private set; | Primary key. Also the physical file name on disk, combined with `Extension`. Max length 36. |
| `TenantId` | `string?` | `null` | get; set; | The tenant this file belongs to. `null` places the file in the no-tenant scope. Max length 36. |
| `FileName` | `string` | `""` | get; private set; | The file name **without** its extension. Set via `SetFileName`. Required, max length 256. |
| `Extension` | `string` | `""` | get; private set; | The extension including its leading dot (e.g. `.pdf`). Set via `SetFileName`. Required, max length 64. |
| `FolderName` | `string` | `""` | get; private set; | The name of the folder holding this file. Set via `SetFolderName`. Required, max length 256. |

### Methods

#### SetFileName(string fileName, string? ext = null)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fileName` | `string` | — | The file name. May include an extension and directory components; both are stripped from the stored value. |
| `ext` | `string?` | `null` | Fallback extension, used only when `fileName` carries none. The leading dot is optional. |

Splits `fileName` into its name and extension and assigns `FileName` and `Extension`. An extension present on `fileName` always takes precedence — `ext` is consulted only when there is none, so `SetFileName("report.pdf", "txt")` stores `.pdf`. The stored extension always begins with a dot, whether or not `ext` supplied one.

Throws `ArgumentException` when: `fileName` is null or whitespace; `fileName` carries no extension and `ext` is null or whitespace; nothing remains of `fileName` once the extension is removed (as with `".gitignore"`); the resulting name exceeds 256 characters; or the resulting extension exceeds 64 characters.

#### SetFolderName(FolderModel folder)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folder` | `FolderModel` | — | The folder this file belongs to. Its tenant must match the file's. |

Assigns `FolderName` from `folder.Name` after checking that the folder's tenant matches `TenantId`. When `TenantId` is null or empty, `folder.Tenant` must be `FolderModel.NullTenantName`; otherwise `folder.Tenant` must equal `TenantId`.

Throws `ArgumentException` on a mismatch in either direction. `TenantId` must therefore be assigned before this is called.

### Obsolete members

| Member | Kind | Replaced by |
|--------|------|-------------|
| `NormaliseFileName(string fileName)` | `static string` | `NormalisationHelper.NormaliseFileName` |

Marked `[Obsolete]` as a warning, not an error, and behaves exactly as the member it forwards to.

## FolderModel

**Namespace:** `JC.FileStorage.Models`

Immutable descriptor of a folder within a tenant, and of the size and type limits that apply to it. Folders are a single level of separation — there is no nesting.

`Tenant` and `TenantId` differ: `Tenant` is the path segment and sentinel-normalised, never null; `TenantId` is the raw tenant identifier as supplied, and is null for a no-tenant folder.

### Fields

| Field | Type | Value | Description |
|-------|------|-------|-------------|
| `NullTenantName` | `const string` | `NO__TENANT` | Sentinel used as `Tenant` for folders in the no-tenant scope, and as the directory name on disk. |

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Name` | `string` | — | get; | The folder name. |
| `Tenant` | `string` | `NullTenantName` | get; | The tenant path segment. Either a tenant identifier or `NullTenantName`. Never null. |
| `TenantId` | `string?` | `null` | get; | The raw tenant identifier as passed to the constructor. Null for a no-tenant folder. |
| `MaxBytes` | `long?` | `null` | get; | Maximum size of a file in this folder. Null falls back to `FolderRegistry.DefaultMaxBytes`. |
| `AllowedExtensions` | `IReadOnlyList<string>?` | `null` | get; | Extensions this folder accepts, normalised to lower case with a leading dot. Null falls back to `FolderRegistry.DefaultAllowedExtensions`. Never overrides `ValidationHelper.BlockedExtensions`. |

### Constructors

#### FolderModel(string name)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The folder name. |

Creates a folder in the no-tenant scope with no limits of its own. `Tenant` is set to `NullTenantName` and `TenantId` to `null`.

Throws `ArgumentException` if `name` exceeds 256 characters, or contains any of `/`, `\`, `.` or `?`.

#### FolderModel(string name, string? tenantId)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The folder name. Validated as above. |
| `tenantId` | `string?` | — | The owning tenant. Null or whitespace produces a no-tenant folder. |

Creates a folder for a specific tenant, with no limits of its own. When `tenantId` is null or whitespace, `Tenant` falls back to `NullTenantName`.

Throws `ArgumentException` if the resolved tenant exceeds 36 characters, in addition to the name validation above.

#### FolderModel(string name, string? tenantId, long? maxBytes, IEnumerable\<string\>? allowedExtensions)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The folder name. Validated as above. |
| `tenantId` | `string?` | — | The owning tenant. Null or whitespace produces a no-tenant folder. |
| `maxBytes` | `long?` | — | Maximum file size in bytes, or null to inherit the registry default. |
| `allowedExtensions` | `IEnumerable<string>?` | — | Accepted extensions, or null to inherit the registry default. Leading dots and casing are normalised. |

Creates a folder with its own limits. Applies the name and tenant validation above, then:

Throws `ArgumentOutOfRangeException` if `maxBytes` is zero or negative, or exceeds `ValidationHelper.MaxAllowedBytes`. Throws `ArgumentException` if `allowedExtensions` is supplied but empty once blanks are removed, or names any extension in `ValidationHelper.BlockedExtensions`.

Limits take a four-argument constructor rather than optional parameters, because `new FolderModel("x", null)` would otherwise be ambiguous between `tenantId` and `maxBytes`. Pass `null` for the tenant on a no-tenant folder.

### Obsolete members

Validation and normalisation live in `ValidationHelper` and `NormalisationHelper`, which the static file types share. These members forward to them and are marked `[Obsolete]` as warnings, not errors.

| Member | Kind | Replaced by |
|--------|------|-------------|
| `MaxAllowedBytes` | `const long` | `ValidationHelper.MaxAllowedBytes` |
| `BlockedExtensions` | `static IReadOnlyCollection<string>` | `ValidationHelper.BlockedExtensions` |
| `IsBlockedExtension(string extension)` | `static bool` | `ValidationHelper.IsBlockedExtension` |
| `NormaliseExtension(string extension, bool lowerCase = true)` | `static string` | `NormalisationHelper.NormaliseExtension` |

Each behaves exactly as the member it forwards to. `MaxAllowedBytes` is a `const`, so a consumer that reads it has the value baked in at their own compile time.

## StaticFile

**Namespace:** `JC.FileStorage.Models`

Descriptor of a static file — one placed beneath `FileStorage:StaticPath` at deploy time and only ever read. It carries no tenant, no identifier and no audit information, because a static file has no database record.

Its identity is immutable: `Name`, `Extension` and `SubFolders` are fixed at construction. Only `LastModifiedUtc` changes, and only ever to a timestamp read from disk.

All three constructors take the file name whole. The two-part `(name, extension)` forms are private, so `new StaticFile("terms.md", "legal")` unambiguously means a file in the `legal` subfolder rather than a file named `terms.md.legal` — a `string` second argument only matches the `subFolders` constructor. Compose a name from separate parts with `NormalisationHelper.GetFileName`.

`new StaticFile("terms.md")` binds to the `lastModifiedUtc` constructor with no timestamp, which describes the same file as the `subFolders` form with no subfolders. No constructor takes both subfolders and a timestamp, and none needs to — registering a file sets the timestamp from disk.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Name` | `string` | — | get; | The file name without its extension, in the casing it was created with. Empty for a dot file such as `.gitignore`. |
| `Extension` | `string` | — | get; | The extension including its leading dot, in the casing it was created with. |
| `FileName` | `string` | — | get; | `Name` and `Extension` rejoined. The name used to build the physical path. |
| `SubFolders` | `IReadOnlyList<string>` | empty | get; | The subfolders beneath the static path, outermost first. Empty for a file at the root. |
| `Key` | `string` | — | get; | `SubFolders` and `FileName` combined into a relative path and lower-cased. The registry's dictionary key, which is why static file lookups are case-insensitive. Recomputed on each access. |
| `LastModifiedUtc` | `DateTime?` | `null` | get; internal set; | The file's last write time on disk, in UTC. Null when the file was not there the last time it was looked for. |

Casing is preserved on `Name`, `Extension` and `SubFolders` because they build a path that must match what is actually on disk; only `Key` is lower-cased, because it is only ever compared.

`LastModifiedUtc` is set from disk when the file is registered, and again on every read that reaches the disk. Its setter is internal by design: the value can only ever be a real timestamp for this file, never one a caller chose. `RefreshLastModified` is the supported way to re-read it from outside the package.

### Constructors

#### StaticFile(string fileName, DateTime? lastModifiedUtc = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fileName` | `string` | — | The file name including its extension. Any directory component is stripped. |
| `lastModifiedUtc` | `DateTime?` | `null` | Seeds `LastModifiedUtc`. |

Creates a file at the root of the static path. Splits `fileName` through `Path.GetFileNameWithoutExtension` and `Path.GetExtension`, so a name containing directory separators or `..` cannot address anything outside the static path.

`lastModifiedUtc` is overwritten from disk the moment the file is registered, so it only survives on a `StaticFile` that never reaches the registry. Give it a `DateTime` whose `Kind` is `Utc`. `LastModified` goes through `ToLocalTime`, which reads an `Unspecified` value as UTC but leaves a `Local` one alone — so a local-kind timestamp is formatted with no conversion at all.

Throws `ArgumentException` if `fileName` is null, or carries no extension.

#### StaticFile(string fileName, params IEnumerable\<string\> subFolders)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fileName` | `string` | — | The file name including its extension. Any directory component is stripped. |
| `subFolders` | `params IEnumerable<string>` | empty | The subfolders beneath the static path, outermost first. |

Creates a file within one or more subfolders. `subFolders` is copied on construction, so a later change to the collection passed in does not affect the file.

The subfolders are not validated here — `FilePathProvider.GetStaticPath` drops any that are unusable when the path is built. Throws the same `ArgumentException` as the constructor above.

### Methods

#### LastModified(string format)

**Returns:** `string?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `format` | `string` | — | A standard or custom `DateTime` format string. |

`LastModifiedUtc` converted to local time and formatted, or null when `LastModifiedUtc` is null — so a view can bind it directly and render nothing for a file that was never found on disk.

**"Local" is the server's time zone, not the viewer's,** and `format` is applied under the ambient `CultureInfo.CurrentCulture`. Read `LastModifiedUtc` and convert it yourself where either matters. Throws `FormatException` if `format` is not a valid format string.

#### RefreshLastModified(FilePathProvider pathProvider)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `pathProvider` | `FilePathProvider` | — | Resolves the file's path beneath the static root. Registered as a singleton by `AddFileStorage`. |

Re-reads this file's last write time from disk into `LastModifiedUtc`, setting it to null if the file is no longer there. The path is rebuilt from the file's own `SubFolders` and `FileName`, so the timestamp can only ever come from the file the instance describes.

This is the only way to change `LastModifiedUtc` from outside the package. A read through `StaticFileService`, or an uncached read through `StaticFileCache`, already does it — so call it only when the content is being served from the cache and the date is wanted fresh.

**The instance the registry hands out is shared**, so a refresh updates the timestamp for every holder of that file, not only the caller.

## ResponseBase

**Namespace:** `JC.FileStorage.Models`

Abstract record and the root of every response in the package — managed and static, byte and text. Its constructors are internal, so it cannot be extended outside the package; it exists so a caller can treat any response uniformly.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Result` | `bool` | `false` | get; | Whether the operation succeeded. |
| `ErrorMessage` | `string?` | `null` | get; | Why it failed, when `Result` is `false`. Null on success. |

Both are get-only. The derived records add their payload as `init` properties, all of which stay nullable even on success — check `Result` first, then dereference.

## FileValidationResponse

**Namespace:** `JC.FileStorage.Models`

Sealed record carrying the outcome of `FolderRegistry.ValidateFile`. Construct via the `Valid` and `Invalid` factory methods.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Result` | `bool` | `false` | get; init; | Whether the file may be stored. |
| `ErrorMessage` | `string?` | `null` | get; init; | Why the file was rejected, when `Result` is `false`. Null on success. |
| `Error` | `FileValidationError` | `None` | get; init; | What the file failed on, when `Result` is `false`. |

### Methods

#### Valid()

**Returns:** `FileValidationResponse`

Static. A passing result — `Result` is `true`, `Error` is `None`, `ErrorMessage` is null.

#### Invalid(FileValidationError error, string errorMessage)

**Returns:** `FileValidationResponse`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `error` | `FileValidationError` | — | What the file failed on. |
| `errorMessage` | `string` | — | Why it was rejected. |

Static. A failing result carrying the reason and its category.

## GetFileResponseBase

**Namespace:** `JC.FileStorage.Models`

Abstract record and base of the managed file retrieval responses. Extends `ResponseBase`. Not returned directly — see `GetFileByteResponse` and `GetFileTextResponse`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `File` | `SavedFile?` | `null` | get; init; | The record, when `Result` is `true`. Null on failure. |

Inherits `Result` and `ErrorMessage` from `ResponseBase`.

### Constructors

#### GetFileResponseBase(SavedFile file)

Sets `Result` to `true` and `File` to the record supplied.

#### GetFileResponseBase(string errorMessage)

Sets `Result` to `false` and `ErrorMessage` to the message supplied. `File` remains null.

## GetFileByteResponse

**Namespace:** `JC.FileStorage.Models`

Record returned by `StorageService.GetSavedFileBytes` and `GetSavedFileBytesForTenant`. Extends `GetFileResponseBase`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `FileContent` | `byte[]?` | `null` | get; init; | The file's bytes, when `Result` is `true`. Null on failure. |

### Constructors

#### GetFileByteResponse(SavedFile file, byte[] fileContent)

Success. Sets `Result` to `true`, `File`, and `FileContent`.

#### GetFileByteResponse(string errorMessage)

Failure. Sets `Result` to `false` and `ErrorMessage`. `FileContent` remains null.

## GetFileTextResponse

**Namespace:** `JC.FileStorage.Models`

Record returned by `StorageService.GetSavedFileText` and `GetSavedFileTextForTenant`. Extends `GetFileResponseBase`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `FileContentText` | `string?` | `null` | get; init; | The file's contents as text, when `Result` is `true`. Null on failure. |

### Constructors

#### GetFileTextResponse(SavedFile file, string fileContentText)

Success. Sets `Result` to `true`, `File`, and `FileContentText`.

#### GetFileTextResponse(string errorMessage)

Failure. Sets `Result` to `false` and `ErrorMessage`. `FileContentText` remains null.

## GetStaticFileResponseBase

**Namespace:** `JC.FileStorage.Models`

Abstract record and base of the static file retrieval responses. Extends `ResponseBase`. Not returned directly — see `GetStaticFileByteResponse` and `GetStaticFileTextResponse`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `File` | `StaticFile?` | `null` | get; init; | The registered file, when `Result` is `true`. Null on failure. |

Inherits `Result` and `ErrorMessage` from `ResponseBase`.

`File` is the registry's own instance rather than a copy — the same object carried by every response for that file. Its `LastModifiedUtc` is rewritten each time the file is read from disk, so the value on a response held in `StaticFileCache` is the one captured by the read that filled the entry, and moves when that entry expires and the file is read again.

## GetStaticFileByteResponse

**Namespace:** `JC.FileStorage.Models`

Record returned by `StaticFileService.GetStaticFileBytes` and `StaticFileCache.GetStaticFileBytes`. Extends `GetStaticFileResponseBase`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `FileContent` | `byte[]?` | `null` | get; init; | The file's bytes, when `Result` is `true`. Null on failure. |

A successful response may be held in `StaticFileCache` and handed to every later caller as the same instance, so writing into `FileContent` changes what those callers see. Copy the array before mutating it.

### Constructors

#### GetStaticFileByteResponse(StaticFile file, byte[] fileContent)

Success. Sets `Result` to `true`, `File`, and `FileContent`.

#### GetStaticFileByteResponse(string errorMessage)

Failure. Sets `Result` to `false` and `ErrorMessage`. `File` and `FileContent` remain null.

## GetStaticFileTextResponse

**Namespace:** `JC.FileStorage.Models`

Record returned by `StaticFileService.GetStaticFileText` and `StaticFileCache.GetStaticFileText`. Extends `GetStaticFileResponseBase`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `FileContentText` | `string?` | `null` | get; init; | The file's contents as text, when `Result` is `true`. Null on failure. |

### Constructors

#### GetStaticFileTextResponse(StaticFile file, string fileContentText)

Success. Sets `Result` to `true`, `File`, and `FileContentText`.

#### GetStaticFileTextResponse(string errorMessage)

Failure. Sets `Result` to `false` and `ErrorMessage`. `File` and `FileContentText` remain null.

## FileUploadResponse

**Namespace:** `JC.FileStorage.Web.Models`

Sealed record returned by `WebStorageService.TryUploadFile` and `TryUploadFileForTenant`. Construct via the factory methods.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Result` | `bool` | `false` | get; init; | Whether the file was stored. |
| `ErrorMessage` | `string?` | `null` | get; init; | Why the upload failed, when `Result` is `false`. Null on success. Safe to surface to a user. |
| `ValidationError` | `FileValidationError` | `None` | get; init; | What the file failed validation on, when it was rejected before being stored. `None` on success, and when the upload failed for a reason other than validation. |

### Methods

#### Success()

**Returns:** `FileUploadResponse`

Static. The file was stored.

#### Failed(string errorMessage)

**Returns:** `FileUploadResponse`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `errorMessage` | `string` | — | Why the upload failed. |

Static. The upload failed for a reason other than validation — a blocked overwrite, or an IO or database failure. `ValidationError` stays `None`.

#### Rejected(FileValidationResponse validation)

**Returns:** `FileUploadResponse`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `validation` | `FileValidationResponse` | — | The failing validation result to carry over. |

Static. The file was rejected by validation before anything was read or written. Copies the message and error across.

## FileDownloadResponse

**Namespace:** `JC.FileStorage.Web.Models`

Sealed record returned by `WebStorageService.GetFileForDownload` and `GetFileForDownloadForTenant`, carrying everything an `IActionResult` needs.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Result` | `bool` | `false` | get; init; | Whether the file was read. |
| `ErrorMessage` | `string?` | `null` | get; init; | Why the read failed, when `Result` is `false`. Null on success. |
| `File` | `SavedFile?` | `null` | get; init; | The record, when `Result` is `true`. Null on failure. |
| `Content` | `byte[]?` | `null` | get; init; | The file's bytes, when `Result` is `true`. Null on failure. |
| `ContentType` | `string?` | `null` | get; init; | The MIME type for the file's extension, when `Result` is `true`. Null on failure. |
| `DownloadName` | `string?` | `null` | get; init; | The name to serve the file under, when `Result` is `true`. Null on failure. The name on disk is the record's ID, so it is never suitable to hand to a user. |

### Methods

#### Success(SavedFile file, byte[] content, string contentType, string downloadName)

**Returns:** `FileDownloadResponse`

Static. The file was read. Sets every property from the arguments.

#### Failed(string errorMessage)

**Returns:** `FileDownloadResponse`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `errorMessage` | `string` | — | Why the read failed. |

Static. The file could not be read. Every content property stays null.

---

# Enums

## FileValidationError

**Namespace:** `JC.FileStorage.Models`

Why a file failed validation. Lets a caller tell the reasons apart without parsing the message.

| Member | Value | Description |
|--------|-------|-------------|
| `None` | `0` | The file passed validation. |
| `BlockedExtension` | `1` | The extension is in `ValidationHelper.BlockedExtensions` and can never be stored. |
| `ExtensionNotAllowed` | `2` | The extension is not in the folder's allowed list, or the registry default list. |
| `TooLarge` | `3` | The file is larger than the folder's limit, or the registry default limit. |

---

# Services

## FolderRegistry

**Namespace:** `JC.FileStorage.Services`

Thread-safe in-memory registry of folders, held per tenant, and the home of the fallback limits applied to folders that declare none of their own. Registered as a singleton, so entries persist for the lifetime of the application. Populated at startup via `AddFolders` — see [Setup](Setup.md).

Folders are keyed by tenant, then matched by name case-insensitively. The no-tenant scope is keyed under `FolderModel.NullTenantName`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `DefaultMaxBytes` | `long?` | `null` | get; set; | Size limit for folders with no `FolderModel.MaxBytes`. Null means no limit for those folders. Setting throws `ArgumentOutOfRangeException` if the value is zero, negative, or above `ValidationHelper.MaxAllowedBytes`. |
| `DefaultAllowedExtensions` | `IReadOnlyList<string>?` | `null` | get; set; | Extensions accepted by folders with no `FolderModel.AllowedExtensions`. Null means any non-blocked extension. Entries are normalised on set. Setting throws `ArgumentException` if the list is empty or names a blocked extension. |

### Methods

#### TryAddFolder(FolderModel folder)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folder` | `FolderModel` | — | The folder to register, under its own `Tenant`. |

Adds `folder` to its tenant's collection, creating the collection if the tenant has none. Returns `false` without adding if a folder of the same name (compared case-insensitively) is already registered for that tenant; the same name under a different tenant is not a conflict.

Writes are serialised under a lock, so concurrent registrations of distinct folders are all retained and concurrent registrations of the same name yield exactly one winner.

#### TryGetFolder(string name, string? tenantId, out FolderModel? folder)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The folder name. Matched case-insensitively. |
| `tenantId` | `string?` | — | The tenant to search. Null or empty searches the no-tenant scope. |
| `folder` | `out FolderModel?` | — | The folder found, or null. |

Resolves the tenant's folders and returns the one matching `name`. Returns `false` with `folder` set to null when the tenant has no folders registered or no name matches.

#### TryGetFolders(string? tenantId, out IReadOnlyList<FolderModel>? folders)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantId` | `string?` | — | The tenant to search. Null or empty searches the no-tenant scope. |
| `folders` | `out IReadOnlyList<FolderModel>?` | — | All folders registered for the tenant, or null. |

Returns every folder registered for `tenantId`. Returns `false` with `folders` set to null when the tenant has none.

#### GetFolderNames(string? tenantId = null)

**Returns:** `IReadOnlyList<string>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantId` | `string?` | `null` | The tenant to list. Null or empty lists the no-tenant scope. |

Returns the names of every folder registered for `tenantId`, or an empty list when the tenant has none. Does not throw.

#### ResolveMaxBytes(FolderModel folder)

**Returns:** `long?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folder` | `FolderModel` | — | The folder to resolve the limit for. |

The size limit in force for `folder` — its own `MaxBytes` if set, otherwise `DefaultMaxBytes`, otherwise null for no limit. Does not consult the registered folders, so a `FolderModel` that was never registered resolves the same way.

#### ResolveAllowedExtensions(FolderModel folder)

**Returns:** `IReadOnlyList<string>?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folder` | `FolderModel` | — | The folder to resolve the extensions for. |

The allowed extensions in force for `folder` — its own `AllowedExtensions` if set, otherwise `DefaultAllowedExtensions`, otherwise null for any extension that is not blocked.

#### ValidateFile(FolderModel folder, string extension, long sizeBytes)

**Returns:** `FileValidationResponse`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folder` | `FolderModel` | — | The folder the file would be stored in. |
| `extension` | `string` | — | The file's extension. The leading dot is optional. |
| `sizeBytes` | `long` | — | The file's size in bytes. |

Checks the file in three steps, returning on the first failure.

`ValidationHelper.BlockedExtensions` is checked first and always applies, so a blocked extension can never be re-enabled by a folder or a default. Next, the resolved allowed extensions — if a list is in force and does not contain the extension, the file is rejected. Last, the resolved size limit — if one is in force and `sizeBytes` exceeds it, the file is rejected.

`StorageService` calls this itself before writing anything, so a rejected file never reaches disk or the database whichever entry point was used. Callers may also invoke it directly to fail fast and report the reason, which is what `WebStorageService` does.

## FilePathProvider

**Namespace:** `JC.FileStorage.Services`

Resolves physical paths for folders and files, and creates directories on demand. Registered as a singleton.

Managed file paths are built as `{BasePath}/{folder.Tenant}/{folder.Name}/{savedFileId}{extension}`, so each tenant's files occupy their own directory. Static file paths are built as `{StaticPath}/{subFolders}/{fileName}` and carry no tenant.

### Constructor

#### FilePathProvider(IConfiguration config, FolderRegistry folderRegistry)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `config` | `IConfiguration` | — | Application configuration. Read for `FileStorage:BasePath` and `FileStorage:StaticPath`. |
| `folderRegistry` | `FolderRegistry` | — | The registry used to resolve folders. |

Reads `FileStorage:BasePath` and caches it, throwing `InvalidOperationException` if the key is missing. `FileStorage:StaticPath` is cached without validation — a missing key throws from `GetStaticPath` instead, so an application that stores no static files never needs to set it.

### Methods

#### GetPath(FolderModel folder)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folder` | `FolderModel` | — | The folder to resolve. Must already be registered for its tenant. |

Resolves the registered folder matching `folder`'s tenant and name, combines the base path with the folder's tenant and name, creates the directory if it does not exist, and returns the path.

Throws `ArgumentException` if no folders are registered for the folder's tenant, or if no registered folder matches its name.

#### GetPath(string folderName, string? tenantId)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folderName` | `string` | — | The folder name. |
| `tenantId` | `string?` | — | The owning tenant. Null or whitespace resolves the no-tenant scope. |

Constructs a `FolderModel` from the arguments and delegates to `GetPath(FolderModel)`, with the same directory creation and exceptions. The name is validated by `FolderModel`'s constructor.

#### GetStaticPath(params IEnumerable\<string\> subFolders)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `subFolders` | `params IEnumerable<string>` | empty | Subfolders to append to the static path, outermost first. |

Combines `FileStorage:StaticPath` with `subFolders`, creates the directory if it does not exist, and returns the path. Called with no arguments it returns the static root, creating it if absent — which is why a mistyped `StaticPath` yields an empty directory rather than an error.

Unusable subfolder names are **dropped rather than rejected**: any that is null or whitespace, starts with `..`, `/` or `\`, or contains `..`, `/`, `\`, `*`, `?`, `"`, `<`, `>` or `|`. A dropped name resolves to the parent directory, so the caller sees a file-not-found rather than an exception. A leading dot is allowed, so `.well-known` resolves normally.

Throws `InvalidOperationException` if `FileStorage:StaticPath` is missing or empty.

#### GetFilePath(string path, string id, string ext)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `path` | `string` | — | The folder path, typically from `GetPath`. |
| `id` | `string` | — | The `SavedFile.Id` used as the physical file name. |
| `ext` | `string` | — | The extension. The leading dot is optional. |

Combines `path` with `id` and `ext` into a full file path, prefixing a dot to `ext` if it lacks one. Does not touch the file system and does not check that the file exists.

Throws `ArgumentException` if any argument is null or whitespace.

#### EnsureFolderExists(string path)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `path` | `string` | — | A directory path. |

Creates the directory at `path` if it does not already exist.

#### EnsureFolderExists(FolderModel folder)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folder` | `FolderModel` | — | The folder whose directory should exist. |

Delegates to `GetPath(FolderModel)`, which creates the directory as a side effect. Throws the same exceptions as `GetPath` when the folder is not registered.

#### EnsureFolderExists(string folderName, string? tenantId)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folderName` | `string` | — | The folder name. |
| `tenantId` | `string?` | — | The owning tenant. Null or whitespace resolves the no-tenant scope. |

Delegates to `GetPath(string, string?)`, which creates the directory as a side effect. Throws the same exceptions as `GetPath` when the folder is not registered.

#### CheckFileExists(string path)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `path` | `string` | — | A full file path. |

Returns whether a file exists at `path`.

## StorageService

**Namespace:** `JC.FileStorage.Services`

The entry point for consuming applications. Registered as scoped. Inject via `StorageService`.

Every operation exists in two forms. The scoped form takes no tenant and operates on the tenant the current operation is scoped to, read from `ITenantContext.TenantId`. The `*ForTenant` form takes a tenant explicitly and, when it differs from the operational one, **bypasses the global tenant query filter**.

`ITenantContext` is resolved optionally from the service provider. When JC.Tenancy is not registered it is absent, the tenant reads as null, and every scoped call operates in the no-tenant scope.

It reads the operational tenant rather than `IUserInfo.TenantId` deliberately. `SavedFile` is *filtered* by the operational tenant, so stamping writes from anything else would let the two disagree — a background job scoped to a tenant would write files it could not then read back.

> **The `*ForTenant` methods perform no authorisation check.** JC.FileStorage references only JC.Core, so it can see neither an identity package's roles nor JC.Tenancy's bypass authoriser. Any caller reaching these methods can reach any tenant's files. The consuming application must authorise every call — see the [Guide](Guide.md#cross-tenant-access).

All methods validate that the folder's tenant matches the tenant being operated on, throwing `ArgumentException` before any read or write when it does not.

### Constructor

#### StorageService(IRepositoryManager repos, IServiceProvider serviceProvider, ILogger&lt;StorageService&gt; logger, FilePathProvider pathProvider, FolderRegistry folderRegistry)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `repos` | `IRepositoryManager` | — | Repository manager bound to the ambient DbContext registered by `AddCore`. |
| `serviceProvider` | `IServiceProvider` | — | Used to resolve `ITenantContext` optionally. |
| `logger` | `ILogger<StorageService>` | — | Receives errors from failed reads, writes and deletes, and warnings from rejected files. |
| `pathProvider` | `FilePathProvider` | — | Resolves physical paths. |
| `folderRegistry` | `FolderRegistry` | — | Supplies the size and type limits enforced on save. |

### Methods

#### ChangeContext(Type contextType)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `contextType` | `Type` | — | The DbContext type to bind to. Must derive from `DbContext`. |

Rebinds this instance's repository manager to the given context via `IRepositoryManager.For`. Affects every subsequent call on the instance, not just the next one, and each context carries its own transaction.

Throws `ArgumentException` if `contextType` does not derive from `DbContext`.

#### GetSavedFileBytes(FolderModel folder, string fileName)

**Returns:** `Task<GetFileByteResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folder` | `FolderModel` | — | The folder to read from. Must be registered and belong to the caller's tenant. |
| `fileName` | `string` | — | The file name. Any extension is ignored for the lookup. |

Delegates to `GetSavedFileBytesForTenant` with the current user's tenant.

#### GetSavedFileText(FolderModel folder, string fileName)

**Returns:** `Task<GetFileTextResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folder` | `FolderModel` | — | The folder to read from. Must be registered and belong to the caller's tenant. |
| `fileName` | `string` | — | The file name. Any extension is ignored for the lookup. |

Delegates to `GetSavedFileTextForTenant` with the current user's tenant.

#### GetSavedFileBytesForTenant(string? tenantId, FolderModel folder, string fileName)

**Returns:** `Task<GetFileByteResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantId` | `string?` | — | The tenant to read from. Null addresses the no-tenant scope. Bypasses the tenant query filter when it differs from the caller's tenant. |
| `folder` | `FolderModel` | — | The folder to read from. Must be registered and belong to `tenantId`. |
| `fileName` | `string` | — | The file name. Any extension is ignored for the lookup. |

Locates the active record matching the folder and name within `tenantId`, resolves its path from the folder and the record's `Id` and `Extension`, and reads the file's bytes.

Returns a response with `ErrorMessage` of `"File not found."` when no active record matches, or when a record matches but no file exists at its path. Returns `"Error reading file."` if reading throws, logging the exception. On success `Result` is `true`, `File` holds the record, and `FileContent` holds the bytes.

Throws `ArgumentException` if `folder`'s tenant does not match `tenantId`, or if `folder` is not registered.

#### GetSavedFileTextForTenant(string? tenantId, FolderModel folder, string fileName)

**Returns:** `Task<GetFileTextResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantId` | `string?` | — | The tenant to read from. Null addresses the no-tenant scope. Bypasses the tenant query filter when it differs from the caller's tenant. |
| `folder` | `FolderModel` | — | The folder to read from. Must be registered and belong to `tenantId`. |
| `fileName` | `string` | — | The file name. Any extension is ignored for the lookup. |

Behaves as `GetSavedFileBytesForTenant`, reading the file's contents as text instead of bytes and returning them in `FileContentText`. The file's bytes are decoded regardless of whether they are textual.

#### TrySaveFile(FolderModel folder, string fileName, byte[] content, string ext, bool blockOverwrite = true)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folder` | `FolderModel` | — | The folder to write to. Must be registered and belong to the caller's tenant. |
| `fileName` | `string` | — | The file name. Any extension it carries takes precedence over `ext`. |
| `content` | `byte[]` | — | The bytes to write. |
| `ext` | `string` | — | Fallback extension, used only when `fileName` carries none. |
| `blockOverwrite` | `bool` | `true` | When `true`, returns `false` rather than replacing an existing file. |

Delegates to `TrySaveFileForTenant` with the current user's tenant.

#### TrySaveFile(FolderModel folder, string fileName, string fileText, string ext, bool blockOverwrite = true)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folder` | `FolderModel` | — | The folder to write to. Must be registered and belong to the caller's tenant. |
| `fileName` | `string` | — | The file name. Any extension it carries takes precedence over `ext`. |
| `fileText` | `string` | — | The text to write. Encoded as UTF-8. |
| `ext` | `string` | — | Fallback extension, used only when `fileName` carries none. |
| `blockOverwrite` | `bool` | `true` | When `true`, returns `false` rather than replacing an existing file. |

Delegates to `TrySaveFileForTenant` with the current user's tenant.

#### TrySaveFileForTenant(string? tenantId, FolderModel folder, string fileName, byte[] content, string ext, bool blockOverwrite = true)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantId` | `string?` | — | The tenant to write to. Null addresses the no-tenant scope. Bypasses the tenant query filter when it differs from the caller's tenant. |
| `folder` | `FolderModel` | — | The folder to write to. Must be registered and belong to `tenantId`. |
| `fileName` | `string` | — | The file name. Any extension it carries takes precedence over `ext`. |
| `content` | `byte[]` | — | The bytes to write. |
| `ext` | `string` | — | Fallback extension, used only when `fileName` carries none. |
| `blockOverwrite` | `bool` | `true` | When `true`, returns `false` rather than replacing an existing file. |

Looks for an active record matching the folder and name within `tenantId`. If none exists, creates a `SavedFile` owned by the folder's tenant. If one exists and `blockOverwrite` is `true`, returns `false` without writing anything.

The name and extension are then assigned from `fileName` and `ext`, and the physical path is built from the record's `Id` and its **stored** extension, so the record and the file cannot disagree.

The file is then checked against the folder's limits via `FolderRegistry.ValidateFile`, using the stored extension and `content.Length`. A rejected file logs a warning and returns `false` before the transaction opens, so nothing reaches disk or the database. This runs on every entry point, so no caller can store a file the folder forbids — a caller wanting the reason should call `ValidateFile` itself first.

Within a transaction, the record is inserted or updated, the file is created (truncating any existing content), flushed, and the transaction committed. If the extension changed during an overwrite, the file at the previous extension is deleted after the commit; a failure to remove it is logged as a warning and does not fail the call.

Any failure rolls the transaction back, logs the exception, and returns `false` — nothing is thrown for IO or database errors. Returns `true` on success.

Throws `ArgumentException` if `folder`'s tenant does not match `tenantId`, if `folder` is not registered, or if `fileName` and `ext` do not yield a valid name and extension.

#### TrySaveFileForTenant(string? tenantId, FolderModel folder, string fileName, string fileText, string ext, bool blockOverwrite = true)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantId` | `string?` | — | The tenant to write to. Null addresses the no-tenant scope. Bypasses the tenant query filter when it differs from the caller's tenant. |
| `folder` | `FolderModel` | — | The folder to write to. Must be registered and belong to `tenantId`. |
| `fileName` | `string` | — | The file name. Any extension it carries takes precedence over `ext`. |
| `fileText` | `string` | — | The text to write. |
| `ext` | `string` | — | Fallback extension, used only when `fileName` carries none. |
| `blockOverwrite` | `bool` | `true` | When `true`, returns `false` rather than replacing an existing file. |

Encodes `fileText` as UTF-8 and delegates to the `byte[]` overload.

#### TryDeleteFile(FolderModel folder, string fileName)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folder` | `FolderModel` | — | The folder holding the file. Must be registered and belong to the caller's tenant. |
| `fileName` | `string` | — | The file name. Any extension is ignored for the lookup. |

Delegates to `TryDeleteFileForTenant` with the current user's tenant.

#### TryDeleteFileForTenant(string? tenantId, FolderModel folder, string fileName)

**Returns:** `Task<bool>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantId` | `string?` | — | The tenant to delete from. Null addresses the no-tenant scope. Bypasses the tenant query filter when it differs from the caller's tenant. |
| `folder` | `FolderModel` | — | The folder holding the file. Must be registered and belong to `tenantId`. |
| `fileName` | `string` | — | The file name. Any extension is ignored for the lookup. |

Locates the active record matching the folder and name within `tenantId`, returning `false` if none matches.

Within a transaction, the record is **soft-deleted** — populating `DeletedById` and `DeletedUtc` so an audit retains who removed the file and when — and the file is then **permanently deleted** from disk. The transaction is committed only once both succeed.

The package offers no restore path. A consuming application may restore the record through the repository, but the file is gone and only metadata is recovered. Soft-deleted records are eventually removed permanently by JC.Core's `SoftDeleteCleanupJob`.

Deleting a file that has no file on disk succeeds — `File.Delete` is a no-op for a missing file, and the record is still soft-deleted.

Any failure rolls the transaction back, logs the exception, and returns `false`, leaving both the record and the file unchanged so the call can be retried. Returns `true` on success.

Throws `ArgumentException` if `folder`'s tenant does not match `tenantId`, or if `folder` is not registered.

## StaticFileRegistry

**Namespace:** `JC.FileStorage.Services`

Thread-safe in-memory registry of static files. Registered as a singleton by `AddFileStorage(useStaticFiles: true)`, and only then — see [Setup](Setup.md#addstaticfiles--static-file-registration).

Files are keyed by `StaticFile.Key`, the path beneath the static root lower-cased, so lookups are case-insensitive and a file in one subfolder does not collide with the same name in another.

The registry is the gate on reading: `StaticFileService` resolves a name through it before touching the disk, so an unregistered name never reaches the file system.

Its factory calls the internal `AutoDiscoverStaticFiles` during construction when `autoDiscoverStaticFiles` is on. Because the registration is a singleton, that runs the first time the registry is resolved rather than at startup.

### Constructor

#### StaticFileRegistry(FilePathProvider pathProvider, ILogger&lt;StaticFileRegistry&gt; logger)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `pathProvider` | `FilePathProvider` | — | Supplies the static root that discovery walks. |
| `logger` | `ILogger<StaticFileRegistry>` | — | Receives failures from `TryAddStaticFile` and `TryGetStaticFile`. |

### Methods

#### TryAddStaticFile(StaticFile file)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `file` | `StaticFile` | — | The file to register, under its own `Key`. |

Adds `file` to the registry. Returns `false` without replacing anything if a file is already registered under the same key.

On an add, `file.LastModifiedUtc` is set from the file's last write time on disk, or to null if nothing is there — overwriting whatever the constructor was given. It is not refreshed by `TryGetStaticFile`; that happens on a read through `StaticFileService`, or on demand through `StaticFile.RefreshLastModified`.

Also returns `false` rather than propagating if the add itself throws, logging the exception at error level. Writes are serialised under a lock.

#### TryAddStaticFile(string fileName, params IEnumerable\<string\> subFolders)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fileName` | `string` | — | The file name including its extension. |
| `subFolders` | `params IEnumerable<string>` | empty | The subfolders beneath the static path, outermost first. |

Constructs a `StaticFile` and delegates to the overload above.

**This overload throws where `TryGetStaticFile` does not.** The `StaticFile` is built in the argument expression, outside the other overload's `try`, so a name `StaticFile` rejects — one carrying no extension — propagates `ArgumentException` rather than returning `false`. `TryGetStaticFile` builds its own inside the `try` and treats the same input as a miss. Validate a name before registering it, or catch.

#### TryGetStaticFile(string fileName, out StaticFile? file, params IEnumerable\<string\> subFolders)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fileName` | `string` | — | The file name including its extension. Matched case-insensitively. |
| `file` | `out StaticFile?` | — | The registered file, or null. |
| `subFolders` | `params IEnumerable<string>` | empty | The subfolders beneath the static path, outermost first. |

Builds the key from the arguments and returns the registered file matching it. Returns `false` with `file` set to null when nothing matches.

Returns `false` rather than throwing when the arguments cannot form a file name — a name without an extension is an ordinary miss, logged at debug level. Anything else is logged at error level.

The instance returned is the registered one, not one built from the arguments, so it carries the casing and subfolders discovery found. That is what lets a case-insensitive lookup still produce a path that matches disk.

The `out` parameter sits between `fileName` and `subFolders`, so unlike the service and cache methods this one needs no placeholder argument to reach the subfolders.

## StaticFileService

**Namespace:** `JC.FileStorage.Services`

Reads registered static files from disk. Registered as a singleton by `AddFileStorage(useStaticFiles: true)`. Holds no per-request state.

Most applications should inject `StaticFileCache` instead, which wraps this and holds content in memory. Inject this directly where every read must reach the disk.

Every read resolves the name through `StaticFileRegistry` first, so an unregistered name returns a failed response without a file system call. Failures are returned, never thrown.

### Constructor

#### StaticFileService(FilePathProvider pathProvider, ILogger&lt;StaticFileService&gt; logger, StaticFileRegistry registry)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `pathProvider` | `FilePathProvider` | — | Resolves the static path and checks the file exists. |
| `logger` | `ILogger<StaticFileService>` | — | Receives exceptions thrown while reading a file. |
| `registry` | `StaticFileRegistry` | — | Resolves a name to a registered file. |

### Methods

#### GetStaticFileBytes(string fileName, CancellationToken ct = default, params IEnumerable\<string\> subfolders)

**Returns:** `Task<GetStaticFileByteResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fileName` | `string` | — | The file name including its extension. Matched case-insensitively. |
| `ct` | `CancellationToken` | `default` | Cancels the read. Cannot be omitted when `subfolders` is given, since it precedes a `params` parameter. |
| `subfolders` | `params IEnumerable<string>` | empty | The subfolders the file sits under, outermost first. |

Resolves the file through the registry, builds its path from the registered `SubFolders` and `FileName`, and reads its bytes.

Returns `"Static file not found"` when the name is not registered, when it cannot be formed into a file name, or when the registered file no longer exists on disk. Returns `"Error reading file."` if the read throws, logging the exception. On success `Result` is `true`, `File` holds the registered `StaticFile`, and `FileContent` holds the bytes.

A successful read also updates `File.LastModifiedUtc`, taken from the path the content came from and immediately after it was read, so the timestamp describes the bytes being returned. Because `File` is the registry's shared instance, that update is visible to every other holder of the file.

#### GetStaticFileText(string fileName, CancellationToken ct = default, params IEnumerable\<string\> subfolders)

**Returns:** `Task<GetStaticFileTextResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fileName` | `string` | — | The file name including its extension. Matched case-insensitively. |
| `ct` | `CancellationToken` | `default` | Cancels the read. Cannot be omitted when `subfolders` is given. |
| `subfolders` | `params IEnumerable<string>` | empty | The subfolders the file sits under, outermost first. |

Behaves as `GetStaticFileBytes`, reading the file as text instead of bytes and returning it in `FileContentText`, and updating `File.LastModifiedUtc` the same way. The bytes are decoded regardless of whether they are textual.

## StaticFileCache

**Namespace:** `JC.FileStorage.Services`

Holds static file content in `IMemoryCache`, so a file read on every request reaches the disk once per cache window. Registered as a singleton by `AddFileStorage(useStaticFiles: true)`, which also calls `AddMemoryCache`. The type most applications inject.

Text and bytes are held under separate keys — `StaticFile:Text:{key}` and `StaticFile:Bytes:{key}` — so reading a file one way does not hand back the other form.

Its read methods carry the same names and signatures as `StaticFileService`'s, so switching between the two to bypass the cache changes only the injected type.

### Constructor

#### StaticFileCache(IMemoryCache cache, StaticFileService staticFileService, StaticFileRegistry registry, int cacheDurationMinutes = 10)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cache` | `IMemoryCache` | — | The application's memory cache. |
| `staticFileService` | `StaticFileService` | — | Reads a file when it is not held in the cache. |
| `registry` | `StaticFileRegistry` | — | Resolves a name to a registered file, before the cache key is built. |
| `cacheDurationMinutes` | `int` | `10` | How long content is held. `0` disables caching, so every read passes through to the service. Supplied by `AddFileStorage`'s `staticFileCacheDurationMinutes`. |

Throws `ArgumentOutOfRangeException` if `cacheDurationMinutes` is negative. Zero is rejected by `IMemoryCache` as an expiry, so it is treated as "no caching" rather than passed on.

### Methods

#### GetStaticFileBytes(string fileName, CancellationToken ct = default, params IEnumerable\<string\> subFolders)

**Returns:** `Task<GetStaticFileByteResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fileName` | `string` | — | The file name including its extension. Matched case-insensitively. |
| `ct` | `CancellationToken` | `default` | Cancels the read when the content is not cached. Cannot be omitted when `subFolders` is given. |
| `subFolders` | `params IEnumerable<string>` | empty | The subfolders the file sits under, outermost first. |

Resolves the name through the registry, returning `"Static file not found"` without consulting the cache when nothing matches. Otherwise returns the cached response if one is held, and otherwise reads through `StaticFileService` and caches the result.

Only a successful response is cached, so a transient read failure is retried on the next call rather than held for the cache window. The registered file is passed to the service directly, so the lookup is not repeated.

The cached response is a single shared instance. Its `FileContent` array is not copied per caller — see `GetStaticFileByteResponse`.

A cache hit does not touch the disk, so `File.LastModifiedUtc` stays as the read that filled the entry left it — matching the content being handed back. Call `StaticFile.RefreshLastModified` where the date has to be current while the content is still served from memory.

#### GetStaticFileText(string fileName, CancellationToken ct = default, params IEnumerable\<string\> subFolders)

**Returns:** `Task<GetStaticFileTextResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fileName` | `string` | — | The file name including its extension. Matched case-insensitively. |
| `ct` | `CancellationToken` | `default` | Cancels the read when the content is not cached. Cannot be omitted when `subFolders` is given. |
| `subFolders` | `params IEnumerable<string>` | empty | The subfolders the file sits under, outermost first. |

Behaves as `GetStaticFileBytes`, against the text cache key and returning the content in `FileContentText`.

## WebStorageService

**Namespace:** `JC.FileStorage.Web.Services`

Wraps `StorageService` for web applications: takes `IFormFile` uploads, rejects them against the folder's limits before reading the stream, and returns stored files with the MIME type and download name an action result needs. Registered as scoped by `AddFileStorageWeb`. Inject via `WebStorageService`.

Validation here is a fail-fast convenience, not the gate — `StorageService` enforces the same rules itself, so a file rejected here could not have been stored anyway, and injecting `StorageService` directly instead is safe.

Covers uploads, downloads and validation only. Anything else — text saves, reading as text — means injecting `StorageService`, which stays registered alongside.

### Constructor

#### WebStorageService(StorageService storageService, FolderRegistry folderRegistry)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `storageService` | `StorageService` | — | The service every operation delegates to. |
| `folderRegistry` | `FolderRegistry` | — | Supplies the limits checked before an upload is read. |

### Methods

#### ChangeContext(Type contextType)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `contextType` | `Type` | — | The DbContext type to bind to. Must derive from `DbContext`. |

Forwards to `StorageService.ChangeContext`, with the same consequences — it rebinds for the rest of the instance's lifetime, not just the next call.

#### ValidateFile(FolderModel folder, IFormFile? file)

**Returns:** `FileValidationResponse`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folder` | `FolderModel` | — | The folder the file would be stored in. |
| `file` | `IFormFile?` | — | The upload to check. |

Checks an upload against the folder's limits without storing it, for populating `ModelState` before committing to the upload.

Returns a failing result with `FileValidationError.None` when `file` is null or empty, and `ExtensionNotAllowed` when the name carries no extension. Otherwise delegates to `FolderRegistry.ValidateFile` using the upload's extension and `IFormFile.Length`.

#### TryUploadFile(FolderModel folder, IFormFile? file, bool blockOverwrite = true, CancellationToken cancellationToken = default)

**Returns:** `Task<FileUploadResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folder` | `FolderModel` | — | The folder to write to. Must be registered and belong to the caller's tenant. |
| `file` | `IFormFile?` | — | The upload. |
| `blockOverwrite` | `bool` | `true` | When `true`, an existing file of that name is not replaced. |
| `cancellationToken` | `CancellationToken` | `default` | Cancels reading the upload. |

Stores an upload in the current user's tenant, delegating to `StorageService.TrySaveFile`.

#### TryUploadFileForTenant(string? tenantId, FolderModel folder, IFormFile? file, bool blockOverwrite = true, CancellationToken cancellationToken = default)

**Returns:** `Task<FileUploadResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantId` | `string?` | — | The tenant to write to. Null addresses the no-tenant scope. Bypasses the tenant query filter when it differs from the caller's tenant. |
| `folder` | `FolderModel` | — | The folder to write to. Must be registered and belong to `tenantId`. |
| `file` | `IFormFile?` | — | The upload. |
| `blockOverwrite` | `bool` | `true` | When `true`, an existing file of that name is not replaced. |
| `cancellationToken` | `CancellationToken` | `default` | Cancels reading the upload. |

Validates the upload first, returning a `Rejected` response without reading the stream — so an oversized file is never buffered into memory. On passing, reads the bytes, takes the name and extension from the upload, and delegates to `StorageService.TrySaveFileForTenant`.

A `false` from the save becomes a `Failed` response. The underlying call reports only success or failure, so the message covers a blocked overwrite and an IO or database failure together.

Throws `ArgumentException` if `folder`'s tenant does not match `tenantId`, or if `folder` is not registered.

#### GetFileForDownload(FolderModel folder, string fileName)

**Returns:** `Task<FileDownloadResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `folder` | `FolderModel` | — | The folder to read from. Must be registered and belong to the caller's tenant. |
| `fileName` | `string` | — | The file name. Any extension is ignored for the lookup. |

Reads a stored file from the current user's tenant, delegating to `StorageService.GetSavedFileBytes`.

#### GetFileForDownloadForTenant(string? tenantId, FolderModel folder, string fileName)

**Returns:** `Task<FileDownloadResponse>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tenantId` | `string?` | — | The tenant to read from. Null addresses the no-tenant scope. Bypasses the tenant query filter when it differs from the caller's tenant. |
| `folder` | `FolderModel` | — | The folder to read from. Must be registered and belong to `tenantId`. |
| `fileName` | `string` | — | The file name. Any extension is ignored for the lookup. |

Reads the file via `StorageService.GetSavedFileBytesForTenant`, then adds the MIME type from its stored extension and the download name from its `FileName` and `Extension`. A failed read is carried across as a `Failed` response with the underlying message.

#### TryDeleteFile(FolderModel folder, string fileName)

**Returns:** `Task<bool>`

Forwards to `StorageService.TryDeleteFile`. The record is soft-deleted; the file is removed from disk permanently.

#### TryDeleteFileForTenant(string? tenantId, FolderModel folder, string fileName)

**Returns:** `Task<bool>`

Forwards to `StorageService.TryDeleteFileForTenant`. Bypasses the tenant query filter when the tenant differs from the caller's own.

---

# Framework dictionary

The CSS class dictionary for this package's tag helper. Every property holds a **complete** class attribute value rather than a single token, and defaults to `""`, so adding one does not break an existing implementation. Registered by `AddFileStorageWeb` via JC.Web's `AddFrameworkDictionary`.

## IFileStorageFrameworkDictionary

**Namespace:** `JC.FileStorage.Web.Framework`

The class dictionary contract for `<upload-constraints>`. Extends `IFrameworkDictionary`. One implementation exists per supported `UIFramework`; the configured framework decides which is resolved from the container.

The contract belongs to JC.FileStorage.Web rather than JC.Web, so adding a tag helper here needs no JC.Web change and no JC.Web release.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `UploadConstraints` | `UploadConstraintsClasses` | get; | Classes for the upload constraints help text. |

## UploadConstraintsClasses

**Namespace:** `JC.FileStorage.Web.Framework`

Sealed record holding the classes for the upload constraints help text.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Container` | `string` | `""` | get; init; | The element wrapping the constraint text. |

## BootstrapFileStorageDictionary

**Namespace:** `JC.FileStorage.Web.Framework`

Sealed `IFileStorageFrameworkDictionary` implementation holding Bootstrap 5 class names. Selected when `UIFramework.Bootstrap` is configured, which is the default. `Container` is `form-text`, reproducing the markup this tag helper emitted before class names were made configurable.

## TailwindFileStorageDictionary

**Namespace:** `JC.FileStorage.Web.Framework`

Sealed `IFileStorageFrameworkDictionary` implementation holding Tailwind CSS classes, chosen to reproduce Bootstrap's `form-text` appearance. Selected when `UIFramework.Tailwind` is configured. Targets Tailwind v4 and assumes Preflight. `Container` is `mt-1 text-sm text-gray-500`.

Every class must reach Tailwind's scanner. These values live in a compiled assembly it never reads, so `@source` over application markup does not find them — an application file repeating them verbatim is the practical fix.

## CustomJCTailwindFileStorageDictionary

**Namespace:** `JC.FileStorage.Web.Framework`

Sealed `IFileStorageFrameworkDictionary` implementation holding jc-tailwind-ui classes. Selected when `UIFramework.CustomJCTailwind` is configured. That framework ships `form-text` as its own help-text treatment, so `Container` matches `BootstrapFileStorageDictionary` exactly.

It exists as a distinct type so the framework is a deliberate choice rather than a fallback to the Bootstrap dictionary, and so the value can diverge later without a registration change.

---

# Helpers

## NormalisationHelper

**Namespace:** `JC.FileStorage.Helpers`

Static class holding the name and extension normalisation both halves of the package share. `SavedFile`, `StaticFile` and `FolderRegistry` all route through it, so a name or extension behaves the same whichever entry point it arrives at.

### Methods

#### NormaliseExtension(string extension, bool lowerCase = true)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `extension` | `string` | — | The extension to normalise. The leading dot is optional. |
| `lowerCase` | `bool` | `true` | Whether to lower-case the result. |

Static. Trims `extension`, lower-cases it when `lowerCase` is `true`, and gives it a leading dot if it lacks one — so `PDF` returns `.pdf` by default and `.PDF` with `lowerCase: false`.

Leave `lowerCase` at its default for any value that will be compared: a blocked-extension check, an allowed-extension list, a lookup key. Pass `false` where the result becomes part of a physical path, because the file on disk keeps whatever casing it was given and a case-sensitive file system will not match a lower-cased spelling of it. `StaticFile` takes the second route; `FolderRegistry.ValidateFile` takes the first.

#### NormaliseFileName(string fileName)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fileName` | `string` | — | A file name, with or without an extension or directory components. |

Static. Strips any directory and extension from `fileName`, returning the value `SavedFile.SetFileName` would store in `FileName`. Delegates to `Path.GetFileNameWithoutExtension`.

Anything querying on `SavedFile.FileName` must key off this method, or the comparison will not match what was persisted.

#### GetFileName(string name, string extension)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The base name, without its extension. |
| `extension` | `string` | — | The extension. The leading dot is optional. |

Static. Rejoins a name and an extension into a complete file name. Casing is preserved on both parts, since the result names a file rather than a value to be compared.

This is how a caller holding a name and extension separately reaches the static file API, whose public constructors and methods all take a whole file name — `StaticFile` deliberately offers no public two-part constructor, so that `new StaticFile("terms.md", "legal")` can only mean a subfolder.

## ValidationHelper

**Namespace:** `JC.FileStorage.Helpers`

Static class holding the limits and the blocked-extension list enforced on managed files. `FolderModel` and `FolderRegistry` both use it, and `FolderModel` retains obsolete forwarders to its public members.

**These apply to managed files only.** Nothing here is consulted for static files, which are placed at deploy time by a developer or a build step rather than uploaded, so no part of them is untrusted.

### Fields

| Field | Type | Value | Description |
|-------|------|-------|-------------|
| `MaxAllowedBytes` | `const long` | `10737418240` | Hard ceiling (10GB) on any configured size limit. No folder or registry default may exceed it. |

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `BlockedExtensions` | `IReadOnlyCollection<string>` | ~60 entries | static get; | Extensions that can never be stored, whatever a folder or the registry allows. Executables, libraries, installers, shell scripts, scripts the Windows shell runs on open, shell and registry entry points, and platform packages. Compared case-insensitively. |

### Methods

#### IsBlockedExtension(string extension)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `extension` | `string` | — | The extension to test. The leading dot is optional. |

Static. Whether `extension` is in `BlockedExtensions`. Normalises before comparing, so `EXE`, `.exe` and `.EXE` all return `true`. Returns `false` for null or whitespace.

## FormFileHelper

**Namespace:** `JC.FileStorage.Web.Helpers`

Static class translating `IFormFile` uploads into the name, extension and bytes JC.FileStorage works in, and mapping extensions to MIME types for serving files back.

MIME lookups use ASP.NET Core's `FileExtensionContentTypeProvider`, which ships with the shared framework and carries roughly 380 mappings.

### Fields

| Field | Type | Value | Description |
|-------|------|-------|-------------|
| `DefaultContentType` | `const string` | `application/octet-stream` | Returned by `GetContentType` for extensions it does not recognise. |

### Methods

#### GetFileName(IFormFile file)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `file` | `IFormFile` | — | The upload. |

Static. The upload's file name with any directory component stripped. Browsers have historically sent full client paths, so `IFormFile.FileName` is not safe to use as a name — always prefer this.

#### GetExtension(IFormFile file)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `file` | `IFormFile` | — | The upload. |

Static. The upload's extension, lower-cased with a leading dot. Empty when the name carries none.

#### GetBytesAsync(IFormFile file, CancellationToken cancellationToken = default)

**Returns:** `Task<byte[]>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `file` | `IFormFile` | — | The upload. |
| `cancellationToken` | `CancellationToken` | `default` | Cancels the copy. |

Static. Reads the whole upload into memory. Buffers the entire file, so validate the size before calling — `WebStorageService` does this for you.

#### GetContentType(string extension)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `extension` | `string` | — | The extension. The leading dot is optional. |

Static. The MIME type for an extension, or `DefaultContentType` when it is not recognised or the argument is blank.

#### GetContentType(SavedFile file)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `file` | `SavedFile` | — | The stored file. |

Static. The MIME type for a stored file, from its `Extension`.

#### GetDownloadName(SavedFile file)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `file` | `SavedFile` | — | The stored file. |

Static. The name to serve a stored file under — its `FileName` and `Extension` rejoined. The name on disk is the record's ID, so it is never suitable to hand to a user.

#### FormatFileSize(long bytes)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `bytes` | `long` | — | The byte count. |

Static. A byte count as readable text — `1024` gives `1 KB`, `1572864` gives `1.5 MB`. Steps through bytes, KB, MB and GB, keeping up to two decimals above bytes. For display only; JC.FileStorage's own messages report raw bytes.

Throws `ArgumentOutOfRangeException` if `bytes` is negative.

## UploadConstraintsTagHelper

**Namespace:** `JC.FileStorage.Web.TagHelpers`

Renders a folder's upload constraints as help text, using the configured UI framework's classes. Targets `<upload-constraints>` with no end tag. Requires `@addTagHelper *, JC.FileStorage.Web` — see [Setup](Setup.md#jcfilestorageweb--aspnet-core-integration).

Constructor dependencies: `FolderRegistry`, `HtmlHelper`, `IFileStorageFrameworkDictionary`. The latter two come from `AddFileStorageWeb`; without that call, resolution fails when the page renders rather than at build or startup.

Reads the limits through `FolderRegistry.ResolveAllowedExtensions` and `ResolveMaxBytes` — the same values `ValidateFile` enforces — so the text cannot drift from the rule.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Folder` | `string` | — | get; set; | Attribute `folder`. The folder name. Required. |
| `TenantId` | `string?` | `null` | get; set; | Attribute `tenant-id`. The tenant owning the folder. Defaults to the operational tenant from `ITenantContext`, or the no-tenant scope when JC.Tenancy is not registered. |
| `ShowTypes` | `bool` | `true` | get; set; | Attribute `show-types`. Whether to show the accepted types. |
| `ShowSize` | `bool` | `true` | get; set; | Attribute `show-size`. Whether to show the maximum size. |
| `TypesLabel` | `string` | `Accepted types` | get; set; | Attribute `types-label`. Label before the accepted types. |
| `SizeLabel` | `string` | `Maximum size` | get; set; | Attribute `size-label`. Label before the maximum size. |
| `AnyTypeText` | `string` | `Any type except executable files` | get; set; | Attribute `any-type-text`. Shown when no type restriction applies. |
| `CssClass` | `string?` | `null` | get; set; | Attribute `css-class`. Classes applied to the wrapper `div`. Falls back to `IFileStorageFrameworkDictionary.UploadConstraints.Container` when null or whitespace. |
| `ViewContext` | `ViewContext` | — | get; set; | Not bound. Supplies the request services used to resolve `ITenantContext`. |

### Methods

#### Process(TagHelperContext context, TagHelperOutput output)

**Returns:** `void`

Resolves the folder for the tenant, then renders a `div` containing the constraints in force, separated by a middot.

The types half shows the resolved allowed extensions, or `AnyTypeText` when none is in force — the blocked list still applies, so the wording says so rather than implying anything goes. The size half is omitted entirely when no limit is in force. When both halves are suppressed, the element renders nothing. Labels are HTML-encoded.

Throws `InvalidOperationException` if `Folder` is blank, or if no folder of that name is registered for the tenant. Folders are registered per tenant, so a page shared across tenants needs the folder registered for each of them.

---

# Data

## IFileStorageDbContext

**Namespace:** `JC.FileStorage.Data`

Marker interface for a DbContext that supports file storage entities. Implement it on the consuming application's DbContext — see [Setup](Setup.md).

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `SavedFiles` | `DbSet<SavedFile>` | — | get; set; | The saved file records table. |

## SavedFileMap

**Namespace:** `JC.FileStorage.Data.DataMappings`

`IEntityTypeConfiguration<SavedFile>` describing the `SavedFile` entity. Applied via `ApplyFileStorageMappings` — see [Setup](Setup.md).

### Methods

#### Configure(EntityTypeBuilder&lt;SavedFile&gt; builder)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `builder` | `EntityTypeBuilder<SavedFile>` | — | The entity type builder to configure. |

Configures `Id` as the key with a maximum length of 36; `TenantId` at 36; `FileName` and `FolderName` as required at 256; and `Extension` as required at 64.

Configures no relationship to the tenant. `TenantId` is a plain column: `IMultiTenancy` marks a partition rather than a relationship, and the tenant record may live in another context or another database, so no foreign key can be assumed. Deleting a tenant therefore leaves its files untouched, pointing at an identifier that no longer resolves — which is what lets a restore bring them back intact.

Adds a composite index over `TenantId`, `FolderName` and `FileName` covering the lookup every read, save and delete performs, then applies the `AuditModel` column configuration and indexes via `AuditModelMapping<SavedFile>`.