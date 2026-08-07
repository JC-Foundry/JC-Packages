# JC.Web: UI — API reference

Complete reference of all public types, properties, and methods in the JC.Web UI area — content sanitisation, HTML builders, dropdown helpers, QR codes, model state, and tag helpers. See [Setup](UI-Setup.md) for registration and [Guide](UI-Guide.md) for usage examples.

> **Note:** Options classes are documented in [Setup](UI-Setup.md), not here. This area registers nothing in the service container, so there are no registration extensions to exclude — `ContentSanitiserOptions` is covered under [Setup](UI-Setup.md#contentsanitiseroptions).

---

# Enums

## AlertType

**Namespace:** `JC.Web.UI.HTML`

The type of Bootstrap alert to render.

| Member | Value | Description |
|--------|-------|-------------|
| `Success` | `0` | A success alert (green). |
| `Warning` | `1` | A warning alert (yellow). |
| `Error` | `2` | An error or danger alert (red). |
| `Info` | `3` | An informational alert (blue). |

---

## QrCodeFormat

**Namespace:** `JC.Web.UI.Helpers`

The output format for generated QR codes.

| Member | Value | Description |
|--------|-------|-------------|
| `Svg` | `0` | SVG markup string. |
| `Base64` | `1` | Base64-encoded PNG data URI. |

---

# Helpers

## ContentSanitiser

**Namespace:** `JC.Web.UI.Helpers`

Server-side sanitisation for HTML authored by a user, typically the output of a rich-text editor. Everything outside the configured allowlist is removed: scripts, event handlers, `javascript:` URLs and unknown elements. The allowlists come from [`ContentSanitiserOptions`](UI-Setup.md#contentsanitiseroptions).

Treat this as the only XSS control on that content — an editor's own sanitiser runs in the browser and can be bypassed by posting directly. Sanitise on write rather than on render, so the stored value is trustworthy for every reader.

A fresh underlying sanitiser is built per call: the library documents no thread-safety guarantee, and the options are mutable, so a shared instance could otherwise be reconfigured mid-sanitise.

### Constructors

#### ContentSanitiser()

Creates a sanitiser using `ContentSanitiserOptions.RichText()`.

---

#### ContentSanitiser(ContentSanitiserOptions options)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `options` | `ContentSanitiserOptions` | — | The allowlists to enforce. |

Creates a sanitiser using the supplied options. Throws `ArgumentNullException` if `options` is null.

---

#### ContentSanitiser(Action\<ContentSanitiserOptions\> configure)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configure` | `Action<ContentSanitiserOptions>` | — | Receives the rich-text options to modify. |

Creates a sanitiser from `ContentSanitiserOptions.RichText()` with adjustments applied — the shorthand for "the usual policy, but…". Throws `ArgumentNullException` if `configure` is null.

### Methods

#### Sanitise(string? html)

**Returns:** `string?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `html` | `string?` | — | The untrusted HTML to sanitise. |

Returns the HTML with everything outside this instance's allowlist removed, or `null` when `html` is null, empty or whitespace.

---

#### SanitiseContent(string? html)

**Returns:** `string?`

**Static.**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `html` | `string?` | — | The untrusted HTML to sanitise. |

Sanitises against `ContentSanitiserOptions.RichText()` without constructing an instance. Equivalent to `new ContentSanitiser().Sanitise(html)`.

---

## QrCodeHelper

**Namespace:** `JC.Web.UI.Helpers`

Generates QR codes as SVG markup or a base64 PNG data URI, using QRCoder.

### Constants

| Constant | Type | Value | Description |
|----------|------|-------|-------------|
| `Base64ImgPrefix` | `string` | `"data:image/png;base64,"` | The data URI prefix applied to base64 PNG output. |

### Constructors

#### QrCodeHelper()

Creates a helper with SVG format, 10 pixels per module, and error correction level `M` (15%).

---

#### QrCodeHelper(QrCodeFormat format, int pixelsPerModule, QRCodeGenerator.ECCLevel eccLevel = QRCodeGenerator.ECCLevel.M)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `format` | `QrCodeFormat` | — | Output format: SVG markup or base64 PNG. |
| `pixelsPerModule` | `int` | — | Size of each QR module in pixels. Clamped to 10 when zero or negative. |
| `eccLevel` | `QRCodeGenerator.ECCLevel` | `M` | Error correction level: `L` 7%, `M` 15%, `Q` 25%, `H` 30%. |

### Methods

#### GenerateQrCode(string content)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string` | — | The data to encode. |

Generates the QR code, returning SVG markup or a base64 PNG data URI according to the configured format. Throws `ArgumentException` if `content` is empty.

---

## DropdownHelper

**Namespace:** `JC.Web.UI.Helpers`

Static helpers building `SelectListItem` collections from enums, collections and dictionaries.

### Methods

#### ToDropdownEntry(string text, string value, bool selected = false)

**Returns:** `SelectListItem`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `text` | `string` | — | The display text. |
| `value` | `string` | — | The option value. |
| `selected` | `bool` | `false` | Whether this item is selected. |

Creates a single `SelectListItem`.

---

#### FromEnum\<T\>(T? selected = null)

**Returns:** `List<SelectListItem>`

**Constraint:** `T : struct, Enum`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `selected` | `T?` | `null` | The currently selected value, if any. |

Converts every value of the enum to a dropdown item. Display text comes from JC.Core's `EnumExtensions.ToDisplayName`, and the option value is the member's underlying integer.

---

#### FromCollection\<T\>(IEnumerable\<T\> items, Func\<T, string\> textSelector, Func\<T, string\> valueSelector, Func\<T, bool\>? selectedPredicate = null)

**Returns:** `List<SelectListItem>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `items` | `IEnumerable<T>` | — | The source collection. |
| `textSelector` | `Func<T, string>` | — | Extracts the display text. |
| `valueSelector` | `Func<T, string>` | — | Extracts the option value. |
| `selectedPredicate` | `Func<T, bool>?` | `null` | Determines which items are selected. |

Converts a collection to dropdown items using the supplied selectors.

---

#### FromDictionary(Dictionary\<string, string\> items, string? selected = null)

**Returns:** `List<SelectListItem>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `items` | `Dictionary<string, string>` | — | Source dictionary; key becomes the option value, value becomes the display text. |
| `selected` | `string?` | `null` | The key of the selected item. |

Converts a dictionary to dropdown items.

---

#### GetCountryDropdown(string? selected = null)

**Returns:** `List<SelectListItem>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `selected` | `string?` | `null` | ISO 3166-1 alpha-2 code of the selected country. |

Builds a country dropdown from JC.Core's `CountryHelper`. Selection comparison is case-insensitive.

---

#### WithPlaceholder(this List\<SelectListItem\> items, string text = "Please select...", string value = "")

**Returns:** `List<SelectListItem>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `items` | `List<SelectListItem>` | — | The existing dropdown items. |
| `text` | `string` | `"Please select..."` | The placeholder display text. |
| `value` | `string` | `""` | The placeholder value. |

Extension method inserting a placeholder at index 0. The default empty value pairs with a `[Required]` model property so that leaving the placeholder selected fails validation.

---

## ModelStateWrapper

**Namespace:** `JC.Web.UI.Helpers`

Wraps a `ModelStateDictionary` with automatic key prefixing, for Razor Pages and MVC scenarios where bound properties sit under a prefix such as `"Input."`. Reads and writes the underlying dictionary directly rather than holding its own state.

### Constructor

#### ModelStateWrapper(ModelStateDictionary modelState, string? prefix = null, bool ignorePrefix = false)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `modelState` | `ModelStateDictionary` | — | The dictionary to wrap. |
| `prefix` | `string?` | `null` | The key prefix. Defaults to `"Input."`. A trailing `.` is appended when missing. |
| `ignorePrefix` | `bool` | `false` | Disables prefixing entirely. |

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `IsValid` | `bool` | — | get; | Whether the underlying model state is valid. |

### Indexer

#### this[string key]

**Returns:** `string`

Returns the first error message for the key with the prefix applied, or an empty string when there are none.

### Methods

#### AddModelError(string key, string errorMessage)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `key` | `string` | — | The property name, without prefix. |
| `errorMessage` | `string` | — | The error message. |

Adds a model error against the prefixed key. Because it writes to the wrapped dictionary, the error surfaces through `asp-validation-for` as normal.

---

#### HasError(string key)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `key` | `string` | — | The property name, without prefix. |

Returns whether the prefixed key has any validation errors.

---

#### GetErrors(string key)

**Returns:** `IEnumerable<string>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `key` | `string` | — | The property name, without prefix. |

Returns every error message for the key, or an empty collection when there are none.

---

#### GetAllErrors()

**Returns:** `Dictionary<string, string[]>`

Returns all validation errors across every key, mapping **full** keys — prefix included — to their error message arrays. Unlike the other members, this does not strip the prefix from the keys it returns.

---

## AlertHelper

**Namespace:** `JC.Web.UI.HTML`

Static helper rendering Bootstrap 5 alert markup from code.

### Methods

#### Success(string message, bool dismissible = true)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `message` | `string` | — | The alert message content. May contain HTML. |
| `dismissible` | `bool` | `true` | Whether the alert can be dismissed. |

Renders a Bootstrap success alert (`alert-success`).

---

#### Warning(string message, bool dismissible = true)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `message` | `string` | — | The alert message content. |
| `dismissible` | `bool` | `true` | Whether the alert can be dismissed. |

Renders a Bootstrap warning alert (`alert-warning`).

---

#### Error(string message, bool dismissible = true)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `message` | `string` | — | The alert message content. |
| `dismissible` | `bool` | `true` | Whether the alert can be dismissed. |

Renders a Bootstrap danger alert (`alert-danger`).

---

#### Info(string message, bool dismissible = true)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `message` | `string` | — | The alert message content. |
| `dismissible` | `bool` | `true` | Whether the alert can be dismissed. |

Renders a Bootstrap info alert (`alert-info`).

---

#### ForType(AlertType type, string message, bool dismissible = true)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `type` | `AlertType` | — | The alert type. |
| `message` | `string` | — | The alert message content. May contain HTML. |
| `dismissible` | `bool` | `true` | Whether the alert can be dismissed. |

Renders an alert for the supplied type, allowing the type to be chosen at runtime. Dismissible alerts add the `alert-dismissible`, `fade` and `show` classes along with a close button.

---

## BreadcrumbBuilder

**Namespace:** `JC.Web.UI.HTML`

Fluent builder for Bootstrap 5 breadcrumb navigation. The last item added always renders as the active page. Supports implicit conversion to `string`, so an instance can be used wherever a string is expected without calling `Build()`.

### Methods

#### Add(string label, string? url = null)

**Returns:** `BreadcrumbBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `label` | `string` | — | The display text for this item. |
| `url` | `string?` | `null` | The URL to link to. When null, the item renders as plain text. |

Appends a breadcrumb item and returns the builder for chaining.

---

#### Build()

**Returns:** `string`

Builds the complete breadcrumb as a `<nav>` containing an `<ol class="breadcrumb">`. The final item carries `aria-current="page"`. Labels and URLs are HTML-encoded. Returns an empty string when no items have been added.

---

## HtmlHelper

**Namespace:** `JC.Web.UI.HTML`

Static helper for assembling HTML elements, with specific methods for the parts of a pagination control.

### Methods

#### CreateElement(string tagName, string content = "", bool isActive = false, bool isDisabled = false, Dictionary\<string, string\>? attributes = null, params string[] classes)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tagName` | `string` | — | The HTML tag name. |
| `content` | `string` | `""` | The inner HTML content. |
| `isActive` | `bool` | `false` | Adds the `active` CSS class. |
| `isDisabled` | `bool` | `false` | Adds the `disabled` CSS class. |
| `attributes` | `Dictionary<string, string>?` | `null` | Additional HTML attributes. Values are encoded. |
| `classes` | `params string[]` | — | Additional CSS classes. |

Creates an element with optional state classes, attributes and CSS classes.

**`content` is inserted as raw HTML and is not encoded.** This is the exception in an area that otherwise encodes automatically — pass user-supplied text through `WebUtility.HtmlEncode` or `ContentSanitiser` before it reaches this method.

---

#### PaginationItem(string content, bool isActive = false, bool isDisabled = false)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string` | — | Inner HTML, usually a link produced by `PaginationLink`. |
| `isActive` | `bool` | `false` | Whether this is the active page. |
| `isDisabled` | `bool` | `false` | Whether the item is disabled. |

Builds a pagination list item (`<li class="page-item">`) with optional active and disabled states.

---

#### PaginationLink(string text, string href, string? buttonClass = null, bool isActive = false)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `text` | `string` | — | The link text. |
| `href` | `string` | — | The URL to navigate to. |
| `buttonClass` | `string?` | `null` | Additional CSS classes for the link. |
| `isActive` | `bool` | `false` | Adds `aria-current="page"`. |

Builds a pagination link (`<a class="page-link">`).

---

## HtmlTagBuilder

**Namespace:** `JC.Web.UI.HTML`

Fluent builder for HTML tags with attributes, classes and content, supporting implicit conversion to `string`.

**The constructor is internal and no public member returns an instance**, so this type cannot be used from consuming code. It backs `AlertHelper` and `HtmlHelper` internally, and is public only because those helpers share the assembly. Its members are listed here for completeness; use `HtmlHelper.CreateElement` for equivalent functionality.

### Methods

#### AddClass(string className)

**Returns:** `HtmlTagBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `className` | `string` | — | The CSS class name. Empty or whitespace names are ignored. |

Adds a CSS class to the tag.

---

#### AddAttribute(string name, string value)

**Returns:** `HtmlTagBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The attribute name. |
| `value` | `string` | — | The attribute value. HTML-encoded in the output. |

Adds or replaces an HTML attribute.

---

#### AddActiveAttribute()

**Returns:** `HtmlTagBuilder`

Adds the `active` CSS class.

---

#### AddCurrentPageAttribute()

**Returns:** `HtmlTagBuilder`

Adds the `aria-current="page"` attribute.

---

#### AddDisabledClass()

**Returns:** `HtmlTagBuilder`

Adds the `disabled` CSS class.

---

#### SetContent(string content)

**Returns:** `HtmlTagBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string` | — | The text content. HTML-encoded. |

Sets the inner text content, replacing anything previously set.

---

#### SetRawContent(string rawHtml)

**Returns:** `HtmlTagBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `rawHtml` | `string` | — | The raw HTML content. Not encoded. |

Sets the inner HTML without encoding. Do not pass unsanitised user input.

---

#### Build()

**Returns:** `string`

Builds and returns the complete HTML tag.

---

## TableBuilder\<T\>

**Namespace:** `JC.Web.UI.HTML`

Fluent builder rendering a Bootstrap table from a collection. Header text and cell values are HTML-encoded, so values drawn from user data cannot inject markup.

### Methods

#### AddColumn(string header, Func\<T, string?\> valueSelector, string? cssClass = null)

**Returns:** `TableBuilder<T>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `header` | `string` | — | The column header text. |
| `valueSelector` | `Func<T, string?>` | — | Extracts the cell value from each item. |
| `cssClass` | `string?` | `null` | CSS class applied to both the `<th>` and its `<td>` cells. |

Adds a column with a string value selector.

---

#### AddColumn(string header, Func\<T, object?\> valueSelector, string? cssClass = null)

**Returns:** `TableBuilder<T>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `header` | `string` | — | The column header text. |
| `valueSelector` | `Func<T, object?>` | — | Extracts the cell value, converted via `ToString`. |
| `cssClass` | `string?` | `null` | CSS class applied to both the `<th>` and its `<td>` cells. |

Adds a column with an object value selector.

---

#### Build(IEnumerable\<T\> items, string? tableClass = null)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `items` | `IEnumerable<T>` | — | The rows to render. |
| `tableClass` | `string?` | `null` | CSS classes for the `<table>` element. Falls back to `"table"` when null or whitespace. |

Builds the complete table. Columns render in the order they were added.

---

# Tag helpers

All require `@addTagHelper *, JC.Web` in `_ViewImports.cshtml`. Without it Razor emits the element name literally rather than raising an error.

## AlertTagHelper

**Namespace:** `JC.Web.UI.TagHelpers`

Renders a Bootstrap 5 alert. Targets the `<alert>` element, self-closing. Suppresses output entirely when `Message` is null or whitespace, so binding to an optional model property produces no empty alert.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Type` | `AlertType` | `Info` | get; set; | The alert type. HTML attribute: `type`. |
| `Message` | `string?` | `null` | get; set; | The alert message content. HTML attribute: `message`. |
| `Dismissible` | `bool` | `true` | get; set; | Whether the alert is dismissible. HTML attribute: `dismissible`. |

---

## PaginationTagHelper

**Namespace:** `JC.Web.UI.TagHelpers`

Renders Bootstrap pagination from an `IPagination<T>` model. Targets the `<pagination>` element, self-closing. Renders previous and next links, numbered page buttons with ellipsis once `MaxVisiblePages` is exceeded, and optional first and last links. Suppresses output when the model is null or has one page or fewer.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Model` | `IPagination<object>?` | `null` | get; set; | The pagination model. HTML attribute: `model`. |
| `HrefFormat` | `string` | `"?page={0}"` | get; set; | URL format string with `{0}` as the page number. HTML attribute: `href-format`. |
| `MaxVisiblePages` | `int` | `5` | get; set; | Page links shown before ellipsis. HTML attribute: `max-pages`. |
| `PreviousText` | `string` | `"&laquo;"` | get; set; | Text for the previous link. HTML attribute: `previous-text`. |
| `NextText` | `string` | `"&raquo;"` | get; set; | Text for the next link. HTML attribute: `next-text`. |
| `ShowFirstLast` | `bool` | `true` | get; set; | Whether first and last links are shown. HTML attribute: `show-first-last`. |
| `FirstText` | `string` | `"First"` | get; set; | Text for the first-page link. HTML attribute: `first-text`. |
| `LastText` | `string` | `"Last"` | get; set; | Text for the last-page link. HTML attribute: `last-text`. |
| `ContainerClass` | `string?` | `null` | get; set; | Additional CSS classes for the `<nav>` container. HTML attribute: `container-class`. |

`HrefFormat` is a plain format string, not a route, so existing query parameters are not carried over — include any filter or sort values in the format itself.

---

## BreadcrumbTagHelper

**Namespace:** `JC.Web.UI.TagHelpers`

Renders Bootstrap 5 breadcrumb navigation from nested `<crumb>` elements. Targets the `<breadcrumb>` element. The last crumb renders as the active page. Suppresses output when no crumbs are supplied.

Collects its children by exposing a list through `TagHelperContext.Items`, which each `CrumbTagHelper` appends to, then delegates rendering to `BreadcrumbBuilder`.

---

## CrumbTagHelper

**Namespace:** `JC.Web.UI.TagHelpers`

A single breadcrumb item. Targets the `<crumb>` element, self-closing, and must be nested inside `<breadcrumb>`. Suppresses its own output, contributing its values to the parent instead.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Label` | `string` | `""` | get; set; | The display text. HTML attribute: `label`. |
| `Href` | `string?` | `null` | get; set; | The URL. When omitted, the item renders as plain text. HTML attribute: `href`. |

---

## BugReporterTagHelper

**Namespace:** `JC.Web.UI.TagHelpers`

Renders a floating bug reporter widget — a toggle button, a report form taking a type and description, and the JavaScript that POSTs the report. Targets the `<bug-reporter>` element, self-closing. Throws `InvalidOperationException` when `Endpoint` is not set. Assumes Bootstrap 5, and carries `d-print-none` so it does not appear in printed output.

Attaches `RequestMetadata` to the payload and sends an anti-forgery token in the `RequestVerificationToken` header when one is available. This is the only component in this area depending on another — without [client profiling](ClientProfiling-Setup.md) registered and its middleware in the pipeline, the widget still renders and submits but the metadata is empty.

Metadata is serialised via `RequestMetadata.ToLogEntry(maskPath: MaskRequestPath, maskQuery: MaskQuery)`. Client IP, origin, referer and city keep their default masking and cannot be unmasked here — only the request path and query string are configurable, and by default the path is **shown** while the query string is **masked**.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Endpoint` | `string?` | `null` | get; set; | The POST endpoint receiving reports. Required. HTML attribute: `endpoint`. |
| `Icon` | `string` | `"🐞"` | get; set; | Icon on the floating button. HTML attribute: `icon`. |
| `Title` | `string` | `"Send Feedback"` | get; set; | Title text on the report form. HTML attribute: `title`. |
| `Colour` | `string` | `"danger"` | get; set; | Bootstrap contextual suffix for the card border, title and submit button. HTML attribute: `colour`. |
| `MaskRequestPath` | `bool` | `false` | get; set; | Masks the request path in the submitted metadata. Binds by convention as `mask-request-path`. |
| `MaskQuery` | `bool` | `true` | get; set; | Masks the query string in the submitted metadata. Binds by convention as `mask-query`. |
| `ViewContext` | `ViewContext` | — | get; set; | Injected by the framework. Not bound to an HTML attribute. |

---

## Next steps

- [Setup](UI-Setup.md) — `_ViewImports` registration, sanitiser options, and QR code configuration.
- [Guide](UI-Guide.md) — tag helpers, sanitisation, HTML building, dropdowns, QR codes, and model state.
