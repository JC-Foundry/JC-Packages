# JC.Web

Hardening and helpers for ASP.NET Core, in five areas that register independently — security headers and cookies, client profiling, rate limiting, SEO, and UI tag helpers whose class names come from a swappable framework dictionary.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.Web/JC.Web.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

## Prerequisites

- .NET 9.0 SDK, and an ASP.NET Core project
- **JC.Core**, registered with `AddCore<AppDbContext>()`
- For the UI components: a CSS framework matching the one you register — Bootstrap 5, Tailwind v4, or jc-tailwind-ui
- For encrypted cookies: a writable Data Protection key directory

## Quick start

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();

// Security headers, cookies, client profiling and the UI services
builder.Services.AddWebDefaults(builder.Configuration);

// Opt-in, not part of the defaults
builder.Services.AddRateLimiting();
builder.Services.AddSeo(builder.Configuration);
```

### Middleware — `Program.cs`

```csharp
var app = builder.Build();

// SEO must precede the bot filter, which UseWebDefaults registers
app.UseSeo();

app.UseWebDefaults();
app.UseRateLimiting();
```

### Tag helpers — `_ViewImports.cshtml`

```cshtml
@addTagHelper *, JC.Web
```

### Configuration — `appsettings.json`

Required only for encrypted cookies, which are on by default:

```json
{
  "Web": {
    "Cookies": {
      "DataProtection_Path": "/path/to/keys"
    }
  }
}
```

Pass `useEncryptedCookies: false` to skip it.

## Feature areas

### Security headers

A response-header middleware with a fluent content-security-policy builder:

```csharp
builder.Services.AddSecurityHeaders(options =>
{
    options.ContentSecurityPolicy = csp => csp
        .DefaultSrc("'self'")
        .ScriptSrc("'self'", "https://cdn.example.com")
        .ImgSrc("'self'", "data:");
});
```

`ContentSecurityPolicy` is a callback rather than a built string, so the builder is handed to you and no CSP header is emitted at all while it stays null.

Options are validated eagerly, so an invalid header configuration fails at start-up.

### Cookies

Plain and Data-Protection-encrypted implementations, selected by keyed injection:

```csharp
public class PreferenceService(
    [FromKeyedServices(ICookieService.EncryptedCookieDIKey)] ICookieService cookies)
{
    public bool Remember(string theme) => cookies.TryCreateCookie("theme", theme);

    public string? Preferred() => cookies.GetCookie("theme");
}
```

Every cookie must have a `CookieProfile` registered at start-up before it can be read or written — `TryCreateCookie` returns `false` for an unregistered name rather than writing an unconfigured cookie. That is what keeps flags and lifetimes in one place instead of scattered across call sites.

### Client profiling

Per-request metadata — client IP, parsed user agent, optional geo-location — plus bot filtering:

```csharp
public class HomeModel : PageModel
{
    public void OnGet()
    {
        var metadata = HttpContext.GetRequestMetadata();
        var isBot = metadata?.UserAgent.IsBot ?? false;
    }
}
```

Proxy headers are trusted only when told to, since `RemoteIpAddress` behind a reverse proxy is the proxy. `RequestMetadata.ToLogEntry()` masks IP, path, query, origin, referer and city by default, so structured logging does not quietly become a personal-data store.

### Rate limiting

Opt-in, wrapping the framework's limiter with simpler partitioning:

```csharp
builder.Services.AddRateLimiting(options =>
{
    options.Strategy = RateLimitingStrategy.SlidingWindow;
    options.PermitLimit = 100;
    options.Window = TimeSpan.FromMinutes(1);
    options.PartitionBy = RateLimitPartitionBy.ClientIp;
});
```

Four strategies — fixed and sliding window, token bucket, concurrency — partitioned by IP, user, endpoint or IP-and-endpoint. Static files are excluded by default.

### SEO

A sitemap that discovers Razor routes, robots.txt, meta tags and typed JSON-LD:

```csharp
builder.Services.AddSeo(builder.Configuration, sitemap: o => o.BaseUrl = "https://example.com");
builder.Services.AddSitemapProvider<ProductSitemapProvider>();
```

```cshtml
<seo-meta title="Widget" description="A widget." canonical="/products/widget" />
```

The sitemap merges discovered routes, explicit entries and provider results, and splits behind an index past 50,000 URLs. **`UseSeo` must come before the bot filter** — otherwise the crawlers these files exist for get a 403.

### UI

Tag helpers and builders that name no framework of their own:

```cshtml
<alert type="Success" message="Saved." />
<pagination model="Model.Products" href-format="/products?page={0}" />
<breadcrumb>
    <crumb label="Home" href="/" />
    <crumb label="Products" />
</breadcrumb>
<bug-reporter endpoint="/api/feedback" />
```

Class names come from a dictionary chosen at registration:

```csharp
builder.Services.AddWebDefaults(builder.Configuration, uiFramework: UIFramework.Tailwind);
```

| `UIFramework` | Classes |
|---------------|---------|
| `Bootstrap` | Bootstrap 5 |
| `Tailwind` | Tailwind v4 utilities, reproducing Bootstrap's appearance |
| `CustomJCTailwind` | jc-tailwind-ui, using its tone engine so any colour composes |

Packages layered above register their own dictionaries against the same choice, so they cannot disagree about which framework is in play.

Under either Tailwind framework, import the shipped safelist — Tailwind cannot see class names inside a compiled assembly:

```css
@import "../path/to/JC.Web/UI/jc-web.tailwind.css";
```

### Content sanitisation

For HTML authored by a user, typically a rich-text editor. Sanitise on **write**, so the stored value is trustworthy for every reader:

```csharp
var clean = ContentSanitiser.SanitiseContent(model.Body);
var comment = new ContentSanitiser(ContentSanitiserOptions.Basic()).Sanitise(model.Comment);
```

Treat it as the only XSS control on that content — an editor's own cleanup runs in the browser and anything with a valid antiforgery token can post straight past it.

## Defaults

| Behaviour | Default |
|-----------|---------|
| `AddWebDefaults` | Security headers, cookies (encrypted), client profiling, UI services |
| Rate limiting | **Not** included — opt in with `AddRateLimiting` |
| SEO | **Not** included — opt in with `AddSeo` |
| UI framework / icon set | Bootstrap / Bootstrap Icons |
| Rate limiting strategy | Sliding window, 100 requests per minute, partitioned by client IP |
| Proxy header trust | Off |
| `ContentSanitiser` | `RichText` policy; not registered in DI |
| Sitemap / robots paths | `/sitemap.xml`, `/robots.txt` |

## Documentation

- Security — [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Web/Security-Setup.md) · [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Web/Security-Guide.md) · [API](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Web/Security-API.md)
- Client Profiling — [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Web/ClientProfiling-Setup.md) · [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Web/ClientProfiling-Guide.md) · [API](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Web/ClientProfiling-API.md)
- SEO — [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Web/SEO-Setup.md) · [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Web/SEO-Guide.md) · [API](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Web/SEO-API.md)
- UI — [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Web/UI-Setup.md) · [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Web/UI-Guide.md) · [API](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Web/UI-API.md)

Rate limiting is documented under Client Profiling, which supplies the IP resolution it partitions on.

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
