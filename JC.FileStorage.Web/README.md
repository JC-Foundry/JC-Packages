# JC.FileStorage.Web

ASP.NET Core integration for [JC.FileStorage](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.FileStorage) — `IFormFile` uploads, MIME type inference for downloads, a tag helper showing a folder's limits, and an `IApplicationBuilder` overload of `AddFolders`.

Everything ASP.NET-specific lives here, which is what keeps JC.FileStorage itself host-agnostic.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.FileStorage.Web/JC.FileStorage.Web.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

## Prerequisites

- .NET 9.0 SDK, and an ASP.NET Core project
- **JC.Core**, registered with `AddCore<AppDbContext>()`
- **JC.Web**, which supplies the UI framework services the tag helper resolves
- A `DbContext` implementing `IFileStorageDbContext`, and `FileStorage:BasePath` in configuration

## Quick start

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();

// Registers WebStorageService and the UI services, plus everything AddFileStorage registers
builder.Services.AddFileStorageWeb();
```

Calling `AddFileStorage` separately is not required.

### Folders — after `Build()`

```csharp
var app = builder.Build();
app.AddFolders(true, "invoices", "reports");
```

### Tag helper — `_ViewImports.cshtml`

```cshtml
@addTagHelper *, JC.FileStorage.Web
```

## Feature areas

### Uploads

```csharp
public class UploadModel(WebStorageService storage) : PageModel
{
    private static readonly FolderModel Invoices = new("invoices");

    public async Task<IActionResult> OnPostAsync(IFormFile upload)
    {
        var result = await storage.TryUploadFile(Invoices, upload);
        if (!result.Result)
        {
            ModelState.AddModelError("upload", result.ErrorMessage ?? "Upload failed.");
            return Page();
        }

        return RedirectToPage();
    }
}
```

The upload is checked against the folder's limits **before** the stream is read, so an oversized file is rejected without being buffered. Validate without storing when you want to populate `ModelState` first:

```csharp
var check = storage.ValidateFile(Invoices, upload);
```

That check is a fail-fast convenience rather than the gate — `StorageService` enforces the same rules itself, so a file rejected here could not have been stored anyway.

### Downloads

```csharp
var file = await storage.GetFileForDownload(Invoices, name);
return File(file.Content, file.ContentType, file.DownloadName);
```

The MIME type is inferred from the extension using the shared framework's table of roughly 380 mappings, falling back to `application/octet-stream`.

### Upload constraints tag helper

```cshtml
<input type="file" name="upload" class="form-control" />
<upload-constraints folder="invoices" />
```

```html
<div class="form-text">Accepted types: .pdf, .csv &middot; Maximum size: 1 MB</div>
```

The text is read from the same `FolderRegistry` values the server enforces, so the help text cannot drift from the rule. The wrapper's class comes from the configured framework's dictionary.

### File name handling

`FormFileHelper` strips the directory component browsers have historically sent in `IFormFile.FileName`, normalises the extension to lower case with a leading dot, and maps extensions to MIME types. Use it directly when working with `StorageService` rather than `WebStorageService`.

### Choosing a framework

```csharp
builder.Services.AddFileStorageWeb(UIFramework.Tailwind);
```

| `UIFramework` | `<upload-constraints>` wrapper |
|---------------|-------------------------------|
| `Bootstrap` | `form-text` |
| `Tailwind` | `mt-1 text-sm text-gray-500` |
| `CustomJCTailwind` | `form-text` — jc-tailwind-ui ships its own |

This package registers no icon dictionary; its tag helper renders no glyphs.

Under either Tailwind framework, import the shipped safelists so the class names survive Tailwind's scanner:

```css
@import "../path/to/JC.Web/UI/jc-web.tailwind.css";
@import "../path/to/JC.FileStorage.Web/jc-filestorage.tailwind.css";
```

### Multiple DbContexts

```csharp
storage.ChangeContext(typeof(ArchiveDbContext));
```

Forwards to the underlying `StorageService` and affects every later call on that instance.

## Defaults

| Behaviour | Default |
|-----------|---------|
| `WebStorageService` lifetime | Scoped |
| Framework / icon set | Bootstrap / Bootstrap Icons |
| `<upload-constraints>` | Shows both accepted types and maximum size; suppressed entirely when both are turned off |
| Unknown extension MIME type | `application/octet-stream` |
| Everything else | Inherited from JC.FileStorage |

## Documentation

JC.FileStorage.Web is documented alongside its base package:

- [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.FileStorage/Setup.md#jcfilestorageweb--aspnet-core-integration)
- [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.FileStorage/Guide.md#web-applications)
- [API Reference](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.FileStorage/API.md)

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
