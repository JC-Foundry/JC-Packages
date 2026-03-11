# JC.Web — Guide

Covers security headers, Content Security Policy, cookie management, client profiling, bot filtering, rate limiting, tag helpers, and UI helpers. See [Setup](Setup.md) for registration.

## Security headers

### How they work

`SecurityHeaderMiddleware` pre-computes all header values at construction time and applies them via `OnStarting` to every response. There is no per-request overhead beyond appending the pre-built headers.

### Customising headers

Headers are configured at registration time. To change them, adjust the options in `AddSecurityHeaders` or `AddWebDefaults` — see [Setup](Setup.md).

At runtime, the headers are immutable. If you need per-request CSP (e.g. with nonces), use the CSP builder's nonce methods at registration and generate nonces per request in your own middleware.

**Nuance:** HSTS (`Strict-Transport-Security`) is only sent on HTTPS responses. If `HstsProductionOnly` is `true` (the default), it's also skipped in non-production environments.

**Nuance:** `RemoveServerHeader` and `RemoveXPoweredByHeader` remove these headers from every response. Some hosting environments (e.g. IIS) re-add them — you may need server-level configuration to fully suppress them.

## Content Security Policy

### Building a CSP

The `ContentSecurityPolicyBuilder` provides a fluent API for constructing a `Content-Security-Policy` header:

```csharp
builder.Services.AddSecurityHeaders(headers =>
{
    headers.ContentSecurityPolicy = csp => csp
        .DefaultSrc("'self'")
        .ScriptSrc("'self'", "https://cdn.example.com")
        .StyleSrc("'self'", "'unsafe-inline'")
        .ImgSrc("'self'", "data:", "https:")
        .FontSrc("'self'", "https://fonts.gstatic.com")
        .ConnectSrc("'self'", "https://api.example.com")
        .ObjectSrc("'none'")
        .FrameAncestors("'none'")
        .BaseUri("'self'")
        .FormAction("'self'")
        .UpgradeInsecureRequests();
});
```

This produces:

```
default-src 'self'; script-src 'self' https://cdn.example.com; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' https://fonts.gstatic.com; connect-src 'self' https://api.example.com; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'; upgrade-insecure-requests
```

### Available directives

| Method | CSP directive | Controls |
|--------|--------------|----------|
| `DefaultSrc` | `default-src` | Fallback for other fetch directives |
| `ScriptSrc` | `script-src` | Script execution |
| `ScriptSrcElem` | `script-src-elem` | `<script>` element sources |
| `ScriptSrcAttr` | `script-src-attr` | Inline event handler sources |
| `StyleSrc` | `style-src` | Stylesheet sources |
| `StyleSrcElem` | `style-src-elem` | `<link>` and `<style>` element sources |
| `StyleSrcAttr` | `style-src-attr` | Inline `style` attribute sources |
| `ImgSrc` | `img-src` | Image sources |
| `FontSrc` | `font-src` | Font sources |
| `ConnectSrc` | `connect-src` | fetch, XHR, WebSocket, EventSource |
| `MediaSrc` | `media-src` | Audio and video sources |
| `ObjectSrc` | `object-src` | Plugin sources |
| `FrameSrc` | `frame-src` | Nested browsing contexts (`<iframe>`) |
| `ChildSrc` | `child-src` | Web workers and nested contexts |
| `WorkerSrc` | `worker-src` | Web/shared/service workers |
| `ManifestSrc` | `manifest-src` | Application manifests |
| `BaseUri` | `base-uri` | `<base>` element URLs |
| `FormAction` | `form-action` | Form submission targets |
| `FrameAncestors` | `frame-ancestors` | Which parents can embed the page |

### Nonces and hashes

For inline scripts or styles, use nonces or hashes instead of `'unsafe-inline'`:

```csharp
headers.ContentSecurityPolicy = csp => csp
    .DefaultSrc("'self'")
    .ScriptSrc("'self'")
    .ScriptNonce("YWJjZGVmMTIzNDU2")                     // Adds 'nonce-YWJjZGVmMTIzNDU2' to script-src
    .ScriptHash("sha256", "RFWPLDbv2BY+rCkDzsE+0fr8ylGr") // Adds 'sha256-RFWPLDbv2BY+rCkDzsE+0fr8ylGr' to script-src
    .StyleSrc("'self'")
    .StyleNonce("c3R5bGVOb25jZQ==");                       // Adds nonce to style-src
```

Pass the raw base64 value — the builder wraps it in the `'nonce-...'` or `'sha...-...'` syntax automatically.

**Nuance:** If you pass the full `'nonce-...'` token instead of the raw value, the builder throws `ArgumentException`. Same for hashes — pass raw base64, not `'sha256-...'`.

Nonce-capable directives: `script-src`, `script-src-elem`, `style-src`, `style-src-elem`.
Hash-capable directives: `script-src`, `script-src-elem`, `script-src-attr`, `style-src`, `style-src-elem`, `style-src-attr`.

### Sandbox and reporting

```csharp
headers.ContentSecurityPolicy = csp => csp
    .DefaultSrc("'self'")
    .Sandbox("allow-scripts", "allow-forms")  // Restrict page capabilities
    .ReportUri("/csp-violations")              // Where browsers send violation reports
    .ReportTo("csp-endpoint");                 // Reporting API group name
```

`Sandbox()` with no arguments applies the most restrictive sandbox. Pass tokens to selectively re-enable capabilities.

### Validation

The builder validates eagerly at registration time:

- Keywords are validated per directive — e.g. `'strict-dynamic'` is valid for `script-src` but not `style-src`
- `'none'` cannot be combined with other sources in the same directive
- Sources are validated as keywords, schemes (`https:`, `data:`, etc.), nonces, hashes, or host patterns
- Invalid values throw `ArgumentException` immediately

You can pass keywords with or without quotes — `"self"` and `"'self'"` both normalise to `'self'`.

## Cookie management

### Creating and reading cookies

All cookie operations require a registered cookie profile. Operations against unregistered cookie names return `false` or `null`.

```csharp
public class PreferenceService(ICookieService cookies)
{
    public void SetTheme(string theme)
    {
        cookies.TryCreateCookie("theme", theme);
    }

    public string GetTheme()
    {
        return cookies.GetCookie("theme") ?? "light";
    }

    public void ClearTheme()
    {
        cookies.TryDeleteCookie("theme");
    }

    public bool HasTheme()
    {
        return cookies.CookieExists("theme");
    }
}
```

### Validating cookies

```csharp
var result = cookies.ValidateCookie("session-token", expectedToken);

if (result.IsValid)
{
    // Cookie value matches expected value
}
else if (result.ValidationError)
{
    // Cookie not found or profile not registered
}
else
{
    // Cookie exists but value doesn't match
    var actual = result.ActualValue; // The actual cookie value
}
```

`ValidateCookie` accepts a `StringComparison` parameter — defaults to `StringComparison.Ordinal`.

### Encrypted cookies

When encrypted cookies are enabled, inject using `[FromKeyedServices]`:

```csharp
public class TokenService(
    [FromKeyedServices(ICookieService.EncryptedCookieDIKey)] ICookieService encryptedCookies)
{
    public void StoreToken(string token)
    {
        encryptedCookies.TryCreateCookie("auth-token", token);
    }

    public string? ReadToken()
    {
        return encryptedCookies.GetCookie("auth-token");
    }
}
```

Encrypted cookies use ASP.NET Core Data Protection. Each cookie profile has a `ProtectorPurpose` that creates an isolated protector — cookies encrypted with one purpose cannot be decrypted with another.

**Nuance:** If decryption fails (e.g. key rotation, tampered cookie), `GetCookie` returns `null` and logs a warning. It does not throw.

**Nuance:** Unkeyed `ICookieService` injection always resolves to the standard (unencrypted) service, even when both are registered. Use keyed injection for either service when encryption is enabled.

### Cookie profiles

Cookies must be registered as profiles at startup. Profiles define the cookie name, optional encryption, and optional overrides to the global defaults:

```csharp
// Standard cookies
app.PopulateStandardCookieProfiles(
    ("user-pref", null),                                           // Uses global defaults
    ("theme", new CookieDefaultOverride(SameSiteMode.Strict)),     // Overrides SameSite
    ("consent", new CookieDefaultOverride(
        SameSiteMode.Lax,
        httpOnly: false,                                            // Accessible to JavaScript
        secure: true,
        maxAge: TimeSpan.FromDays(365)))                            // Persistent for 1 year
);

// Encrypted cookies — each needs a protector purpose
app.PopulateEncryptedCookieProfiles(
    ("auth-token", "AuthTokenProtector", null),
    ("session-data", "SessionProtector", new CookieDefaultOverride(SameSiteMode.Strict))
);
```

**Nuance:** Duplicate cookie names throw `InvalidOperationException`. Cookie names must be unique across both standard and encrypted profiles.

**Nuance:** `CookieDefaultOverride` properties are nullable — only non-null values override the global `CookieDefaultOptions`. Unset properties inherit the global defaults.

### Managing profiles at runtime

The `CookieProfileDictionary` singleton allows runtime profile management:

```csharp
public class CookieAdmin(CookieProfileDictionary profiles)
{
    public void RegisterDynamic(string name)
    {
        profiles.TryCreateProfile(name);
    }

    public void UpdateOverride(string name, CookieDefaultOverride @override)
    {
        profiles.TryUpdateProfileOverride(name, @override);
    }

    public void Remove(string name)
    {
        profiles.TryRemoveProfile(name);
    }
}
```

Cookie profiles are not limited to startup registration — you can create, update, and remove them at runtime using the `CookieProfileDictionary` singleton, making it easy to support dynamic scenarios like per-tenant cookie configurations.

## Client profiling

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

### Logging request metadata

`ToLogEntry()` serialises all metadata to JSON with sensitive fields masked by default:

```csharp
var metadata = context.GetRequestMetadata();

// All sensitive fields masked (IP, path, query, origin, referer, city)
var masked = metadata.ToLogEntry();

// Selectively unmask fields
var unmasked = metadata.ToLogEntry(
    maskIp: false,
    maskPath: false,
    maskQuery: true,
    maskOrigin: true,
    maskReferer: true,
    maskCity: true
);
```

Masking uses `StringExtensions.Mask(0)` from JC.Core — replaces the entire value with asterisks.

### Client IP resolution

By default, the client IP comes from `HttpContext.Connection.RemoteIpAddress`. When `TrustProxyHeaders` is enabled, proxy headers are checked first in this order:

1. `CF-Connecting-IPv6` (Cloudflare)
2. `CF-Connecting-IP` (Cloudflare)
3. `X-Real-IP` (nginx)
4. `X-Forwarded-For` (first IP in the comma-separated list)

**Nuance:** Only enable `TrustProxyHeaders` when your application is behind a trusted reverse proxy (Cloudflare, nginx, etc.). If the app is directly exposed to the internet, clients can spoof these headers.

### Implementing a geo-location provider

Register a custom `IGeoLocationProvider` to enrich `RequestMetadata` with geo-location data:

```csharp
public class MaxMindGeoProvider : IGeoLocationProvider
{
    public GeoLocation? Resolve(string ipAddress, GeoLocationOptions options)
    {
        // Look up the IP in your MaxMind database
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
builder.Services.AddWebDefaults<MaxMindGeoProvider>(builder.Configuration);
```

Without a custom provider, `EmptyGeoLocationProvider` is registered and `RequestMetadata.GeoLocation` is always `null`.

## Bot filtering

### How bots are detected

The `UserAgentService` parses user agent strings using UAParser. A request is classified as a bot if:

- The UA family contains "bot", "crawler", "spider", or "slurp"
- The raw UA string contains "bot/", "crawler", "spider", "headlesschrome", "phantomjs", or "lighthouse"

### Allowing specific bots

```csharp
builder.Services.AddWebDefaults(builder.Configuration, configureBotFilter: bots =>
{
    bots.AllowedBots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Googlebot",
        "Bingbot",
        "Slurp"  // Yahoo
    };
});
```

Allowed bots are matched against the parsed browser name (case-insensitive).

### Filtering specific paths

By default, all paths are subject to bot filtering. Use `PathFilter` to restrict it:

```csharp
bots.PathFilter = path => path.StartsWith("/api");
// Only API routes are protected — bots can still access public pages
```

When `PathFilter` is set, only requests where the predicate returns `true` are checked for bots.

### Choosing a response code

```csharp
bots.StatusCode = BotFilterStatusCode.NotFound; // Return 404 instead of 403
```

Available status codes: `NoContent` (204), `BadRequest` (400), `Unauthorized` (401), `Forbidden` (403, default), `NotFound` (404).

## Rate limiting

### Strategies

| Strategy | Description |
|----------|-------------|
| `FixedWindow` | Allows `PermitLimit` requests per `Window`. Counter resets at the end of each window |
| `SlidingWindow` | Like fixed window but divided into `SegmentsPerWindow` segments. Smooths out burst traffic |
| `TokenBucket` | Tokens replenish at `TokensPerPeriod` per `Window`, up to `TokenLimit`. Each request consumes one token |
| `Concurrency` | Limits concurrent in-flight requests to `ConcurrencyLimit` |

### Partition strategies

| Partition | Key | Use case |
|-----------|-----|----------|
| `ClientIp` | Client IP address | General API protection |
| `User` | Authenticated user name (falls back to endpoint path) | Per-user limits |
| `Endpoint` | Request path | Per-endpoint limits |
| `ClientIpAndEndpoint` | `"{ip}:{path}"` | Per-IP per-endpoint limits |

### Static file exclusion

When `ExcludeStaticFiles` is `true` (the default), requests for static assets bypass the rate limiter entirely. This prevents CSS, JS, and image requests from consuming the rate limit budget. Excluded extensions: `.css`, `.js`, `.png`, `.jpg`, `.jpeg`, `.gif`, `.svg`, `.ico`, `.woff`, `.woff2`, `.ttf`, `.eot`, `.map`, `.webp`, `.avif`, `.bmp`.

## Tag helpers

### Bug reporter

Renders a floating feedback widget with a toggle button, type dropdown, and description textarea:

```html
<bug-reporter endpoint="/Bug/ReportBug" />
```

| Attribute | Default | Description |
|-----------|---------|-------------|
| `endpoint` | *required* | POST endpoint that receives the report |
| `icon` | `"🐞"` | Icon on the floating button |
| `title` | `"Send Feedback"` | Form title text |
| `colour` | `"danger"` | Bootstrap contextual colour suffix (`border-{colour}`, `text-{colour}`, `btn-{colour}`) |

The widget submits a JSON POST:

```json
{
    "type": "bug",
    "description": "The save button doesn't work",
    "metadata": "{\"RequestId\":\"abc\",\"Timestamp\":\"...\",\"Browser\":\"Chrome\",...}"
}
```

The `metadata` field contains the `RequestMetadata.ToLogEntry()` JSON (with sensitive fields masked). An anti-forgery token is included in the `RequestVerificationToken` header if available.

**Nuance:** Requires Bootstrap 5 for styling. The widget uses `d-print-none` to hide itself in print views.

### Alert

Renders a Bootstrap 5 alert:

```html
<alert type="Success" message="Changes saved successfully!" />
<alert type="Warning" message="Your session expires in 5 minutes." dismissible="false" />
<alert type="Error" message="Failed to save changes." />
<alert type="Info" message="A new version is available." />
```

| Attribute | Default | Description |
|-----------|---------|-------------|
| `type` | `Info` | `Success`, `Warning`, `Error`, `Info` |
| `message` | *required* | Alert text |
| `dismissible` | `true` | Adds a dismiss button |

If `message` is null or empty, the tag suppresses output entirely.

### Pagination

Renders Bootstrap pagination controls from an `IPagination<T>` model:

```html
<pagination model="Model.Products" href-format="/products?page={0}" />
```

| Attribute | Default | Description |
|-----------|---------|-------------|
| `model` | *required* | `IPagination<T>` instance (from JC.Core's `ToPagedListAsync`) |
| `href-format` | `"?page={0}"` | URL template — `{0}` is replaced with the page number |
| `max-pages` | `5` | Maximum visible page links before ellipsis |
| `previous-text` | `"«"` | Previous button text |
| `next-text` | `"»"` | Next button text |
| `show-first-last` | `true` | Shows first/last page buttons |
| `container-class` | — | Additional CSS classes on the `<nav>` element |

If `TotalPages` is 1 or less, the tag suppresses output.

### Breadcrumbs

Renders a Bootstrap breadcrumb navigation:

```html
<breadcrumb>
    <crumb label="Home" href="/" />
    <crumb label="Products" href="/products" />
    <crumb label="Widget" />
</breadcrumb>
```

The last `<crumb>` is automatically rendered as the active page (with `aria-current="page"`). Omit `href` on the last crumb to render it as plain text.

## UI helpers

### TableBuilder

Generates a Bootstrap HTML table from a collection:

```csharp
var html = new TableBuilder<User>()
    .AddColumn("Name", u => u.Name)
    .AddColumn("Email", u => u.Email)
    .AddColumn("Age", u => u.Age, cssClass: "text-end")
    .Build(users, "table table-striped table-hover");
```

Cell content is HTML-encoded automatically to prevent XSS. The `cssClass` parameter applies to both the `<th>` and `<td>` elements.

### DropdownHelper

Builds `SelectListItem` collections for `<select>` elements:

```csharp
// From an enum — uses ToDisplayName() for text
var statusOptions = DropdownHelper.FromEnum<OrderStatus>(selected: OrderStatus.InProgress);

// From a collection
var userOptions = DropdownHelper.FromCollection(
    users,
    textSelector: u => u.DisplayName,
    valueSelector: u => u.Id,
    selectedPredicate: u => u.Id == currentUserId
);

// From a dictionary
var options = DropdownHelper.FromDictionary(
    new Dictionary<string, string> { ["gb"] = "United Kingdom", ["us"] = "United States" },
    selected: "gb"
);

// Country dropdown (from JC.Core's CountryHelper)
var countries = DropdownHelper.GetCountryDropdown(selected: "GB");

// Add a placeholder to any dropdown
var withPlaceholder = countries.WithPlaceholder("Select a country...");
```

### QrCodeHelper

Generates QR codes in SVG or base64 PNG format:

```csharp
// SVG (default)
var qr = new QrCodeHelper();
var svg = qr.GenerateQrCode("https://example.com");
// Returns SVG markup string — embed directly in HTML

// Base64 PNG
var qrPng = new QrCodeHelper(QrCodeFormat.Base64, pixelsPerModule: 15);
var dataUri = qrPng.GenerateQrCode("https://example.com");
// Returns "data:image/png;base64,..." — use in <img src="...">

// Custom error correction (L=7%, M=15%, Q=25%, H=30%)
var qrHigh = new QrCodeHelper(QrCodeFormat.Svg, 10, QRCodeGenerator.ECCLevel.H);
```

### ModelStateWrapper

Simplifies `ModelStateDictionary` access when model properties are nested under a prefix (common in Razor Pages with `Input.` models):

```csharp
// In a Razor Page
var state = new ModelStateWrapper(ModelState); // Default prefix: "Input."

if (state.HasError("Email"))
{
    var message = state["Email"]; // Reads ModelState["Input.Email"]
}

state.AddModelError("Email", "Email is already taken."); // Adds to "Input.Email"

// Custom prefix
var state2 = new ModelStateWrapper(ModelState, prefix: "Form.");

// No prefix
var state3 = new ModelStateWrapper(ModelState, ignorePrefix: true);
```

### AlertHelper

Generates Bootstrap alert HTML from code-behind:

```csharp
var html = AlertHelper.Success("Record saved.");
var warning = AlertHelper.Warning("Session expiring soon.", dismissible: false);
var error = AlertHelper.Error("Validation failed.");
var info = AlertHelper.Info("New version available.");
```

### BreadcrumbBuilder

Generates breadcrumb HTML programmatically:

```csharp
var html = new BreadcrumbBuilder()
    .Add("Home", "/")
    .Add("Products", "/products")
    .Add("Widget")
    .Build();
```

The last item is rendered as the active page. Implicit string conversion is supported — you can assign the builder directly to a string or pass it to `Html.Raw()`.
