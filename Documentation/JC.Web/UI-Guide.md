# JC.Web: UI — Guide

Covers the tag helpers, the framework dictionary that decides their class names, rendering untrusted HTML safely, programmatic HTML building, dropdown construction, QR codes, and model state handling. See [Setup](UI-Setup.md) for `AddUI` and the `_ViewImports` registration.

## Tag helpers

All four require `@addTagHelper *, JC.Web` in `_ViewImports.cshtml`. Without it Razor renders the element name literally into the page rather than failing, so an unstyled `<alert>` appearing in the output means the directive is missing.

They also need `AddUI()` to have been called, because each one takes a constructor dependency from the container. A missing directive shows up as literal text; a missing `AddUI` shows up as a resolution exception when the page renders.

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

A null, empty or whitespace-only `message` suppresses the element entirely, so binding straight to an optional model property is safe — no empty alert box appears when there is nothing to say:

```cshtml
<alert type="Error" message="@Model.ErrorMessage" />
```

**`message` is inserted as raw HTML.** It is passed through unencoded so an alert can contain a link or bold text. Encode anything user-supplied before binding it.

**A dismissible alert's close button carries `data-bs-dismiss="alert"` under every framework.** This is the one place JC.Web emits a Bootstrap behavioural attribute, and it stays because the alternative is shipping JavaScript for Bootstrap users who already have working behaviour from Bootstrap's own bundle. The class on the button always comes from the dictionary; only the attribute is fixed.

Under Bootstrap it works with no further action. Under Tailwind or jc-tailwind-ui the attribute is inert markup until something reads it, so dismissal needs one handler of your own:

```html
<script type="module">
  document.querySelectorAll('[data-bs-dismiss="alert"]').forEach(btn =>
      btn.addEventListener('click', () => btn.closest('[role="alert"]')?.remove()));
</script>
```

Nothing else in JC.Web depends on a framework's JavaScript — `<bug-reporter>` ships its own vanilla script, and `<pagination>`, `<breadcrumb>` and non-dismissible alerts are static markup.

### Pagination

```cshtml
<pagination model="Model.Products" href-format="/products?page={0}" />
```

| Attribute | Default | Description |
|-----------|---------|-------------|
| `model` | *required* | `IPagination<object>`, as returned by JC.Core's `ToPagedListAsync` |
| `href-format` | `"?page={0}"` | URL template; `{0}` is replaced with the page number |
| `max-pages` | `5` | Maximum visible page links before ellipsis |
| `previous-text` | `"&laquo;"` | Previous button text |
| `next-text` | `"&raquo;"` | Next button text |
| `show-first-last` | `true` | Shows first and last page buttons |
| `first-text` | `"First"` | First-page button text |
| `last-text` | `"Last"` | Last-page button text |
| `container-class` | `null` | Additional CSS classes on the `<nav>` element |

Output is suppressed when `model` is null or `TotalPages` is 1 or less, so a single-page result renders no controls.

`href-format` is a raw format string, not a route. Existing query parameters are not preserved — if the page also has filters or sorting, include them in the format so paging does not silently discard them:

```cshtml
<pagination model="Model.Products"
            href-format="@($"/products?search={Model.Search}&sort={Model.Sort}&page={{0}}")" />
```

`container-class` is combined with the framework's own nav class rather than replacing it. Under Bootstrap that class is empty, so the attribute value stands alone and the `class` attribute is omitted entirely when neither is set.

### Breadcrumbs

```cshtml
<breadcrumb>
    <crumb label="Home" href="/" />
    <crumb label="Products" href="/products" />
    <crumb label="Widget" />
</breadcrumb>
```

The final `<crumb>` renders as the active page with `aria-current="page"`. Omit `href` on it — a link to the page you are already on is noise for screen reader users.

A `<breadcrumb>` containing no `<crumb>` suppresses its own output, so a conditionally-empty trail leaves nothing behind. Labels and URLs are HTML-encoded.

### Bug reporter

```cshtml
<bug-reporter endpoint="/Bug/ReportBug" />
```

| Attribute | Default | Description |
|-----------|---------|-------------|
| `endpoint` | *required* | POST endpoint receiving the report |
| `icon` | `"🐞"` | Icon on the floating button |
| `title` | `"Send Feedback"` | Form title text |
| `colour` | dictionary's default | Contextual colour. Falls back to the framework's `DefaultColour` when unset |
| `mask-request-path` | `false` | Masks the request path in the submitted metadata |
| `mask-query` | `true` | Masks the query string in the submitted metadata |

A missing or whitespace `endpoint` throws `InvalidOperationException` when the page renders.

**What `colour` means depends on the registered framework**, because the dictionary owns how a colour becomes a class:

| Framework | Value to pass | Produces |
|---|---|---|
| Bootstrap | `danger`, `info`, … | `border-danger`, `text-danger`, `btn-danger` |
| Tailwind | `red-600`, `sky-600`, … | `border-red-600`, `text-red-600`, `bg-red-600` |
| jc-tailwind-ui | `danger`, `info`, or a custom tone | `tone-danger` on the panel, inherited by its contents |

The widget posts JSON:

```json
{
    "type": "bug",
    "description": "The save button doesn't work",
    "metadata": "{\"RequestId\":\"abc\",\"Timestamp\":\"...\",\"RequestPath\":\"GET /orders/42\",\"Browser\":\"Chrome\",...}"
}
```

`metadata` carries `RequestMetadata.ToLogEntry()`. The request path is included by default so you can see which page a report came from, while the query string is masked. An anti-forgery token is sent in the `RequestVerificationToken` header when available, so the receiving endpoint should read the header rather than expect a form field.

This is the only component here that depends on another area: it needs [client profiling](ClientProfiling-Setup.md) registered and `UseClientProfiling()` in the pipeline. Without it the widget still renders and submits, but `metadata` arrives empty — which is most of what makes a bug report useful.

The widget ships its own vanilla JavaScript rather than relying on the CSS framework's, so it works the same under all three. Its toggle button carries the dictionary's print-hiding class, so it does not appear in printed pages.

## The framework dictionary

Class names are not hardcoded in the tag helpers. Each is read from `IWebFrameworkDictionary`, resolved from the container according to the `UIFramework` passed to `AddUI`. Three implementations ship: `BootstrapDictionary`, `TailwindDictionary` and `CustomJCTailwindDictionary`.

### Reading classes in your own code

Inject the dictionary wherever you need the same class names the tag helpers use:

```csharp
public class ReportPageModel(IWebFrameworkDictionary dictionary) : PageModel
{
    public string TableClass => dictionary.Table.Table;
    public string ActiveCrumb => dictionary.Breadcrumb.ActiveItem;
}
```

Every value is a **complete class attribute value**, not a token — `Alert.Dismissible` is `"alert-dismissible fade show"` under Bootstrap, three classes in one string. States are complete too: `Pagination.ActiveItem` is `"page-item active"`, not just `"active"`, so a framework whose active item shares nothing with its inactive one can express that.

Every property defaults to an empty string. An unset entry produces no class rather than an error, and the builders skip empty values when composing a `class` attribute — so a dictionary that leaves `Table.Head` blank emits a bare `<thead>`.

### Adding a dictionary for another package

A package layered on JC.Web declares its own contract rather than extending `IWebFrameworkDictionary`, so adding a component never requires a JC.Web change:

```csharp
public interface IReportingFrameworkDictionary : IFrameworkDictionary
{
    ChartClasses Chart { get; }
}

// In that package's own AddX method
services.AddUI(framework);
services.AddFrameworkDictionary<IReportingFrameworkDictionary>(f => f switch
{
    UIFramework.Tailwind => new TailwindReportingDictionary(),
    _ => new BootstrapReportingDictionary()
});
```

`AddUI` uses `TryAdd`, so calling it from a downstream package is harmless when the application has already chosen a framework — the first registration stands, and every dictionary resolves from the same `UIFrameworkService`, so they cannot disagree.

**Group class names into records, one per component.** Adding a property to an existing record compiles against every dictionary already written, because the property defaults to empty. Adding a whole new group to the contract is the breaking case, and it is far rarer.

### Icons

Icons are a separate choice from the CSS framework, because an icon set is a different library — a Tailwind application may still use Bootstrap Icons. They resolve from `IIconDictionary` implementations registered with `AddIconDictionary`, selected by `UIFrameworkService.IconFramework`.

JC.Web registers no icon dictionary of its own; none of its components render a glyph.

## Rendering untrusted HTML

### Where sanitisation lives

JC.Web does not sanitise. `ContentSanitiser` and its policies belong to JC.Content, and are documented in [JC.Content — Guide](../JC.Content/Guide.md).

What remains JC.Web's concern is the handful of places that write raw HTML on your behalf. `HtmlHelper`'s element-building methods insert their `content` argument verbatim, which is the exception in an area that otherwise encodes automatically:

```csharp
// content is written as-is, so it must already be safe
var cell = html.CreateElement("td", WebUtility.HtmlEncode(userSuppliedText));
```

**Sanitise on write, not on render.** The stored value is then trustworthy for every reader, including other applications sharing the database, instead of each render site having to remember. That is what keeps `@Html.Raw` and these helpers honest.

Treat the sanitiser as the **only** XSS control on user-authored HTML. A rich-text editor's own cleanup runs in the browser, and the value reaches you through an ordinary form field — anything holding a valid anti-forgery token can post straight past it.

## Building HTML from code

### TableBuilder

`TableBuilder<T>` holds per-use state and is generic, so it is constructed per table rather than resolved from the container. Inject the dictionary and pass it in:

```csharp
public class ExportService(IWebFrameworkDictionary dictionary)
{
    public string BuildUserTable(IEnumerable<User> users)
        => new TableBuilder<User>(dictionary)
            .AddColumn("Name", u => u.Name)
            .AddColumn("Email", u => u.Email)
            .AddColumn("Age", u => u.Age, cssClass: "text-end")
            .Build(users, "table table-striped table-hover");
}
```

Cell content and headers are HTML-encoded automatically, so a user's display name containing markup cannot inject into the page. `cssClass` applies to both the `<th>` and the matching `<td>`.

The `tableClass` argument to `Build` **replaces** the dictionary's table class rather than adding to it. Omit it to take the framework's own.

### AlertHelper

Registered as a singleton by `AddUI`. Inject it for alerts built in code rather than markup — a service returning a formatted result, say:

```csharp
public class SaveResultPresenter(AlertHelper alerts)
{
    public string Render(bool succeeded)
        => succeeded
            ? alerts.Success("Record saved.")
            : alerts.Error("Validation failed.");
}
```

```csharp
var warning = alerts.Warning("Session expiring soon.", dismissible: false);
var info = alerts.Info("New version available.");
var dynamic = alerts.ForType(alertType, message);
```

`ForType` takes the `AlertType` as a value, which suits mapping an outcome to an alert without a switch at the call site. All five methods insert `message` as raw HTML.

### BreadcrumbBuilder

Like `TableBuilder<T>`, it accumulates state and takes the dictionary directly:

```csharp
var html = new BreadcrumbBuilder(dictionary)
    .Add("Home", "/")
    .Add("Products", "/products")
    .Add("Widget")
    .Build();
```

The last item renders as the active page. `Build()` returns an empty string when nothing has been added. Implicit string conversion is supported, so the builder can be assigned straight to a string or passed to `Html.Raw()` without calling `Build()`.

Prefer the `<breadcrumb>` tag helper in views; this is for cases where the trail is computed — from a category hierarchy, for instance — and passing a built string to the view is simpler than passing the structure.

### HtmlHelper

Registered as a singleton by `AddUI`. Assembles elements, primarily the pieces of a pagination control:

```csharp
public class PagerPartial(HtmlHelper html)
{
    public string BuildLink(int page, bool isCurrent)
    {
        var link = html.PaginationLink(page.ToString(), $"/products?page={page}", isActive: isCurrent);
        return html.PaginationItem(link, isActive: isCurrent);
    }
}
```

```csharp
var badge = html.CreateElement("span", "New",
    attributes: new Dictionary<string, string> { ["title"] = "Recently added" },
    classes: ["badge", "bg-primary"]);
```

**`CreateElement` inserts `content` as raw HTML, unencoded.** Everything else in this area encodes for you, so this is the exception worth remembering — pass user-supplied text through `WebUtility.HtmlEncode` or a sanitiser first. Attribute *values* are encoded.

`PaginationListClass` and `PaginationNavClass` expose the framework's list and nav classes for code assembling its own pagination markup.

`HtmlTagBuilder`, which these helpers use internally, has an internal constructor and no public factory, so it cannot be constructed from consuming code.

## Dropdowns

`DropdownHelper` is static and needs no registration.

```csharp
// From an enum — text comes from ToDisplayName(), value is the underlying integer
var statusOptions = DropdownHelper.FromEnum<OrderStatus>(selected: OrderStatus.InProgress);

// From a collection
var userOptions = DropdownHelper.FromCollection(
    users,
    textSelector: u => u.DisplayName,
    valueSelector: u => u.Id,
    selectedPredicate: u => u.Id == currentUserId
);

// From a dictionary — key becomes the option value, value becomes the display text
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

**`FromEnum` sets the option value to the enum's integer**, not its name, so the posted value binds back to the enum by numeric value.

`GetCountryDropdown` matches `selected` case-insensitively, so `"gb"` and `"GB"` both work.

`WithPlaceholder` is an extension on `List<SelectListItem>`, so it chains onto any of the builders. Its default text is `"Please select..."` and its default value is an empty string, which pairs with a `[Required]` model property to make "nothing chosen" a validation failure rather than a silent default.

**`WithPlaceholder` mutates the list in place** and returns the same instance. Chaining reads like a copy, but a list shared between two dropdowns gains a placeholder in both.

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

SVG scales without blurring and is usually the better choice on a web page. Base64 PNG suits contexts that cannot render inline SVG, such as email clients. The returned Base64 string carries the `data:image/png;base64,` prefix already, exposed as `QrCodeHelper.Base64ImgPrefix`.

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

// Custom prefix — a trailing dot is added if you omit it
var formState = new ModelStateWrapper(ModelState, prefix: "Form");

// No prefix — for MVC controllers binding at the top level
var flat = new ModelStateWrapper(ModelState, ignorePrefix: true);
```

The indexer returns an empty string when there is no error for the key, so it can be written straight into a view without a null check. `GetErrors` returns every message for one key; `GetAllErrors` returns the whole dictionary.

The wrapper reads and writes the same `ModelStateDictionary`, so errors added through it surface in `asp-validation-for` tags as normal. It is a convenience over the key strings, not a separate store.

## Next steps

- [Setup](UI-Setup.md) — `AddUI`, framework selection, and the bug reporter's pipeline requirements.
- [API Reference](UI-API.md)
