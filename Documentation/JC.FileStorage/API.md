# JC.FileStorage — API reference

Complete reference of all public types, properties, and methods in JC.FileStorage. See [Setup](Setup.md) for registration and [Guide](Guide.md) for usage examples.

> **Note:** Registration extensions (`IServiceCollection`, `IServiceProvider`, `IApplicationBuilder`) and the `ModelBuilder` mapping extension are documented in [Setup](Setup.md), not here.

---

# Models

## SavedFile

**Namespace:** `JC.FileStorage.Models`

Entity representing a stored file. Extends `AuditModel` (JC.Core) for full audit trail and soft-delete support, and implements `IMultiTenancy` so it is scoped by the tenant query filter applied by `IdentityDataDbContext`. See the JC.Core API reference for inherited members.

The file name and its extension are held in separate columns, and the physical file on disk is named after `Id`, not `FileName`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | New GUID | get; private set; | Primary key. Also the physical file name on disk, combined with `Extension`. Max length 36. |
| `TenantId` | `string?` | `null` | get; set; | The tenant this file belongs to. `null` places the file in the no-tenant scope. Max length 36. |
| `Tenant` | `Tenant?` | `null` | get; set; | Navigation to the owning tenant. Foreign key is `TenantId`. |
| `FileName` | `string` | `""` | get; private set; | The file name **without** its extension. Set via `SetFileName`. Required, max length 256. |
| `Extension` | `string` | `""` | get; private set; | The extension including its leading dot (e.g. `.pdf`). Set via `SetFileName`. Required, max length 64. |
| `FolderName` | `string` | `""` | get; private set; | The name of the folder holding this file. Set via `SetFolderName`. Required, max length 256. |

### Methods

#### NormaliseFileName(string fileName)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fileName` | `string` | — | A file name, with or without an extension or directory components. |

Static. Strips any directory and extension from `fileName`, returning the value `SetFileName` would store in `FileName`. Delegates to `Path.GetFileNameWithoutExtension`.

Anything querying on `FileName` must key off this method, or the comparison will not match what was persisted.

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

## FolderModel

**Namespace:** `JC.FileStorage.Models`

Immutable descriptor of a folder within a tenant, and of the size and type limits that apply to it. Folders are a single level of separation — there is no nesting.

`Tenant` and `TenantId` differ: `Tenant` is the path segment and sentinel-normalised, never null; `TenantId` is the raw tenant identifier as supplied, and is null for a no-tenant folder.

### Fields

| Field | Type | Value | Description |
|-------|------|-------|-------------|
| `NullTenantName` | `const string` | `NO__TENANT` | Sentinel used as `Tenant` for folders in the no-tenant scope, and as the directory name on disk. |
| `MaxAllowedBytes` | `const long` | `10737418240` | Hard ceiling (10GB) on any configured size limit. No folder or registry default may exceed it. |

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Name` | `string` | — | get; | The folder name. |
| `Tenant` | `string` | `NullTenantName` | get; | The tenant path segment. Either a tenant identifier or `NullTenantName`. Never null. |
| `TenantId` | `string?` | `null` | get; | The raw tenant identifier as passed to the constructor. Null for a no-tenant folder. |
| `MaxBytes` | `long?` | `null` | get; | Maximum size of a file in this folder. Null falls back to `FolderRegistry.DefaultMaxBytes`. |
| `AllowedExtensions` | `IReadOnlyList<string>?` | `null` | get; | Extensions this folder accepts, normalised to lower case with a leading dot. Null falls back to `FolderRegistry.DefaultAllowedExtensions`. Never overrides `BlockedExtensions`. |
| `BlockedExtensions` | `IReadOnlyCollection<string>` | ~60 entries | static get; | Extensions that can never be stored, whatever a folder or the registry allows. Executables, libraries, installers, shell scripts, scripts the Windows shell runs on open, shell and registry entry points, and platform packages. Compared case-insensitively. |

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

Throws `ArgumentOutOfRangeException` if `maxBytes` is zero or negative, or exceeds `MaxAllowedBytes`. Throws `ArgumentException` if `allowedExtensions` is supplied but empty once blanks are removed, or names any extension in `BlockedExtensions`.

Limits take a four-argument constructor rather than optional parameters, because `new FolderModel("x", null)` would otherwise be ambiguous between `tenantId` and `maxBytes`. Pass `null` for the tenant on a no-tenant folder.

### Methods

#### IsBlockedExtension(string extension)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `extension` | `string` | — | The extension to test. The leading dot is optional. |

Static. Whether `extension` is in `BlockedExtensions`. Normalises before comparing, so `EXE`, `.exe` and `.EXE` all return `true`. Returns `false` for null or whitespace.

#### NormaliseExtension(string extension)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `extension` | `string` | — | The extension to normalise. |

Static. Trims `extension`, lower-cases it, and gives it a leading dot if it lacks one — so `PDF` returns `.pdf`. Used wherever extensions are compared, so they behave the same whatever form they arrive in.

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

Abstract record and base of the file retrieval responses. Not returned directly — see `GetFileByteResponse` and `GetFileTextResponse`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Result` | `bool` | `false` | get; init; | Whether the file was retrieved. |
| `File` | `SavedFile?` | `null` | get; init; | The record, when `Result` is `true`. Null on failure. |
| `ErrorMessage` | `string?` | `null` | get; init; | Why retrieval failed, when `Result` is `false`. Null on success. |

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
| `BlockedExtension` | `1` | The extension is in `FolderModel.BlockedExtensions` and can never be stored. |
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
| `DefaultMaxBytes` | `long?` | `null` | get; set; | Size limit for folders with no `FolderModel.MaxBytes`. Null means no limit for those folders. Setting throws `ArgumentOutOfRangeException` if the value is zero, negative, or above `FolderModel.MaxAllowedBytes`. |
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

`FolderModel.BlockedExtensions` is checked first and always applies, so a blocked extension can never be re-enabled by a folder or a default. Next, the resolved allowed extensions — if a list is in force and does not contain the extension, the file is rejected. Last, the resolved size limit — if one is in force and `sizeBytes` exceeds it, the file is rejected.

`StorageService` calls this itself before writing anything, so a rejected file never reaches disk or the database whichever entry point was used. Callers may also invoke it directly to fail fast and report the reason, which is what `WebStorageService` does.

## FilePathProvider

**Namespace:** `JC.FileStorage.Services`

Resolves physical paths for folders and files, and creates directories on demand. Registered as a singleton.

Paths are built as `{BasePath}/{folder.Tenant}/{folder.Name}/{savedFileId}{extension}`, so each tenant's files occupy their own directory.

### Constructor

#### FilePathProvider(IConfiguration config, FolderRegistry folderRegistry)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `config` | `IConfiguration` | — | Application configuration. Read for `FileStorage:BasePath`. |
| `folderRegistry` | `FolderRegistry` | — | The registry used to resolve folders. |

Reads `FileStorage:BasePath` and caches it. Throws `InvalidOperationException` if the key is missing.

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

Every operation exists in two forms. The scoped form takes no tenant and operates on the current user's tenant, read from `IUserInfo.TenantId`. The `*ForTenant` form takes a tenant explicitly and, when it differs from the caller's own, **bypasses the global tenant query filter**.

`IUserInfo` is resolved optionally from the service provider. When JC.Identity is not registered it is absent, `IUserInfo.TenantId` reads as null, and every scoped call operates in the no-tenant scope.

> **The `*ForTenant` methods perform no authorisation check.** JC.FileStorage cannot check the `SystemAdmin` role, which lives in JC.Identity. Any caller reaching these methods can reach any tenant's files. The consuming application must authorise every call — see the [Guide](Guide.md#cross-tenant-access).

All methods validate that the folder's tenant matches the tenant being operated on, throwing `ArgumentException` before any read or write when it does not.

### Constructor

#### StorageService(IRepositoryManager repos, IServiceProvider serviceProvider, ILogger&lt;StorageService&gt; logger, FilePathProvider pathProvider, FolderRegistry folderRegistry)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `repos` | `IRepositoryManager` | — | Repository manager bound to the ambient DbContext registered by `AddCore`. |
| `serviceProvider` | `IServiceProvider` | — | Used to resolve `IUserInfo` optionally. |
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

# Helpers

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

Renders a folder's upload constraints as Bootstrap help text. Targets `<upload-constraints>` with no end tag. Requires `@addTagHelper *, JC.FileStorage.Web` — see [Setup](Setup.md#jcfilestorageweb--aspnet-core-integration).

Reads the limits through `FolderRegistry.ResolveAllowedExtensions` and `ResolveMaxBytes` — the same values `ValidateFile` enforces — so the text cannot drift from the rule.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Folder` | `string` | — | get; set; | Attribute `folder`. The folder name. Required. |
| `TenantId` | `string?` | `null` | get; set; | Attribute `tenant-id`. The tenant owning the folder. Defaults to the current user's tenant, or the no-tenant scope when JC.Identity is not registered. |
| `ShowTypes` | `bool` | `true` | get; set; | Attribute `show-types`. Whether to show the accepted types. |
| `ShowSize` | `bool` | `true` | get; set; | Attribute `show-size`. Whether to show the maximum size. |
| `TypesLabel` | `string` | `Accepted types` | get; set; | Attribute `types-label`. Label before the accepted types. |
| `SizeLabel` | `string` | `Maximum size` | get; set; | Attribute `size-label`. Label before the maximum size. |
| `AnyTypeText` | `string` | `Any type except executable files` | get; set; | Attribute `any-type-text`. Shown when no type restriction applies. |
| `CssClass` | `string` | `form-text` | get; set; | Attribute `css-class`. Classes applied to the wrapper `div`. |
| `ViewContext` | `ViewContext` | — | get; set; | Not bound. Supplies the request services used to resolve `IUserInfo`. |

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

Configures the optional relationship to `Tenant` over the `TenantId` foreign key with `DeleteBehavior.SetNull`, so deleting a tenant moves its files into the no-tenant scope rather than deleting their records.

Adds a composite index over `TenantId`, `FolderName` and `FileName` covering the lookup every read, save and delete performs, then applies the `AuditModel` column configuration and indexes via `AuditModelMapping<SavedFile>`.