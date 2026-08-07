# JC.Web: Security — Setup

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
// Security headers and cookie services only
builder.Services.AddSecurityDefaults(builder.Configuration);

// Or: security, cookies and client profiling together
builder.Services.AddWebDefaults(builder.Configuration);
```

### Middleware — `Program.cs`

```csharp
var app = builder.Build();

// Matches AddSecurityDefaults
app.UseSecurityHeaders();

// Or: matches AddWebDefaults — security headers plus client profiling
app.UseWebDefaults();
```

Place either early in the pipeline so headers are applied to every response, including those short-circuited by later middleware.

### Configuration — `appsettings.json`

Required when encrypted cookies are enabled, which is the default:

```json
{
  "Web": {
    "Cookies": {
      "DataProtection_Path": "/path/to/keys"
    }
  }
}
```

> **Upgrading from an earlier version?** This key was previously `Cookies:DataProtection_Path`, without the `Web` root. Registration throws `InvalidOperationException` at startup if it cannot find the key, so the failure is immediate and obvious rather than silently falling back to unencrypted cookies.

### Cookie profiles — `Program.cs`

Cookies cannot be read or written until their profile is registered. This happens on `IApplicationBuilder`, after `Build()`:

```csharp
app.PopulateStandardCookieProfiles(
    ("user-pref", null),
    ("theme", new CookieDefaultOverride(SameSiteMode.Strict))
);

app.PopulateEncryptedCookieProfiles(
    ("auth-token", "AuthTokenProtector", null)
);
```

Every `ICookieService` operation against an unregistered name returns `false` or `null` rather than throwing, so a forgotten profile presents as a cookie that silently never persists.

### Defaults

`AddSecurityDefaults` registers:

| Registration | Lifetime | Description |
|-------------|----------|-------------|
| `IOptions<SecurityHeaderOptions>` | Singleton | Security header configuration |
| `IOptions<CookieDefaultOptions>` | Singleton | Global cookie defaults |
| `ICookieService` → `CookieService` | Scoped | Unencrypted service, resolved by unkeyed injection |
| `ICookieService` → `CookieService` | Scoped (keyed) | Keyed as `ICookieService.StandardCookieDIKey` |
| `ICookieService` → `EncryptedCookieService` | Scoped (keyed) | Keyed as `ICookieService.EncryptedCookieDIKey` |
| `CookieProfileDictionary` | Singleton | Cookie profile registry |
| `IHttpContextAccessor` | Singleton | Required by the cookie services |

`AddWebDefaults` registers all of the above plus everything in [Client profiling setup](ClientProfiling-Setup.md#defaults).

Default headers applied to every response:

| Header | Default value |
|--------|---------------|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `SAMEORIGIN` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Permissions-Policy` | `geolocation=(), microphone=(), camera=()` |
| `Strict-Transport-Security` | `max-age=15552000` (180 days, HTTPS only, production only) |
| `Server` | Removed |
| `X-Powered-By` | Removed |
| `Content-Security-Policy` | Not set |
| `Cross-Origin-Opener-Policy` | Not set |
| `Cross-Origin-Resource-Policy` | Not set |
| `Cross-Origin-Embedder-Policy` | Not set |

Default cookie attributes applied to every cookie created through `ICookieService`:

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

> **No Content Security Policy is applied by default.** CSP is the single most effective header against cross-site scripting, but a wrong policy breaks a working site, so it cannot be switched on blindly. See [Content Security Policy](Security-Guide.md#content-security-policy) for how to build one.

`UseWebDefaults` registers middleware in this order:

1. `UseSecurityHeaders()` — adds the headers above to every response
2. `UseClientProfiling()` — `UseRequestMetadata()` then `UseBotFilter()`

## 2. Full configuration

### AddWebDefaults — combined registration

Registers security headers, cookie services and client profiling in one call. Every parameter is optional.

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

Every value shown is the default, so this is equivalent to `AddWebDefaults(builder.Configuration)`.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configuration` | `IConfiguration?` | `null` | Application configuration. Required when `useEncryptedCookies` is `true` — reads `Web:Cookies:DataProtection_Path` |
| `useEncryptedCookies` | `bool` | `true` | Registers `EncryptedCookieService` alongside `CookieService` as keyed services, and configures Data Protection key storage |
| `configureHeaderFilter` | `Action<SecurityHeaderOptions>?` | `null` | Callback to configure security header options |
| `configureCookieFilter` | `Action<CookieDefaultOptions>?` | `null` | Callback to configure global cookie defaults |
| `configureBotFilter` | `Action<BotFilterOptions>?` | `null` | Callback to configure bot filtering — see [Client profiling](ClientProfiling-Setup.md) |
| `configureClientIp` | `Action<ClientIpOptions>?` | `null` | Callback to configure client IP resolution — see [Client profiling](ClientProfiling-Setup.md) |

A generic overload, `AddWebDefaults<TGeoService>`, adds a `configureGeoLocation` parameter and registers a custom geo-location provider. It is documented in [Client profiling setup](ClientProfiling-Setup.md#addclientprofiling-with-a-geo-location-provider).

### AddSecurityDefaults — security without client profiling

```csharp
builder.Services.AddSecurityDefaults(
    configuration: builder.Configuration,
    useEncryptedCookies: true,
    headerOptions: headers =>
    {
        headers.XFrameOptions = XFrameOptionsMode.SameOrigin;
        headers.EnableHsts = true;
    },
    cookieOptions: cookies =>
    {
        cookies.HttpOnly = true;
        cookies.Secure = true;
        cookies.SameSite = SameSiteMode.Lax;
    }
);
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configuration` | `IConfiguration?` | `null` | Required when `useEncryptedCookies` is `true` |
| `useEncryptedCookies` | `bool` | `true` | Registers the encrypted cookie service and Data Protection |
| `headerOptions` | `Action<SecurityHeaderOptions>?` | `null` | Callback to configure security header options |
| `cookieOptions` | `Action<CookieDefaultOptions>?` | `null` | Callback to configure global cookie defaults |

### AddSecurityHeaders — headers only

```csharp
builder.Services.AddSecurityHeaders(headers =>
{
    headers.XFrameOptions = XFrameOptionsMode.Deny;
    headers.HstsIncludeSubDomains = true;
});
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configure` | `Action<SecurityHeaderOptions>?` | `null` | Callback to configure security header options |

Options are validated eagerly at registration, so an invalid value throws at startup rather than producing a malformed header at runtime.

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
| `ContentSecurityPolicy` | `Action<ContentSecurityPolicyBuilder>?` | `null` | Callback building the `Content-Security-Policy` header. `null` means no CSP header |

> **`HstsIncludeSubDomains` is hard to undo.** Once a browser has seen it, every subdomain is HTTPS-only for the full `max-age`, including ones that do not have certificates yet. Confirm every subdomain serves HTTPS before enabling it.

### AddCookieServices — cookies only

```csharp
builder.Services.AddCookieServices(
    configuration: builder.Configuration,
    useEncryptedCookies: true,
    configure: cookies =>
    {
        cookies.HttpOnly = true;
        cookies.Secure = true;
        cookies.SameSite = SameSiteMode.Lax;
        cookies.Path = "/";
        cookies.MaxAge = null;
        cookies.Domain = null;
        cookies.Expires = null;
        cookies.IsEssential = false;
    }
);
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configuration` | `IConfiguration?` | `null` | Required when `useEncryptedCookies` is `true`. Throws `ArgumentNullException` if omitted in that case |
| `useEncryptedCookies` | `bool` | `true` | Registers `EncryptedCookieService` as a keyed service and configures Data Protection |
| `configure` | `Action<CookieDefaultOptions>?` | `null` | Callback to configure global cookie defaults |

#### Encryption modes

**With encryption (the default).** Both services register as keyed. Inject with `[FromKeyedServices]`:

```csharp
public class MyService(
    [FromKeyedServices(ICookieService.StandardCookieDIKey)] ICookieService cookies,
    [FromKeyedServices(ICookieService.EncryptedCookieDIKey)] ICookieService encryptedCookies)
```

Unkeyed `ICookieService` injection always resolves to the **standard, unencrypted** service, even when both are registered. Nothing warns you about this — a cookie you expected to be encrypted is simply written in plain text.

Requires `Web:Cookies:DataProtection_Path`. The directory is created if missing, and Data Protection keys are persisted there.

**Without encryption:**

```csharp
builder.Services.AddCookieServices(useEncryptedCookies: false);
```

Only `CookieService` is registered, and neither `IConfiguration` nor a Data Protection path is needed.

#### CookieDefaultOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `HttpOnly` | `bool` | `true` | Inaccessible to client-side JavaScript |
| `Secure` | `bool` | `true` | Only sent over HTTPS |
| `SameSite` | `SameSiteMode` | `Lax` | SameSite attribute |
| `Path` | `string?` | `"/"` | URL path the cookie is valid for |
| `MaxAge` | `TimeSpan?` | `null` | Cookie lifetime. `null` means a session cookie |
| `Domain` | `string?` | `null` | Cookie domain. `null` means the current request host |
| `Expires` | `DateTimeOffset?` | `null` | Absolute expiry. When both `MaxAge` and `Expires` are set, `MaxAge` wins, per the HTTP specification |
| `IsEssential` | `bool` | `false` | Bypasses consent checks |

Individual cookies override any of these through `CookieDefaultOverride` when their profile is registered.

### Cookie profile registration

Profiles are registered on `IApplicationBuilder`, after `Build()`:

```csharp
// Standard (unencrypted)
app.PopulateStandardCookieProfiles(
    ("user-pref", null),
    ("theme", new CookieDefaultOverride(SameSiteMode.Strict))
);

// Encrypted — each needs its own protector purpose
app.PopulateEncryptedCookieProfiles(
    ("auth-token", "AuthTokenProtector", null),
    ("session-data", "SessionProtector", new CookieDefaultOverride(SameSiteMode.Strict, httpOnly: true))
);

// Or both in one call
app.PopulateCookieProfiles(
    standardCookies: [("user-pref", null)],
    encryptedCookies: [("auth-token", "AuthTokenProtector", null)]
);
```

Overloads taking pre-built `CookieProfile` instances exist for all three. Duplicate cookie names throw `InvalidOperationException`, and names must be unique across standard and encrypted profiles together.

### Middleware — individual registration

```csharp
app.UseSecurityHeaders();
```

`UseSecurityHeaders` is the only middleware in this area — cookie services are called directly rather than sitting in the pipeline. Register it first, so headers reach responses produced by middleware that short-circuits later on.

## 3. Verify

1. Run the application and inspect response headers on any page — `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` and `Permissions-Policy` should be present, and `Server` and `X-Powered-By` absent.
2. Over HTTPS in a production environment, confirm `Strict-Transport-Security` appears.
3. Write a cookie through `ICookieService` and confirm it appears in the browser with the expected `HttpOnly`, `Secure` and `SameSite` attributes.

## Next steps

- [Guide](Security-Guide.md) — header behaviour, building a Content Security Policy, and cookie management.
- [API Reference](Security-API.md)
