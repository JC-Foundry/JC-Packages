# JC.Web: Client Profiling — API reference

Complete reference of all public types, properties, and methods in the JC.Web client profiling area, including bot filtering and rate limiting. See [Setup](ClientProfiling-Setup.md) for registration and [Guide](ClientProfiling-Guide.md) for usage examples.

> **Note:** Registration extensions (`IServiceCollection`, `IApplicationBuilder`) and options classes are documented in [Setup](ClientProfiling-Setup.md), not here.

---

# Models

## RequestMetadata

**Namespace:** `JC.Web.ClientProfiling.Models`

Captures structured metadata about an HTTP request including client IP, user agent, protocol, and request properties. Built by `RequestMetadataMiddleware` and stored in `HttpContext.Items` for downstream access.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `ClientIp` | `string` | — | get; | The resolved client IP address. Never null — falls back to `"unknown"`. |
| `UserAgent` | `UserAgent` | — | get; | The parsed user agent information. |
| `GeoLocation` | `GeoLocation?` | `null` | get; | Geographic location resolved from the client IP, if a provider is registered. |
| `IsHttps` | `bool` | — | get; | Whether the request was made over HTTPS. |
| `RequestTimestamp` | `DateTimeOffset` | — | get; | UTC timestamp of when the request was processed by the middleware. |
| `RequestPath` | `string?` | `null` | get; | The HTTP method and request path (e.g. `"GET /api/users"`). |
| `RequestQuery` | `string?` | `null` | get; | The query string portion of the request URL. |
| `RequestOrigin` | `string?` | `null` | get; | The `Origin` header value, if present. |
| `RequestReferer` | `string?` | `null` | get; | The `Referer` header value, if present. |
| `RequestId` | `string?` | `null` | get; | The trace identifier from `HttpContext.TraceIdentifier`. |

### Constructor

#### RequestMetadata(string clientIp, UserAgent agent, bool isHttps, DateTimeOffset requestTimestamp, GeoLocation? geoLocation = null, string? requestPath = null, string? requestQuery = null, string? requestOrigin = null, string? requestReferer = null, string? requestId = null)

All properties are get-only and set via the constructor.

### Methods

#### ToLogEntry(bool maskIp = true, bool maskPath = true, bool maskQuery = true, bool maskOrigin = true, bool maskReferer = true, bool maskCity = true)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `maskIp` | `bool` | `true` | Whether to mask the client IP address. |
| `maskPath` | `bool` | `true` | Whether to mask the request path. |
| `maskQuery` | `bool` | `true` | Whether to mask the request query string. |
| `maskOrigin` | `bool` | `true` | Whether to mask the request origin. |
| `maskReferer` | `bool` | `true` | Whether to mask the request referer. |
| `maskCity` | `bool` | `true` | Whether to mask the city. |

Returns a JSON string representation of the request metadata for structured logging. Includes all request properties, user agent details (browser, version, OS, device type, bot flag, raw value), and geolocation data (country, country code, region, city).

Masked properties use `StringExtensions.Mask` from JC.Core with zero visible characters, so the value is replaced entirely rather than partially redacted. Every mask parameter defaults to `true` — see [Logging request metadata](ClientProfiling-Guide.md#logging-request-metadata) for why unmasking should be a per-call-site decision.

---

## UserAgent

**Namespace:** `JC.Web.ClientProfiling.Models`

Represents a parsed user agent with browser, operating system, and device type information. All properties are get-only, set via the constructor.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `RawValue` | `string` | — | get; | The original user agent string. |
| `Browser` | `string?` | — | get; | The detected browser name, or `null` if unrecognised. |
| `BrowserVersion` | `string?` | — | get; | The detected browser version, or `null`. |
| `OperatingSystem` | `string?` | — | get; | The detected operating system name, or `null`. |
| `OS` | `string?` | — | get; | Alias for `OperatingSystem`. |
| `OperatingSystemVersion` | `string?` | — | get; | The detected OS version, or `null`. |
| `OSVersion` | `string?` | — | get; | Alias for `OperatingSystemVersion`. |
| `DeviceType` | `DeviceType` | `Unknown` | get; | The detected device type. |
| `IsMobile` | `bool` | Computed | get; | `true` if `DeviceType` is `Mobile` or `Tablet`. |
| `IsBot` | `bool` | Computed | get; | `true` if `DeviceType` is `Bot`. This is what `BotFilterMiddleware` keys on. |

### Constructor

#### UserAgent(string rawValue, string? browser, string? browserVersion, string? os, string? osVersion, DeviceType type = DeviceType.Unknown)

All properties are get-only and set via the constructor.

---

## GeoLocation

**Namespace:** `JC.Web.ClientProfiling.Models`

Represents the geographic location resolved from a client IP address. All properties are get-only, set via the constructor.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Country` | `string?` | — | get; | The country name (e.g. "United Kingdom"). |
| `CountryCode` | `string?` | — | get; | The ISO 3166-1 alpha-2 country code (e.g. "GB"). |
| `Region` | `string?` | `null` | get; | The region, state, or province. Populated only when the provider honours `GeoLocationOptions.IncludeRegion`. |
| `City` | `string?` | `null` | get; | The city or town. Populated only when the provider honours `GeoLocationOptions.IncludeCity`. |

### Constructor

#### GeoLocation(string? country, string? countryCode, string? region = null, string? city = null)

All properties are get-only and set via the constructor.

---

# Enums

## DeviceType

**Namespace:** `JC.Web.ClientProfiling.Models`

The type of device detected from a user agent string.

| Member | Value | Description |
|--------|-------|-------------|
| `Desktop` | `0` | A desktop computer. |
| `Mobile` | `1` | A mobile phone. |
| `Tablet` | `2` | A tablet device. |
| `Bot` | `3` | An automated bot or crawler. |
| `Unknown` | `4` | Device type could not be determined. |

---

## BotFilterStatusCode

**Namespace:** `JC.Web.ClientProfiling.Models.Options`

HTTP status codes that can be returned by the bot filtering middleware.

| Member | Value | Description |
|--------|-------|-------------|
| `NoContent` | `204` | 204 No Content. |
| `BadRequest` | `400` | 400 Bad Request. |
| `Unauthorized` | `401` | 401 Unauthorized. |
| `Forbidden` | `403` | 403 Forbidden. The default. |
| `NotFound` | `404` | 404 Not Found. |

---

## RateLimitingStrategy

**Namespace:** `JC.Web.RateLimiting`

The rate limiting algorithm applied by the limiter.

| Member | Value | Description |
|--------|-------|-------------|
| `FixedWindow` | `0` | Allows `PermitLimit` requests per `Window`, resetting the counter at the end of each window. |
| `SlidingWindow` | `1` | Divides the window into `SegmentsPerWindow` segments for smoother limiting. The default. |
| `TokenBucket` | `2` | Tokens replenish at `TokensPerPeriod` per `Window` up to `TokenLimit`, allowing controlled bursts. |
| `Concurrency` | `3` | Limits concurrent in-flight requests to `ConcurrencyLimit` rather than request rate. |

`FixedWindow` permits up to twice the limit across a window boundary, since a client may spend its full allowance at the end of one window and again at the start of the next. `SlidingWindow` exists to smooth exactly that.

---

## RateLimitPartitionBy

**Namespace:** `JC.Web.RateLimiting`

How requests are grouped into rate limit buckets.

| Member | Value | Description |
|--------|-------|-------------|
| `ClientIp` | `0` | Partition by client IP, resolved via `ClientIpResolver` honouring `ClientIpOptions.TrustProxyHeaders`. The default. |
| `User` | `1` | Partition by authenticated user name. Falls back to the **endpoint path** for anonymous requests, so all anonymous callers of a path share one bucket. |
| `Endpoint` | `2` | Partition by request path. |
| `ClientIpAndEndpoint` | `3` | Partition by `"{ip}:{path}"`, giving each path its own budget per client. |

Both IP-based members depend on `ClientIpOptions.TrustProxyHeaders` being correct for the deployment — see [Rate limiting and client IP](ClientProfiling-Guide.md#rate-limiting-and-client-ip).

---

# Services

## UserAgentService

**Namespace:** `JC.Web.ClientProfiling.Services`

Parses user agent strings into structured `UserAgent` objects using the UAParser library. Registered as a singleton and maintains a single `Parser` instance for efficient repeated parsing.

### Methods

#### Parse(string? userAgentString)

**Returns:** `UserAgent`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userAgentString` | `string?` | — | The raw user agent header value. |

Parses the raw user agent string into a `UserAgent` model, detecting browser, browser version, operating system, OS version, and device type.

Returns a model with `DeviceType.Unknown` and null properties when the input is null or empty. Browser and OS values that resolve to `"Other"` in UAParser are normalised to `null`. Device type detection checks for bots (including headless Chrome, PhantomJS and Lighthouse), tablets (iPad, Android without `mobile`), mobile devices (iPhone, iPod, Android with `mobile`), and desktops (Windows, Mac OS, Linux, Chrome OS), falling back to `Unknown`.

---

## IGeoLocationProvider

**Namespace:** `JC.Web.ClientProfiling.Services`

Contract for resolving geographic location from an IP address. JC.Web ships no built-in implementation — supply your own using your chosen data source (MaxMind GeoLite2, IP2Location, ip-api, or similar).

When registered, `RequestMetadataMiddleware` calls it on every request and enriches `RequestMetadata` with the result. An internal `EmptyGeoLocationProvider` returning `null` is registered by default when no provider is configured. See [Implementing a geo-location provider](ClientProfiling-Guide.md#implementing-a-geo-location-provider) for the performance and null-handling expectations.

### Methods

#### Resolve(string ipAddress, GeoLocationOptions options)

**Returns:** `GeoLocation?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ipAddress` | `string` | — | The client IP address to look up. |
| `options` | `GeoLocationOptions` | — | Controls the granularity of the lookup (region, city). |

Returns a `GeoLocation` if the lookup succeeded, or `null` if the IP could not be resolved. Unresolvable addresses are routine — private ranges, `"unknown"`, and addresses absent from the dataset — so return `null` rather than throwing.

---

#### ResolveAsync(string ipAddress, GeoLocationOptions options)

**Returns:** `Task<GeoLocation?>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ipAddress` | `string` | — | The client IP address to look up. |
| `options` | `GeoLocationOptions` | — | Controls the granularity of the lookup (region, city). |

Asynchronous version for API-backed providers. This is the method the middleware calls. The default implementation delegates to the synchronous `Resolve`, so a local-database provider need only implement `Resolve`.

---

# Helpers

## ClientIpResolver

**Namespace:** `JC.Web.ClientProfiling.Helpers`

Static helper for resolving the client IP address from an HTTP request. The primary strategy uses `ConnectionInfo.RemoteIpAddress`, which is correct when ASP.NET Core's `UseForwardedHeaders()` middleware is configured with trusted proxies. An optional header fallback mode is available for non-standard proxy setups.

### Methods

#### Resolve(HttpContext context, bool useHeaderFallback = false)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `HttpContext` | — | The current HTTP context. |
| `useHeaderFallback` | `bool` | `false` | When `true`, inspects forwarded headers before falling back to `RemoteIpAddress`. Only enable behind a trusted proxy. |

With `useHeaderFallback` set to `true`, checks headers in order: `CF-Connecting-IPv6`, `CF-Connecting-IP`, `X-Real-IP`, then the first entry in `X-Forwarded-For`, falling back to `RemoteIpAddress` if none are present. With `false`, returns `RemoteIpAddress` directly. Returns `"unknown"` if no IP could be determined.

Driven by `ClientIpOptions.TrustProxyHeaders` when called from the middleware. These headers are caller-controlled on a directly exposed application — see [Client IP resolution](ClientProfiling-Guide.md#client-ip-resolution).

---

# Extensions

## HttpContextExtensions

**Namespace:** `JC.Web.ClientProfiling`

Static extension methods for accessing client profiling data from `HttpContext`.

### Methods

#### GetRequestMetadata(this HttpContext context)

**Returns:** `RequestMetadata?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `HttpContext` | — | The current HTTP context. |

Retrieves the `RequestMetadata` stored by `RequestMetadataMiddleware` from `HttpContext.Items`, keyed by `typeof(RequestMetadata)`. Returns `null` if the middleware has not run or no metadata is stored — which also happens when calling code sits earlier in the pipeline than `UseRequestMetadata()`.

---

# Middleware

## RequestMetadataMiddleware

**Namespace:** `JC.Web.ClientProfiling.Middleware`

Builds `RequestMetadata` early in the pipeline and stores it in `HttpContext.Items` for downstream access. Resolves the client IP via `ClientIpResolver`, parses the user agent via `UserAgentService`, and enriches with geolocation if a provider is registered. Retrieve the result via `HttpContextExtensions.GetRequestMetadata`.

### Methods

#### InvokeAsync(HttpContext context, UserAgentService userAgentService, IGeoLocationProvider geoLocationProvider, IOptions\<GeoLocationOptions\>? geoLocationOptions = null, IOptions\<ClientIpOptions\>? clientIpOptions = null)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `HttpContext` | — | The HTTP context for the current request. |
| `userAgentService` | `UserAgentService` | — | The user agent parsing service, injected by DI. |
| `geoLocationProvider` | `IGeoLocationProvider` | — | The geolocation provider, injected by DI. |
| `geoLocationOptions` | `IOptions<GeoLocationOptions>?` | `null` | Optional geolocation granularity options. |
| `clientIpOptions` | `IOptions<ClientIpOptions>?` | `null` | Optional client IP resolution options. |

Resolves the client IP using `ClientIpResolver.Resolve` with the configured `TrustProxyHeaders` setting, parses the `User-Agent` header, and calls the provider's `ResolveAsync`. Builds a `RequestMetadata` capturing client IP, user agent, HTTPS status, timestamp, request path (prefixed with the HTTP method), query string, origin, referer, and trace identifier. Stores the result in `HttpContext.Items` keyed by `typeof(RequestMetadata)`, then invokes the next middleware.

---

## BotFilterMiddleware

**Namespace:** `JC.Web.ClientProfiling.Middleware`

Blocks requests from detected bots based on the `RequestMetadata` stored in `HttpContext.Items`. Must be registered after `RequestMetadataMiddleware`.

### Methods

#### InvokeAsync(HttpContext context)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `HttpContext` | — | The HTTP context for the current request. |

If `BotFilterOptions.IsEnabled` is `false`, passes the request straight through. Otherwise retrieves `RequestMetadata` and, when `UserAgent.IsBot` is `true`, applies `PathFilter` (passing through when set and unmatched), then checks `AllowedBots` against the parsed browser name (passing through on a match). Failing both, the request is short-circuited with the configured `BotFilterStatusCode` and the pipeline does not continue.

When no `RequestMetadata` is present — because `UseRequestMetadata()` was not registered, or was registered after this middleware — every request passes through unfiltered. The failure is silent, so verify ordering rather than assuming filtering is active.

---

## Next steps

- [Setup](ClientProfiling-Setup.md) — registration, options, and middleware ordering.
- [Guide](ClientProfiling-Guide.md) — reading metadata, logging, IP resolution, geo-location, bot filtering, and rate limiting.
