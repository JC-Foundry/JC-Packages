# JC.Web: Client Profiling — Setup

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
builder.Services.AddClientProfiling();

// Opt-in — not included in AddClientProfiling or AddWebDefaults
builder.Services.AddRateLimiting();
```

Client profiling is also registered by `AddWebDefaults`, which covers security headers, cookies and profiling in one call. See [Security setup](Security-Setup.md) for that route. Rate limiting is never included in either convenience method.

### Middleware — `Program.cs`

```csharp
app.UseClientProfiling();

// Opt-in — only needed if AddRateLimiting was called
app.UseRateLimiting();
```

Place `UseClientProfiling` early in the pipeline so downstream middleware and handlers can read the metadata. It is equivalent to `UseRequestMetadata()` followed by `UseBotFilter()`.

### Defaults

`AddClientProfiling` registers:

| Registration | Lifetime | Description |
|-------------|----------|-------------|
| `IHttpContextAccessor` | Singleton | Required to reach `HttpContext` outside a request handler |
| `UserAgentService` | Singleton | Parses user agent strings via UAParser |
| `IGeoLocationProvider` → `EmptyGeoLocationProvider` | Singleton | Placeholder that always returns `null` |
| `IOptions<BotFilterOptions>` | Singleton | Bot filtering configuration |
| `IOptions<ClientIpOptions>` | Singleton | Client IP resolution configuration |

Default option values:

| Option | Default | Description |
|--------|---------|-------------|
| `BotFilterOptions.IsEnabled` | `true` | Bot filtering is active |
| `BotFilterOptions.StatusCode` | `Forbidden` (403) | Returned to blocked bots |
| `BotFilterOptions.AllowedBots` | Empty | **No bots are allowed through** |
| `BotFilterOptions.PathFilter` | `null` | **All paths are filtered** |
| `ClientIpOptions.TrustProxyHeaders` | `false` | IP comes from `RemoteIpAddress` only |

> **The bot filter defaults are strict.** With an empty `AllowedBots` and no `PathFilter`, every detected crawler is blocked on every path — including Googlebot on your public pages. This is a safe default for internal applications, but a public site that wants to be indexed must allow the search engines it cares about. See [Bot filtering](ClientProfiling-Guide.md#bot-filtering).

`AddRateLimiting` with no arguments applies a sliding window of 100 requests per minute, partitioned by client IP, with static files excluded and no queuing — rejected requests receive `429 Too Many Requests` immediately. Every option is listed under [RateLimitingOptions](#ratelimitingoptions).

## 2. Full configuration

### AddClientProfiling — standard registration

```csharp
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
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configureBotFilter` | `Action<BotFilterOptions>?` | `null` | Bot detection and blocking behaviour |
| `configureClientIp` | `Action<ClientIpOptions>?` | `null` | How the client IP is resolved |

### AddClientProfiling with a geo-location provider

JC.Web ships no geo-location implementation. Supply your own (MaxMind, IP2Location, ip-api, or similar) through the generic overload:

```csharp
builder.Services.AddClientProfiling<MaxMindGeoProvider>(
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

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configureBotFilter` | `Action<BotFilterOptions>?` | `null` | Bot detection and blocking behaviour |
| `configureGeoLocation` | `Action<GeoLocationOptions>?` | `null` | Lookup granularity passed to your provider |
| `configureClientIp` | `Action<ClientIpOptions>?` | `null` | How the client IP is resolved |

Your provider is registered **scoped**, so it may depend on scoped services such as a DbContext. Registration uses `TryAdd`, and the generic overload registers your provider before delegating to the standard one — so `EmptyGeoLocationProvider` is never substituted over the top of it.

Without this overload, `RequestMetadata.GeoLocation` is always `null`. See [Implementing a geo-location provider](ClientProfiling-Guide.md#implementing-a-geo-location-provider).

### BotFilterOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IsEnabled` | `bool` | `true` | When `false`, all requests pass through without bot inspection |
| `StatusCode` | `BotFilterStatusCode` | `Forbidden` (403) | HTTP status returned to blocked bots. Values: `NoContent` (204), `BadRequest` (400), `Unauthorized` (401), `Forbidden` (403), `NotFound` (404) |
| `AllowedBots` | `HashSet<string>` | Empty (case-insensitive) | Bot browser names allowed through the filter (e.g. `"Googlebot"`). Matched against the parsed browser name |
| `PathFilter` | `Func<string, bool>?` | `null` | When set, only requests matching this predicate are subject to bot filtering. `null` means all paths are filtered |

### ClientIpOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `TrustProxyHeaders` | `bool` | `false` | When `true`, proxy headers are checked **before** `RemoteIpAddress` |

Headers are checked in order: `CF-Connecting-IPv6`, `CF-Connecting-IP`, `X-Real-IP`, then the first entry in `X-Forwarded-For`.

> **Only enable `TrustProxyHeaders` behind a trusted reverse proxy.** These headers are attacker-controlled on a directly exposed application — a client can set `X-Forwarded-For` to any value it likes. Anything keyed on the resolved IP, such as rate limiting partitions or audit logs, then trusts a value the caller chose.

### GeoLocationOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IncludeRegion` | `bool` | `true` | Whether to request region, state or province |
| `IncludeCity` | `bool` | `false` | Whether to request city or town |

These are passed to your provider — JC.Web does not enforce them. An implementation is free to ignore them, so honour them if lookup cost or data minimisation matters to you.

### Middleware — individual registration

Register the two middlewares separately when you need something between them:

```csharp
app.UseRequestMetadata();  // Builds RequestMetadata into HttpContext.Items
app.UseBotFilter();        // Must come after UseRequestMetadata
```

`UseBotFilter` reads the `RequestMetadata` that `UseRequestMetadata` stores. Registered the other way round, or without `UseRequestMetadata` at all, the bot filter finds no metadata and silently passes every request through — including bots.

### Rate limiting (opt-in)

Rate limiting is **not** included in `AddClientProfiling` or `AddWebDefaults` — register and apply it separately.

```csharp
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

app.UseRateLimiting();
```

Every value above is the default, so this block is equivalent to calling `AddRateLimiting()` with no arguments.

Rate limiting lives in this area because it consumes it: the default `PartitionBy` is `ClientIp`, and the partition key is resolved by the same `ClientIpResolver` — honouring the same `ClientIpOptions.TrustProxyHeaders` — that produces `RequestMetadata.ClientIp`.

> **Behind a proxy, set `TrustProxyHeaders = true` or IP partitioning does not work.** `RemoteIpAddress` is the proxy's own address, so every visitor resolves to the same key and lands in one shared bucket — turning a per-client limit into a site-wide one. See [Rate limiting and client IP](ClientProfiling-Guide.md#rate-limiting-and-client-ip).

#### RateLimitingOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IsEnabled` | `bool` | `true` | When `false`, the rate limiter is not registered |
| `Strategy` | `RateLimitingStrategy` | `SlidingWindow` | The rate limiting algorithm. Values: `FixedWindow`, `SlidingWindow`, `TokenBucket`, `Concurrency` |
| `PermitLimit` | `int` | `100` | Maximum requests within the window (used by `FixedWindow` and `SlidingWindow`) |
| `Window` | `TimeSpan` | `1 minute` | Time window for rate limiting |
| `SegmentsPerWindow` | `int` | `6` | Segments per window for `SlidingWindow` (each segment = `Window / SegmentsPerWindow`). Ignored by other strategies |
| `PartitionBy` | `RateLimitPartitionBy` | `ClientIp` | How requests are grouped. Values: `ClientIp`, `User` (falls back to endpoint path for anonymous), `Endpoint`, `ClientIpAndEndpoint` |
| `ExcludeStaticFiles` | `bool` | `true` | When `true`, static file requests bypass the rate limiter |
| `QueueLimit` | `int` | `0` | Requests to queue when the limit is reached. `0` = no queuing (immediate 429) |
| `QueueProcessingOrder` | `QueueProcessingOrder` | `OldestFirst` | Processing order for queued requests |
| `TokensPerPeriod` | `int` | `10` | Tokens added per window for `TokenBucket`. Ignored by other strategies |
| `TokenLimit` | `int` | `0` | Maximum bucket capacity for `TokenBucket`. When `0`, uses `PermitLimit`. Ignored by other strategies |
| `ConcurrencyLimit` | `int` | `0` | Maximum concurrent requests for `Concurrency`. When `0`, uses `PermitLimit`. Ignored by other strategies |

Rate limiting is applied as a **global** limiter — every request is subject to it, apart from static files when `ExcludeStaticFiles` is `true`. There is no per-endpoint opt-in; to protect only some routes, partition by `Endpoint` or `ClientIpAndEndpoint` so unrelated paths do not share a budget. The rejection status is `429 Too Many Requests`.

`AddRateLimiting` evaluates `IsEnabled` at registration time — when `false`, the ASP.NET Core rate limiter is never registered at all, and `UseRateLimiting` becomes a no-op.

## 3. Verify

1. Add a temporary endpoint returning `HttpContext.GetRequestMetadata()?.ToLogEntry(maskIp: false)` and request it — you should see your IP, parsed browser and OS. Behind a proxy, confirm this is the real client address and not the proxy's, or IP-based bot filtering and rate limiting will treat every visitor as one client.
2. Request it again with a bot user agent (`curl -A "Googlebot/2.1" ...`) — with default options you should receive a `403`.
3. If rate limiting is enabled, refresh rapidly past `PermitLimit` — you should receive `429 Too Many Requests`.

## Next steps

- [Guide](ClientProfiling-Guide.md) — reading metadata, logging, IP resolution, geo-location providers, bot filtering, and rate limiting behaviour.
- [API Reference](ClientProfiling-API.md)
