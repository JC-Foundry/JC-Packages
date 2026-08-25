# JC.Web: UI — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project using Razor Pages or MVC views
- A CSS framework matching the one you register — Bootstrap 5, Tailwind v4, or jc-tailwind-ui. Class names come from a per-framework dictionary; without the matching stylesheet the output renders as unstyled HTML rather than breaking
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

## 0. Add the package

Add a project reference to `JC.Web`:

```xml
<ProjectReference Include="path/to/JC.Web/JC.Web.csproj" />
```

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### Services — `Program.cs`

```csharp
// Registers the framework service, JC.Web's class dictionary, AlertHelper and HtmlHelper
builder.Services.AddUI();
```

`AddWebDefaults` calls `AddUI` for you, so applications using it need no separate call:

```csharp
builder.Services.AddWebDefaults(builder.Configuration);
```

### Tag helpers — `_ViewImports.cshtml`

```cshtml
@addTagHelper *, JC.Web
```

This enables `<alert>`, `<pagination>`, `<breadcrumb>` with its nested `<crumb>`, and `<bug-reporter>`. Without it, Razor treats them as unknown HTML elements and renders them literally into the page rather than raising an error.

**Tag helpers are activated by Razor but their constructor dependencies come from DI.** `<alert>` injects `AlertHelper`, `<pagination>` injects `HtmlHelper`, and `<breadcrumb>` and `<bug-reporter>` inject `IWebFrameworkDictionary`. Adding the tag helper directive without calling `AddUI` fails at render time, not at build or startup.

### Defaults

`AddUI()` with no arguments registers:

| Registration | Lifetime | Value |
|---|---|---|
| `UIFrameworkService` | Singleton | `Framework` = `Bootstrap`, `IconFramework` = `Bootstrap` |
| `IWebFrameworkDictionary` | Singleton | `BootstrapDictionary` |
| `AlertHelper` | Singleton | — |
| `HtmlHelper` | Singleton | — |

All four use `TryAdd`, so an earlier registration wins and calling `AddUI` more than once is harmless.

Component defaults:

| Type | Default behaviour |
|------|-------------------|
| `QrCodeHelper()` | SVG output, 10 pixels per module, error correction level `M` (15%) |
| `TableBuilder<T>` | No columns until added; cell content HTML-encoded |
| `BreadcrumbBuilder` | No items until added; last item rendered as the current page |
| `ModelStateWrapper` | Prefix `"Input."` |
| `<alert>` | Type `Info`, dismissible |
| `<pagination>` | `href-format` `"?page={0}"`, 5 visible pages, first/last buttons shown |
| `<breadcrumb>` | Suppresses output entirely when it contains no `<crumb>` |
| `<bug-reporter>` | Icon `🐞`, title `"Send Feedback"`, colour from the dictionary (`danger` under Bootstrap), request path included, query string masked |

## 2. Full configuration

### AddUI — choosing the frameworks

```csharp
builder.Services.AddUI(
    framework: UIFramework.Bootstrap,
    iconFramework: IconFramework.Bootstrap);
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `framework` | `UIFramework` | `Bootstrap` | The CSS framework tag helpers and builders render classes for |
| `iconFramework` | `IconFramework` | `Bootstrap` | The icon set components render glyphs from, chosen independently of `framework` |

Both are `[Flags]` enums. If several flags are combined, `UIFrameworkService` resolves to one value in its constructor, so nothing downstream ever sees an unresolved combination.

#### `UIFramework`

| Member | Value | Dictionary selected |
|--------|-------|---------------------|
| `Bootstrap` | `1` | `BootstrapDictionary` |
| `Tailwind` | `2` | `TailwindDictionary` |
| `CustomJCTailwind` | `4` | `CustomJCTailwindDictionary` |

Resolution precedence is `CustomJCTailwind` > `Tailwind` > `Bootstrap`, so a combined value picks the most specific.

#### `IconFramework`

| Member | Value | Notes |
|--------|-------|-------|
| `Bootstrap` | `1` | Bootstrap Icons. Also the result when the value is `0` |
| `FontAwesome` | `2` | Selected only when `Bootstrap` is not also set |

JC.Web registers **no icon dictionary** — none of its own components render a glyph. The icon choice is still resolved here so packages layered above can register theirs against one application-level decision.

### AddWebDefaults — framework selection

Both `AddWebDefaults` overloads accept the same two values and pass them to `AddUI`:

```csharp
builder.Services.AddWebDefaults(
    builder.Configuration,
    uiFramework: UIFramework.Bootstrap,
    iconFramework: IconFramework.Bootstrap);
```

### Framework-specific requirements

Bootstrap needs no setup beyond loading Bootstrap 5, because Bootstrap ships finished CSS. Both Tailwind dictionaries need their classes declared, and for the same reason.

**Tailwind generates utilities by scanning source files.** JC.Web's class names live in a compiled assembly it never reads, so `@source` over your own markup does not reach them — the components render with valid class names and no CSS behind them.

The package ships a safelist declaring them. It is in the `.nupkg`, so it reaches you either way you consume the suite:

```css
/* Project reference */
@import "../path/to/JC.Web/UI/jc-web.tailwind.css";

/* Package reference — under the global packages folder */
@import "<nuget-root>/jc.web/<version>/contentFiles/any/any/jc-web.tailwind.css";
```

`<nuget-root>` is `%USERPROFILE%\.nuget\packages` on Windows and `~/.nuget/packages` elsewhere.

**On NuGet, prefer copying the file into your own `Styles` folder.** The package path carries the version number, so every upgrade breaks the import until you edit it. Tailwind resolves paths on disk and never sees MSBuild, so there is no package-relative token to use instead. Copying once and re-copying on upgrade is the less brittle option — the file is small and changes rarely.

The file has one block per dictionary, so delete whichever you do not use.

| Dictionary | Requirement |
|---|---|
| `TailwindDictionary` | Targets Tailwind v4 and assumes Preflight. Import the safelist above |
| `CustomJCTailwindDictionary` | Needs jc-tailwind-ui, and the same safelist import. Pagination is in that framework's opt-in `interactive` layer, so `@import "jc-tailwind-ui/interactive"` is also required for `<pagination>` to be styled |

**jc-tailwind-ui is not exempt.** It compiles from source through Tailwind rather than shipping finished CSS, so the split is between *authored CSS rules* and *generated utilities*, not between frameworks. Its own component classes — `btn`, `card`, `form-control`, `tone-*` — are real rules in its bundle and need nothing. Every stock utility and arbitrary value the dictionary uses is generated on demand and needs declaring exactly as under plain Tailwind.

**The safelist is best-effort.** It covers each dictionary's values and its default contextual colour. A `colour="…"` attribute set to anything else — `<bug-reporter colour="info">` needing `btn-info` — cannot be predicted from the dictionary, so add those to your own entry CSS.

Under `CustomJCTailwindDictionary` the `<bug-reporter>` `colour` attribute is a **tone name** (`danger`, `info`, or any custom tone the application defines). Under `TailwindDictionary` it is a Tailwind colour fragment such as `red-600`. Under Bootstrap it is a contextual name such as `danger`.

### AddFrameworkDictionary — registering another package's dictionary

Packages layered above JC.Web declare their own dictionary contract deriving from `IFrameworkDictionary` and register it with this method. The factory receives the framework already resolved by `UIFrameworkService`, so every dictionary in the application agrees on which framework is in play.

```csharp
services.AddFrameworkDictionary<ICommunicationFrameworkDictionary>(f => f switch
{
    UIFramework.Tailwind => new TailwindCommunicationDictionary(),
    _ => new BootstrapCommunicationDictionary()
});
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `factory` | `Func<UIFramework, TDictionary>` | Returns the implementation for the resolved framework. Called once, on first resolution |

`TDictionary` must be a reference type implementing `IFrameworkDictionary`. Registered with `TryAddSingleton`. Requires `AddUI` to have been called.

### AddIconDictionary — registering another package's icon dictionary

The icon counterpart, reading `UIFrameworkService.IconFramework` instead:

```csharp
services.AddIconDictionary<ICommunicationIconDictionary>(
    _ => new BootstrapIconsCommunicationDictionary());
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `factory` | `Func<IconFramework, TDictionary>` | Returns the implementation for the resolved icon set. Called once, on first resolution |

`TDictionary` must be a reference type implementing `IIconDictionary`. A package may register a class dictionary, an icon dictionary, or both.

### Bug reporter — pipeline requirements

`<bug-reporter>` is the only component here with dependencies outside this area. It reads `RequestMetadata` to attach diagnostic context to the report, so [client profiling](ClientProfiling-Setup.md) must be registered and its middleware must have run:

```csharp
builder.Services.AddClientProfiling();   // or AddWebDefaults

var app = builder.Build();
app.UseClientProfiling();                // must run before the page renders
```

Without it the widget still renders and submits, but its `metadata` field is empty — the report arrives with no browser, path or request identifier, which is most of its value.

The widget posts JSON to the endpoint given in its `endpoint` attribute, including an anti-forgery token in the `RequestVerificationToken` header when one is available. Your receiving endpoint should expect that header rather than a form field.

### Sanitising editor output

HTML sanitisation belongs to JC.Content, which owns `ContentSanitiser` and its options. See [JC.Content — Setup](../JC.Content/Setup.md) for the policies and presets, and [JC.Content — Guide](../JC.Content/Guide.md) for how they are applied.

Nothing in JC.Web sanitises on your behalf. The one place it matters here is `HtmlHelper`, whose element-building methods insert their `content` argument as raw HTML: sanitise or encode user-supplied text before it reaches them.

### QrCodeHelper

Configured through its constructor rather than options. Not registered by `AddUI`.

```csharp
var svg = new QrCodeHelper();                                            // defaults
var png = new QrCodeHelper(QrCodeFormat.Base64, pixelsPerModule: 15);
var robust = new QrCodeHelper(QrCodeFormat.Svg, 10, QRCodeGenerator.ECCLevel.H);
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `format` | `QrCodeFormat` | `Svg` | `Svg` returns markup; `Base64` returns a `data:image/png;base64,...` URI |
| `pixelsPerModule` | `int` | `10` | Size of each QR module in pixels. Larger values give a bigger, more scannable code |
| `eccLevel` | `QRCodeGenerator.ECCLevel` | `M` | Error correction: `L` 7%, `M` 15%, `Q` 25%, `H` 30%. Higher levels survive damage and overlaid logos, at the cost of a denser code |

The parameterless constructor fixes `eccLevel` at `M`; use the three-argument form to change it.

## 3. Verify

1. Add `<alert type="Success" message="It works" />` to a view — it should render a styled alert with a dismiss button, not literal `<alert>` text. Literal text means `@addTagHelper` is missing; an `InvalidOperationException` about `AlertHelper` means `AddUI` was not called.
2. If using `<bug-reporter>`, submit a report and confirm the `metadata` field is populated rather than empty.

## Next steps

- [Guide](UI-Guide.md) — tag helpers, the framework dictionary system, table and dropdown building, QR codes, and model state handling.
- [API Reference](UI-API.md)
