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
| `ContentSanitiser()` | Uses `ContentSanitiserOptions.RichText()` — headings, tables, links, lists, inline `data:` images, and editor inline styles |
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

Bootstrap needs no setup beyond loading Bootstrap 5. The two Tailwind dictionaries do:

| Dictionary | Requirement |
|---|---|
| `TailwindDictionary` | Targets Tailwind v4 and assumes Preflight. **Every class it emits must be declared to Tailwind** — utilities are generated by scanning source files, and these values live in a compiled assembly Tailwind never reads, so `@source` over your markup does not reach them |
| `CustomJCTailwindDictionary` | Needs jc-tailwind-ui. Pagination is in that framework's opt-in `interactive` layer, so `@import "jc-tailwind-ui/interactive"` is required for `<pagination>` to be styled. A short `@source inline(...)` list covers the few stock Tailwind utilities used — the framework's own classes are always in its bundle |

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

### Registering a shared sanitiser

`ContentSanitiser` is not registered by `AddUI`. It is safe to reuse and slightly cheaper to share, since building the allowlists is not free:

```csharp
builder.Services.AddSingleton(new ContentSanitiser(ContentSanitiserOptions.RichText()));

// Or a keyed pair when different content needs different policies
builder.Services.AddKeyedSingleton("rich", new ContentSanitiser(ContentSanitiserOptions.RichText()));
builder.Services.AddKeyedSingleton("comments", new ContentSanitiser(ContentSanitiserOptions.Basic()));
```

This is your registration, not the package's — nothing in JC.Web resolves `ContentSanitiser` from the container.

### ContentSanitiserOptions

Passed to the `ContentSanitiser` constructor, either as a preset or through a configuration callback.

| Property | Type | Default (`RichText()`) | Description |
|----------|------|------------------------|-------------|
| `AllowedTags` | `HashSet<string>` | Headings, tables, lists, links, images, inline formatting | Element names permitted in the output |
| `AllowedAttributes` | `HashSet<string>` | `class`, `style`, `title`, `dir`, `href`, `target`, `rel`, `src`, `alt`, `width`, `height`, `colspan`, `rowspan`, `span` | Attribute names permitted on allowed elements |
| `AllowedSchemes` | `HashSet<string>` | `http`, `https`, `mailto` — plus `data` added automatically when `AllowInlineImages` is on | URL schemes permitted in attribute values |
| `AllowedCssProperties` | `HashSet<string>` | Font, colour, alignment, dimensions, table and spacing properties | CSS properties permitted in inline `style` attributes |
| `AllowedClasses` | `HashSet<string>` | Empty — all class names allowed | When populated, restricts which class names survive |
| `AllowInlineImages` | `bool` | `true` | Permits `data:` URIs, narrowed to `data:image/*` on `<img>` |
| `KeepChildNodes` | `bool` | `true` | When a tag is removed, keeps its text content rather than discarding the subtree |
| `Configure` | `Action<HtmlSanitizer>?` | `null` | Escape hatch applied last, able to override every other setting |

**`AllowInlineImages` defaults to `false` on a bare `new ContentSanitiserOptions()`.** The `true` above is what `RichText()` sets; `Basic()` and `Empty()` leave it off.

#### Presets

| Preset | Allows |
|--------|--------|
| `RichText()` | The default. Headings, tables, images including inline `data:` images, links, lists, and the inline styles editors write for font, colour and alignment |
| `Basic()` | Inline formatting, lists, quotes and links. No images, tables, styles or classes, so output cannot carry layout or colour into the page |
| `Empty()` | Nothing. With `KeepChildNodes` on, reduces markup to its text — a strip-all-HTML policy |

Presets are **methods, not properties** — each call returns a fresh instance, so adjusting one never affects another caller.

```csharp
var comments = new ContentSanitiser(ContentSanitiserOptions.Basic());

var noImages = new ContentSanitiser(o =>
{
    o.AllowInlineImages = false;
    o.AllowedTags.Remove("img");
});
```

> **`class` and the sizing CSS properties are load-bearing, not cosmetic.** Editors store image alignment, captions and table styling as theme classes, and write `width`/`height`/`max-width` onto images to keep them fluid. Removing those entries silently breaks the layout of previously-saved content. Narrow `AllowedClasses` rather than dropping the `class` attribute. See [Load-bearing allowlist entries](UI-Guide.md#load-bearing-allowlist-entries).

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
2. Call `ContentSanitiser.SanitiseContent("<script>alert(1)</script><b>ok</b>")` — it should return `<b>ok</b>` with the script removed.
3. If using `<bug-reporter>`, submit a report and confirm the `metadata` field is populated rather than empty.

## Next steps

- [Guide](UI-Guide.md) — tag helpers, the framework dictionary system, content sanitisation, table and dropdown building, QR codes, and model state handling.
- [API Reference](UI-API.md)
