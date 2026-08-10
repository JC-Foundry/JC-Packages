# JC.Web: SEO — Guide

Covers what ends up in the sitemap and how to control it, robots.txt rules, per-page meta tags, and JSON-LD structured data. See [Setup](SEO-Setup.md) for registration, options and pipeline ordering.

## The sitemap

### What ends up in it

Three sources are merged on every request:

1. **Discovered routes** — Razor Pages found in the application's endpoints, and MVC controller actions when `DiscoverControllers` is on.
2. **Explicit entries** — anything added through `SitemapOptions.Add`.
3. **Providers** — every registered `ISitemapUrlProvider`.

The merged list is de-duplicated by location, compared case-insensitively, then ordered.

**Nuance:** later sources win a duplicate. That is deliberate — a provider or an explicit entry can supply a real `lastmod` for a path discovery already found with nothing but the application's start time.

### What discovery skips

```csharp
// Discovered
/                       // Pages/Index.cshtml
/about                  // Pages/About.cshtml
/products               // Pages/Products/Index.cshtml — collapsed from /products/Index

// Skipped
/products/{slug}        // parameterised — supply from a provider
/api/orders             // matches the /api excluded prefix
/_framework/blazor.js   // matches the /_ excluded prefix
```

A route is skipped when it is parameterised, matches an excluded prefix, carries `[SitemapIgnore]`, or is not reachable by `GET`. Endpoints that declare no HTTP methods at all count as gettable, since they answer any verb.

**Nuance:** `/Index` collapses onto its parent directory, because Razor Pages serves the same content at both. Emitting the pair would hand crawlers two URLs for identical content — the definition of a duplicate-content problem.

### Excluding a page

Attribute the page model:

```csharp
[SitemapIgnore]
public class AdminDashboardModel : PageModel
{
    public void OnGet() { }
}
```

Or exclude a whole area by prefix, which is cheaper than attributing every page under it:

```csharp
builder.Services.AddSitemap(builder.Configuration, options =>
{
    options.ExcludePrefixes = ["/api", "/_", "/admin", "/account"];
});
```

**Nuance:** `[SitemapIgnore]` only affects *discovery*. A URL added explicitly or returned by a provider is included regardless — nothing cross-checks the attribute.

### Supplying URLs from the database

Discovery cannot resolve `/products/{slug}`, so database-backed content comes from a provider:

```csharp
public class ProductSitemapProvider(AppDbContext db) : ISitemapUrlProvider
{
    public async Task<IEnumerable<SitemapUrl>> GetUrlsAsync(CancellationToken cancellationToken = default)
        => await db.Products
            .Where(p => p.IsPublished)
            .Select(p => new SitemapUrl($"/products/{p.Slug}")
            {
                LastModified = p.UpdatedUtc,
                ChangeFrequency = ChangeFrequency.Weekly,
                Priority = 0.7
            })
            .ToListAsync(cancellationToken);
}
```

```csharp
builder.Services.AddSitemapProvider<ProductSitemapProvider>();
```

Providers are scoped, so injecting a `DbContext` is safe. Register as many as you have content types — all are invoked and their results merged.

**Nuance:** results are **not cached**. Every request that reaches `/sitemap.xml` re-runs every provider, so a provider that scans a large table runs that query each time. `ClientCacheDuration` only helps downstream caches; it does nothing for a request that arrives at the server. Filter in the database rather than in memory, and return only published content.

**Nuance:** return an empty sequence rather than `null` when there is nothing to contribute. A null result throws when the aggregator enumerates it.

### Adding URLs directly

For a handful of fixed pages that discovery misses:

```csharp
builder.Services.AddSitemap(builder.Configuration, options =>
{
    options
        .Add("/", priority: 1.0, changeFrequency: ChangeFrequency.Daily)
        .Add("/about", priority: 0.8, changeFrequency: ChangeFrequency.Monthly)
        .Add("https://blog.example.com/", priority: 0.5);
});
```

Locations may be site-relative or absolute. Relative paths resolve against `BaseUrl`, falling back to the request's scheme and host; absolute URLs are emitted unchanged, which is what lets you list a subdomain you do not serve.

### Priority, change frequency and lastmod

```csharp
new SitemapUrl("/about")
{
    LastModified = new DateTime(2026, 8, 10),  // written as 2026-08-10
    ChangeFrequency = ChangeFrequency.Monthly, // written as "monthly"
    Priority = 0.8                             // written as 0.8
};
```

All three are optional and omitted from the XML when null.

**Nuance:** Google ignores `priority` and `changefreq` entirely, and has said so publicly. They are emitted for crawlers that do read them, but tuning them is not time well spent. `lastmod` is the element that still carries weight — which is why it is worth giving providers a real timestamp rather than letting everything inherit the application's start time.

**Nuance:** a `priority` outside 0.0–1.0 is clamped rather than emitted, because an out-of-range value invalidates the entire document — one bad row would cost you the whole sitemap.

**Nuance:** a `lastmod` at exactly midnight is written as a plain date, otherwise as a full ISO-8601 UTC timestamp. A date is the more honest representation when only the day is actually known.

### When the sitemap gets large

Past `MaxUrlsPerFile` — 50,000 by default, the protocol's own limit — `/sitemap.xml` stops being a URL list and becomes an index:

```xml
<!-- /sitemap.xml once there are more than 50,000 URLs -->
<sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
  <sitemap>
    <loc>https://example.com/sitemap-1.xml</loc>
    <lastmod>2026-08-10T09:14:22Z</lastmod>
  </sitemap>
  <sitemap>
    <loc>https://example.com/sitemap-2.xml</loc>
    <lastmod>2026-08-10T09:14:22Z</lastmod>
  </sitemap>
</sitemapindex>
```

The numbered files are served from the same middleware and derive their paths from `Path`, so a custom `Path` of `/seo/urls.xml` yields `/seo/urls-1.xml`. Nothing extra is registered.

**Nuance:** a single file's worth of URLs is served directly rather than behind an index of one, which would cost crawlers a pointless round trip.

**Nuance:** requesting a numbered file outside the current range returns 404. That happens when the sitemap shrinks between a crawler reading the index and fetching the files — a 404 is the correct answer, and the crawler re-reads the index.

**Nuance:** ordering is deterministic — locations are sorted ordinally — precisely so that splitting is stable. If the order varied between requests, a crawler fetching the index and then each file would see URLs shift between them and miss some entirely.

## robots.txt

### Basic rules

```csharp
builder.Services.AddRobots(builder.Configuration, options =>
{
    options.Disallow("*", "/admin", "/account", "/checkout");
});
```

```
User-agent: *
Disallow: /admin
Disallow: /account
Disallow: /checkout

Sitemap: https://example.com/sitemap.xml
```

With no rules configured at all, a single permissive `User-agent: *` group is written with nothing disallowed. An empty file would leave crawler behaviour to interpretation; an explicit permissive group does not.

### Per-crawler groups

```csharp
builder.Services.AddRobots(builder.Configuration, options =>
{
    options.Disallow("*", "/admin");
    options.Allow("Googlebot", "/admin/public");
    options.Rules.Add(new RobotsRule
    {
        UserAgent = "AhrefsBot",
        Disallow = ["/"],
        CrawlDelay = 10
    });
});
```

`Disallow` and `Allow` find the group by user agent, comparing case-insensitively, and create it when absent — so repeated calls for the same agent accumulate rather than replacing. `Allow` lines are written before `Disallow` lines within a group.

### Blocking an environment

The reason the rules live in configuration rather than code — staging disallows everything through its own `appsettings.Staging.json`, with no branch in startup:

```json
{
  "Web": {
    "SEO": {
      "Robots": {
        "Rules": [ { "UserAgent": "*", "Disallow": [ "/" ] } ]
      }
    }
  }
}
```

Pair it with `DefaultIndex = false` on `SeoMetaOptions` so pages emit `noindex` as well. robots.txt stops well-behaved crawlers fetching; `noindex` stops a URL that leaked by another route from being indexed. They solve different halves.

### The Sitemap directive

```csharp
// Derived automatically from the registered sitemap
options.IncludeSitemapDirective = true;

// Or stated outright, when the sitemap is served elsewhere
options.SitemapUrl = "https://cdn.example.com/sitemap.xml";

// Extra sitemaps this application does not serve
options.AdditionalSitemaps = ["https://example.com/news-sitemap.xml"];
```

**Nuance:** the derived directive is only written when a sitemap is genuinely registered. Calling `AddRobots` without `AddSitemap` produces a robots.txt with no `Sitemap:` line rather than one advertising a path that 404s. Setting `SitemapUrl` explicitly bypasses that check, since you are asserting the URL yourself.

**Nuance:** `AdditionalSitemaps` are written whatever `IncludeSitemapDirective` says. That flag governs the derived directive only.

## Page meta tags

### The tag helper

Put it in `_Layout.cshtml` inside `<head>`, driven by whatever each page sets:

```cshtml
<head>
    <seo-meta title="@ViewData["Title"]"
              description="@ViewData["Description"]"
              canonical="@ViewData["Canonical"]" />
</head>
```

Or per page, when the values are known there:

```cshtml
<seo-meta title="Widget"
          description="A widget that does widget things."
          canonical="/products/widget"
          image="/img/widget.png"
          og-type="product" />
```

Which renders:

```html
<title>Widget | Example</title>
<meta name="description" content="A widget that does widget things.">
<link rel="canonical" href="https://example.com/products/widget">
<meta name="robots" content="index,follow">
<meta property="og:type" content="product">
<meta property="og:title" content="Widget | Example">
<meta property="og:description" content="A widget that does widget things.">
<meta property="og:url" content="https://example.com/products/widget">
<meta property="og:image" content="https://example.com/img/widget.png">
<meta property="og:site_name" content="Example">
<meta name="twitter:card" content="summary_large_image">
<meta name="twitter:title" content="Widget | Example">
<meta name="twitter:description" content="A widget that does widget things.">
<meta name="twitter:image" content="https://example.com/img/widget.png">
```

The title suffix, base URL and site name come from `SeoMetaOptions`; the relative `canonical` and `image` became absolute against `BaseUrl`.

**Nuance:** the `robots` meta is **always** emitted, even when neither `index` nor `follow` is set, because the configured defaults still describe an intent. To suppress a single page instead, set `index="false"`.

**Nuance:** the `<title>` element is only written when a title is supplied — but `og:title` and `twitter:title` still fall back to `SiteName`. A page with no title therefore keeps a sensible social card while leaving the browser tab to whatever else set it.

**Nuance:** the Twitter card type is inferred when not given — `summary_large_image` where an image resolved, `summary` where none did. Set `twitter-card` to override.

**Nuance:** `<seo-meta>` works without `AddSeoMeta`. The options system supplies an unconfigured instance, so the tag helper renders using the built-in defaults — no site name, no title suffix, relative URLs left relative, and `index,follow`.

### Excluding a page from indexing

```cshtml
<seo-meta title="Checkout" index="false" />
```

```html
<meta name="robots" content="noindex,follow">
```

`follow` defaults independently, so links on the page are still crawled. Set `follow="false"` to stop that too.

### Building meta in code

The tag helper covers the flat attributes. Use `SeoBuilder` where the values come from a service, or where you need JSON-LD:

```csharp
public class ProductModel(IOptions<SeoMetaOptions> seoOptions) : PageModel
{
    public HtmlString SeoHead { get; private set; } = HtmlString.Empty;

    public async Task OnGetAsync(string slug)
    {
        var product = await catalogue.GetAsync(slug);

        SeoHead = new HtmlString(new SeoBuilder(seoOptions.Value)
            .Title(product.Name)
            .Description(product.Summary)
            .Canonical($"/products/{product.Slug}")
            .Image(product.ImageUrl)
            .OpenGraphType("product")
            .Build());
    }
}
```

```cshtml
<head>
    @Model.SeoHead
</head>
```

**Nuance:** all meta content is HTML-encoded on the way out. Titles and descriptions routinely come from user or database content, and an unencoded quote would close the attribute and let the rest of the value become markup.

## Structured data

### Built-in schemas

`SeoBuilder.JsonLd` accepts any of the built-in schema types and emits a `<script type="application/ld+json">` block:

```csharp
var head = new SeoBuilder(options)
    .Title(product.Name)
    .Canonical($"/products/{product.Slug}")
    .JsonLd(new ProductSchema
    {
        Name = product.Name,
        Description = product.Summary,
        Sku = product.Sku,
        Image = [product.ImageUrl],
        Brand = new OrganisationSchema { Name = "Example" },
        Offers = new OfferSchema
        {
            Price = product.Price,
            PriceCurrency = "GBP",
            Availability = "https://schema.org/InStock",
            Url = $"https://example.com/products/{product.Slug}"
        }
    })
    .Build();
```

`@context` and `@type` are written for you and always come first. Null properties are omitted, so a partly-populated schema stays valid.

### Breadcrumbs

`BreadcrumbListSchema` numbers positions as you add, so the trail cannot drift out of order:

```csharp
var crumbs = new BreadcrumbListSchema()
    .Add("Home", "https://example.com/")
    .Add("Products", "https://example.com/products")
    .Add("Widget");   // no URL — this is the current page

var head = new SeoBuilder(options).JsonLd(crumbs).Build();
```

This is what lets a search result render the site hierarchy instead of a raw URL.

### More than one block

Call `JsonLd` repeatedly. Each becomes its own script block, which is what search engines expect for unrelated entities:

```csharp
new SeoBuilder(options)
    .JsonLd(new WebSiteSchema { Name = "Example", Url = "https://example.com" })
    .JsonLd(new OrganisationSchema { Name = "Example Ltd", Url = "https://example.com" })
    .Build();
```

### Types the built-ins do not cover

Derive from `SchemaObject` and override `Type`:

```csharp
public sealed class RecipeSchema : SchemaObject
{
    public override string Type => "Recipe";

    public string? Name { get; set; }
    public string? RecipeYield { get; set; }
    public List<string>? RecipeIngredient { get; set; }
}
```

Property names are camel-cased on serialisation, so `RecipeIngredient` becomes `recipeIngredient` — which is what schema.org expects.

Or skip the type entirely and hand over any object:

```csharp
new SeoBuilder(options).JsonLd(new
{
    context = "https://schema.org",
    type = "FAQPage"
});
```

**Nuance:** the anonymous form gets no help with `@context` or `@type` — camel-casing will not turn `context` into `@context`. Deriving from `SchemaObject` is the safer route unless you are shaping the payload deliberately.

**Nuance:** JSON-LD is serialised with the **default** encoder, not the relaxed one. That matters: the relaxed encoder leaves `<` unescaped, so a `</script>` sequence in any database-sourced field — a product name, an article headline — would close the script element and execute whatever followed. The default encoder escapes it, so the payload cannot break out. This is worth knowing if you ever consider swapping the serialiser.

## Pipeline ordering

The single most common way to get no benefit from any of this:

```csharp
var app = builder.Build();

app.UseSeo();          // must come first
app.UseWebDefaults();  // registers the bot filter
```

The bot filter blocks detected crawlers by default. Register it before the SEO endpoints and every search engine gets a 403 from the two files that exist purely for them. See [Client profiling](ClientProfiling-Guide.md) for what the filter does and how to allow specific agents through.

## Next steps

- [Setup](SEO-Setup.md) — registration, every option, and its default.
- [API Reference](SEO-API.md)
