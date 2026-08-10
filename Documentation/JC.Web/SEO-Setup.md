# JC.Web: SEO — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project using Razor Pages or MVC views
- Routing registered, which every web application has — sitemap route discovery reads the application's endpoints
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

## 0. Add the package

Add a project reference to `JC.Web`:

```xml
<ProjectReference Include="path/to/JC.Web/JC.Web.csproj" />
```

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

**SEO is opt-in.** Unlike the UI services, it is deliberately absent from `AddWebDefaults` — internal tools and line-of-business applications have no use for it, and its configuration is specific enough per application that folding it in would bloat the defaults.

### Services — `Program.cs`

```csharp
// Registers all three parts: sitemap, robots.txt and meta defaults
builder.Services.AddSeo(builder.Configuration);
```

### Middleware — `Program.cs`

```csharp
var app = builder.Build();

// Serves /sitemap.xml and /robots.txt
app.UseSeo();

// Anything that blocks crawlers must come after
app.UseWebDefaults();
```

**Ordering is not optional.** `UseSeo` must come **before** `UseBotFilter`, and therefore before `UseWebDefaults`, which registers it. The bot filter blocks every detected crawler by default; a sitemap and a robots.txt exist for nothing but crawlers. Registered the other way round, the search engines these were written for receive a 403 — and some crawlers treat an unreadable robots.txt as permission to crawl everything.

### Tag helper — `_ViewImports.cshtml`

```cshtml
@addTagHelper *, JC.Web
```

This enables `<seo-meta>`. Without it, Razor treats it as an unknown HTML element and renders it literally into the page rather than raising an error.

### Configuration — `appsettings.json`

Optional. Everything can be set in code instead, but keeping rules in configuration lets a staging environment behave differently without a branch in startup:

```json
{
  "Web": {
    "SEO": {
      "Sitemap": {
        "BaseUrl": "https://example.com"
      },
      "Robots": {
        "Rules": [
          { "UserAgent": "*", "Disallow": [ "/admin" ] }
        ]
      },
      "Meta": {
        "SiteName": "Example",
        "TitleSuffixSeparator": " | ",
        "BaseUrl": "https://example.com"
      }
    }
  }
}
```

Pass the **root** configuration to `AddSeo`, not `builder.Configuration.GetSection("Web:SEO")`. Each part resolves its own section from the root, so a pre-sectioned instance would be searched for `Web:SEO:Web:SEO:Sitemap` and bind nothing.

### Defaults

With `AddSeo(builder.Configuration)` and no further configuration:

| Default | Value |
|---------|-------|
| Sitemap path | `/sitemap.xml` |
| robots.txt path | `/robots.txt` |
| Route discovery | On for Razor Pages, off for MVC controllers |
| Excluded route prefixes | `/api` and `/_` |
| URLs per sitemap file | 50,000 — the sitemap protocol's limit |
| Sitemap `Cache-Control` | `public, max-age=3600` |
| `lastmod` for discovered URLs | The application's start time |
| Base URL | The request's scheme and host |
| robots.txt rules | None — a single permissive `User-agent: *` group with nothing disallowed |
| `Sitemap:` directive | Written, but only when a sitemap is actually registered |
| Page indexing | `index,follow` |
| Site name, title suffix, default image, default description, Twitter handle | None |

Behaviour you get without configuring anything: `/sitemap.xml` listing every non-parameterised Razor Page reachable by GET outside `/api` and `/_`, `/robots.txt` permitting everything and pointing at that sitemap, and `<seo-meta>` emitting a title, description, canonical, robots directive, Open Graph and Twitter tags from whatever each page supplies.

## 2. Full configuration

### AddSeo — the whole area

Registers all three parts. Equivalent to calling `AddSitemap`, `AddRobots` and `AddSeoMeta` with the same configuration.

```csharp
builder.Services.AddSeo(
    builder.Configuration,
    sitemap: options =>
    {
        options.BaseUrl = "https://example.com";
        options.Path = "/sitemap.xml";
        options.DiscoverRoutes = true;
        options.DiscoverControllers = false;
        options.ExcludePrefixes = ["/api", "/_"];
        options.MaxUrlsPerFile = 50_000;
        options.ClientCacheDuration = TimeSpan.FromHours(1);
    },
    robots: options =>
    {
        options.Path = "/robots.txt";
        options.IncludeSitemapDirective = true;
        options.Disallow("*", "/admin");
    },
    meta: options =>
    {
        options.SiteName = "Example";
        options.TitleSuffixSeparator = " | ";
        options.BaseUrl = "https://example.com";
        options.DefaultIndex = true;
        options.DefaultFollow = true;
    });
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configuration` | `IConfiguration?` | `null` | Root configuration. Each part binds its own section from it. Pass `null` to configure entirely in code |
| `sitemap` | `Action<SitemapOptions>?` | `null` | Applied after configuration binding, so code wins over `appsettings.json` |
| `robots` | `Action<RobotsOptions>?` | `null` | As above |
| `meta` | `Action<SeoMetaOptions>?` | `null` | As above |

Register the three parts individually when an application needs only some of them — a site with no sitemap still benefits from robots.txt and meta tags.

### AddSitemap — sitemap generation

Two overloads: one taking only a callback, one taking configuration as well.

```csharp
builder.Services.AddSitemap(builder.Configuration, options =>
{
    options.BaseUrl = "https://example.com";
    options.Path = "/sitemap.xml";
    options.DiscoverRoutes = true;
    options.DiscoverControllers = false;
    options.ExcludePrefixes = ["/api", "/_"];
    options.MaxUrlsPerFile = 50_000;
    options.ClientCacheDuration = TimeSpan.FromHours(1);

    // Explicit entries, merged with discovered routes and provider results
    options.Add("/about", priority: 0.8, changeFrequency: ChangeFrequency.Monthly);
});
```

#### `SitemapOptions`

Bound from `Web:SEO:Sitemap`, available as `SitemapOptions.ConfigSection`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `BaseUrl` | `string?` | `null` | Absolute base URL used to resolve site-relative locations. When null, the request's scheme and host are used |
| `Path` | `string` | `/sitemap.xml` | Path the sitemap is served from. Numbered files derive from it — `/sitemap-1.xml`, `/sitemap-2.xml` |
| `DiscoverRoutes` | `bool` | `true` | Whether Razor Page routes are discovered from the application's endpoints |
| `DiscoverControllers` | `bool` | `false` | Whether discovery also covers MVC controller actions. Off by default, since controller routes more often expose endpoints that should not be indexed |
| `ExcludePrefixes` | `List<string>` | `["/api", "/_"]` | Route prefixes excluded from discovery. Compared case-insensitively |
| `MaxUrlsPerFile` | `int` | `50000` | URLs per file before the sitemap splits and `Path` serves an index instead. 50,000 is the protocol limit |
| `ClientCacheDuration` | `TimeSpan` | 1 hour | Value for the `Cache-Control: public, max-age` header. `TimeSpan.Zero` omits the header |
| `Urls` | `List<SitemapUrl>` | empty | URLs added explicitly, merged with discovered routes and provider results |

**`BaseUrl` is worth setting explicitly.** The request-host fallback reports the *internal* host behind a reverse proxy unless forwarded headers are configured, which produces a sitemap full of URLs no crawler can reach — and does so silently.

**`ClientCacheDuration` is downstream caching only.** The sitemap is regenerated whenever a request actually reaches the server, so every provider runs again on each cache miss.

#### `SitemapOptions.Add`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `location` | `string` | — | A site-relative path such as `/about`, or an absolute URL |
| `priority` | `double?` | `null` | Relative importance from 0.0 to 1.0. Values outside the range are clamped when written |
| `changeFrequency` | `ChangeFrequency?` | `null` | Expected change frequency |
| `lastModified` | `DateTime?` | `null` | Last-modified timestamp |

Returns the options instance, so calls chain.

### AddSitemapProvider — URLs discovery cannot find

Route discovery skips parameterised routes such as `/products/{slug}`, because it has no way to know the values. Supply those from a provider:

```csharp
builder.Services.AddSitemapProvider<ProductSitemapProvider>();
```

| Type parameter | Constraint | Description |
|-----------|------|-------------|
| `TProvider` | `class, ISitemapUrlProvider` | The provider implementation |

Registered **scoped**, so a provider may depend on a `DbContext`. Call once per provider — every registered provider is invoked and the results merged. See the [Guide](SEO-Guide.md#supplying-urls-from-the-database) for an implementation.

### AddRobots — robots.txt

```csharp
builder.Services.AddRobots(builder.Configuration, options =>
{
    options.Path = "/robots.txt";
    options.IncludeSitemapDirective = true;
    options.SitemapUrl = "https://example.com/sitemap.xml";
    options.AdditionalSitemaps = ["https://example.com/news-sitemap.xml"];

    options.Disallow("*", "/admin", "/account");
    options.Allow("Googlebot", "/admin/public");
});
```

#### `RobotsOptions`

Bound from `Web:SEO:Robots`, available as `RobotsOptions.ConfigSection`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Path` | `string` | `/robots.txt` | Path robots.txt is served from. This is the only location crawlers look in — change it only if something else already serves that path |
| `Rules` | `List<RobotsRule>` | empty | The user-agent groups written to the file. When empty, a single permissive `User-agent: *` group is written with nothing disallowed |
| `IncludeSitemapDirective` | `bool` | `true` | Whether to append a `Sitemap:` directive |
| `SitemapUrl` | `string?` | `null` | Explicit absolute sitemap URL. When null, the URL is derived from the registered sitemap options |
| `AdditionalSitemaps` | `List<string>` | empty | Further absolute sitemap URLs to advertise, for sitemaps this application does not serve itself. Written whatever `IncludeSitemapDirective` says |

**The `Sitemap:` directive is never written for a sitemap that does not exist.** With `IncludeSitemapDirective` on but `AddSitemap` never called, and no explicit `SitemapUrl`, the directive is omitted rather than pointing at a 404.

#### `RobotsOptions.Disallow` and `Allow`

| Parameter | Type | Description |
|-----------|------|-------------|
| `userAgent` | `string` | The crawler this applies to, or `*` for all. Matched case-insensitively against existing groups |
| `paths` | `params string[]` | One or more paths |

Both create the group if it does not exist and append to it otherwise, and both return the options instance so calls chain.

#### `RobotsRule`

The shape bound from configuration, and what `Rules` holds.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `UserAgent` | `string` | `*` | The crawler this group applies to |
| `Disallow` | `List<string>` | empty | Paths this crawler must not fetch. A single `/` blocks the whole site |
| `Allow` | `List<string>` | empty | Paths this crawler may fetch, carving exceptions out of a broader `Disallow` |
| `CrawlDelay` | `int?` | `null` | Seconds a crawler should wait between requests. Omitted when null. Google ignores it; most other crawlers honour it |

`Allow` lines are written before `Disallow` lines within a group.

### AddSeoMeta — site-wide meta defaults

```csharp
builder.Services.AddSeoMeta(builder.Configuration, options =>
{
    options.SiteName = "Example";
    options.TitleSuffixSeparator = " | ";
    options.BaseUrl = "https://example.com";
    options.DefaultImage = "/img/social-card.png";
    options.DefaultDescription = "An example site.";
    options.TwitterSite = "@example";
    options.DefaultIndex = true;
    options.DefaultFollow = true;
});
```

#### `SeoMetaOptions`

Bound from `Web:SEO:Meta`, available as `SeoMetaOptions.ConfigSection`. These are the fallbacks `SeoBuilder` and `<seo-meta>` apply when a page supplies nothing.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SiteName` | `string?` | `null` | Used for `og:site_name`, and as the title suffix when `TitleSuffixSeparator` is set |
| `TitleSuffixSeparator` | `string?` | `null` | Placed between a page title and `SiteName`, for example `" | "`. When null the title is emitted exactly as supplied |
| `BaseUrl` | `string?` | `null` | Absolute base URL used to resolve relative canonical and image URLs |
| `DefaultImage` | `string?` | `null` | Fallback for `og:image` and the Twitter card |
| `DefaultDescription` | `string?` | `null` | Fallback description |
| `TwitterSite` | `string?` | `null` | Twitter `@handle`, emitted as `twitter:site` |
| `DefaultIndex` | `bool` | `true` | Whether pages are indexable unless they say otherwise |
| `DefaultFollow` | `bool` | `true` | Whether links are followed unless a page says otherwise |

**Set `DefaultIndex = false` on staging.** Every page then emits `noindex` unless it opts in, which is the cheapest protection against a staging environment being indexed.

**Canonical URLs and `og:image` should be absolute.** Without `BaseUrl`, a page supplying a relative path emits that relative value unchanged, and a relative canonical is widely ignored.

### Middleware — individual registration

`UseSeo` calls both. Register them separately when an application serves only one:

```csharp
app.UseSitemap();   // requires AddSitemap
app.UseRobots();    // requires AddRobots

app.UseWebDefaults();
```

Both only handle `GET`; any other method passes straight through. Both must precede the bot filter.

## 3. Verify

1. Request `/robots.txt` — it should return `text/plain` with a `User-agent:` group and, when a sitemap is registered, a `Sitemap:` line pointing at an absolute URL.
2. Request `/sitemap.xml` — it should return `application/xml` with a `<urlset>` listing your Razor Pages. If it returns your application's 404 page instead, `UseSitemap` is missing or registered after something that handled the request first; if it returns 403, it is registered after the bot filter.
3. Add `<seo-meta title="Test" />` to a view and check the rendered source for a `<title>` and the `og:` and `twitter:` meta tags.

## Next steps

- [Guide](SEO-Guide.md) — route discovery and its exclusions, database-backed URLs, splitting, robots rules, per-page meta, and JSON-LD structured data.
- [API Reference](SEO-API.md)
