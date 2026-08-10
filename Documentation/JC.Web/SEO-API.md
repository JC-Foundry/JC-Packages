# JC.Web: SEO — API reference

Complete reference for the public types in JC.Web's SEO area. See [Setup](SEO-Setup.md) for registration and [Guide](SEO-Guide.md) for usage.

> **Note:** Registration extensions (`AddSeo`, `AddSitemap`, `AddSitemapProvider`, `AddRobots`, `AddSeoMeta`, `UseSeo`, `UseSitemap`, `UseRobots`) and the options classes they configure (`SitemapOptions`, `RobotsOptions`, `SeoMetaOptions`) are documented in [Setup](SEO-Setup.md), not here.

# Models

## SitemapUrl

**Namespace:** `JC.Web.SEO.Models`

A single entry in the sitemap. Sealed.

### Constructors

#### SitemapUrl()

Creates an empty entry. Exists for configuration binding, which needs a parameterless constructor.

#### SitemapUrl(string location)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `location` | `string` | — | A site-relative path such as `/about`, or an absolute URL. |

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Location` | `string` | `""` | get; set; | The page location. Site-relative paths resolve against the configured base URL when written; absolute URLs are emitted unchanged. |
| `LastModified` | `DateTime?` | `null` | get; set; | When the page last changed. Omitted from the output when null. Written as a plain date when the time component is midnight, otherwise as an ISO-8601 UTC timestamp. |
| `ChangeFrequency` | `ChangeFrequency?` | `null` | get; set; | How often the page is expected to change. Omitted when null. Written lower-cased. Advisory only — Google ignores it. |
| `Priority` | `double?` | `null` | get; set; | Relative importance within this site, from 0.0 to 1.0. Omitted when null, and clamped to that range when written, since an out-of-range value invalidates the whole document. Advisory only — Google ignores it. |

---

## RobotsRule

**Namespace:** `JC.Web.SEO.Models`

A single `User-agent` group in robots.txt, with its allow and disallow paths. Sealed.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `UserAgent` | `string` | `"*"` | get; set; | The crawler this group applies to. `*` matches all crawlers. |
| `Disallow` | `List<string>` | empty | get; set; | Paths this crawler must not fetch. A single `/` blocks the whole site. |
| `Allow` | `List<string>` | empty | get; set; | Paths this crawler may fetch, used to carve exceptions out of a broader `Disallow`. |
| `CrawlDelay` | `int?` | `null` | get; set; | Seconds a crawler should wait between requests. Omitted when null. Google ignores this; most other crawlers honour it. |

Within a rendered group, `Allow` lines are written before `Disallow` lines, followed by `Crawl-delay` when present.

---

## SitemapIgnoreAttribute

**Namespace:** `JC.Web.SEO.Models`

Excludes a Razor Page model or controller from sitemap route discovery. Sealed, extends `Attribute`, declares no members.

Declared as `[AttributeUsage(AttributeTargets.Class, Inherited = false)]` — it applies to classes only, and a derived page model does not inherit it from its base.

Affects discovery alone. A URL supplied by an `ISitemapUrlProvider` or added through `SitemapOptions.Add` is included regardless; nothing cross-checks the attribute against those sources.

---

# Enums

## ChangeFrequency

**Namespace:** `JC.Web.SEO.Models`

How frequently a page is expected to change, as defined by the sitemap protocol. Written to the XML lower-cased.

| Member | Value | Description |
|--------|-------|-------------|
| `Always` | `0` | Changes each time it is accessed. |
| `Hourly` | `1` | Changes hourly. |
| `Daily` | `2` | Changes daily. |
| `Weekly` | `3` | Changes weekly. |
| `Monthly` | `4` | Changes monthly. |
| `Yearly` | `5` | Changes yearly. |
| `Never` | `6` | Archived; will not change again. |

---

# Services

## ISitemapUrlProvider

**Namespace:** `JC.Web.SEO.Services`

Supplies sitemap URLs that route discovery cannot resolve — typically database-backed content behind a parameterised route such as `/products/{id}`, which discovery skips because it has no way to know the values.

No in-package implementation exists; this is the extension point applications implement. Registered with `AddSitemapProvider<TProvider>`, which registers it scoped, so implementations may depend on scoped services such as a `DbContext`. Every registered provider is invoked and the results merged.

### Methods

#### GetUrlsAsync(CancellationToken cancellationToken = default)

**Returns:** `Task<IEnumerable<SitemapUrl>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | Cancellation token for the request. Bound to `HttpContext.RequestAborted` when called from the sitemap middleware. |

Returns the URLs this provider contributes. Return an empty sequence rather than null when there are none — the result is enumerated directly.

Results are not cached. Every request that reaches the sitemap path re-runs every provider.

---

# Helpers

## SeoBuilder

**Namespace:** `JC.Web.SEO.Helpers`

Fluent builder for a page's SEO head content — title, description, canonical URL, robots directives, Open Graph and Twitter Card tags, and JSON-LD structured data. Not registered in DI; construct one per page.

### Constructor

#### SeoBuilder(SeoMetaOptions? options = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `options` | `SeoMetaOptions?` | `null` | Site-wide defaults. When null, an unconfigured `SeoMetaOptions` is used, so no site name, base URL or fallbacks apply. |

### Methods

Every method below except `Build` returns the same builder instance, so calls chain.

#### Title(string title)

**Returns:** `SeoBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `title` | `string` | — | The title text. |

Sets the page title. On `Build`, the title is suffixed with `SeoMetaOptions.SiteName` when both that and `TitleSuffixSeparator` are set.

#### Description(string description)

**Returns:** `SeoBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `description` | `string` | — | The description text. |

Sets the meta description. Falls back to `SeoMetaOptions.DefaultDescription` when never called.

#### Canonical(string url)

**Returns:** `SeoBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `url` | `string` | — | Absolute URL, or a site-relative path. |

Sets the canonical URL, also emitted as `og:url`. A relative value is resolved against `SeoMetaOptions.BaseUrl`; without a base URL it is emitted unchanged, and a relative canonical is widely ignored by search engines.

#### Image(string url)

**Returns:** `SeoBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `url` | `string` | — | Absolute URL, or a site-relative path. |

Sets the image used for `og:image` and `twitter:image`. Resolved against the base URL on the same terms as `Canonical`. Falls back to `SeoMetaOptions.DefaultImage` when never called.

#### OpenGraphType(string type)

**Returns:** `SeoBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `type` | `string` | — | An Open Graph type such as `article` or `product`. |

Sets `og:type`. Defaults to `website` when never called.

#### TwitterCard(string cardType)

**Returns:** `SeoBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cardType` | `string` | — | A card type such as `summary` or `summary_large_image`. |

Sets `twitter:card`. When never called, the value is inferred — `summary_large_image` where an image resolved, `summary` where none did.

#### Robots(bool index, bool follow = true)

**Returns:** `SeoBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `index` | `bool` | — | Whether the page may be indexed. |
| `follow` | `bool` | `true` | Whether links on the page may be followed. |

Sets both robots directives for this page, overriding `SeoMetaOptions.DefaultIndex` and `DefaultFollow`. The `robots` meta tag is emitted by `Build` whether or not this is called, since the configured defaults still express an intent.

#### JsonLd(SchemaObject schema)

**Returns:** `SeoBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `schema` | `SchemaObject` | — | The structured data to embed. |

Serialises the schema and queues it as a `<script type="application/ld+json">` block. Delegates to the object overload.

#### JsonLd(object schema)

**Returns:** `SeoBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `schema` | `object` | — | Any serialisable object. |

For schema.org types the built-in schemas do not cover. Serialised with camel-cased property names, nulls omitted, and the **default** JSON encoder rather than the relaxed one — so a `</script>` sequence in the data is escaped and cannot close the script element.

May be called repeatedly; each call produces its own script block.

#### Build()

**Returns:** `string`

Renders the head content and returns it as HTML, or an empty string when nothing has been set.

Emits, in order: `<title>` (only when a title was supplied), `description`, `<link rel="canonical">`, `robots`, the `og:` property tags (`type`, `title`, `description`, `url`, `image`, `site_name`), the `twitter:` name tags (`card`, `title`, `description`, `image`, `site`), then one script block per queued JSON-LD payload.

Any tag whose content resolves to null or whitespace is skipped entirely. `og:title` and `twitter:title` fall back to `SeoMetaOptions.SiteName` when no title was supplied, even though no `<title>` element is written in that case.

All content values are HTML-encoded, since titles and descriptions routinely carry user or database text and an unencoded quote would close the attribute.

---

## SchemaObject

**Namespace:** `JC.Web.SEO.Helpers.JsonLd`

Abstract base for schema.org structured data emitted as JSON-LD. Derive from it for types the built-in schemas do not cover.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Context` | `string` | `https://schema.org` | get; | The JSON-LD context. Serialised as `@context` and ordered first. |
| `Type` | `string` | — | abstract get; | The schema.org type name. Serialised as `@type` and ordered second. Must be overridden. |

---

## OrganisationSchema

**Namespace:** `JC.Web.SEO.Helpers.JsonLd`

An organisation, typically emitted once on the home page to describe the site owner. Sealed, extends `SchemaObject`. `Type` returns `Organization`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Name` | `string?` | `null` | get; set; | The organisation's name. |
| `Url` | `string?` | `null` | get; set; | The organisation's website URL. |
| `Logo` | `string?` | `null` | get; set; | Absolute URL of the organisation's logo. |
| `Telephone` | `string?` | `null` | get; set; | Contact telephone number. |
| `SameAs` | `List<string>?` | `null` | get; set; | URLs of official profiles elsewhere, used to link identities together. |

---

## WebSiteSchema

**Namespace:** `JC.Web.SEO.Helpers.JsonLd`

The website itself, describing the site rather than any single page. Sealed, extends `SchemaObject`. `Type` returns `WebSite`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Name` | `string?` | `null` | get; set; | The site name. |
| `AlternateName` | `string?` | `null` | get; set; | An alternate or shortened name. |
| `Url` | `string?` | `null` | get; set; | The site's base URL. |

---

## BreadcrumbListSchema

**Namespace:** `JC.Web.SEO.Helpers.JsonLd`

A breadcrumb trail. Search engines use this to render the page's position in the site hierarchy in place of a raw URL. Sealed, extends `SchemaObject`. `Type` returns `BreadcrumbList`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `ItemListElement` | `List<BreadcrumbItem>` | empty | get; set; | The ordered trail items. |

### Methods

#### Add(string name, string? url = null)

**Returns:** `BreadcrumbListSchema`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The display name of this step. |
| `url` | `string?` | `null` | Absolute URL of this step. Omit for the current page, which needs no link. |

Appends an item, setting `Position` from the current count so numbering follows insertion order and cannot drift. Returns the schema instance, so calls chain.

---

## BreadcrumbItem

**Namespace:** `JC.Web.SEO.Helpers.JsonLd`

A single step within a `BreadcrumbListSchema`. Sealed. Does **not** extend `SchemaObject` — a nested item carries its own `@type` but no `@context`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Type` | `string` | `ListItem` | get; | Serialised as `@type` and ordered first. |
| `Position` | `int` | `0` | get; set; | 1-based position in the trail. Set by `BreadcrumbListSchema.Add`. |
| `Name` | `string?` | `null` | get; set; | The display name of this step. |
| `Item` | `string?` | `null` | get; set; | Absolute URL of this step. Null for the current page. |

---

## ArticleSchema

**Namespace:** `JC.Web.SEO.Helpers.JsonLd`

An article or blog post. Sealed, extends `SchemaObject`. `Type` returns `Article`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Headline` | `string?` | `null` | get; set; | The article headline. |
| `Description` | `string?` | `null` | get; set; | A short summary. |
| `Image` | `List<string>?` | `null` | get; set; | Absolute URLs of images associated with the article. |
| `DatePublished` | `DateTime?` | `null` | get; set; | When the article was first published. |
| `DateModified` | `DateTime?` | `null` | get; set; | When the article was last changed. |
| `Author` | `PersonSchema?` | `null` | get; set; | The article's author. |

---

## PersonSchema

**Namespace:** `JC.Web.SEO.Helpers.JsonLd`

A person, used for article authorship. Sealed, extends `SchemaObject`. `Type` returns `Person`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Name` | `string?` | `null` | get; set; | The person's name. |
| `Url` | `string?` | `null` | get; set; | A URL identifying the person, such as a profile page. |

---

## ProductSchema

**Namespace:** `JC.Web.SEO.Helpers.JsonLd`

A product, optionally carrying a price offer. Sealed, extends `SchemaObject`. `Type` returns `Product`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Name` | `string?` | `null` | get; set; | The product name. |
| `Description` | `string?` | `null` | get; set; | A description of the product. |
| `Image` | `List<string>?` | `null` | get; set; | Absolute URLs of product images. |
| `Sku` | `string?` | `null` | get; set; | The stock keeping unit. |
| `Brand` | `OrganisationSchema?` | `null` | get; set; | The manufacturer or brand. |
| `Offers` | `OfferSchema?` | `null` | get; set; | Pricing and availability. |

---

## OfferSchema

**Namespace:** `JC.Web.SEO.Helpers.JsonLd`

A price offer attached to a `ProductSchema`. Sealed, extends `SchemaObject`. `Type` returns `Offer`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Price` | `decimal?` | `null` | get; set; | The price, as a decimal value. |
| `PriceCurrency` | `string?` | `null` | get; set; | ISO 4217 currency code, for example `GBP`. |
| `Availability` | `string?` | `null` | get; set; | Availability, as a schema.org URL such as `https://schema.org/InStock`. |
| `Url` | `string?` | `null` | get; set; | The URL where the product can be purchased. |

---

# Middleware

## SitemapMiddleware

**Namespace:** `JC.Web.SEO.Middleware`

Serves the sitemap at the configured path, splitting into numbered files behind an index once the URL count exceeds `SitemapOptions.MaxUrlsPerFile`. Registered by `UseSitemap`.

Constructor takes `RequestDelegate` and `IOptions<SitemapOptions>`.

### Methods

#### InvokeAsync(HttpContext context)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `HttpContext` | — | The HTTP context for the current request. |

Passes the request along unchanged unless the method is `GET` and the path matches either the configured sitemap path or one of its numbered derivatives.

For a matching request it resolves the URL aggregator and the recorded start time from `RequestServices`, collects every URL, and divides the count by `MaxUrlsPerFile` to decide the file count. At the root path it writes a `urlset` directly when one file suffices, and a `sitemapindex` otherwise — an index pointing at a single child would only cost crawlers a round trip. At a numbered path it writes the corresponding slice, and returns 404 when the number falls outside the current range, which happens when the sitemap shrank after a crawler read the index.

Responses are written as `application/xml; charset=utf-8`, with a `Cache-Control: public, max-age` header when `ClientCacheDuration` is greater than zero.

The base URL is `SitemapOptions.BaseUrl` when set, otherwise the request's scheme and host.

Must be registered before `BotFilterMiddleware`, which blocks detected crawlers and would otherwise return 403 to the search engines the sitemap exists for.

---

## RobotsMiddleware

**Namespace:** `JC.Web.SEO.Middleware`

Serves robots.txt at the configured path. Registered by `UseRobots`.

Constructor takes `RequestDelegate`, `IOptions<RobotsOptions>` and `IOptions<SitemapOptions>` — the sitemap options are read to build the `Sitemap:` directive, which is why `AddRobots` registers them even when `AddSitemap` was never called.

### Methods

#### InvokeAsync(HttpContext context)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `HttpContext` | — | The HTTP context for the current request. |

Passes the request along unchanged unless the method is `GET` and the path matches `RobotsOptions.Path` exactly, compared case-insensitively.

For a matching request it renders the configured rules, falling back to a single permissive `User-agent: *` group when none are configured, and writes the result as `text/plain; charset=utf-8`.

The `Sitemap:` directive is resolved in order: an explicit `RobotsOptions.SitemapUrl` wins; otherwise, when `IncludeSitemapDirective` is set and a sitemap registration is present in the container, the URL is derived from the sitemap options and the request host; otherwise no directive is written. That last case is what stops robots.txt advertising a path that would 404. Entries in `AdditionalSitemaps` are appended regardless.

Must be registered before `BotFilterMiddleware` — a robots.txt crawlers cannot read tells them nothing, and some treat the failure as permission to crawl everything.

---

# Tag helpers

Requires `@addTagHelper *, JC.Web` in `_ViewImports.cshtml`. Without it Razor emits the element name literally rather than raising an error.

## SeoMetaTagHelper

**Namespace:** `JC.Web.SEO.TagHelpers`

**Tag:** `<seo-meta>` — **Structure:** `TagStructure.WithoutEndTag`

Renders a page's SEO head content. Place inside `<head>`. Takes `IOptions<SeoMetaOptions>` by constructor injection.

For JSON-LD structured data use `SeoBuilder` directly — a tag helper attribute is a poor fit for a nested object graph.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Title` | `string?` | `null` | get; set; | The page title. HTML attribute: `title`. |
| `Description` | `string?` | `null` | get; set; | The meta description. HTML attribute: `description`. |
| `Canonical` | `string?` | `null` | get; set; | The canonical URL. Relative values resolve against the configured base URL. HTML attribute: `canonical`. |
| `Image` | `string?` | `null` | get; set; | Image for Open Graph and the Twitter card. HTML attribute: `image`. |
| `OpenGraphType` | `string?` | `null` | get; set; | The Open Graph object type, such as `article`. Defaults to `website`. HTML attribute: `og-type`. |
| `TwitterCard` | `string?` | `null` | get; set; | The Twitter card type. Inferred from the presence of an image when omitted. HTML attribute: `twitter-card`. |
| `Index` | `bool?` | `null` | get; set; | Whether the page may be indexed. Falls back to the configured default. HTML attribute: `index`. |
| `Follow` | `bool?` | `null` | get; set; | Whether links may be followed. Falls back to the configured default. HTML attribute: `follow`. |

### Methods

#### Process(TagHelperContext context, TagHelperOutput output)

**Returns:** `void`

Constructs a `SeoBuilder` from the injected options and applies each attribute that was supplied and is not whitespace, leaving the rest to the configured defaults. The robots directives are applied only when `Index` or `Follow` was set, though `SeoBuilder` emits a `robots` tag either way from the configured defaults.

Suppresses its own tag name and writes the built markup as the element's content, so `<seo-meta>` leaves no wrapper element in the output.

Renders whether or not `AddSeoMeta` was called: the options system supplies an unconfigured `SeoMetaOptions` in its absence, giving no site name, no base URL, and `index,follow`.

## Next steps

- [Setup](SEO-Setup.md) — registration, every option, and its default.
- [Guide](SEO-Guide.md) — usage, exclusions and nuances.
