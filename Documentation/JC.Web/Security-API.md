# JC.Web: Security — API reference

Complete reference of all public types, properties, and methods in the JC.Web security area — security headers, Content Security Policy, and cookie management. See [Setup](Security-Setup.md) for registration and [Guide](Security-Guide.md) for usage examples.

> **Note:** Registration extensions (`IServiceCollection`, `IApplicationBuilder`) and options classes are documented in [Setup](Security-Setup.md), not here.

---

# Models

## CookieProfile

**Namespace:** `JC.Web.Security.Models`

Defines a cookie's identity, optional encryption configuration, and optional default overrides. Registered in a `CookieProfileDictionary` and resolved by cookie name whenever an `ICookieService` operation runs.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `CookieName` | `string` | — | get; | The name of the cookie. |
| `ProtectorPurpose` | `string?` | `null` | get; | The Data Protection protector purpose. When set, the cookie is treated as encrypted. |
| `IsEncrypted` | `bool` | Computed | get; | `true` when `ProtectorPurpose` is non-empty. |
| `DefaultOverride` | `CookieDefaultOverride?` | `null` | get; | Overrides merged on top of the global `CookieDefaultOptions`. |

### Constructors

#### CookieProfile(string cookieName, CookieDefaultOverride? @override = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name. Must not be null, empty, or whitespace. |
| `override` | `CookieDefaultOverride?` | `null` | Optional overrides. |

Creates an unencrypted cookie profile. Throws `ArgumentException` if `cookieName` is null, empty, or whitespace.

---

#### CookieProfile(string cookieName, string protectorPurpose, CookieDefaultOverride? @override = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name. |
| `protectorPurpose` | `string` | — | The Data Protection protector purpose. |
| `override` | `CookieDefaultOverride?` | `null` | Optional overrides. |

Creates an encrypted cookie profile. Throws `ArgumentNullException` if `protectorPurpose` is null or empty.

---

#### CookieProfile(CookieProfile profile, CookieDefaultOverride @override)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `profile` | `CookieProfile` | — | The existing profile to copy identity and encryption settings from. |
| `override` | `CookieDefaultOverride` | — | The replacement override. |

Creates a copy carrying a replacement `CookieDefaultOverride`. Used by `CookieProfileDictionary.TryUpdateProfileOverride` to swap overrides atomically.

---

## CookieDefaultOverride

**Namespace:** `JC.Web.Security.Models`

Selective overrides merged on top of the global `CookieDefaultOptions`. Only non-null properties apply; anything left `null` inherits the configured default, so an override acts as a patch rather than a replacement.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `HttpOnly` | `bool?` | `null` | get; set; | Override for `CookieOptions.HttpOnly`. |
| `Secure` | `bool?` | `null` | get; set; | Override for `CookieOptions.Secure`. |
| `SameSite` | `SameSiteMode?` | `null` | get; set; | Override for `CookieOptions.SameSite`. |
| `MaxAge` | `TimeSpan?` | `null` | get; set; | Override for `CookieOptions.MaxAge`. |
| `Path` | `string?` | `null` | get; set; | Override for `CookieOptions.Path`. |
| `Domain` | `string?` | `null` | get; set; | Override for `CookieOptions.Domain`. |
| `Expires` | `DateTimeOffset?` | `null` | get; set; | Override for `CookieOptions.Expires`. |
| `IsEssential` | `bool?` | `null` | get; | Override for `CookieOptions.IsEssential`. |

### Constructors

#### CookieDefaultOverride()

Creates an empty override with every property `null`, so all global defaults apply. Use this when the parameterised constructors do not cover the combination you need, setting individual properties afterwards.

---

#### CookieDefaultOverride(SameSiteMode sameSite, bool? httpOnly = null, bool? secure = null, TimeSpan? maxAge = null, bool? isEssential = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `sameSite` | `SameSiteMode` | — | The SameSite mode override. |
| `httpOnly` | `bool?` | `null` | Optional HttpOnly override. |
| `secure` | `bool?` | `null` | Optional Secure override. |
| `maxAge` | `TimeSpan?` | `null` | Optional MaxAge override. |
| `isEssential` | `bool?` | `null` | Optional IsEssential override. |

Creates an override covering the most commonly adjusted properties.

---

#### CookieDefaultOverride(SameSiteMode sameSite, bool httpOnly, bool secure, string path, string? domain = null, TimeSpan? maxAge = null, DateTimeOffset? expires = null, bool? isEssential = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `sameSite` | `SameSiteMode` | — | The SameSite mode override. |
| `httpOnly` | `bool` | — | The HttpOnly override. |
| `secure` | `bool` | — | The Secure override. |
| `path` | `string` | — | The path override. |
| `domain` | `string?` | `null` | Optional domain override. |
| `maxAge` | `TimeSpan?` | `null` | Optional MaxAge override. |
| `expires` | `DateTimeOffset?` | `null` | Optional absolute expiry override. |
| `isEssential` | `bool?` | `null` | Optional IsEssential override. |

Creates a fully specified override.

---

## CookieValidationResponse

**Namespace:** `JC.Web.Security.Models`

The outcome of a cookie validation, carrying both the comparison result and the value actually read.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `IsValid` | `bool` | — | get; init; | Whether the cookie value matched the expected value. |
| `ActualValue` | `string?` | — | get; init; | The value read from the cookie, or `null` if it could not be read. |
| `ValidationError` | `bool` | Computed | get; | `true` when `IsValid` is `false` and `ActualValue` is `null`, distinguishing "could not read the cookie" from "value did not match". |

---

# Enums

## XFrameOptionsMode

**Namespace:** `JC.Web.Security.Models`

Value for the `X-Frame-Options` response header.

| Member | Value | Description |
|--------|-------|-------------|
| `Deny` | `0` | The page cannot be displayed in a frame. |
| `SameOrigin` | `1` | The page can only be framed by the same origin. |

---

## ReferrerPolicyMode

**Namespace:** `JC.Web.Security.Models`

Value for the `Referrer-Policy` response header.

| Member | Value | Description |
|--------|-------|-------------|
| `NoReferrer` | `0` | No referrer information is sent. |
| `NoReferrerWhenDowngrade` | `1` | Full referrer same-origin; origin only for cross-origin HTTPS-to-HTTPS; nothing on downgrade. |
| `Origin` | `2` | Only the origin (scheme, host, port) is sent. |
| `OriginWhenCrossOrigin` | `3` | Full referrer same-origin; origin only cross-origin. |
| `SameOrigin` | `4` | Full referrer same-origin only; nothing cross-origin. |
| `StrictOrigin` | `5` | Origin only at the same security level; nothing on downgrade. |
| `StrictOriginWhenCrossOrigin` | `6` | Full referrer same-origin; origin cross-origin at the same security level; nothing on downgrade. |
| `UnsafeUrl` | `7` | The full referrer is always sent. |

---

## CrossOriginOpenerPolicyMode

**Namespace:** `JC.Web.Security.Models`

Value for the `Cross-Origin-Opener-Policy` response header.

| Member | Value | Description |
|--------|-------|-------------|
| `UnsafeNone` | `0` | Allows the document to join its opener's browsing context group. |
| `SameOriginAllowPopups` | `1` | Same-origin isolation, but popups keep a reference to the opener. |
| `SameOrigin` | `2` | Isolates the browsing context to same-origin documents only. |
| `NoOpenerAllowPopups` | `3` | Breaks opener references on cross-origin navigation while allowing popups. |

---

## CrossOriginResourcePolicyMode

**Namespace:** `JC.Web.Security.Models`

Value for the `Cross-Origin-Resource-Policy` response header.

| Member | Value | Description |
|--------|-------|-------------|
| `SameSite` | `0` | Only same-site requests may load the resource. |
| `SameOrigin` | `1` | Only same-origin requests may load the resource. |
| `CrossOrigin` | `2` | Any origin may load the resource. |

---

## CrossOriginEmbedderPolicyMode

**Namespace:** `JC.Web.Security.Models`

Value for the `Cross-Origin-Embedder-Policy` response header.

| Member | Value | Description |
|--------|-------|-------------|
| `UnsafeNone` | `0` | Allows cross-origin resources without CORS or CORP headers. |
| `RequireCorp` | `1` | Requires cross-origin resources to carry a valid `Cross-Origin-Resource-Policy` header or be served via CORS. |
| `Credentialless` | `2` | No-CORS cross-origin requests are sent without credentials. |

---

# Services

## ICookieService

**Namespace:** `JC.Web.Security.Services`

Creates, reads, deletes and validates HTTP cookies. Every operation references a cookie by name, which must already be registered as a `CookieProfile` in the `CookieProfileDictionary`. Operations against an unregistered name return `false`, `null`, or a `CookieValidationResponse` with `ValidationError` set — they never throw.

Two implementations exist: `CookieService` (standard) and `EncryptedCookieService` (ASP.NET Core Data Protection). With only unencrypted cookies registered, inject `ICookieService` directly. With encryption enabled both register as keyed services, and unkeyed injection resolves to the **standard** implementation — use `[FromKeyedServices(ICookieService.StandardCookieDIKey)]` or `[FromKeyedServices(ICookieService.EncryptedCookieDIKey)]` to be explicit.

### Constants

| Constant | Type | Value | Description |
|----------|------|-------|-------------|
| `StandardCookieDIKey` | `string` | `"CookieService"` | Keyed service key for the standard implementation. |
| `EncryptedCookieDIKey` | `string` | `"EncryptedCookieService"` | Keyed service key for the encrypted implementation. |

### Methods

#### TryCreateCookie(string cookieName, string content)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name, matching a registered `CookieProfile`. |
| `content` | `string` | — | The content to store. |

Looks up the profile by name. If found, resolves `CookieOptions` from the global `CookieDefaultOptions` merged with the profile's `CookieDefaultOverride`, then appends the cookie to the response. The encrypted implementation encrypts the content under the profile's `ProtectorPurpose` first; the standard implementation logs a warning if the profile carries a `ProtectorPurpose`. Returns `true` when the profile was found and the cookie written, `false` when no profile is registered.

---

#### GetCookie(string cookieName)

**Returns:** `string?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name, matching a registered `CookieProfile`. |

Reads the cookie value from the request. The encrypted implementation decrypts using the profile's `ProtectorPurpose` and returns `null` if decryption fails — a tampered value or a rotated Data Protection key — logging a warning rather than throwing. Also returns `null` when no profile is registered or the cookie is absent, so all three failures are indistinguishable to the caller.

---

#### ValidateCookie(string cookieName, string expectedValue, StringComparison comparison = StringComparison.Ordinal)

**Returns:** `CookieValidationResponse`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name, matching a registered `CookieProfile`. |
| `expectedValue` | `string` | — | The value expected in the cookie. |
| `comparison` | `StringComparison` | `Ordinal` | The comparison used. |

Reads the cookie, decrypting if the profile is encrypted, and compares it against `expectedValue`. `ValidationError` is `true` when the cookie could not be read at all — no profile registered, cookie absent, or decryption failed — in which case `IsValid` is `false` and `ActualValue` is `null`. When the read succeeds, `ActualValue` is populated and `IsValid` carries the comparison result.

---

#### TryDeleteCookie(string cookieName)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name, matching a registered `CookieProfile`. |

Deletes the cookie using options resolved from the profile's override merged with the global defaults. Returns `true` when the profile was found and the delete issued, `false` when no profile is registered.

---

#### CookieExists(string cookieName)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name, matching a registered `CookieProfile`. |

Returns `true` when a profile is registered and the cookie is present in the current request; `false` otherwise. Does not attempt decryption, so an encrypted cookie that cannot be decrypted still reports as existing.

---

## CookieProfileDictionary

**Namespace:** `JC.Web.Security.Services`

Thread-safe registry of `CookieProfile` instances keyed by cookie name. Registered as a singleton and used by the `ICookieService` implementations to resolve configuration. Profiles are usually registered at startup but may be created, updated and removed at runtime.

### Methods

#### TryCreateProfile(CookieProfile profile)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `profile` | `CookieProfile` | — | The profile to register. |

Registers a pre-built profile. Returns `true` if registered, `false` if a profile with that name already exists.

---

#### TryCreateProfile(string cookieName, CookieDefaultOverride? @override = null)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name. Must not already be registered. |
| `override` | `CookieDefaultOverride?` | `null` | Overrides merged on top of the global defaults. |

Creates and registers an unencrypted profile. Returns `true` if registered, `false` if the name is taken.

---

#### TryCreateProfile(string cookieName, string protectorPurpose, CookieDefaultOverride? @override = null)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name. Must not already be registered. |
| `protectorPurpose` | `string` | — | The Data Protection protector purpose. |
| `override` | `CookieDefaultOverride?` | `null` | Overrides merged on top of the global defaults. |

Creates and registers an encrypted profile. Returns `true` if registered, `false` if the name is taken.

---

#### GetProfile(string cookieName)

**Returns:** `CookieProfile?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name to look up. |

Returns the registered profile, or `null` when none exists for the name.

---

#### TryUpdateProfileOverride(string cookieName, CookieDefaultOverride @override)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name of the profile to update. |
| `override` | `CookieDefaultOverride` | — | The replacement override. |

Atomically replaces the profile's `CookieDefaultOverride`, preserving its name and encryption settings. Returns `true` when found and updated, `false` when no profile exists or the update lost a race against a concurrent change.

---

#### TryRemoveProfile(string cookieName)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name of the profile to remove. |

Removes the profile. Returns `true` when found and removed, `false` when none exists. Because the dictionary is a singleton, removal takes effect immediately for in-flight requests, whose subsequent cookie operations begin returning `false`.

---

#### HasProfile(string cookieName)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name to check. |

Returns `true` when a profile is registered for the name; otherwise `false`.

---

# Helpers

## ContentSecurityPolicyBuilder

**Namespace:** `JC.Web.Security.Helpers`

Fluent builder for `Content-Security-Policy` header values with directive-aware validation. Each directive method validates its sources against a per-directive allowlist of keywords, schemes, hosts, nonces and hashes. Invalid sources, or keywords not permitted for the directive, throw `ArgumentException` at the point of the call — so an invalid policy fails at startup rather than reaching browsers.

Keywords may be passed with or without quotes; `"self"` and `"'self'"` both normalise to `'self'`. `'none'` cannot be combined with other sources in the same directive.

### Directive methods

All directive methods accept `params string[] sources`, return the builder for chaining, and throw `ArgumentException` when a source is invalid for that directive.

#### DefaultSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `default-src`, the fallback for other fetch directives. Accepts `'self'` and `'none'`, schemes and host sources.

---

#### ScriptSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `script-src`. Accepts the full script keyword set — `'self'`, `'none'`, `'unsafe-inline'`, `'unsafe-eval'`, `'unsafe-hashes'`, `'strict-dynamic'`, `'wasm-unsafe-eval'`, `'report-sample'` — plus schemes, hosts, nonces and hashes.

---

#### ScriptSrcElem(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `script-src-elem`. As `script-src`, without `'unsafe-hashes'`.

---

#### ScriptSrcAttr(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `script-src-attr`. Accepts `'self'`, `'none'`, `'unsafe-inline'`, `'unsafe-eval'`, `'unsafe-hashes'` and `'report-sample'`. Does not accept `'strict-dynamic'` or `'wasm-unsafe-eval'`.

---

#### StyleSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `style-src`. Accepts `'self'`, `'none'`, `'unsafe-inline'`, `'unsafe-hashes'` and `'report-sample'`.

---

#### StyleSrcElem(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `style-src-elem`. As `style-src`, without `'unsafe-hashes'`.

---

#### StyleSrcAttr(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `style-src-attr`. Accepts `'self'`, `'none'`, `'unsafe-inline'`, `'unsafe-hashes'` and `'report-sample'`.

---

#### ImgSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `img-src`. Accepts `'self'`, `'none'`, schemes and hosts.

---

#### FontSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `font-src`. Accepts `'self'`, `'none'`, schemes and hosts.

---

#### ConnectSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `connect-src`, covering fetch, XHR, WebSocket and EventSource. Accepts `'self'`, `'none'`, schemes and hosts.

---

#### MediaSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `media-src`. Accepts `'self'`, `'none'`, schemes and hosts.

---

#### ObjectSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `object-src`, covering plugin sources. Accepts `'self'`, `'none'`, schemes and hosts.

---

#### FrameSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `frame-src`, covering nested browsing contexts. Accepts `'self'`, `'none'`, schemes and hosts.

---

#### ChildSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `child-src`, covering web workers and nested contexts. Accepts `'self'`, `'none'`, schemes and hosts.

---

#### WorkerSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `worker-src`. Accepts `'self'`, `'none'`, schemes and hosts.

---

#### ManifestSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `manifest-src`. Accepts `'self'`, `'none'`, schemes and hosts.

---

#### BaseUri(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `base-uri`, constraining `<base>` element URLs. Accepts `'self'`, `'none'`, schemes and hosts.

---

#### FormAction(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `form-action`, constraining form submission targets. Accepts `'self'`, `'none'`, schemes and hosts.

---

#### FrameAncestors(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to `frame-ancestors`, controlling which parents may embed the page. Accepts `'self'`, `'none'`, schemes and hosts.

---

#### UpgradeInsecureRequests()

**Returns:** `ContentSecurityPolicyBuilder`

Adds the `upgrade-insecure-requests` directive, instructing browsers to upgrade HTTP requests to HTTPS. Takes no sources; the directive is added once and further calls are ignored.

---

#### Sandbox(params string[] values)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `values` | `params string[]` | — | Optional sandbox tokens, such as `"allow-scripts"` or `"allow-forms"`. |

Adds the `sandbox` directive. Called with no arguments it adds an empty `sandbox`, the most restrictive form, and does nothing if the directive already exists. Called with tokens it validates each one, then appends them to the directive, ignoring duplicates.

Valid tokens: `allow-downloads`, `allow-forms`, `allow-modals`, `allow-orientation-lock`, `allow-pointer-lock`, `allow-popups`, `allow-popups-to-escape-sandbox`, `allow-presentation`, `allow-same-origin`, `allow-scripts`, `allow-top-navigation`, `allow-top-navigation-by-user-activation`, `allow-top-navigation-to-custom-protocols`. Throws `ArgumentException` for anything else.

Both forms write to the same directive, so a call with tokens after a no-argument call adds those tokens to the sandbox the first call created — relaxing it rather than being ignored. `Sandbox()` followed by `Sandbox("allow-scripts")` yields `sandbox allow-scripts`, not a fully restrictive sandbox.

---

#### ReportUri(string uri)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `uri` | `string` | — | A relative path such as `/csp-report`, or an absolute URI. |

Sets the `report-uri` directive for violation reporting. Throws `ArgumentException` if the URI is empty, protocol-relative, or otherwise invalid.

---

#### ReportTo(string groupName)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `groupName` | `string` | — | The Reporting API group name, matching a group declared in a `Report-To` header. |

Sets the `report-to` directive. Throws `ArgumentException` if the group name is empty or whitespace.

---

### Nonce and hash helpers

#### ScriptNonce(string nonce)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `nonce` | `string` | — | The raw base64 nonce, without the `'nonce-...'` wrapper. |

Adds a nonce source to `script-src`, wrapping the value as `'nonce-...'`. Throws `ArgumentException` if the nonce is empty, already wrapped, or not valid base64.

---

#### ScriptElemNonce(string nonce)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `nonce` | `string` | — | The raw base64 nonce, without the wrapper. |

Adds a nonce source to `script-src-elem`. Same validation as `ScriptNonce`.

---

#### StyleNonce(string nonce)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `nonce` | `string` | — | The raw base64 nonce, without the wrapper. |

Adds a nonce source to `style-src`. Same validation as `ScriptNonce`.

---

#### StyleElemNonce(string nonce)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `nonce` | `string` | — | The raw base64 nonce, without the wrapper. |

Adds a nonce source to `style-src-elem`. Same validation as `ScriptNonce`.

---

#### ScriptHash(string algorithm, string base64Hash)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `algorithm` | `string` | — | The hash algorithm: `sha256`, `sha384` or `sha512`. |
| `base64Hash` | `string` | — | The raw base64 hash, without the `'sha...-...'` wrapper. |

Adds a hash source to `script-src`. Throws `ArgumentException` for an unsupported algorithm, an already-wrapped hash, or a non-base64 value.

---

#### ScriptElemHash(string algorithm, string base64Hash)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `algorithm` | `string` | — | The hash algorithm. |
| `base64Hash` | `string` | — | The raw base64 hash. |

Adds a hash source to `script-src-elem`. Same validation as `ScriptHash`.

---

#### StyleHash(string algorithm, string base64Hash)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `algorithm` | `string` | — | The hash algorithm. |
| `base64Hash` | `string` | — | The raw base64 hash. |

Adds a hash source to `style-src`. Same validation as `ScriptHash`.

---

#### StyleElemHash(string algorithm, string base64Hash)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `algorithm` | `string` | — | The hash algorithm. |
| `base64Hash` | `string` | — | The raw base64 hash. |

Adds a hash source to `style-src-elem`. Same validation as `ScriptHash`.

---

### Build

#### Build()

**Returns:** `string?`

Joins all configured directives with `"; "` to produce the complete header value. Returns `null` when no directives have been configured, which is what allows `SecurityHeaderOptions.ContentSecurityPolicy` to omit the header entirely rather than emitting an empty one.

---

# Middleware

## SecurityHeaderMiddleware

**Namespace:** `JC.Web.Security.Middleware`

Applies security headers to every response based on `SecurityHeaderOptions`. Header values are pre-computed at construction, so there is no per-request cost beyond appending them — and no way to vary them per request.

### Methods

#### InvokeAsync(HttpContext context)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `HttpContext` | — | The HTTP context for the current request. |

Registers a callback on `Response.OnStarting` that applies the configured headers, then invokes the next middleware. Adds `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Cross-Origin-Embedder-Policy` and `Content-Security-Policy` from the pre-computed values, omitting any whose option is `null`.

`Strict-Transport-Security` is added only on HTTPS requests, and only outside development when `HstsProductionOnly` is `true`. `Server` and `X-Powered-By` are removed when configured, though a host such as IIS may re-add them after the application has finished with the response.

---

## Next steps

- [Setup](Security-Setup.md) — registration, options, and cookie profile registration.
- [Guide](Security-Guide.md) — header behaviour, Content Security Policy, and cookie management.
