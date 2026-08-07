# JC.Web: UI — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project using Razor Pages or MVC views
- Bootstrap 5 — every helper and tag helper in this area emits Bootstrap 5 markup and classes. Without it the output renders as unstyled HTML rather than breaking, but it will not look right
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

## 0. Add the package

Add a project reference to `JC.Web`:

```xml
<ProjectReference Include="path/to/JC.Web/JC.Web.csproj" />
```

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### Tag helpers — `_ViewImports.cshtml`

Tag helpers are the only part of this area that needs registering, and it happens in Razor rather than in `Program.cs`:

```cshtml
@addTagHelper *, JC.Web
```

This enables `<alert>`, `<pagination>`, `<breadcrumb>` with its nested `<crumb>`, and `<bug-reporter>`. Without it, Razor treats them as unknown HTML elements and renders them literally into the page rather than raising an error.

### Services — `Program.cs`

**There is no `AddUI` registration.** Nothing in this area is resolved from the container:

- Tag helpers are activated by Razor, not DI
- `DropdownHelper`, `AlertHelper` and `HtmlHelper` are static
- `ContentSanitiser`, `QrCodeHelper`, `TableBuilder<T>`, `BreadcrumbBuilder`, `HtmlTagBuilder` and `ModelStateWrapper` are instantiated directly where used

If you want a shared, pre-configured `ContentSanitiser` or `QrCodeHelper` rather than constructing one per call site, register your own singleton — see [Registering a shared sanitiser](#registering-a-shared-sanitiser).

### Defaults

| Type | Default behaviour |
|------|-------------------|
| `ContentSanitiser()` | Uses `ContentSanitiserOptions.RichText()` — headings, tables, links, lists, inline `data:` images, and editor inline styles |
| `QrCodeHelper()` | SVG output, 10 pixels per module, error correction level `M` (15%) |
| `TableBuilder<T>` | No columns until added; cell content HTML-encoded |
| `ModelStateWrapper` | Prefix `"Input."` |
| `<alert>` | Type `Info`, dismissible |
| `<pagination>` | `href-format` `"?page={0}"`, 5 visible pages, first/last buttons shown |
| `<bug-reporter>` | Icon `🐞`, title `"Send Feedback"`, colour `danger`, request path included, query string masked |

## 2. Full configuration

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

`ContentSanitiser` is safe to reuse and slightly cheaper to share, since building the allowlists is not free:

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
| `AllowedAttributes` | `HashSet<string>` | Includes `class`, `style`, `href`, `src`, `alt`, `title` | Attribute names permitted on allowed elements |
| `AllowedSchemes` | `HashSet<string>` | `http`, `https`, `mailto`, and `data` when `AllowInlineImages` is on | URL schemes permitted in attribute values |
| `AllowedCssProperties` | `HashSet<string>` | Font, colour, alignment, `width`, `height`, `max-width` | CSS properties permitted in inline `style` attributes |
| `AllowedClasses` | `HashSet<string>` | Empty — all class names allowed | When populated, restricts which class names survive |
| `AllowInlineImages` | `bool` | `true` | Permits `data:` URIs, narrowed to `data:image/*` on `<img>` |
| `KeepChildNodes` | `bool` | `true` | When a tag is removed, keeps its text content rather than discarding the subtree |
| `Configure` | `Action<HtmlSanitizer>?` | `null` | Escape hatch applied last, able to override every other setting |

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

Configured through its constructor rather than options:

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

## 3. Verify

1. Add `<alert type="Success" message="It works" />` to a view — it should render a green Bootstrap alert with a dismiss button, not literal `<alert>` text. Literal text means `@addTagHelper` is missing.
2. Call `ContentSanitiser.SanitiseContent("<script>alert(1)</script><b>ok</b>")` — it should return `<b>ok</b>` with the script removed.
3. If using `<bug-reporter>`, submit a report and confirm the `metadata` field is populated rather than empty.

## Next steps

- [Guide](UI-Guide.md) — tag helpers, content sanitisation, table and dropdown building, QR codes, and model state handling.
- [API Reference](UI-API.md)
