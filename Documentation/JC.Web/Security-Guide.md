# JC.Web: Security — Guide

Covers security header behaviour, building a Content Security Policy with the fluent builder, and cookie management including encryption and runtime profile changes. See [Setup](Security-Setup.md) for registration.

## Security headers

### How they work

Header values are computed once, when `SecurityHeaderMiddleware` is constructed, and applied through `Response.OnStarting` on every response:

```csharp
app.UseSecurityHeaders();
```

There is no per-request work beyond appending the pre-built strings, so the middleware costs effectively nothing per request. The trade-off is that headers are fixed for the lifetime of the application — see [Per-request policies](#per-request-policies) if you need to vary them.

Register it first in the pipeline. Middleware registered before it that short-circuits a request — a bot filter returning 403, a rate limiter returning 429 — produces a response the header middleware never sees.

### Nuances

**HSTS is only sent over HTTPS**, and with `HstsProductionOnly` at its default of `true`, only in production. A missing `Strict-Transport-Security` in development or over plain HTTP is expected, not a misconfiguration.

**`RemoveServerHeader` and `RemoveXPoweredByHeader` only remove what ASP.NET Core controls.** IIS and some reverse proxies re-add them after the application has finished with the response, so fully suppressing them may need server-level configuration as well.

### Per-request policies

The headers are immutable at runtime, which matters most for CSP nonces — a nonce must be unique per response to be worth anything, and a nonce baked in at registration is a constant that any attacker can read.

If you need genuine per-request nonces, generate them in your own middleware and write the header there, rather than through `SecurityHeaderOptions`:

```csharp
app.Use(async (context, next) =>
{
    var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    context.Items["csp-nonce"] = nonce;

    context.Response.Headers.ContentSecurityPolicy =
        $"default-src 'self'; script-src 'self' 'nonce-{nonce}'";

    await next();
});
```

Views then read `Context.Items["csp-nonce"]` and emit it on each `<script>` tag. Leave `SecurityHeaderOptions.ContentSecurityPolicy` unset in that case, or the two will both try to write the header.

## Content Security Policy

### Building a policy

`ContentSecurityPolicyBuilder` is a fluent builder invoked from `SecurityHeaderOptions`:

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

Producing:

```
default-src 'self'; script-src 'self' https://cdn.example.com; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' https://fonts.gstatic.com; connect-src 'self' https://api.example.com; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'; upgrade-insecure-requests
```

Quoting is normalised, so `"self"` and `"'self'"` both emit `'self'`.

**Roll out a new policy in report-only mode first.** A CSP that is subtly too strict breaks the site for every visitor at once, and the browser console is the only place it shows up. Use [reporting](#sandbox-and-reporting) to collect violations against real traffic before enforcing.

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
| `WorkerSrc` | `worker-src` | Web, shared and service workers |
| `ManifestSrc` | `manifest-src` | Application manifests |
| `BaseUri` | `base-uri` | `<base>` element URLs |
| `FormAction` | `form-action` | Form submission targets |
| `FrameAncestors` | `frame-ancestors` | Which parents may embed the page |

`'unsafe-inline'` on `script-src` disables most of what CSP is for, since injected inline script is the usual payload of an XSS. On `style-src` it is far less severe and often unavoidable with component libraries.

### Nonces and hashes

For inline content, nonces and hashes are the alternative to `'unsafe-inline'`:

```csharp
headers.ContentSecurityPolicy = csp => csp
    .DefaultSrc("'self'")
    .ScriptSrc("'self'")
    .ScriptNonce("YWJjZGVmMTIzNDU2")                        // adds 'nonce-YWJjZGVmMTIzNDU2'
    .ScriptHash("sha256", "RFWPLDbv2BY+rCkDzsE+0fr8ylGr")   // adds 'sha256-RFWPLDbv2BY+rCkDzsE+0fr8ylGr'
    .StyleSrc("'self'")
    .StyleNonce("c3R5bGVOb25jZQ==");
```

Pass the raw base64 value — the builder adds the `'nonce-...'` and `'sha256-...'` wrappers. Passing a pre-wrapped token throws `ArgumentException`, as does a value that is not valid base64, so a malformed nonce fails at startup rather than producing a policy the browser silently ignores.

Nonce-capable directives: `script-src`, `script-src-elem`, `style-src`, `style-src-elem`.
Hash-capable directives: `script-src`, `script-src-elem`, `script-src-attr`, `style-src`, `style-src-elem`, `style-src-attr`.

A nonce configured here is fixed for the application's lifetime, which defeats the purpose — nonces are only meaningful when unpredictable per response. Use hashes for genuinely static inline blocks, and [per-request middleware](#per-request-policies) for real nonces.

### Sandbox and reporting

```csharp
headers.ContentSecurityPolicy = csp => csp
    .DefaultSrc("'self'")
    .Sandbox("allow-scripts", "allow-forms")
    .ReportUri("/csp-violations")
    .ReportTo("csp-endpoint");
```

`Sandbox()` with no arguments applies the most restrictive sandbox — no scripts, no forms, no popups, a unique origin. Pass tokens to re-enable capabilities selectively.

`ReportUri` is deprecated but still the most widely supported; `ReportTo` is its replacement and needs a matching `Reporting-Endpoints` header. Setting both is the usual approach while support catches up.

### Validation

The builder validates as you call it, at registration time:

- Keywords are checked per directive — `'strict-dynamic'` is valid on `script-src` but not `style-src`
- `'none'` cannot be combined with other sources in the same directive
- Sources must parse as a keyword, scheme, nonce, hash or host pattern
- Nonces must be base64 and unwrapped

Anything invalid throws `ArgumentException` immediately, so a broken policy stops the application at startup instead of reaching browsers.

## Cookie management

### Creating and reading cookies

Every operation goes through `ICookieService` and requires a registered profile:

```csharp
public class PreferenceService(ICookieService cookies)
{
    public void SetTheme(string theme) => cookies.TryCreateCookie("theme", theme);

    public string GetTheme() => cookies.GetCookie("theme") ?? "light";

    public void ClearTheme() => cookies.TryDeleteCookie("theme");

    public bool HasTheme() => cookies.CookieExists("theme");
}
```

**Operations on an unregistered cookie name return `false` or `null` — they do not throw.** A cookie whose profile was never registered simply never persists, with nothing in the logs. If a cookie mysteriously fails to appear, check its profile registration first. See [Cookie profiles](Security-Setup.md#cookie-profile-registration).

### Validating cookies

```csharp
var result = cookies.ValidateCookie("session-token", expectedToken);

if (result.IsValid)
{
    // Value matched
}
else if (result.ValidationError)
{
    // Cookie missing, or profile not registered
}
else
{
    // Cookie present, value differs
    var actual = result.ActualValue;
}
```

`ValidationError` and a simple mismatch are distinct outcomes, which matters when deciding whether to re-issue a cookie or treat the request as tampered. A `StringComparison` overload is available, defaulting to `Ordinal` — keep it ordinal for tokens, where culture-aware comparison could treat different byte sequences as equal.

### Encrypted cookies

With encryption enabled, inject the keyed service:

```csharp
public class TokenService(
    [FromKeyedServices(ICookieService.EncryptedCookieDIKey)] ICookieService encryptedCookies)
{
    public void StoreToken(string token) => encryptedCookies.TryCreateCookie("auth-token", token);

    public string? ReadToken() => encryptedCookies.GetCookie("auth-token");
}
```

**Unkeyed `ICookieService` injection always resolves to the standard, unencrypted service** — even when encryption is registered. Nothing warns you. A constructor taking a plain `ICookieService` and writing what you believe is an encrypted cookie writes plain text instead, and it looks correct until someone reads the cookie in a browser. Use keyed injection for both services once encryption is enabled.

Encryption uses ASP.NET Core Data Protection. Each profile's `ProtectorPurpose` creates an isolated protector, so a cookie encrypted under one purpose cannot be decrypted under another — this is what stops a value from one cookie being replayed into another.

**Decryption failure returns `null` and logs a warning rather than throwing.** Key rotation, a tampered value, or a cookie written before the Data Protection path changed all present identically as "the cookie is not there". Treat `null` as "no valid value" rather than "no cookie".

### Cookie profiles

Profiles define the name, optional encryption purpose, and any overrides to the global defaults:

```csharp
app.PopulateStandardCookieProfiles(
    ("user-pref", null),                                        // global defaults
    ("theme", new CookieDefaultOverride(SameSiteMode.Strict)),
    ("consent", new CookieDefaultOverride(
        SameSiteMode.Lax,
        httpOnly: false,                                        // readable by JavaScript
        secure: true,
        maxAge: TimeSpan.FromDays(365)))
);

app.PopulateEncryptedCookieProfiles(
    ("auth-token", "AuthTokenProtector", null),
    ("session-data", "SessionProtector", new CookieDefaultOverride(SameSiteMode.Strict))
);
```

`CookieDefaultOverride` properties are nullable, and only non-null values override the global `CookieDefaultOptions` — anything left unset inherits. That makes an override a patch rather than a replacement, so adding a new global default automatically reaches cookies that never mentioned it.

Duplicate names throw `InvalidOperationException`, and the name space is shared between standard and encrypted profiles — the same name cannot be both.

### Managing profiles at runtime

`CookieProfileDictionary` is a singleton, so profiles are not limited to startup:

```csharp
public class TenantCookieAdmin(CookieProfileDictionary profiles)
{
    public void RegisterForTenant(string tenantId) =>
        profiles.TryCreateProfile($"tenant-{tenantId}-pref",
            new CookieDefaultOverride(SameSiteMode.Strict));

    public void Retire(string tenantId) =>
        profiles.TryRemoveProfile($"tenant-{tenantId}-pref");
}
```

This supports scenarios startup registration cannot, such as per-tenant cookies discovered after the application has started.

Because the dictionary is a singleton shared across all requests, a profile removed while another request is mid-flight makes that request's cookie operations start returning `false`. Removal is best kept to genuine lifecycle events rather than per-request logic.

## Next steps

- [Setup](Security-Setup.md) — registration, options, and cookie profile registration.
- [API Reference](Security-API.md)
