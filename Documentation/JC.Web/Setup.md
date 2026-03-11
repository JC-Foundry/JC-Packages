# JC.Web — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project with JC.Core registered
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

## 0. Add the package

Add a project reference to `JC.Web`:

```xml
<ProjectReference Include="path/to/JC.Web/JC.Web.csproj" />
```

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();

// Register all JC.Web services: security headers, cookie services, and client profiling
builder.Services.AddWebDefaults(builder.Configuration);

// Optional: rate limiting (not included in WebDefaults — opt-in)
builder.Services.AddRateLimiting();
```

### Middleware — `Program.cs`

```csharp
var app = builder.Build();

// Apply security headers and client profiling middleware
app.UseWebDefaults();

// Optional: rate limiting middleware (must match AddRateLimiting above)
app.UseRateLimiting();
```

### Configuration — `appsettings.json`

Required when encrypted cookies are enabled (the default):

```json
{
  "Cookies": {
    "DataProtection_Path": "/path/to/keys"
  }
}
```

### Defaults

When called with no configuration callbacks, `AddWebDefaults` registers:

| Registration | Lifetime | Description |
|-------------|----------|-------------|
| `IOptions<SecurityHeaderOptions>` | Singleton | Security header configuration |
| `ICookieService` → `CookieService` | Scoped | Unencrypted cookie service (unkeyed injection) |
| `ICookieService` → `CookieService` | Scoped (keyed) | Keyed as `ICookieService.StandardCookieDIKey` |
| `ICookieService` → `EncryptedCookieService` | Scoped (keyed) | Keyed as `ICookieService.EncryptedCookieDIKey` |
| `CookieProfileDictionary` | Singleton | Cookie profile registry |
| `UserAgentService` | Singleton | User agent string parser (UAParser) |
| `IGeoLocationProvider` → `EmptyGeoLocationProvider` | Singleton | No-op geo-location (returns `null`) |
| `IOptions<BotFilterOptions>` | Singleton | Bot filter configuration |
| `IOptions<ClientIpOptions>` | Singleton | Client IP resolution configuration |
| `IOptions<CookieDefaultOptions>` | Singleton | Global cookie defaults |

Default security headers applied to all responses:

| Header | Default value |
|--------|---------------|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `SAMEORIGIN` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Permissions-Policy` | `geolocation=(), microphone=(), camera=()` |
| `Strict-Transport-Security` | `max-age=15552000` (180 days, HTTPS only, production only) |
| `Server` | Removed |
| `X-Powered-By` | Removed |
| `Content-Security-Policy` | Not set (no CSP by default) |
| `Cross-Origin-Opener-Policy` | Not set |
| `Cross-Origin-Resource-Policy` | Not set |
| `Cross-Origin-Embedder-Policy` | Not set |

Default cookie options applied to all cookies created through `ICookieService`:

| Option | Default |
|--------|---------|
| `HttpOnly` | `true` |
| `Secure` | `true` |
| `SameSite` | `Lax` |
| `Path` | `"/"` |
| `MaxAge` | `null` (session cookie) |
| `Domain` | `null` (current request host) |
| `Expires` | `null` |
| `IsEssential` | `false` |

Default client profiling behaviour:

| Option | Default |
|--------|---------|
| Bot filtering | Enabled — all detected bots are blocked (403) |
| Allowed bots | None |
| Path filter | `null` (all paths filtered) |
| Proxy header trust | Disabled — uses `RemoteIpAddress` only |
| Geo-location | `EmptyGeoLocationProvider` (no-op, returns `null`) |

`UseWebDefaults` registers middleware in this order:
1. `UseSecurityHeaders()` — adds security headers to all responses
2. `UseClientProfiling()` — which registers:
   - `UseRequestMetadata()` — builds `RequestMetadata` (client IP, user agent, geo-location) and stores it in `HttpContext.Items`
   - `UseBotFilter()` — blocks detected bots unless they're in the allowed list

## 2. Full configuration

### AddWebDefaults — combined registration

Registers security headers, cookie services, and client profiling in one call. All parameters are optional.

```csharp
builder.Services.AddWebDefaults(
    configuration: builder.Configuration,
    useEncryptedCookies: true,
    configureHeaderFilter: headers =>
    {
        headers.EnableXContentTypeOptions = true;
        headers.XFrameOptions = XFrameOptionsMode.SameOrigin;
        headers.ReferrerPolicy = ReferrerPolicyMode.StrictOriginWhenCrossOrigin;
        headers.PermissionsPolicy = "geolocation=(), microphone=(), camera=()";
        headers.CrossOriginOpenerPolicy = null;
        headers.CrossOriginResourcePolicy = null;
        headers.CrossOriginEmbedderPolicy = null;
        headers.EnableHsts = true;
        headers.HstsMaxAge = TimeSpan.FromDays(180);
        headers.HstsIncludeSubDomains = false;
        headers.HstsProductionOnly = true;
        headers.RemoveServerHeader = true;
        headers.RemoveXPoweredByHeader = true;
        headers.ContentSecurityPolicy = null;
    },
    configureCookieFilter: cookies =>
    {
        cookies.HttpOnly = true;
        cookies.Secure = true;
        cookies.SameSite = SameSiteMode.Lax;
        cookies.Path = "/";
        cookies.MaxAge = null;
        cookies.Domain = null;
        cookies.Expires = null;
        cookies.IsEssential = false;
    },
    configureBotFilter: bots =>
    {
        bots.IsEnabled = true;
        bots.StatusCode = BotFilterStatusCode.Forbidden;
        bots.AllowedBots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bots.PathFilter = null;
    },
    configureClientIp: ip =>
    {
        ip.TrustProxyHeaders = false;
    }
);
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configuration` | `IConfiguration?` | `null` | Application configuration. Required when `useEncryptedCookies` is `true` — reads `Cookies:DataProtection_Path` |
| `useEncryptedCookies` | `bool` | `true` | Registers `EncryptedCookieService` alongside `CookieService` as keyed services, and configures Data Protection key storage |
| `configureHeaderFilter` | `Action<SecurityHeaderOptions>?` | `null` | Callback to configure security header options |
| `configureCookieFilter` | `Action<CookieDefaultOptions>?` | `null` | Callback to configure global cookie defaults |
| `configureBotFilter` | `Action<BotFilterOptions>?` | `null` | Callback to configure bot filtering |
| `configureClientIp` | `Action<ClientIpOptions>?` | `null` | Callback to configure client IP resolution |

### AddWebDefaults with geo-location provider

If you have an `IGeoLocationProvider` implementation (e.g. MaxMind, ip-api), use the generic overload to register it:

```csharp
builder.Services.AddWebDefaults<MyGeoLocationProvider>(
    configuration: builder.Configuration,
    useEncryptedCookies: true,
    configureGeoLocation: geo =>
    {
        geo.IncludeRegion = true;
        geo.IncludeCity = false;
    }
);
```

This overload accepts all the same parameters as `AddWebDefaults`, plus:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configureGeoLocation` | `Action<GeoLocationOptions>?` | `null` | Controls lookup granularity passed to your provider |

#### GeoLocationOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IncludeRegion` | `bool` | `true` | Whether to include region/state/province in the result |
| `IncludeCity` | `bool` | `false` | Whether to include city/town in the result |

Without the generic overload, `EmptyGeoLocationProvider` is registered (always returns `null`). The `RequestMetadata` will have no geo-location data.

### Standalone registration — security

If you only need security features (headers + cookies) without client profiling:

```csharp
builder.Services.AddSecurityDefaults(builder.Configuration, useEncryptedCookies: true);
```

Or register each individually:

```csharp
// Security headers only
builder.Services.AddSecurityHeaders(headers =>
{
    headers.EnableXContentTypeOptions = true;
    headers.XFrameOptions = XFrameOptionsMode.Deny;
    // ...
});

// Cookie services only
builder.Services.AddCookieServices(builder.Configuration, useEncryptedCookies: true, cookies =>
{
    cookies.HttpOnly = true;
    cookies.Secure = true;
    // ...
});
```

#### SecurityHeaderOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableXContentTypeOptions` | `bool` | `true` | Adds `X-Content-Type-Options: nosniff` |
| `XFrameOptions` | `XFrameOptionsMode?` | `SameOrigin` | `X-Frame-Options` value. `null` to omit. Values: `Deny`, `SameOrigin` |
| `ReferrerPolicy` | `ReferrerPolicyMode?` | `StrictOriginWhenCrossOrigin` | `Referrer-Policy` value. `null` to omit. Values: `NoReferrer`, `NoReferrerWhenDowngrade`, `Origin`, `OriginWhenCrossOrigin`, `SameOrigin`, `StrictOrigin`, `StrictOriginWhenCrossOrigin`, `UnsafeUrl` |
| `PermissionsPolicy` | `string?` | `"geolocation=(), microphone=(), camera=()"` | Raw `Permissions-Policy` header value. `null` to omit |
| `CrossOriginOpenerPolicy` | `CrossOriginOpenerPolicyMode?` | `null` | `Cross-Origin-Opener-Policy` value. `null` to omit. Values: `UnsafeNone`, `SameOriginAllowPopups`, `SameOrigin`, `NoOpenerAllowPopups` |
| `CrossOriginResourcePolicy` | `CrossOriginResourcePolicyMode?` | `null` | `Cross-Origin-Resource-Policy` value. `null` to omit. Values: `SameSite`, `SameOrigin`, `CrossOrigin` |
| `CrossOriginEmbedderPolicy` | `CrossOriginEmbedderPolicyMode?` | `null` | `Cross-Origin-Embedder-Policy` value. `null` to omit. Values: `UnsafeNone`, `RequireCorp`, `Credentialless` |
| `EnableHsts` | `bool` | `true` | Adds `Strict-Transport-Security` on HTTPS responses |
| `HstsMaxAge` | `TimeSpan` | `180 days` | HSTS `max-age` duration |
| `HstsIncludeSubDomains` | `bool` | `false` | Adds `includeSubDomains` to HSTS |
| `HstsProductionOnly` | `bool` | `true` | Only applies HSTS in production environments |
| `RemoveServerHeader` | `bool` | `true` | Removes the `Server` response header |
| `RemoveXPoweredByHeader` | `bool` | `true` | Removes the `X-Powered-By` response header |
| `ContentSecurityPolicy` | `Action<ContentSecurityPolicyBuilder>?` | `null` | Callback to build a `Content-Security-Policy` header using the fluent builder. `null` means no CSP header |

Options are validated eagerly at registration time — invalid values throw immediately rather than failing at runtime.

#### Cookie services — encryption modes

**With encryption (default):**

Both `CookieService` and `EncryptedCookieService` are registered as keyed services. Inject using `[FromKeyedServices]`:

```csharp
public class MyService(
    [FromKeyedServices(ICookieService.StandardCookieDIKey)] ICookieService cookies,
    [FromKeyedServices(ICookieService.EncryptedCookieDIKey)] ICookieService encryptedCookies)
```

Unkeyed `ICookieService` injection always resolves to the standard (unencrypted) service.

Requires `Cookies:DataProtection_Path` in configuration. The directory is created if it doesn't exist. Data Protection keys are persisted to this path.

```json
{
  "Cookies": {
    "DataProtection_Path": "/path/to/keys"
  }
}
```

**Without encryption:**

```csharp
builder.Services.AddCookieServices(useEncryptedCookies: false);
```

Only `CookieService` is registered. No `IConfiguration` or Data Protection path is needed.

#### CookieDefaultOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `HttpOnly` | `bool` | `true` | Inaccessible to client-side JavaScript |
| `Secure` | `bool` | `true` | Only sent over HTTPS |
| `SameSite` | `SameSiteMode` | `Lax` | SameSite attribute |
| `Path` | `string?` | `"/"` | URL path the cookie is valid for |
| `MaxAge` | `TimeSpan?` | `null` | Cookie lifetime. `null` = session cookie |
| `Domain` | `string?` | `null` | Cookie domain. `null` = current request host |
| `Expires` | `DateTimeOffset?` | `null` | Absolute expiration. If both `MaxAge` and `Expires` are set, `MaxAge` takes precedence per the HTTP spec |
| `IsEssential` | `bool` | `false` | Bypasses consent checks |

Individual cookies can override any of these defaults via `CookieDefaultOverride` when registering cookie profiles.

### Standalone registration — client profiling

```csharp
// Without geo-location
builder.Services.AddClientProfiling(
    configureBotFilter: bots =>
    {
        bots.IsEnabled = true;
        bots.StatusCode = BotFilterStatusCode.Forbidden;
        bots.AllowedBots = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Googlebot", "Bingbot" };
        bots.PathFilter = path => path.StartsWith("/api");
    },
    configureClientIp: ip =>
    {
        ip.TrustProxyHeaders = false;
    }
);

// With geo-location provider
builder.Services.AddClientProfiling<MyGeoLocationProvider>(
    configureBotFilter: bots => { /* ... */ },
    configureGeoLocation: geo =>
    {
        geo.IncludeRegion = true;
        geo.IncludeCity = false;
    },
    configureClientIp: ip =>
    {
        ip.TrustProxyHeaders = true;
    }
);
```

#### BotFilterOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IsEnabled` | `bool` | `true` | When `false`, all requests pass through without bot inspection |
| `StatusCode` | `BotFilterStatusCode` | `Forbidden` (403) | HTTP status returned to blocked bots. Values: `NoContent` (204), `BadRequest` (400), `Unauthorized` (401), `Forbidden` (403), `NotFound` (404) |
| `AllowedBots` | `HashSet<string>` | Empty (case-insensitive) | Bot browser names allowed through the filter (e.g. `"Googlebot"`) |
| `PathFilter` | `Func<string, bool>?` | `null` | When set, only requests matching this predicate are subject to bot filtering. `null` = all paths are filtered |

#### ClientIpOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `TrustProxyHeaders` | `bool` | `false` | When `true`, proxy headers (`CF-Connecting-IP`, `CF-Connecting-IPv6`, `X-Real-IP`, `X-Forwarded-For`) are checked **before** `RemoteIpAddress`. Enable when behind a trusted reverse proxy (Cloudflare, nginx, etc.) where `RemoteIpAddress` is the proxy's local address. **Do not enable if the application is directly exposed to the internet** — clients can spoof these headers |

Headers are checked in order: `CF-Connecting-IPv6`, `CF-Connecting-IP`, `X-Real-IP`, then the first entry in `X-Forwarded-For`.

### Rate limiting (opt-in)

Rate limiting is **not** included in `AddWebDefaults` — it must be registered and applied separately.

```csharp
// Register
builder.Services.AddRateLimiting(options =>
{
    options.IsEnabled = true;
    options.Strategy = RateLimitingStrategy.SlidingWindow;
    options.PermitLimit = 100;
    options.Window = TimeSpan.FromMinutes(1);
    options.SegmentsPerWindow = 6;
    options.PartitionBy = RateLimitPartitionBy.ClientIp;
    options.ExcludeStaticFiles = true;
    options.QueueLimit = 0;
    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    options.TokensPerPeriod = 10;
    options.TokenLimit = 0;
    options.ConcurrencyLimit = 0;
});

// Apply middleware
app.UseRateLimiting();
```

#### RateLimitingOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IsEnabled` | `bool` | `true` | When `false`, the rate limiter is not registered |
| `Strategy` | `RateLimitingStrategy` | `SlidingWindow` | The rate limiting algorithm. Values: `FixedWindow`, `SlidingWindow`, `TokenBucket`, `Concurrency` |
| `PermitLimit` | `int` | `100` | Maximum requests within the window (used by `FixedWindow` and `SlidingWindow`) |
| `Window` | `TimeSpan` | `1 minute` | Time window for rate limiting |
| `SegmentsPerWindow` | `int` | `6` | Segments per window for `SlidingWindow` (each segment = `Window / SegmentsPerWindow`). Ignored by other strategies |
| `PartitionBy` | `RateLimitPartitionBy` | `ClientIp` | How requests are grouped. Values: `ClientIp`, `User` (falls back to endpoint path for anonymous), `Endpoint`, `ClientIpAndEndpoint` |
| `ExcludeStaticFiles` | `bool` | `true` | When `true`, static file requests (.css, .js, .png, .jpg, .jpeg, .gif, .svg, .ico, .woff, .woff2, .ttf, .eot, .map, .webp, .avif, .bmp) bypass the rate limiter |
| `QueueLimit` | `int` | `0` | Requests to queue when the limit is reached. `0` = no queuing (immediate 429) |
| `QueueProcessingOrder` | `QueueProcessingOrder` | `OldestFirst` | Processing order for queued requests |
| `TokensPerPeriod` | `int` | `10` | Tokens added per window for `TokenBucket`. Ignored by other strategies |
| `TokenLimit` | `int` | `0` | Maximum bucket capacity for `TokenBucket`. When `0`, uses `PermitLimit`. Ignored by other strategies |
| `ConcurrencyLimit` | `int` | `0` | Maximum concurrent requests for `Concurrency`. When `0`, uses `PermitLimit`. Ignored by other strategies |

Rate limiting is applied as a global limiter — all requests are subject to it (except static files when `ExcludeStaticFiles` is `true`). The rejection status code is `429 Too Many Requests`.

`UseRateLimiting` checks `IOptions<RateLimitingOptions>.IsEnabled` at startup — if `false`, the middleware is skipped entirely.

### Middleware — individual registration

If you need control over middleware ordering, register each component individually instead of calling `UseWebDefaults()`:

```csharp
app.UseSecurityHeaders();
app.UseRequestMetadata();  // Builds RequestMetadata and stores it in HttpContext.Items
app.UseBotFilter();        // Must come after UseRequestMetadata — depends on RequestMetadata
```

`UseClientProfiling()` is equivalent to calling `UseRequestMetadata()` followed by `UseBotFilter()`.

### Cookie profile registration

Cookies must be registered as profiles at startup before they can be used through `ICookieService`. This is done on `IApplicationBuilder` after `Build()`:

```csharp
// Standard (unencrypted) cookies
app.PopulateStandardCookieProfiles(
    ("user-pref", null),
    ("theme", new CookieDefaultOverride(SameSiteMode.Strict))
);

// Encrypted cookies
app.PopulateEncryptedCookieProfiles(
    ("auth-token", "AuthTokenProtector", null),
    ("session-data", "SessionProtector", new CookieDefaultOverride(SameSiteMode.Strict, httpOnly: true))
);

// Or both at once
app.PopulateCookieProfiles(
    standardCookies: [("user-pref", null)],
    encryptedCookies: [("auth-token", "AuthTokenProtector", null)]
);
```

You can also pass pre-built `CookieProfile` instances instead of tuples. Duplicate cookie names throw `InvalidOperationException`.

## 3. Verify

1. Run the application.
2. Open browser developer tools and inspect response headers on any page — you should see `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, and `Permissions-Policy`.
3. If rate limiting is enabled, rapidly refresh a page — after exceeding the permit limit you should receive a `429 Too Many Requests` response.

## Next steps

- [Guide](Guide.md) — security headers, cookie management, client profiling, rate limiting behaviour, tag helpers, and UI helpers.
- [API Reference](API.md)
