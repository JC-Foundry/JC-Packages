# JC.Web: UI — API reference

Complete reference of all public types, properties, and methods in the JC.Web UI area — the framework class dictionary, content sanitisation, HTML builders, dropdown helpers, QR codes, model state, and tag helpers. See [Setup](UI-Setup.md) for registration and [Guide](UI-Guide.md) for usage examples.

> **Note:** Registration extensions (`AddUI`, `AddFrameworkDictionary`, `AddIconDictionary`) and options classes are documented in [Setup](UI-Setup.md), not here. `ContentSanitiserOptions` is covered under [Setup](UI-Setup.md#contentsanitiseroptions).

---

# Enums

## AlertType

**Namespace:** `JC.Web.UI.HTML`

The type of alert to render. Maps to a class through `AlertClasses.Variants`.

| Member | Value | Description |
|--------|-------|-------------|
| `Success` | `0` | A success alert. |
| `Warning` | `1` | A warning alert. |
| `Error` | `2` | An error or danger alert. |
| `Info` | `3` | An informational alert. |

---

## UIFramework

**Namespace:** `JC.Web.UI.Framework`

The CSS framework class names are rendered for. A `[Flags]` enum — several may be combined, and `UIFrameworkService` resolves them to one value.

| Member | Value | Description |
|--------|-------|-------------|
| `Bootstrap` | `1` | Bootstrap 5. Selects `BootstrapDictionary`. |
| `Tailwind` | `2` | Tailwind v4. Selects `TailwindDictionary`. |
| `CustomJCTailwind` | `4` | jc-tailwind-ui. Selects `CustomJCTailwindDictionary`. |

Values are powers of two so flag tests behave. Resolution precedence is `CustomJCTailwind`, then `Tailwind`, then `Bootstrap`.

---

## IconFramework

**Namespace:** `JC.Web.UI.Framework`

The icon set glyphs are rendered from, chosen independently of `UIFramework`. A `[Flags]` enum, resolved to one value by `UIFrameworkService`.

| Member | Value | Description |
|--------|-------|-------------|
| `Bootstrap` | `1` | Bootstrap Icons. |
| `FontAwesome` | `2` | Font Awesome. |

Resolves to `Bootstrap` when the `Bootstrap` flag is set **or** when the value is `0`; otherwise `FontAwesome`.

---

## QrCodeFormat

**Namespace:** `JC.Web.UI.Helpers`

The output format for generated QR codes.

| Member | Value | Description |
|--------|-------|-------------|
| `Svg` | `0` | SVG markup string. |
| `Base64` | `1` | Base64-encoded PNG data URI. |

---

# Framework dictionary

Class names are read from a dictionary resolved from the container rather than hardcoded. Every value is a **complete class attribute value**, not a single token, and every property defaults to `""` — an unset entry renders no class rather than failing.

## IFrameworkDictionary

**Namespace:** `JC.Web.UI.Framework`

Empty marker interface. Each package declares its own contract deriving from this and registers it with `AddFrameworkDictionary`, so adding a component to a package layered above JC.Web never requires changing JC.Web.

## IIconDictionary

**Namespace:** `JC.Web.UI.Framework`

Empty marker interface for a package's icon dictionary, registered with `AddIconDictionary`. Separate from `IFrameworkDictionary` because the two are selected by different choices — `UIFrameworkService.Framework` and `UIFrameworkService.IconFramework` respectively.

JC.Web declares no icon contract; none of its components render a glyph.

## IWebFrameworkDictionary

**Namespace:** `JC.Web.UI.Framework`

JC.Web's own class dictionary. Implemented by `BootstrapDictionary`, `TailwindDictionary` and `CustomJCTailwindDictionary`; the configured `UIFramework` decides which is resolved. Inject `IWebFrameworkDictionary`.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Alert` | `AlertClasses` | get; | Classes for alert components. |
| `Breadcrumb` | `BreadcrumbClasses` | get; | Classes for breadcrumb navigation. |
| `Pagination` | `PaginationClasses` | get; | Classes for pagination controls. |
| `Table` | `TableClasses` | get; | Classes for generated tables. |
| `BugReporter` | `BugReporterClasses` | get; | Classes for the bug reporter widget. |
| `State` | `StateClasses` | get; | Classes for element states shared across components. |

## UIFrameworkService

**Namespace:** `JC.Web.UI.Framework`

Holds the resolved framework and icon set. Registered as a singleton by `AddUI`. Both values are resolved once, in the constructor, so nothing downstream handles unresolved flags.

### Constructor

#### UIFrameworkService(UIFramework framework, IconFramework iconFramework)

| Parameter | Type | Description |
|-----------|------|-------------|
| `framework` | `UIFramework` | The configured framework, which may combine flags. |
| `iconFramework` | `IconFramework` | The configured icon set, which may combine flags. |

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Framework` | `UIFramework` | get; | The single resolved framework. Never a combination. |
| `IconFramework` | `IconFramework` | get; | The single resolved icon set. Never a combination. |

## AlertClasses

**Namespace:** `JC.Web.UI.Framework`

Sealed record. Classes for alert components.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Container` | `string` | `""` | get; init; | The alert container. |
| `Dismissible` | `string` | `""` | get; init; | Added to the container when the alert can be dismissed. |
| `CloseButton` | `string` | `""` | get; init; | The dismiss button. |
| `Variants` | `IReadOnlyDictionary<AlertType, string>` | empty | get; init; | The class for each alert type. |

### Methods

#### Variant(AlertType type)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `type` | `AlertType` | — | The alert type. |

Returns the variant class for the given type, or an empty string when `Variants` does not define one.

## BreadcrumbClasses

**Namespace:** `JC.Web.UI.Framework`

Sealed record. Classes for breadcrumb navigation.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Nav` | `string` | `""` | get; init; | The wrapping navigation element. Empty under Bootstrap, which styles the list instead. |
| `List` | `string` | `""` | get; init; | The list containing the trail. |
| `Item` | `string` | `""` | get; init; | An ordinary trail item. |
| `ActiveItem` | `string` | `""` | get; init; | The final item, representing the current page. |

## PaginationClasses

**Namespace:** `JC.Web.UI.Framework`

Sealed record. Classes for pagination controls.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Nav` | `string` | `""` | get; init; | The wrapping navigation element. |
| `List` | `string` | `""` | get; init; | The list containing the page items. |
| `Item` | `string` | `""` | get; init; | An ordinary page item. |
| `ActiveItem` | `string` | `""` | get; init; | The item for the current page. |
| `DisabledItem` | `string` | `""` | get; init; | An item that cannot be navigated to. |
| `Link` | `string` | `""` | get; init; | The link inside a page item. |

## TableClasses

**Namespace:** `JC.Web.UI.Framework`

Sealed record. Classes for generated tables.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Table` | `string` | `""` | get; init; | The table element. Used when the caller supplies no explicit table class. |
| `Head` | `string` | `""` | get; init; | The table head. |
| `Body` | `string` | `""` | get; init; | The table body. |
| `Row` | `string` | `""` | get; init; | A body row. |
| `HeaderCell` | `string` | `""` | get; init; | A header cell. |
| `Cell` | `string` | `""` | get; init; | A body cell. |

## BugReporterClasses

**Namespace:** `JC.Web.UI.Framework`

Sealed record. Classes for the bug reporter widget. The widget takes a contextual colour at runtime, so the values it appears in are stored as format strings with `{0}` for that colour.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `ToggleButton` | `string` | `""` | get; init; | The floating button that opens the widget. |
| `PanelFormat` | `string` | `""` | get; init; | The report panel. `{0}` is the configured colour. |
| `DefaultColour` | `string` | `""` | get; init; | The contextual colour used when the caller specifies none. |
| `PanelBody` | `string` | `""` | get; init; | The panel's inner body. |
| `TitleFormat` | `string` | `""` | get; init; | The panel heading. `{0}` is the configured colour. |
| `Field` | `string` | `""` | get; init; | The wrapper around a single form field. |
| `Label` | `string` | `""` | get; init; | A field label. |
| `Select` | `string` | `""` | get; init; | The report type select. |
| `TextArea` | `string` | `""` | get; init; | The description textarea. |
| `Hidden` | `string` | `""` | get; init; | Hides an element. Applied to the feedback area until there is something to say. |
| `Actions` | `string` | `""` | get; init; | The row holding the cancel and submit buttons. |
| `CancelButton` | `string` | `""` | get; init; | The cancel button. |
| `SubmitButtonFormat` | `string` | `""` | get; init; | The submit button. `{0}` is the configured colour. |
| `FeedbackFormat` | `string` | `""` | get; init; | The inline feedback message. `{0}` is the outcome — `success`, `warning` or `danger`. Substituted in the browser, so the format is emitted into the widget's script as-is. |

### Methods

#### Panel(string colour)

**Returns:** `string`

Applies `colour` to `PanelFormat`. Returns an empty string when the format is unset.

#### Title(string colour)

**Returns:** `string`

Applies `colour` to `TitleFormat`. Returns an empty string when the format is unset.

#### SubmitButton(string colour)

**Returns:** `string`

Applies `colour` to `SubmitButtonFormat`. Returns an empty string when the format is unset.

## StateClasses

**Namespace:** `JC.Web.UI.Framework`

Sealed record. Classes for element states shared across components.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Active` | `string` | `""` | get; init; | Marks an element as the active or current one. |
| `Disabled` | `string` | `""` | get; init; | Marks an element as unavailable. |

## FrameworkClass

**Namespace:** `JC.Web.UI.Framework`

Static helper for class values that embed a runtime value.

### Methods

#### Format(string format, params object?[] args)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `format` | `string` | — | The class format, as stored on a dictionary record. |
| `args` | `params object?[]` | — | The runtime values to substitute. |

Applies `args` to `format` via `string.Format`. Returns an empty string when `format` is null, empty or whitespace, so an unset dictionary entry short-circuits rather than throwing.

#### Join(params string?[] classes)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `classes` | `params string?[]` | — | The values to combine. |

Joins the values with single spaces, skipping any that are null, empty or whitespace. Returns an empty string when none are set.

## IconClass

**Namespace:** `JC.Web.UI.Framework`

Static helper for icon class values supplied by a caller rather than by a dictionary.

### Methods

#### WithBase(string? icon, string baseClass)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `icon` | `string?` | — | The caller-supplied icon class. |
| `baseClass` | `string` | — | The base class the icon set requires, empty when it needs none. |

Prefixes `icon` with `baseClass` unless it already begins with `baseClass` followed by a space. Returns an empty string when `icon` is null, empty or whitespace, and returns `icon` unchanged when `baseClass` is. The comparison is ordinal.

## BootstrapDictionary

**Namespace:** `JC.Web.UI.Framework`

Sealed. Bootstrap 5 implementation of `IWebFrameworkDictionary`, selected for `UIFramework.Bootstrap`. Reproduces the markup JC.Web emitted before class names became configurable.

## TailwindDictionary

**Namespace:** `JC.Web.UI.Framework`

Sealed. Tailwind v4 implementation of `IWebFrameworkDictionary`, selected for `UIFramework.Tailwind`. Assumes Preflight. Its `BugReporterClasses` colour is a Tailwind colour fragment such as `red-600` rather than a Bootstrap contextual name.

Every class it emits must be declared to Tailwind — see [Setup](UI-Setup.md#framework-specific-requirements).

## CustomJCTailwindDictionary

**Namespace:** `JC.Web.UI.Framework`

Sealed. jc-tailwind-ui implementation of `IWebFrameworkDictionary`, selected for `UIFramework.CustomJCTailwind`. That framework borrows Bootstrap's class vocabulary, so most values match `BootstrapDictionary`. Colour is the exception: it composes as `tone-{0}`, which works for any colour the application defines a tone for.

Pagination requires that framework's opt-in `interactive` layer — see [Setup](UI-Setup.md#framework-specific-requirements).

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

Renders alert markup from code using the configured framework's classes. Registered as a singleton by `AddUI`; inject `AlertHelper`. Stateless.

Takes `IWebFrameworkDictionary` as its only constructor parameter.

All five methods insert `message` as raw HTML without encoding.

When `dismissible` is `true`, the appended close button carries `data-bs-dismiss="alert"` and `aria-label="Close"` under every framework — its class comes from `AlertClasses.CloseButton`, but the attribute is fixed. Bootstrap's own bundle acts on it; under any other framework it is inert until the application supplies a handler. The button is emitted with no children, so a dictionary whose close button draws its glyph through CSS renders correctly.

### Methods

#### Success(string message, bool dismissible = true)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `message` | `string` | — | The alert message content. May contain HTML. |
| `dismissible` | `bool` | `true` | Whether the alert can be dismissed. |

Renders an alert using the dictionary's `Success` variant class.

---

#### Warning(string message, bool dismissible = true)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `message` | `string` | — | The alert message content. |
| `dismissible` | `bool` | `true` | Whether the alert can be dismissed. |

Renders an alert using the dictionary's `Warning` variant class.

---

#### Error(string message, bool dismissible = true)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `message` | `string` | — | The alert message content. |
| `dismissible` | `bool` | `true` | Whether the alert can be dismissed. |

Renders an alert using the dictionary's `Error` variant class.

---

#### Info(string message, bool dismissible = true)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `message` | `string` | — | The alert message content. |
| `dismissible` | `bool` | `true` | Whether the alert can be dismissed. |

Renders an alert using the dictionary's `Info` variant class.

---

#### ForType(AlertType type, string message, bool dismissible = true)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `type` | `AlertType` | — | The alert type. |
| `message` | `string` | — | The alert message content. May contain HTML. |
| `dismissible` | `bool` | `true` | Whether the alert can be dismissed. |

Renders an alert for the supplied type, allowing the type to be chosen at runtime. The element carries `role="alert"`, the dictionary's `Container` class and its variant class for `type`. When `dismissible` is true it also carries the `Dismissible` class and a close button using `CloseButton`; that button's `data-bs-dismiss="alert"` attribute is hardcoded rather than supplied by the dictionary.

---

## BreadcrumbBuilder

**Namespace:** `JC.Web.UI.HTML`

Fluent builder for breadcrumb navigation using the configured framework's classes. The last item added always renders as the active page. Supports implicit conversion to `string`, so an instance can be used wherever a string is expected without calling `Build()`.

Holds per-use state, so it is constructed per trail rather than resolved from the container.

### Constructor

#### BreadcrumbBuilder(IWebFrameworkDictionary dictionary)

| Parameter | Type | Description |
|-----------|------|-------------|
| `dictionary` | `IWebFrameworkDictionary` | The class dictionary for the configured framework. |

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

Builds the complete breadcrumb as a `<nav aria-label="breadcrumb">` containing an `<ol>` with the dictionary's `List` class. The final item carries `aria-current="page"`. Labels and URLs are HTML-encoded. Returns an empty string when no items have been added.

The `class` attribute on the `<nav>` is emitted only when the dictionary supplies a `Nav` value, so a framework that styles the list alone produces a bare `<nav>`.

---

## HtmlHelper

**Namespace:** `JC.Web.UI.HTML`

Assembles HTML elements using the configured framework's classes, with specific methods for the parts of a pagination control. Registered as a singleton by `AddUI`; inject `HtmlHelper`. Stateless.

Takes `IWebFrameworkDictionary` as its only constructor parameter.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `PaginationListClass` | `string` | get; | The dictionary's class for the list wrapping pagination items. |
| `PaginationNavClass` | `string` | get; | The dictionary's class for the navigation element wrapping a pagination control. |

### Methods

#### CreateElement(string tagName, string content = "", bool isActive = false, bool isDisabled = false, Dictionary\<string, string\>? attributes = null, params string[] classes)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tagName` | `string` | — | The HTML tag name. |
| `content` | `string` | `""` | The inner HTML content. |
| `isActive` | `bool` | `false` | Adds the dictionary's `State.Active` class. |
| `isDisabled` | `bool` | `false` | Adds the dictionary's `State.Disabled` class. |
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

Builds a pagination list item. The class is the dictionary's `ActiveItem`, `DisabledItem` or `Item` — a whole value each, not a base plus modifier. `isActive` wins when both flags are set.

---

#### PaginationLink(string text, string href, string? buttonClass = null, bool isActive = false)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `text` | `string` | — | The link text. |
| `href` | `string` | — | The URL to navigate to. |
| `buttonClass` | `string?` | `null` | Additional CSS classes for the link. |
| `isActive` | `bool` | `false` | Adds `aria-current="page"`. |

Builds an anchor carrying the dictionary's `Pagination.Link` class, plus `buttonClass` when supplied.

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

#### AddCurrentPageAttribute()

**Returns:** `HtmlTagBuilder`

Adds the `aria-current="page"` attribute. This is an ARIA attribute rather than a CSS class, so it is identical across frameworks and stays on the builder; state *classes* come from `IWebFrameworkDictionary.State` and are applied through `AddClass`.

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

Fluent builder rendering a table from a collection using the configured framework's classes. Header text and cell values are HTML-encoded, so values drawn from user data cannot inject markup.

Holds per-use state and is generic, so it is constructed per table rather than resolved from the container.

### Constructor

#### TableBuilder(IWebFrameworkDictionary dictionary)

| Parameter | Type | Description |
|-----------|------|-------------|
| `dictionary` | `IWebFrameworkDictionary` | The class dictionary for the configured framework. |

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
| `tableClass` | `string?` | `null` | CSS classes for the `<table>` element. Falls back to the dictionary's `Table.Table` when null or whitespace. |

Builds the complete table. Columns render in the order they were added. `tableClass` **replaces** the dictionary's table class rather than adding to it.

The `<thead>`, `<tbody>`, `<tr>`, `<th>` and `<td>` tags each carry their dictionary class combined with the column's `cssClass` where applicable, and the `class` attribute is omitted entirely when the result is empty — so a framework leaving the structural entries blank produces bare tags.

---

# Tag helpers

All require `@addTagHelper *, JC.Web` in `_ViewImports.cshtml`. Without it Razor emits the element name literally rather than raising an error. Each also takes a constructor dependency from the container, so `AddUI` must have been called or resolution fails at render time.

## AlertTagHelper

**Namespace:** `JC.Web.UI.TagHelpers`

Renders an alert using the configured framework's classes. Injects `AlertHelper`. Targets the `<alert>` element, self-closing. Suppresses output entirely when `Message` is null or whitespace, so binding to an optional model property produces no empty alert.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Type` | `AlertType` | `Info` | get; set; | The alert type. HTML attribute: `type`. |
| `Message` | `string?` | `null` | get; set; | The alert message content. HTML attribute: `message`. |
| `Dismissible` | `bool` | `true` | get; set; | Whether the alert is dismissible. HTML attribute: `dismissible`. |

---

## PaginationTagHelper

**Namespace:** `JC.Web.UI.TagHelpers`

Renders pagination from an `IPagination<object>` model using the configured framework's classes. Injects `HtmlHelper`. Targets the `<pagination>` element, self-closing. Renders previous and next links, numbered page buttons with ellipsis once `MaxVisiblePages` is exceeded, and optional first and last links. Suppresses output when the model is null or has one page or fewer.

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

Renders breadcrumb navigation from nested `<crumb>` elements using the configured framework's classes. Injects `IWebFrameworkDictionary`. Targets the `<breadcrumb>` element. The last crumb renders as the active page. Suppresses output when no crumbs are supplied.

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

Renders a floating bug reporter widget — a toggle button, a report form taking a type and description, and the JavaScript that POSTs the report. Targets the `<bug-reporter>` element, self-closing. Injects `IWebFrameworkDictionary`. Throws `InvalidOperationException` when `Endpoint` is null or whitespace.

Its JavaScript is self-contained rather than relying on the CSS framework's, so the widget behaves the same under every dictionary. The toggle button carries the dictionary's `ToggleButton` class, which hides it in printed output.

Attaches `RequestMetadata` to the payload and sends an anti-forgery token in the `RequestVerificationToken` header when one is available. This is the only component in this area depending on another — without [client profiling](ClientProfiling-Setup.md) registered and its middleware in the pipeline, the widget still renders and submits but the metadata is empty.

Metadata is serialised via `RequestMetadata.ToLogEntry(maskPath: MaskRequestPath, maskQuery: MaskQuery)`. Client IP, origin, referer and city keep their default masking and cannot be unmasked here — only the request path and query string are configurable, and by default the path is **shown** while the query string is **masked**.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Endpoint` | `string?` | `null` | get; set; | The POST endpoint receiving reports. Required. HTML attribute: `endpoint`. |
| `Icon` | `string` | `"🐞"` | get; set; | Icon on the floating button. HTML attribute: `icon`. |
| `Title` | `string` | `"Send Feedback"` | get; set; | Title text on the report form. HTML attribute: `title`. |
| `Colour` | `string?` | `null` | get; set; | Contextual colour for the panel border, title and submit button. Falls back to the dictionary's `BugReporter.DefaultColour` when null or whitespace. What the value means is the dictionary's business — a Bootstrap contextual name, a Tailwind colour fragment, or a jc-tailwind-ui tone. HTML attribute: `colour`. |
| `MaskRequestPath` | `bool` | `false` | get; set; | Masks the request path in the submitted metadata. Binds by convention as `mask-request-path`. |
| `MaskQuery` | `bool` | `true` | get; set; | Masks the query string in the submitted metadata. Binds by convention as `mask-query`. |
| `ViewContext` | `ViewContext` | — | get; set; | Injected by the framework. Not bound to an HTML attribute. |

---

## Next steps

- [Setup](UI-Setup.md) — `_ViewImports` registration, sanitiser options, and QR code configuration.
- [Guide](UI-Guide.md) — tag helpers, sanitisation, HTML building, dropdowns, QR codes, and model state.
