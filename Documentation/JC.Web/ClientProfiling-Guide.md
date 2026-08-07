# JC.Web: Client Profiling — Guide

Covers reading per-request metadata, structured logging with masking, client IP resolution behind proxies, geo-location providers, bot filtering, and rate limiting. See [Setup](ClientProfiling-Setup.md) for registration.

Rate limiting belongs to this area rather than to security because it is built on client profiling: its default partition is the client IP, resolved by the same `ClientIpResolver` and governed by the same `ClientIpOptions` as the rest of this area.

## Request metadata

### Accessing request metadata

`RequestMetadataMiddleware` builds a `RequestMetadata` object for each request and stores it in `HttpContext.Items`. Retrieve it anywhere you have access to `HttpContext`:

```csharp
public class DashboardService(IHttpContextAccessor accessor)
{
    public string GetDashboardView()
    {
        var metadata = accessor.HttpContext?.GetRequestMetadata();
        if (metadata is null) return "Default";

        return metadata.UserAgent.DeviceType switch
        {
            DeviceType.Mobile or DeviceType.Tablet => "Compact",
            _ => "Full"
        };
    }

    public string GetClientSummary()
    {
        var metadata = accessor.HttpContext?.GetRequestMetadata();
        var ip = metadata?.ClientIp;
        var browser = $"{metadata?.UserAgent.Browser} {metadata?.UserAgent.BrowserVersion}";
        var os = metadata?.UserAgent.OperatingSystem;

        return $"{ip} — {browser} on {os}";
    }
}
```

`GetRequestMetadata()` returns `null` when the middleware has not run — either because `UseRequestMetadata()` was never called, or because the code is running before it in the pipeline. Treat `null` as "unknown client" rather than an error.

### Logging request metadata

`ToLogEntry()` serialises the metadata to JSON with sensitive fields masked by default:

```csharp
var metadata = context.GetRequestMetadata();

// All sensitive fields masked (IP, path, query, origin, referer, city)
var masked = metadata.ToLogEntry();

// Selectively unmask
var unmasked = metadata.ToLogEntry(
    maskIp: false,
    maskPath: false,
    maskQuery: true,
    maskOrigin: true,
    maskReferer: true,
    maskCity: true
);
```

Masking uses `StringExtensions.Mask(0)` from JC.Core, which replaces the entire value with asterisks rather than partially redacting it. The masked entry therefore tells you a field was present, not what it contained.

The defaults are deliberately conservative: an IP address is personal data under UK GDPR, and query strings and referers routinely carry identifiers, search terms and tokens. Unmask deliberately and per call site, rather than reaching for `ToLogEntry(false, false, false, false, false, false)` as a habit.

## Client IP resolution

By default the client IP comes from `HttpContext.Connection.RemoteIpAddress`. When `TrustProxyHeaders` is enabled, proxy headers are checked first, in this order:

1. `CF-Connecting-IPv6` (Cloudflare)
2. `CF-Connecting-IP` (Cloudflare)
3. `X-Real-IP` (nginx)
4. `X-Forwarded-For` (first entry in the comma-separated list)

If none are present, resolution falls back to `RemoteIpAddress`, and then to the literal string `"unknown"` if that is also unavailable — so `ClientIp` is never null, and code that keys on it should expect `"unknown"` as a real value.

**Only enable `TrustProxyHeaders` behind a trusted reverse proxy.** On a directly exposed application these headers are set by the caller, so an attacker can present any IP they choose. That matters more than it first appears: a spoofed IP defeats rate-limit partitioning, poisons audit trails, and can be used to frame another address for abuse.

Where the application sits behind a proxy, ASP.NET Core's own `UseForwardedHeaders()` with a configured trusted-proxy list is the stricter option, because it validates the hop chain rather than trusting the first header it finds. `TrustProxyHeaders` exists for setups where that is impractical.

## Geo-location

### Implementing a geo-location provider

JC.Web ships no geo-location implementation — the data sources are all licensed or rate-limited, so the choice is yours. Implement `IGeoLocationProvider`:

```csharp
public class MaxMindGeoProvider : IGeoLocationProvider
{
    public GeoLocation? Resolve(string ipAddress, GeoLocationOptions options)
    {
        var result = _reader.City(ipAddress);

        return new GeoLocation(
            country: result.Country.Name,
            countryCode: result.Country.IsoCode,
            region: options.IncludeRegion ? result.MostSpecificSubdivision.Name : null,
            city: options.IncludeCity ? result.City.Name : null
        );
    }
}
```

Register it with the generic overload:

```csharp
builder.Services.AddClientProfiling<MaxMindGeoProvider>();
```

Two things worth knowing when writing a provider:

**It runs on every request.** `RequestMetadataMiddleware` calls `ResolveAsync` for each request that reaches it. A provider hitting a remote API without caching adds that latency to every page load, and will exhaust a rate-limited plan quickly. Local database lookups (MaxMind's `.mmdb` files) avoid this; API-backed providers should cache.

**Return `null` rather than throwing.** An unresolvable IP is normal — private ranges, `"unknown"`, and IPv6 addresses absent from the dataset all occur routinely. `RequestMetadata.GeoLocation` is nullable precisely so this is expressible.

Without a registered provider, `EmptyGeoLocationProvider` is used and `RequestMetadata.GeoLocation` is always `null`.

## Bot filtering

### How bots are detected

`UserAgentService` parses user agent strings with UAParser. A request is classified as a bot when:

- the parsed UA family contains `bot`, `crawler`, `spider` or `slurp`; or
- the raw UA string contains `bot/`, `crawler`, `spider`, `headlesschrome`, `phantomjs` or `lighthouse`.

This is user-agent inspection, not verification. A bot that presents a browser user agent is not detected, and a real browser running an automation harness may be. Treat it as a way to keep well-behaved crawlers off expensive endpoints, not as a defence against a determined scraper.

### Allowing specific bots

```csharp
builder.Services.AddClientProfiling(configureBotFilter: bots =>
{
    bots.AllowedBots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Googlebot",
        "Bingbot",
        "Slurp"  // Yahoo
    };
});
```

Allowed bots are matched against the parsed browser name, case-insensitively. Note the default is an **empty** set — no bots are allowed until you list them.

### Filtering specific paths

By default every path is subject to filtering. Use `PathFilter` to narrow it:

```csharp
bots.PathFilter = path => path.StartsWith("/api");
// Only API routes are protected — bots can still reach public pages
```

When `PathFilter` is set, only requests where the predicate returns `true` are checked. This is usually the better lever for a public site: restrict filtering to the endpoints that are expensive or private, rather than maintaining an allowlist of every legitimate crawler.

### Choosing a response code

```csharp
bots.StatusCode = BotFilterStatusCode.NotFound; // 404 instead of 403
```

Available: `NoContent` (204), `BadRequest` (400), `Unauthorized` (401), `Forbidden` (403, default), `NotFound` (404).

`NotFound` reveals least — it does not confirm the endpoint exists. `NoContent` is the gentlest for a crawler, since it is a success status and will not accumulate as errors in a webmaster console.

### Bot filtering and SEO

The bot filter and the [SEO area](SEO-Guide.md) pull in opposite directions, and their defaults conflict. `/sitemap.xml` and `/robots.txt` exist solely to be read by crawlers, and the bot filter blocks every crawler on every path unless told otherwise.

Register the SEO middleware **before** the bot filter so those two paths are served and short-circuited before filtering runs:

```csharp
app.UseSeo();             // serves /sitemap.xml and /robots.txt
app.UseClientProfiling(); // bot filter never sees those requests
```

The alternative is to exempt them by path:

```csharp
bots.PathFilter = path => !path.StartsWith("/sitemap") && path != "/robots.txt";
```

Ordering is the more reliable of the two, because it cannot drift out of step with a changed sitemap path. Getting this wrong is quiet — the site simply stops being indexed, with nothing in the logs beyond a 403 to a crawler.

## Rate limiting

### Basic usage

Rate limiting needs no code beyond registration — it applies globally to every request:

```csharp
builder.Services.AddRateLimiting(o =>
{
    o.PermitLimit = 20;
    o.Window = TimeSpan.FromMinutes(1);
    o.PartitionBy = RateLimitPartitionBy.ClientIpAndEndpoint;
});

var app = builder.Build();
app.UseRateLimiting();
```

Each client now gets 20 requests per minute to each path. A visitor hammering `/login` cannot exhaust the allowance for `/search`, and one visitor cannot exhaust another's. Requests over the limit receive `429 Too Many Requests` with no further pipeline execution.

### Strategies

| Strategy | Description |
|----------|-------------|
| `FixedWindow` | Allows `PermitLimit` requests per `Window`. The counter resets at the end of each window |
| `SlidingWindow` | Like fixed window, but divided into `SegmentsPerWindow` segments. Smooths out bursts at a window boundary |
| `TokenBucket` | Tokens replenish at `TokensPerPeriod` per `Window`, up to `TokenLimit`. Each request consumes one |
| `Concurrency` | Limits in-flight requests to `ConcurrencyLimit` rather than rate |

`SlidingWindow` is the default and usually the right choice. `FixedWindow` allows a client to spend its whole allowance at the end of one window and again at the start of the next — effectively double the limit across that boundary. `Concurrency` measures something different from the others: it caps simultaneous requests, so a slow endpoint reaches the limit at a much lower request rate than a fast one.

### Partition strategies

| Partition | Key | Use case |
|-----------|-----|----------|
| `ClientIp` | Client IP address | General protection of anonymous endpoints |
| `User` | Authenticated user name, falling back to endpoint path | Per-user limits |
| `Endpoint` | Request path | Per-endpoint limits |
| `ClientIpAndEndpoint` | `"{ip}:{path}"` | Per-IP, per-endpoint limits |

`User` falls back to the **endpoint path** for anonymous requests, not to the IP. Every unauthenticated caller of a given path therefore shares one bucket. If anonymous traffic is what you are protecting against, `ClientIp` or `ClientIpAndEndpoint` is the correct choice — `User` effectively stops distinguishing callers at exactly the point you need it to.

### Rate limiting and client IP

Behind a proxy, IP partitioning only works if client profiling is told to trust the proxy's headers:

```csharp
builder.Services.AddClientProfiling(configureClientIp: ip => ip.TrustProxyHeaders = true);
builder.Services.AddRateLimiting(o => o.PartitionBy = RateLimitPartitionBy.ClientIp);
```

Both IP-based partitions resolve the key through `ClientIpResolver`, honouring `ClientIpOptions.TrustProxyHeaders` — the same setting and the same resolver that produce `RequestMetadata.ClientIp`, so the two always agree for a given request.

**Without that first line, the limit stops being per-client.** The key comes from `RemoteIpAddress`, which is the proxy's own address on every request, so all visitors collapse into a single partition:

```
TrustProxyHeaders = false, behind a proxy, PermitLimit = 100
  visitor A ─┐
  visitor B ─┼─→ one bucket, 100 requests/min shared between everyone
  visitor C ─┘

TrustProxyHeaders = true
  visitor A ──→ own bucket, 100/min
  visitor B ──→ own bucket, 100/min
  visitor C ──→ own bucket, 100/min
```

The failure is quiet: no error, no warning, just 429s appearing under ordinary traffic with no single client at fault, and legitimate users throttling each other. Set `TrustProxyHeaders = true` when behind a trusted proxy — and only then, since these headers are caller-controlled on a directly exposed application.

When changing this on an existing deployment, revisit your limits. A value tuned while it was applying site-wide becomes far more permissive once it applies per client.

### Static file exclusion

With `ExcludeStaticFiles` at its default of `true`, requests for static assets bypass the limiter entirely, so CSS, JS and images do not consume a visitor's budget. Excluded extensions: `.css`, `.js`, `.png`, `.jpg`, `.jpeg`, `.gif`, `.svg`, `.ico`, `.woff`, `.woff2`, `.ttf`, `.eot`, `.map`, `.webp`, `.avif`, `.bmp`.

The check is on file extension alone, so a static asset served from an extensionless route is still counted, and a dynamic endpoint ending in `.js` is not. Where either matters, partition by `Endpoint` so the two do not share a budget.

### Scope

The limiter is registered globally — every request passes through it. There is no per-endpoint opt-in, so "rate limit only these routes" is expressed by partitioning rather than by exclusion: `ClientIpAndEndpoint` gives each path its own budget per client, so a busy endpoint cannot exhaust the allowance for the rest of the site.

## Next steps

- [Setup](ClientProfiling-Setup.md) — registration, options, and middleware ordering.
- [API Reference](ClientProfiling-API.md)
