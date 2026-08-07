# JC.Web: UI — Guide

Covers the Bootstrap 5 tag helpers, HTML sanitisation of user-authored content, programmatic HTML building, dropdown construction, QR codes, and model state handling. See [Setup](UI-Setup.md) for the `_ViewImports` registration.

## Tag helpers

All four require `@addTagHelper *, JC.Web` in `_ViewImports.cshtml`. Without it Razor renders the element name literally into the page rather than failing, so an unstyled `<alert>` appearing in the output means the directive is missing.

### Alert

```cshtml
<alert type="Success" message="Changes saved successfully!" />
<alert type="Warning" message="Your session expires in 5 minutes." dismissible="false" />
<alert type="Error" message="Failed to save changes." />
<alert type="Info" message="A new version is available." />
```

| Attribute | Default | Description |
|-----------|---------|-------------|
| `type` | `Info` | `Success`, `Warning`, `Error`, `Info` |
| `message` | *required* | Alert text |
| `dismissible` | `true` | Adds a dismiss button |

A null or empty `message` suppresses the element entirely, so binding straight to an optional model property is safe — no empty alert box appears when there is nothing to say:

```cshtml
<alert type="Error" message="@Model.ErrorMessage" />
```

### Pagination

```cshtml
<pagination model="Model.Products" href-format="/products?page={0}" />
```

| Attribute | Default | Description |
|-----------|---------|-------------|
| `model` | *required* | `IPagination<T>`, as returned by JC.Core's `ToPagedListAsync` |
| `href-format` | `"?page={0}"` | URL template; `{0}` is replaced with the page number |
| `max-pages` | `5` | Maximum visible page links before ellipsis |
| `previous-text` | `"&laquo;"` | Previous button text |
| `next-text` | `"&raquo;"` | Next button text |
| `show-first-last` | `true` | Shows first and last page buttons |
| `first-text` | `"First"` | First-page button text |
| `last-text` | `"Last"` | Last-page button text |
| `container-class` | — | Additional CSS classes on the `<nav>` element |

Output is suppressed when `TotalPages` is 1 or less, so a single-page result renders no controls.

`href-format` is a raw format string, not a route. Existing query parameters are not preserved — if the page also has filters or sorting, include them in the format so paging does not silently discard them:

```cshtml
<pagination model="Model.Products"
            href-format="@($"/products?search={Model.Search}&sort={Model.Sort}&page={{0}}")" />
```

### Breadcrumbs

```cshtml
<breadcrumb>
    <crumb label="Home" href="/" />
    <crumb label="Products" href="/products" />
    <crumb label="Widget" />
</breadcrumb>
```

The final `<crumb>` renders as the active page with `aria-current="page"`. Omit `href` on it — a link to the page you are already on is noise for screen reader users.

### Bug reporter

```cshtml
<bug-reporter endpoint="/Bug/ReportBug" />
```

| Attribute | Default | Description |
|-----------|---------|-------------|
| `endpoint` | *required* | POST endpoint receiving the report |
| `icon` | `"🐞"` | Icon on the floating button |
| `title` | `"Send Feedback"` | Form title text |
| `colour` | `"danger"` | Bootstrap contextual colour suffix (`border-{colour}`, `text-{colour}`, `btn-{colour}`) |
| `mask-request-path` | `false` | Masks the request path in the submitted metadata |
| `mask-query` | `true` | Masks the query string in the submitted metadata |

The widget posts JSON:

```json
{
    "type": "bug",
    "description": "The save button doesn't work",
    "metadata": "{\"RequestId\":\"abc\",\"Timestamp\":\"...\",\"RequestPath\":\"GET /orders/42\",\"Browser\":\"Chrome\",...}"
}
```

`metadata` carries `RequestMetadata.ToLogEntry()`. The request path is included by default so you can see which page a report came from, while the query string, client IP, origin, referer and city are masked. An anti-forgery token is sent in the `RequestVerificationToken` header when available, so the receiving endpoint should read the header rather than expect a form field.

This is the only component here that depends on another area: it needs [client profiling](ClientProfiling-Setup.md) registered and `UseClientProfiling()` in the pipeline. Without it the widget still renders and submits, but `metadata` arrives empty — which is most of what makes a bug report useful.

The widget carries `d-print-none`, so it does not appear in printed pages.

## Content sanitisation

### Sanitising user HTML

```csharp
var clean = ContentSanitiser.SanitiseContent(model.Body);
```

Strips user-authored HTML to an allowlist, removing scripts, event handlers, `javascript:` URLs and unknown elements.

Treat this as the **only** XSS control on that content. A rich-text editor's own sanitiser and paste cleanup run in the browser, and the value reaches you through an ordinary form field — anything holding a valid anti-forgery token can post straight past them. Editors offering a source-code view make arbitrary markup an expected input rather than an exotic attack.

**Sanitise on write, not on render.** The stored value is then trustworthy for every reader, including other applications sharing the database, instead of each render site having to remember. That is what keeps `@Html.Raw` honest:

```csharp
public async Task<IActionResult> OnPostAsync()
{
    article.Body = ContentSanitiser.SanitiseContent(Input.Body);
    await articles.UpdateAsync(article);
    return RedirectToPage();
}
```

Null, empty or whitespace-only input returns `null`, so a visually-empty editor stores "no content" rather than stray markup.

### Policies

Three presets cover the usual cases:

```csharp
// Comment-sized policy, reused across calls
var sanitiser = new ContentSanitiser(ContentSanitiserOptions.Basic());
var comment = sanitiser.Sanitise(model.Comment);

// The usual policy, minus inline images
var noImages = new ContentSanitiser(o =>
{
    o.AllowInlineImages = false;
    o.AllowedTags.Remove("img");
});

// Strip all HTML, keep the text
var plain = new ContentSanitiser(ContentSanitiserOptions.Empty()).Sanitise(model.Body);
```

`RichText()` is the default and suits a WYSIWYG editor's full output. `Basic()` allows inline formatting, lists, quotes and links but no images, tables, styles or classes, so the result cannot carry layout or colour into the page. `Empty()` with `KeepChildNodes` on reduces markup to its text.

Presets are methods rather than properties, so each call returns a fresh instance and adjusting one never affects another caller.

### Inline images

`AllowInlineImages`, on in `RichText()`, permits the `data:` scheme but narrows it to `data:image/*` on `<img>` elements. Allowing the scheme outright would also permit `data:text/html` on a link, which executes script.

Turning it off does not remove a `data` entry you added to `AllowedSchemes` yourself. That stays allowed, and unnarrowed, because you asked for it.

### Load-bearing allowlist entries

`class` is required, not cosmetic. An editor's image quick-toolbar stores Align, Caption and Display as theme classes, and its stylesheet is what positions them — strip the attribute and every aligned or captioned image silently loses its layout. Table styles work the same way. Narrow `AllowedClasses` to restrict which names survive rather than dropping the attribute:

```csharp
var sanitiser = new ContentSanitiser(o =>
{
    o.AllowedClasses.Add("text-start");
    o.AllowedClasses.Add("text-center");
    o.AllowedClasses.Add("table-striped");
});
```

`width`, `height` and `max-width` in `AllowedCssProperties` are what editors write onto images to keep them fluid. Drop them and that normalisation is undone on save.

The damage here is retroactive — content saved before the change keeps its markup, but the classes it relies on are stripped the next time it passes through the sanitiser.

### Anything not modelled

`Configure` hands you the underlying `HtmlSanitizer` after every other setting has been applied, so it can override them:

```csharp
var sanitiser = new ContentSanitiser(o =>
{
    o.Configure = s =>
    {
        s.AllowDataAttributes = true;
        s.RemovingTag += (_, e) => logger.LogDebug("Stripped <{Tag}>", e.Tag.NodeName);
    };
});
```

The `RemovingTag` event is the quickest way to find out why an editor's output is losing elements — log it in development and the missing allowlist entries announce themselves.

## Building HTML from code

### TableBuilder

```csharp
var html = new TableBuilder<User>()
    .AddColumn("Name", u => u.Name)
    .AddColumn("Email", u => u.Email)
    .AddColumn("Age", u => u.Age, cssClass: "text-end")
    .Build(users, "table table-striped table-hover");
```

Cell content is HTML-encoded automatically, so a user's display name containing markup cannot inject into the page. `cssClass` applies to both the `<th>` and the matching `<td>`.

### AlertHelper

For alerts built in code rather than markup — a service returning a formatted result, say:

```csharp
var html = AlertHelper.Success("Record saved.");
var warning = AlertHelper.Warning("Session expiring soon.", dismissible: false);
var error = AlertHelper.Error("Validation failed.");
var info = AlertHelper.Info("New version available.");
var dynamic = AlertHelper.ForType(alertType, message);
```

`ForType` takes the `AlertType` as a value, which suits mapping an outcome to an alert without a switch at the call site.

### BreadcrumbBuilder

```csharp
var html = new BreadcrumbBuilder()
    .Add("Home", "/")
    .Add("Products", "/products")
    .Add("Widget")
    .Build();
```

The last item renders as the active page. Implicit string conversion is supported, so the builder can be assigned straight to a string or passed to `Html.Raw()` without calling `Build()`.

Prefer the `<breadcrumb>` tag helper in views; this is for cases where the trail is computed — from a category hierarchy, for instance — and passing a built string to the view is simpler than passing the structure.

### HtmlHelper

Static helpers for assembling elements, primarily the pieces of a pagination control:

```csharp
var link = HtmlHelper.PaginationLink("3", "/products?page=3", isActive: true);
var item = HtmlHelper.PaginationItem(link, isActive: true);

var badge = HtmlHelper.CreateElement("span", "New",
    attributes: new Dictionary<string, string> { ["title"] = "Recently added" },
    classes: ["badge", "bg-primary"]);
```

**`CreateElement` inserts `content` as raw HTML, unencoded.** Everything else in this area encodes for you, so this is the exception worth remembering — pass user-supplied text through `WebUtility.HtmlEncode` or a sanitiser first.

`HtmlTagBuilder`, which these helpers use internally, has an internal constructor and no public factory, so it cannot be constructed from consuming code.

## Dropdowns

```csharp
// From an enum — text comes from ToDisplayName()
var statusOptions = DropdownHelper.FromEnum<OrderStatus>(selected: OrderStatus.InProgress);

// From a collection
var userOptions = DropdownHelper.FromCollection(
    users,
    textSelector: u => u.DisplayName,
    valueSelector: u => u.Id,
    selectedPredicate: u => u.Id == currentUserId
);

// From a dictionary
var options = DropdownHelper.FromDictionary(
    new Dictionary<string, string> { ["gb"] = "United Kingdom", ["us"] = "United States" },
    selected: "gb"
);

// Countries, from JC.Core's CountryHelper
var countries = DropdownHelper.GetCountryDropdown(selected: "GB");

// Add a placeholder to any of the above
var withPlaceholder = countries.WithPlaceholder("Select a country...");
```

`FromEnum` renders member names through JC.Core's `ToDisplayName()`, so `InProgress` becomes "In Progress" and acronyms are preserved. Where a member needs text the normaliser cannot produce, put a `[Description]` attribute on it and build the list with `FromCollection` instead.

`WithPlaceholder` is an extension on `List<SelectListItem>`, so it chains onto any of the builders. Its default value is an empty string, which pairs with a `[Required]` model property to make "nothing chosen" a validation failure rather than a silent default.

## QR codes

```csharp
// SVG (the default) — embed the markup directly
var svg = new QrCodeHelper().GenerateQrCode("https://example.com");

// Base64 PNG — use as an <img> source
var dataUri = new QrCodeHelper(QrCodeFormat.Base64, pixelsPerModule: 15)
    .GenerateQrCode("https://example.com");

// Higher error correction, for codes that may be damaged or overlaid
var robust = new QrCodeHelper(QrCodeFormat.Svg, 10, QRCodeGenerator.ECCLevel.H);
```

SVG scales without blurring and is usually the better choice on a web page. Base64 PNG suits contexts that cannot render inline SVG, such as email clients.

Raise `pixelsPerModule` for a physically larger code, and the error correction level when the code will be printed, partially obscured, or has a logo placed over it — `H` tolerates 30% damage, at the cost of a denser pattern that needs more space to stay scannable.

## Model state

`ModelStateWrapper` removes the repeated prefix when Razor Pages binds through an `Input` model:

```csharp
var state = new ModelStateWrapper(ModelState);   // default prefix "Input."

if (state.HasError("Email"))
{
    var message = state["Email"];                // reads ModelState["Input.Email"]
}

state.AddModelError("Email", "Email is already taken.");   // adds to "Input.Email"

// Custom prefix
var formState = new ModelStateWrapper(ModelState, prefix: "Form.");

// No prefix — for MVC controllers binding at the top level
var flat = new ModelStateWrapper(ModelState, ignorePrefix: true);
```

The wrapper reads and writes the same `ModelStateDictionary`, so errors added through it surface in `asp-validation-for` tags as normal. It is a convenience over the key strings, not a separate store.

## Next steps

- [Setup](UI-Setup.md) — `_ViewImports` registration, sanitiser options, and the bug reporter's pipeline requirements.
- [API Reference](UI-API.md)
