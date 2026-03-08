# JC.Web — API reference

Complete reference of all public types, properties, and methods in JC.Web. See [Setup](Setup.md) for registration and [Guide](Guide.md) for usage examples.

## SecurityHeaderMiddleware

**Namespace:** `JC.Web.Security.Middleware`

Middleware that applies security headers to all HTTP responses based on `SecurityHeaderOptions`. Header values are pre-computed at construction time to avoid per-request overhead.

### Methods

#### InvokeAsync(HttpContext context)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `HttpContext` | — | The HTTP context for the current request. |

Registers a callback on `Response.OnStarting` that applies all configured security headers. Adds `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Cross-Origin-Embedder-Policy`, and `Content-Security-Policy` based on the pre-computed values from `SecurityHeaderOptions`. Adds `Strict-Transport-Security` only on HTTPS requests. Removes the `Server` and `X-Powered-By` headers if configured. Then invokes the next middleware.

---

## SecurityHeaderOptions

**Namespace:** `JC.Web.Security.Models.Options`

Configuration options for the security headers middleware. Controls which HTTP security headers are added to responses and their values.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `EnableXContentTypeOptions` | `bool` | `true` | get; set; | Whether to add the `X-Content-Type-Options: nosniff` header. |
| `XFrameOptions` | `XFrameOptionsMode?` | `SameOrigin` | get; set; | The `X-Frame-Options` header value. Set to `null` to omit. |
| `ReferrerPolicy` | `ReferrerPolicyMode?` | `StrictOriginWhenCrossOrigin` | get; set; | The `Referrer-Policy` header value. Set to `null` to omit. |
| `PermissionsPolicy` | `string?` | `"geolocation=(), microphone=(), camera=()"` | get; set; | The `Permissions-Policy` header value as a raw policy string. Set to `null` to omit. |
| `CrossOriginOpenerPolicy` | `CrossOriginOpenerPolicyMode?` | `null` | get; set; | The `Cross-Origin-Opener-Policy` header value. Not sent by default. |
| `CrossOriginResourcePolicy` | `CrossOriginResourcePolicyMode?` | `null` | get; set; | The `Cross-Origin-Resource-Policy` header value. Not sent by default. |
| `CrossOriginEmbedderPolicy` | `CrossOriginEmbedderPolicyMode?` | `null` | get; set; | The `Cross-Origin-Embedder-Policy` header value. Not sent by default. |
| `EnableHsts` | `bool` | `true` | get; set; | Whether to add the `Strict-Transport-Security` (HSTS) header on HTTPS responses. |
| `HstsMaxAge` | `TimeSpan` | `180 days` | get; set; | The HSTS `max-age` duration. |
| `HstsIncludeSubDomains` | `bool` | `false` | get; set; | Whether to include the `includeSubDomains` directive in the HSTS header. |
| `HstsProductionOnly` | `bool` | `true` | get; set; | Whether HSTS is only applied in production environments. |
| `RemoveServerHeader` | `bool` | `true` | get; set; | Whether to remove the `Server` response header. |
| `RemoveXPoweredByHeader` | `bool` | `true` | get; set; | Whether to remove the `X-Powered-By` response header. |
| `ContentSecurityPolicy` | `Action<ContentSecurityPolicyBuilder>?` | `null` | get; set; | Optional callback to configure the `Content-Security-Policy` header via `ContentSecurityPolicyBuilder`. When `null`, no CSP header is added. |

---

## ContentSecurityPolicyBuilder

**Namespace:** `JC.Web.Security.Helpers`

Fluent builder for constructing Content-Security-Policy header values with directive-aware validation. Each directive method validates sources against a per-directive allowlist of keywords, schemes, hosts, nonces, and hashes. Invalid sources or keyword/directive combinations throw `ArgumentException`.

### Directive methods

All directive methods accept `params string[] sources`, return the builder instance for chaining, and throw `ArgumentException` if a source is invalid for the directive.

#### DefaultSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `default-src` directive (fallback for other fetch directives). Accepts `'self'` and `'none'` keywords, schemes, and host sources.

---

#### ScriptSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `script-src` directive. Accepts all script keywords: `'self'`, `'none'`, `'unsafe-inline'`, `'unsafe-eval'`, `'unsafe-hashes'`, `'strict-dynamic'`, `'wasm-unsafe-eval'`, `'report-sample'`, plus schemes, hosts, nonces, and hashes.

---

#### ScriptSrcElem(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `script-src-elem` directive. Like `script-src` but without `'unsafe-hashes'`.

---

#### ScriptSrcAttr(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `script-src-attr` directive. Accepts `'self'`, `'none'`, `'unsafe-inline'`, `'unsafe-eval'`, `'unsafe-hashes'`, and `'report-sample'`. Does not accept `'strict-dynamic'` or `'wasm-unsafe-eval'`.

---

#### StyleSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `style-src` directive. Accepts `'self'`, `'none'`, `'unsafe-inline'`, `'unsafe-hashes'`, and `'report-sample'`.

---

#### StyleSrcElem(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `style-src-elem` directive. Like `style-src` but without `'unsafe-hashes'`.

---

#### StyleSrcAttr(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `style-src-attr` directive. Accepts `'self'`, `'none'`, `'unsafe-inline'`, `'unsafe-hashes'`, and `'report-sample'`.

---

#### ImgSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `img-src` directive. Accepts `'self'`, `'none'`, schemes, and hosts.

---

#### FontSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `font-src` directive. Accepts `'self'`, `'none'`, schemes, and hosts.

---

#### ConnectSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `connect-src` directive (fetch, XHR, WebSocket, EventSource). Accepts `'self'`, `'none'`, schemes, and hosts.

---

#### MediaSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `media-src` directive. Accepts `'self'`, `'none'`, schemes, and hosts.

---

#### ObjectSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `object-src` directive (plugin sources). Accepts `'self'`, `'none'`, schemes, and hosts.

---

#### FrameSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `frame-src` directive (nested browsing contexts). Accepts `'self'`, `'none'`, schemes, and hosts.

---

#### ChildSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `child-src` directive (web workers and nested contexts). Accepts `'self'`, `'none'`, schemes, and hosts.

---

#### WorkerSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `worker-src` directive. Accepts `'self'`, `'none'`, schemes, and hosts.

---

#### ManifestSrc(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `manifest-src` directive. Accepts `'self'`, `'none'`, schemes, and hosts.

---

#### BaseUri(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `base-uri` directive. Accepts `'self'`, `'none'`, schemes, and hosts.

---

#### FormAction(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `form-action` directive. Accepts `'self'`, `'none'`, schemes, and hosts.

---

#### FrameAncestors(params string[] sources)

**Returns:** `ContentSecurityPolicyBuilder`

Adds sources to the `frame-ancestors` directive. Accepts `'self'`, `'none'`, schemes, and hosts.

---

#### UpgradeInsecureRequests()

**Returns:** `ContentSecurityPolicyBuilder`

Adds the `upgrade-insecure-requests` directive, instructing browsers to upgrade HTTP requests to HTTPS. Takes no sources — the directive is added once and ignored on subsequent calls.

---

#### Sandbox(params string[] values)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `values` | `params string[]` | — | Optional sandbox tokens (e.g. `"allow-scripts"`, `"allow-forms"`). |

Adds the `sandbox` directive. Called with no arguments applies the most restrictive sandbox. Tokens selectively re-enable capabilities. Valid tokens: `allow-downloads`, `allow-forms`, `allow-modals`, `allow-orientation-lock`, `allow-pointer-lock`, `allow-popups`, `allow-popups-to-escape-sandbox`, `allow-presentation`, `allow-same-origin`, `allow-scripts`, `allow-top-navigation`, `allow-top-navigation-by-user-activation`, `allow-top-navigation-to-custom-protocols`. Throws `ArgumentException` for invalid tokens.

---

#### ReportUri(string uri)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `uri` | `string` | — | A relative path (e.g. `/csp-report`) or absolute URI. |

Sets the `report-uri` directive for CSP violation reporting. Throws `ArgumentException` if the URI is empty, protocol-relative, or otherwise invalid.

---

#### ReportTo(string groupName)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `groupName` | `string` | — | The Reporting API group name (must match a `Report-To` header group). |

Sets the `report-to` directive. Throws `ArgumentException` if the group name is empty or whitespace.

---

### Nonce and hash helpers

#### ScriptNonce(string nonce)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `nonce` | `string` | — | The raw base64 nonce value (without the `'nonce-...'` wrapper). |

Adds a nonce source to the `script-src` directive. Throws `ArgumentException` if the nonce is empty, already wrapped, or not valid base64.

---

#### ScriptElemNonce(string nonce)

**Returns:** `ContentSecurityPolicyBuilder`

Adds a nonce source to the `script-src-elem` directive. Same validation as `ScriptNonce`.

---

#### StyleNonce(string nonce)

**Returns:** `ContentSecurityPolicyBuilder`

Adds a nonce source to the `style-src` directive. Same validation as `ScriptNonce`.

---

#### StyleElemNonce(string nonce)

**Returns:** `ContentSecurityPolicyBuilder`

Adds a nonce source to the `style-src-elem` directive. Same validation as `ScriptNonce`.

---

#### ScriptHash(string algorithm, string base64Hash)

**Returns:** `ContentSecurityPolicyBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `algorithm` | `string` | — | The hash algorithm: `sha256`, `sha384`, or `sha512`. |
| `base64Hash` | `string` | — | The raw base64 hash value (without the `'sha...-...'` wrapper). |

Adds a hash source to the `script-src` directive. Throws `ArgumentException` for invalid algorithms, already-wrapped hashes, or non-base64 values.

---

#### ScriptElemHash(string algorithm, string base64Hash)

**Returns:** `ContentSecurityPolicyBuilder`

Adds a hash source to the `script-src-elem` directive. Same validation as `ScriptHash`.

---

#### StyleHash(string algorithm, string base64Hash)

**Returns:** `ContentSecurityPolicyBuilder`

Adds a hash source to the `style-src` directive. Same validation as `ScriptHash`.

---

#### StyleElemHash(string algorithm, string base64Hash)

**Returns:** `ContentSecurityPolicyBuilder`

Adds a hash source to the `style-src-elem` directive. Same validation as `ScriptHash`.

---

### Build

#### Build()

**Returns:** `string?`

Builds the complete Content-Security-Policy header value string by joining all directives with `"; "` separators. Returns `null` if no directives have been configured.

---

## XFrameOptionsMode

**Namespace:** `JC.Web.Security.Models`

Specifies the value for the `X-Frame-Options` HTTP response header.

| Member | Value | Description |
|--------|-------|-------------|
| `Deny` | `0` | The page cannot be displayed in a frame. |
| `SameOrigin` | `1` | The page can only be displayed in a frame on the same origin. |

---

## ReferrerPolicyMode

**Namespace:** `JC.Web.Security.Models`

Specifies the value for the `Referrer-Policy` HTTP response header.

| Member | Value | Description |
|--------|-------|-------------|
| `NoReferrer` | `0` | No referrer information is sent. |
| `NoReferrerWhenDowngrade` | `1` | Full referrer for same-origin; only origin for cross-origin HTTPS-to-HTTPS; nothing for downgrades. |
| `Origin` | `2` | Only the origin (scheme, host, port) is sent. |
| `OriginWhenCrossOrigin` | `3` | Full referrer for same-origin; only origin for cross-origin. |
| `SameOrigin` | `4` | Full referrer for same-origin only; nothing for cross-origin. |
| `StrictOrigin` | `5` | Origin-only for same-security-level; nothing for downgrades. |
| `StrictOriginWhenCrossOrigin` | `6` | Full referrer for same-origin; origin for cross-origin at same security; nothing for downgrades. |
| `UnsafeUrl` | `7` | Full referrer is always sent. |

---

## CrossOriginOpenerPolicyMode

**Namespace:** `JC.Web.Security.Models`

Specifies the value for the `Cross-Origin-Opener-Policy` HTTP response header.

| Member | Value | Description |
|--------|-------|-------------|
| `UnsafeNone` | `0` | Allows the document to be added to its opener's browsing context group. |
| `SameOriginAllowPopups` | `1` | Same-origin isolation but allows popups to retain a reference to the opener. |
| `SameOrigin` | `2` | Isolates the browsing context exclusively to same-origin documents. |
| `NoOpenerAllowPopups` | `3` | Breaks opener references for cross-origin navigations while allowing popups. |

---

## CrossOriginResourcePolicyMode

**Namespace:** `JC.Web.Security.Models`

Specifies the value for the `Cross-Origin-Resource-Policy` HTTP response header.

| Member | Value | Description |
|--------|-------|-------------|
| `SameSite` | `0` | Only requests from the same site can load the resource. |
| `SameOrigin` | `1` | Only requests from the same origin can load the resource. |
| `CrossOrigin` | `2` | Any origin can load the resource. |

---

## CrossOriginEmbedderPolicyMode

**Namespace:** `JC.Web.Security.Models`

Specifies the value for the `Cross-Origin-Embedder-Policy` HTTP response header.

| Member | Value | Description |
|--------|-------|-------------|
| `UnsafeNone` | `0` | Allows loading cross-origin resources without CORS or CORP headers. |
| `RequireCorp` | `1` | Requires all cross-origin resources to have a valid `Cross-Origin-Resource-Policy` header or be served via CORS. |
| `Credentialless` | `2` | No-CORS cross-origin requests are sent without credentials. |

---

## ICookieService

**Namespace:** `JC.Web.Security.Services`

Provides methods for creating, reading, deleting, and validating HTTP cookies. All operations reference cookies by name, which must first be registered as a `CookieProfile` in the `CookieProfileDictionary`. Operations against unregistered cookie names return `false`, `null`, or an invalid `CookieValidationResponse`.

Two implementations are provided: `CookieService` (standard, unencrypted) and `EncryptedCookieService` (uses ASP.NET Core Data Protection). When only unencrypted cookies are registered, inject via `ICookieService` directly. When encryption is enabled, both implementations are registered as keyed services — use `[FromKeyedServices(ICookieService.StandardCookieDIKey)]` or `[FromKeyedServices(ICookieService.EncryptedCookieDIKey)]`.

### Constants

| Constant | Type | Value | Description |
|----------|------|-------|-------------|
| `StandardCookieDIKey` | `string` | `"CookieService"` | Keyed service key for the standard (unencrypted) implementation. |
| `EncryptedCookieDIKey` | `string` | `"EncryptedCookieService"` | Keyed service key for the encrypted (Data Protection) implementation. |

### Methods

#### TryCreateCookie(string cookieName, string content)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name, matching a registered `CookieProfile`. |
| `content` | `string` | — | The content to store in the cookie. |

Looks up the `CookieProfile` by name. If found, resolves `CookieOptions` from the global `CookieDefaultOptions` merged with any `CookieDefaultOverride` on the profile, then appends the cookie to the response. The encrypted implementation encrypts the content using the profile's `ProtectorPurpose` before writing. The standard implementation logs a warning if the profile has a `ProtectorPurpose` set. Returns `true` if the profile was found and the cookie was written; `false` if no profile is registered.

---

#### GetCookie(string cookieName)

**Returns:** `string?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name, matching a registered `CookieProfile`. |

Reads the cookie value from the request. The encrypted implementation decrypts the value using the profile's `ProtectorPurpose`; returns `null` if decryption fails (e.g. tampered cookie or rotated data protection key). Returns `null` if no profile is registered or the cookie does not exist.

---

#### ValidateCookie(string cookieName, string expectedValue, StringComparison comparison = StringComparison.Ordinal)

**Returns:** `CookieValidationResponse`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name, matching a registered `CookieProfile`. |
| `expectedValue` | `string` | — | The value expected to be found in the cookie. |
| `comparison` | `StringComparison` | `Ordinal` | The type of string comparison to use. |

Reads the cookie value (decrypting if encrypted) and compares it against the expected value. `ValidationError` is `true` when the cookie could not be read at all — either no profile is registered, the cookie does not exist, or decryption failed. In these cases `IsValid` is `false` and `ActualValue` is `null`. When the cookie is successfully read, `ActualValue` is populated and `IsValid` reflects the comparison result.

---

#### TryDeleteCookie(string cookieName)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name, matching a registered `CookieProfile`. |

Deletes the cookie using options resolved from the profile's `CookieDefaultOverride` merged with global defaults. Returns `true` if the profile was found and the delete was issued; `false` if no profile is registered.

---

#### CookieExists(string cookieName)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name, matching a registered `CookieProfile`. |

Checks whether a cookie with the specified name exists in the current request. Returns `true` if the profile exists and the cookie is present; `false` if no profile is registered or the cookie is not found.

---

## CookieProfileDictionary

**Namespace:** `JC.Web.Security.Services`

Thread-safe registry of `CookieProfile` instances, keyed by cookie name. Registered as a singleton and used by `ICookieService` implementations to resolve cookie configuration by name. Profiles are typically registered at startup but can be created at runtime.

### Methods

#### TryCreateProfile(CookieProfile profile)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `profile` | `CookieProfile` | — | The cookie profile to register. |

Registers a pre-built `CookieProfile`. Returns `true` if registered; `false` if a profile with the same name already exists.

---

#### TryCreateProfile(string cookieName, CookieDefaultOverride? @override = null)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name. Must not already be registered. |
| `override` | `CookieDefaultOverride?` | `null` | Optional overrides merged on top of global `CookieDefaultOptions`. |

Creates and registers an unencrypted cookie profile. Returns `true` if registered; `false` if a profile with the same name already exists.

---

#### TryCreateProfile(string cookieName, string protectorPurpose, CookieDefaultOverride? @override = null)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name. Must not already be registered. |
| `protectorPurpose` | `string` | — | The Data Protection protector purpose string. |
| `override` | `CookieDefaultOverride?` | `null` | Optional overrides merged on top of global `CookieDefaultOptions`. |

Creates and registers an encrypted cookie profile. Returns `true` if registered; `false` if a profile with the same name already exists.

---

#### GetProfile(string cookieName)

**Returns:** `CookieProfile?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name to look up. |

Returns the registered profile, or `null` if no profile exists for the name.

---

#### TryUpdateProfileOverride(string cookieName, CookieDefaultOverride @override)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name of the profile to update. |
| `override` | `CookieDefaultOverride` | — | The new override to apply. |

Atomically replaces the `CookieDefaultOverride` on an existing profile, preserving the cookie's name and encryption settings. Returns `true` if the profile was found and updated; `false` if no profile exists or the update lost a race.

---

#### TryRemoveProfile(string cookieName)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name of the profile to remove. |

Removes the profile. Returns `true` if found and removed; `false` if no profile exists.

---

#### HasProfile(string cookieName)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cookieName` | `string` | — | The cookie name to check. |

Returns `true` if a profile is registered for the name; otherwise `false`.

---

## CookieProfile

**Namespace:** `JC.Web.Security.Models`

Defines a cookie's identity, optional encryption configuration, and optional default overrides. Registered in a `CookieProfileDictionary` and resolved by cookie name when `ICookieService` operations are performed.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `CookieName` | `string` | — | get; | The name of the cookie. |
| `ProtectorPurpose` | `string?` | `null` | get; | The Data Protection protector purpose string. When set, the cookie is treated as encrypted. |
| `IsEncrypted` | `bool` | — | get; | Computed property — `true` when `ProtectorPurpose` is non-empty. |
| `DefaultOverride` | `CookieDefaultOverride?` | `null` | get; | Optional overrides merged on top of global `CookieDefaultOptions`. |

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
| `protectorPurpose` | `string` | — | The Data Protection protector purpose string. |
| `override` | `CookieDefaultOverride?` | `null` | Optional overrides. |

Creates an encrypted cookie profile. Throws `ArgumentNullException` if `protectorPurpose` is null or empty.

---

#### CookieProfile(CookieProfile profile, CookieDefaultOverride @override)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `profile` | `CookieProfile` | — | The existing profile to copy identity and encryption settings from. |
| `override` | `CookieDefaultOverride` | — | The new override to apply. |

Creates a copy with a replacement `CookieDefaultOverride`. Used by `CookieProfileDictionary.TryUpdateProfileOverride` to atomically swap overrides.

---

## CookieDefaultOverride

**Namespace:** `JC.Web.Security.Models`

Selective overrides merged on top of the global `CookieDefaultOptions`. Only non-null properties are applied — anything left `null` falls back to the configured defaults.

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

Creates an empty override. All properties default to `null` (use global defaults). Use this constructor for fine-grained control when the other constructors don't cover the combination of properties you need — set individual properties after construction.

---

#### CookieDefaultOverride(SameSiteMode sameSite, bool? httpOnly = null, bool? secure = null, TimeSpan? maxAge = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `sameSite` | `SameSiteMode` | — | The SameSite mode override. |
| `httpOnly` | `bool?` | `null` | Optional HttpOnly override. |
| `secure` | `bool?` | `null` | Optional Secure override. |
| `maxAge` | `TimeSpan?` | `null` | Optional MaxAge override. |

Creates an override with the most commonly adjusted properties.

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
| `expires` | `DateTimeOffset?` | `null` | Optional absolute expiration override. |
| `isEssential` | `bool?` | `null` | Optional IsEssential override. |

Creates a fully specified override.

---

## CookieDefaultOptions

**Namespace:** `JC.Web.Security.Models.Options`

Global default options applied to all cookies created by `ICookieService` implementations. These defaults are used as the baseline, with any `CookieDefaultOverride` properties merged on top.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `HttpOnly` | `bool` | `true` | get; set; | Whether cookies are inaccessible to client-side JavaScript. |
| `Secure` | `bool` | `true` | get; set; | Whether cookies are only sent over HTTPS. |
| `SameSite` | `SameSiteMode` | `Lax` | get; set; | The SameSite attribute for cookies. |
| `MaxAge` | `TimeSpan?` | `null` | get; set; | The maximum age. When `null`, the cookie is a session cookie. |
| `Path` | `string?` | `"/"` | get; set; | The URL path the cookie is valid for. |
| `Domain` | `string?` | `null` | get; set; | The domain the cookie is valid for. |
| `Expires` | `DateTimeOffset?` | `null` | get; set; | The absolute expiration date. If both `Expires` and `MaxAge` are set, `MaxAge` takes precedence per the HTTP specification. |
| `IsEssential` | `bool` | `false` | get; set; | Whether the cookie is essential and should bypass consent checks. |

---

## CookieValidationResponse

**Namespace:** `JC.Web.Security.Models`

The result of a cookie validation operation, containing the comparison outcome and the actual cookie value.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `IsValid` | `bool` | — | get; init; | Whether the cookie value matched the expected value. |
| `ActualValue` | `string?` | — | get; init; | The actual value read from the cookie, or `null` if not found or decryption failed. |
| `ValidationError` | `bool` | — | get; | Computed property — `true` when `IsValid` is `false` and `ActualValue` is `null`, indicating the cookie could not be read rather than a value mismatch. |

---

## RequestMetadataMiddleware

**Namespace:** `JC.Web.ClientProfiling.Middleware`

Middleware that builds `RequestMetadata` early in the pipeline and stores it in `HttpContext.Items` for downstream access. Resolves the client IP via `ClientIpResolver`, parses the user agent via `UserAgentService`, and optionally enriches with geolocation data if an `IGeoLocationProvider` is registered. Retrieve the metadata via `HttpContextExtensions.GetRequestMetadata`.

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

Resolves the client IP using `ClientIpResolver.Resolve` with the configured `TrustProxyHeaders` setting. Parses the `User-Agent` header into a `UserAgent` model. Calls the geolocation provider's `ResolveAsync` to optionally enrich with location data. Builds a `RequestMetadata` instance capturing client IP, user agent, HTTPS status, timestamp, request path (with HTTP method prefix), query string, origin, referer, and trace identifier. Stores the result in `HttpContext.Items` keyed by `typeof(RequestMetadata)`, then invokes the next middleware.

---

## RequestMetadata

**Namespace:** `JC.Web.ClientProfiling.Models`

Captures structured metadata about an HTTP request including client IP, user agent, protocol, and request properties. Built by `RequestMetadataMiddleware` and stored in `HttpContext.Items` for downstream access.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `ClientIp` | `string` | get; | The resolved client IP address. |
| `UserAgent` | `UserAgent` | get; | The parsed user agent information. |
| `GeoLocation` | `GeoLocation?` | get; | Geographic location resolved from the client IP, if a provider is registered. |
| `IsHttps` | `bool` | get; | Whether the request was made over HTTPS. |
| `RequestTimestamp` | `DateTimeOffset` | get; | UTC timestamp of when the request was processed by the middleware. |
| `RequestPath` | `string?` | get; | The HTTP method and request path (e.g. `"GET /api/users"`). |
| `RequestQuery` | `string?` | get; | The query string portion of the request URL. |
| `RequestOrigin` | `string?` | get; | The `Origin` header value, if present. |
| `RequestReferer` | `string?` | get; | The `Referer` header value, if present. |
| `RequestId` | `string?` | get; | The trace identifier from `HttpContext.TraceIdentifier`. |

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

Returns a JSON string representation of the request metadata for structured logging. Sensitive properties are masked by default using `StringExtensions.Mask` with 0 visible characters. Includes all request properties, user agent details (browser, version, OS, device type, bot flag, raw value), and geolocation data (country, country code, region, city).

---

## UserAgentService

**Namespace:** `JC.Web.ClientProfiling.Services`

Parses user agent strings into structured `UserAgent` objects using the UAParser library. Maintains a singleton `Parser` instance for efficient repeated parsing.

### Methods

#### Parse(string? userAgentString)

**Returns:** `UserAgent`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userAgentString` | `string?` | — | The raw user agent header value. |

Parses the raw user agent string into a `UserAgent` model. Detects browser, browser version, operating system, OS version, and device type. Returns a model with `DeviceType.Unknown` and null properties if the input is null or empty. Browser and OS values that resolve to "Other" in UAParser are normalised to `null`. Device type detection checks for bots (including headless Chrome, PhantomJS, Lighthouse), tablets (iPad, Android without "mobile"), mobile devices (iPhone, iPod, Android with "mobile"), and desktops (Windows, Mac OS, Linux, Chrome OS). Falls back to `Unknown` if no pattern matches.

---

## UserAgent

**Namespace:** `JC.Web.ClientProfiling.Models`

Represents a parsed user agent with browser, operating system, and device type information. All properties are get-only, set via the constructor.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `RawValue` | `string` | get; | The original user agent string. |
| `Browser` | `string?` | get; | The detected browser name, or `null` if unrecognised. |
| `BrowserVersion` | `string?` | get; | The detected browser version, or `null`. |
| `OperatingSystem` | `string?` | get; | The detected operating system name, or `null`. |
| `OS` | `string?` | get; | Alias for `OperatingSystem`. |
| `OperatingSystemVersion` | `string?` | get; | The detected OS version, or `null`. |
| `OSVersion` | `string?` | get; | Alias for `OperatingSystemVersion`. |
| `DeviceType` | `DeviceType` | get; | The detected device type. |
| `IsMobile` | `bool` | get; | Computed — `true` if `DeviceType` is `Mobile` or `Tablet`. |
| `IsBot` | `bool` | get; | Computed — `true` if `DeviceType` is `Bot`. |

### Constructor

#### UserAgent(string rawValue, string? browser, string? browserVersion, string? os, string? osVersion, DeviceType type = DeviceType.Unknown)

All properties are get-only and set via the constructor.

---

## DeviceType

**Namespace:** `JC.Web.ClientProfiling.Models`

Enum representing the type of device detected from a user agent string.

| Member | Value | Description |
|--------|-------|-------------|
| `Desktop` | `0` | A desktop computer. |
| `Mobile` | `1` | A mobile phone. |
| `Tablet` | `2` | A tablet device. |
| `Bot` | `3` | An automated bot or crawler. |
| `Unknown` | `4` | Device type could not be determined. |

---

## ClientIpResolver

**Namespace:** `JC.Web.ClientProfiling.Helpers`

Static helper for resolving the client IP address from an HTTP request. The primary strategy uses `ConnectionInfo.RemoteIpAddress`, which is correct when ASP.NET Core's `UseForwardedHeaders()` middleware is configured with trusted proxies. An optional header fallback mode is available for non-standard proxy setups.

### Methods

#### Resolve(HttpContext context, bool useHeaderFallback = false)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `HttpContext` | — | The current HTTP context. |
| `useHeaderFallback` | `bool` | `false` | When `true`, inspects forwarded headers first before falling back to `RemoteIpAddress`. Only enable behind a trusted proxy. |

When `useHeaderFallback` is `true`, checks headers in order: `CF-Connecting-IPv6` (Cloudflare IPv6), `CF-Connecting-IP` (Cloudflare), `X-Real-IP` (nginx), then the first entry in `X-Forwarded-For` (general proxies). Falls back to `RemoteIpAddress` if no headers are found. When `useHeaderFallback` is `false`, returns `RemoteIpAddress` directly. Returns `"unknown"` if no IP could be determined.

---

## IGeoLocationProvider

**Namespace:** `JC.Web.ClientProfiling.Services`

Contract for resolving geographic location from an IP address. JC.Web does not ship a built-in implementation — consumers should implement this interface using their chosen provider (e.g. MaxMind GeoLite2, IP2Location, ip-api). When registered in DI, the request metadata middleware automatically enriches `RequestMetadata` with the resolved `GeoLocation`. An internal `EmptyGeoLocationProvider` (always returns `null`) is registered by default when no provider is configured.

### Methods

#### Resolve(string ipAddress, GeoLocationOptions options)

**Returns:** `GeoLocation?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ipAddress` | `string` | — | The client IP address to look up. |
| `options` | `GeoLocationOptions` | — | Controls the granularity of the lookup (region, city). |

Returns a `GeoLocation` if the lookup succeeded; `null` if the IP could not be resolved.

---

#### ResolveAsync(string ipAddress, GeoLocationOptions options)

**Returns:** `Task<GeoLocation?>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ipAddress` | `string` | — | The client IP address to look up. |
| `options` | `GeoLocationOptions` | — | Controls the granularity of the lookup (region, city). |

Asynchronous version for API-based providers. The default implementation delegates to the synchronous `Resolve` method.

---

## GeoLocation

**Namespace:** `JC.Web.ClientProfiling.Models`

Represents the geographic location resolved from a client IP address. All properties are get-only, set via the constructor.

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Country` | `string?` | get; | The country name (e.g. "United Kingdom"). |
| `CountryCode` | `string?` | get; | The ISO 3166-1 alpha-2 country code (e.g. "GB"). |
| `Region` | `string?` | get; | The region, state, or province. Only populated when `GeoLocationOptions.IncludeRegion` is `true`. |
| `City` | `string?` | get; | The city or town. Only populated when `GeoLocationOptions.IncludeCity` is `true`. |

### Constructor

#### GeoLocation(string? country, string? countryCode, string? region = null, string? city = null)

All properties are get-only and set via the constructor.

---

## GeoLocationOptions

**Namespace:** `JC.Web.ClientProfiling.Models.Options`

Controls the granularity of geographic location resolution.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `IncludeRegion` | `bool` | `true` | get; set; | Whether to include region/state/province in the result. |
| `IncludeCity` | `bool` | `false` | get; set; | Whether to include city/town in the result. |

---

## ClientIpOptions

**Namespace:** `JC.Web.ClientProfiling.Models.Options`

Configuration for client IP address resolution.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `TrustProxyHeaders` | `bool` | `false` | get; set; | When `true`, the resolver reads forwarded/proxy headers before falling back to `RemoteIpAddress`. Only enable when all traffic flows through a trusted proxy — clients can spoof forwarded headers on direct connections. |

---

## BotFilterMiddleware

**Namespace:** `JC.Web.ClientProfiling.Middleware`

Middleware that blocks requests from detected bots based on the `RequestMetadata` stored in `HttpContext.Items` by `RequestMetadataMiddleware`. Must be registered after `RequestMetadataMiddleware` in the pipeline.

### Methods

#### InvokeAsync(HttpContext context)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `HttpContext` | — | The HTTP context for the current request. |

If `BotFilterOptions.IsEnabled` is `false`, passes the request through immediately. Otherwise, retrieves `RequestMetadata` from `HttpContext.Items`. If the request is from a bot (`UserAgent.IsBot` is `true`): checks the path filter — if set and the path does not match, the request passes through. Then checks if the bot's browser name is in the `AllowedBots` set — if so, the request passes through. Otherwise, short-circuits the request with the configured `BotFilterStatusCode`.

---

## BotFilterOptions

**Namespace:** `JC.Web.ClientProfiling.Models.Options`

Configuration for the bot filtering middleware.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `StatusCode` | `BotFilterStatusCode` | `Forbidden` | get; set; | The HTTP status code returned to blocked bots. |
| `AllowedBots` | `HashSet<string>` | empty (case-insensitive) | get; set; | Bot browser names that are allowed through. Matched against `UserAgent.Browser`. |
| `PathFilter` | `Func<string, bool>?` | `null` | get; set; | Optional path filter. When set, only matching paths are subject to bot filtering. |
| `IsEnabled` | `bool` | `true` | get; set; | Whether bot filtering is enabled. |

---

## BotFilterStatusCode

**Namespace:** `JC.Web.ClientProfiling.Models.Options`

HTTP status codes that can be returned by the bot filtering middleware.

| Member | Value | Description |
|--------|-------|-------------|
| `NoContent` | `204` | 204 No Content. |
| `BadRequest` | `400` | 400 Bad Request. |
| `Unauthorized` | `401` | 401 Unauthorized. |
| `Forbidden` | `403` | 403 Forbidden. |
| `NotFound` | `404` | 404 Not Found. |

---

## HttpContextExtensions

**Namespace:** `JC.Web.ClientProfiling`

Static extension methods for accessing client profiling data from `HttpContext`.

### Methods

#### GetRequestMetadata(this HttpContext context)

**Returns:** `RequestMetadata?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `HttpContext` | — | The current HTTP context. |

Retrieves the `RequestMetadata` stored by `RequestMetadataMiddleware` from `HttpContext.Items`. Returns `null` if the middleware has not run or no metadata is stored.

---

## RateLimitingOptions

**Namespace:** `JC.Web.RateLimiting`

Configuration for JC.Web rate limiting middleware. Wraps ASP.NET Core's built-in rate limiting with sensible defaults and simplified partitioning.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `IsEnabled` | `bool` | `true` | get; set; | Whether rate limiting is enabled. When `false`, the middleware is not registered. |
| `Strategy` | `RateLimitingStrategy` | `SlidingWindow` | get; set; | The rate limiting algorithm to apply. |
| `PermitLimit` | `int` | `100` | get; set; | Maximum requests within the window. Used by `FixedWindow` and `SlidingWindow`. |
| `Window` | `TimeSpan` | `1 minute` | get; set; | The time window for rate limiting. |
| `PartitionBy` | `RateLimitPartitionBy` | `ClientIp` | get; set; | How requests are partitioned. |
| `SegmentsPerWindow` | `int` | `6` | get; set; | Segments per window for `SlidingWindow`. Ignored by other strategies. |
| `TokensPerPeriod` | `int` | `10` | get; set; | Tokens added per window for `TokenBucket`. Ignored by other strategies. |
| `TokenLimit` | `int` | `0` | get; set; | Maximum tokens the bucket can hold for `TokenBucket`. Ignored by other strategies. |
| `ConcurrencyLimit` | `int` | `0` | get; set; | Maximum concurrent requests for `Concurrency`. Ignored by other strategies. |
| `QueueLimit` | `int` | `0` | get; set; | Maximum requests to queue when the limit is reached. |
| `QueueProcessingOrder` | `QueueProcessingOrder` | `OldestFirst` | get; set; | The processing order for queued requests. |
| `ExcludeStaticFiles` | `bool` | `true` | get; set; | Whether to exclude static file requests (`.css`, `.js`, `.png`, `.jpg`, `.jpeg`, `.gif`, `.svg`, `.ico`, `.woff`, `.woff2`, `.ttf`, `.eot`, `.map`, `.webp`, `.avif`, `.bmp`) from rate limiting. |

---

## RateLimitingStrategy

**Namespace:** `JC.Web.RateLimiting`

The rate limiting algorithm to apply.

| Member | Value | Description |
|--------|-------|-------------|
| `FixedWindow` | `0` | Fixed window — resets the counter at the end of each window. |
| `SlidingWindow` | `1` | Sliding window — divides the window into segments for smoother limiting. |
| `TokenBucket` | `2` | Token bucket — tokens replenish at a fixed rate, allowing controlled bursts. |
| `Concurrency` | `3` | Concurrency — limits the number of concurrent requests rather than rate. |

---

## RateLimitPartitionBy

**Namespace:** `JC.Web.RateLimiting`

How requests are partitioned for rate limiting.

| Member | Value | Description |
|--------|-------|-------------|
| `ClientIp` | `0` | Partition by client IP address, resolved via `ClientIpResolver`. |
| `User` | `1` | Partition by authenticated user identity. Falls back to client IP for anonymous requests. |
| `Endpoint` | `2` | Partition by request endpoint path. |
| `ClientIpAndEndpoint` | `3` | Partition by client IP combined with endpoint path. |

---

## AlertTagHelper

**Namespace:** `JC.Web.UI.TagHelpers`

Tag helper that renders a Bootstrap 5 alert component. Targets the `<alert>` element (self-closing). Suppresses output if `Message` is null or whitespace.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Type` | `AlertType` | `Info` | get; set; | The alert type. HTML attribute: `type`. |
| `Message` | `string?` | `null` | get; set; | The alert message content. HTML attribute: `message`. |
| `Dismissible` | `bool` | `true` | get; set; | Whether the alert is dismissible. HTML attribute: `dismissible`. |

---

## PaginationTagHelper

**Namespace:** `JC.Web.UI.TagHelpers`

Tag helper that renders Bootstrap-compatible pagination from an `IPagination<T>` model. Targets the `<pagination>` element (self-closing). Suppresses output if the model is null or has only one page. Renders previous/next links, numbered page buttons with ellipsis when exceeding `MaxVisiblePages`, and optional first/last page links.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Model` | `IPagination<object>?` | `null` | get; set; | The pagination model. HTML attribute: `model`. |
| `HrefFormat` | `string` | `"?page={0}"` | get; set; | URL format string with `{0}` as the page number placeholder. HTML attribute: `href-format`. |
| `MaxVisiblePages` | `int` | `5` | get; set; | Maximum page links before showing ellipsis. HTML attribute: `max-pages`. |
| `PreviousText` | `string` | `"&laquo;"` | get; set; | Text for the "previous" link. HTML attribute: `previous-text`. |
| `NextText` | `string` | `"&raquo;"` | get; set; | Text for the "next" link. HTML attribute: `next-text`. |
| `ShowFirstLast` | `bool` | `true` | get; set; | Whether to show first/last page links. HTML attribute: `show-first-last`. |
| `ContainerClass` | `string?` | `null` | get; set; | Additional CSS classes for the nav container. HTML attribute: `container-class`. |

---

## BreadcrumbTagHelper

**Namespace:** `JC.Web.UI.TagHelpers`

Tag helper that renders a Bootstrap 5 breadcrumb navigation from nested `<crumb>` elements. Targets the `<breadcrumb>` element. The last crumb is automatically rendered as the active page. Suppresses output if no crumbs are provided.

---

## CrumbTagHelper

**Namespace:** `JC.Web.UI.TagHelpers`

Child tag helper for `BreadcrumbTagHelper`. Defines a single breadcrumb item. Must be nested inside a `<breadcrumb>` element. Targets the `<crumb>` element (self-closing, parent tag: `breadcrumb`).

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Label` | `string` | `""` | get; set; | The display text. HTML attribute: `label`. |
| `Href` | `string?` | `null` | get; set; | The URL. If omitted, renders as plain text. HTML attribute: `href`. |

---

## BugReporterTagHelper

**Namespace:** `JC.Web.UI.TagHelpers`

Tag helper that renders a floating bug reporter widget with a toggle button, a report form (type + description), and JavaScript to submit reports via POST. Targets the `<bug-reporter>` element (self-closing). Automatically includes `RequestMetadata` as context in the submission payload, and sends an anti-forgery token when available. Assumes Bootstrap 5 is available. Throws `InvalidOperationException` if `Endpoint` is not set.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Endpoint` | `string?` | `null` | get; set; | The POST endpoint that receives bug reports. Required. HTML attribute: `endpoint`. |
| `Icon` | `string` | bug emoji | get; set; | The icon on the floating button. HTML attribute: `icon`. |
| `Title` | `string` | `"Send Feedback"` | get; set; | The title text for the report form. HTML attribute: `title`. |
| `Colour` | `string` | `"danger"` | get; set; | Bootstrap contextual suffix for card border, title, and submit button. HTML attribute: `colour`. |
| `ViewContext` | `ViewContext` | — | get; set; | Automatically injected by the framework. Not bound to an HTML attribute. |

---

## TableBuilder\<T\>

**Namespace:** `JC.Web.UI.HTML`

Fluent builder for rendering Bootstrap HTML tables from a collection of items. Cell content is HTML-encoded to prevent XSS.

### Methods

#### AddColumn(string header, Func\<T, string?\> valueSelector, string? cssClass = null)

**Returns:** `TableBuilder<T>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `header` | `string` | — | The column header text. |
| `valueSelector` | `Func<T, string?>` | — | A function that extracts the cell value from each item. |
| `cssClass` | `string?` | `null` | Optional CSS class applied to both the `<th>` and `<td>` elements. |

Adds a column with a string value selector.

---

#### AddColumn(string header, Func\<T, object?\> valueSelector, string? cssClass = null)

**Returns:** `TableBuilder<T>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `header` | `string` | — | The column header text. |
| `valueSelector` | `Func<T, object?>` | — | A function that extracts the cell value from each item. Converted via `ToString`. |
| `cssClass` | `string?` | `null` | Optional CSS class applied to both the `<th>` and `<td>` elements. |

Adds a column with an object value selector.

---

#### Build(IEnumerable\<T\> items, string? tableClass = null)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `items` | `IEnumerable<T>` | — | The collection of items to render as table rows. |
| `tableClass` | `string?` | `null` | CSS classes for the `<table>` element. Defaults to `"table"` if null or whitespace. |

Builds and returns the complete HTML table. Header text and cell values are HTML-encoded.

---

## BreadcrumbBuilder

**Namespace:** `JC.Web.UI.HTML`

Fluent builder for constructing Bootstrap 5 breadcrumb navigation. The last item added is always rendered as the active page. Supports implicit conversion to `string`.

### Methods

#### Add(string label, string? url = null)

**Returns:** `BreadcrumbBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `label` | `string` | — | The display text for the breadcrumb item. |
| `url` | `string?` | `null` | The URL to link to. If `null`, renders as plain text. |

Adds a breadcrumb item. The last item added will be rendered as the active page.

---

#### Build()

**Returns:** `string`

Builds and returns the complete breadcrumb HTML as a Bootstrap 5 `<nav>` with an `<ol class="breadcrumb">`. Returns an empty string if no items have been added. Labels and URLs are HTML-encoded.

---

## AlertHelper

**Namespace:** `JC.Web.UI.HTML`

Static helper for rendering Bootstrap 5 alert components.

### Methods

#### Success(string message, bool dismissible = true)

**Returns:** `string`

Renders a Bootstrap success alert (`alert-success`).

---

#### Warning(string message, bool dismissible = true)

**Returns:** `string`

Renders a Bootstrap warning alert (`alert-warning`).

---

#### Error(string message, bool dismissible = true)

**Returns:** `string`

Renders a Bootstrap danger alert (`alert-danger`).

---

#### Info(string message, bool dismissible = true)

**Returns:** `string`

Renders a Bootstrap info alert (`alert-info`).

---

#### ForType(AlertType type, string message, bool dismissible = true)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `type` | `AlertType` | — | The alert type. |
| `message` | `string` | — | The alert message content (may contain HTML). |
| `dismissible` | `bool` | `true` | Whether the alert can be dismissed. |

Renders a Bootstrap alert for the specified type. Dismissible alerts include `alert-dismissible`, `fade`, and `show` classes with a close button.

---

## AlertType

**Namespace:** `JC.Web.UI.HTML`

Specifies the type of Bootstrap alert to render.

| Member | Value | Description |
|--------|-------|-------------|
| `Success` | `0` | A success alert (green). |
| `Warning` | `1` | A warning alert (yellow). |
| `Error` | `2` | An error/danger alert (red). |
| `Info` | `3` | An informational alert (blue). |

---

## HtmlHelper

**Namespace:** `JC.Web.UI.HTML`

Static helper for building HTML elements, with specific methods for pagination components.

### Methods

#### CreateElement(string tagName, string content = "", bool isActive = false, bool isDisabled = false, Dictionary\<string, string\>? attributes = null, params string[] classes)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `tagName` | `string` | — | The HTML tag name. |
| `content` | `string` | `""` | The inner HTML content. |
| `isActive` | `bool` | `false` | Whether to add the `active` CSS class. |
| `isDisabled` | `bool` | `false` | Whether to add the `disabled` CSS class. |
| `attributes` | `Dictionary<string, string>?` | `null` | Optional HTML attributes. |
| `classes` | `params string[]` | — | Additional CSS classes. |

Creates a generic HTML element with optional state attributes, custom attributes, and CSS classes. Content is inserted as raw HTML.

---

#### PaginationItem(string content, bool isActive = false, bool isDisabled = false)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string` | — | Inner HTML content (usually a link). |
| `isActive` | `bool` | `false` | Whether this is the active page. |
| `isDisabled` | `bool` | `false` | Whether this item is disabled. |

Builds a pagination list item (`<li class="page-item">`) with optional active/disabled states.

---

#### PaginationLink(string text, string href, string? buttonClass = null, bool isActive = false)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `text` | `string` | — | Link text. |
| `href` | `string` | — | URL to navigate to. |
| `buttonClass` | `string?` | `null` | Additional CSS classes for the link. |
| `isActive` | `bool` | `false` | Whether this is the active page (adds `aria-current="page"`). |

Builds a pagination link (`<a class="page-link">`).

---

## HtmlTagBuilder

**Namespace:** `JC.Web.UI.HTML`

Fluent builder for constructing HTML tags with attributes, classes, and content. The constructor is internal — instances are created through `HtmlHelper` or other JC.Web builders. Supports implicit conversion to `string`.

### Methods

#### AddClass(string className)

**Returns:** `HtmlTagBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `className` | `string` | — | The CSS class name. Empty or whitespace names are ignored. |

Adds a CSS class to the tag.

---

#### AddAttribute(string name, string value)

**Returns:** `HtmlTagBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The attribute name. |
| `value` | `string` | — | The attribute value (HTML-encoded in output). |

Adds or updates an HTML attribute.

---

#### AddActiveAttribute()

**Returns:** `HtmlTagBuilder`

Adds the `active` CSS class.

---

#### AddCurrentPageAttribute()

**Returns:** `HtmlTagBuilder`

Adds the `aria-current="page"` attribute.

---

#### AddDisabledClass()

**Returns:** `HtmlTagBuilder`

Adds the `disabled` CSS class.

---

#### SetContent(string content)

**Returns:** `HtmlTagBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string` | — | The text content (HTML-encoded). |

Sets the inner text content. Overwrites any previously set content.

---

#### SetRawContent(string rawHtml)

**Returns:** `HtmlTagBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `rawHtml` | `string` | — | The raw HTML content (not encoded). |

Sets the inner HTML content without encoding. Do not pass unsanitised user input.

---

#### Build()

**Returns:** `string`

Builds and returns the complete HTML tag as a string.

---

## DropdownHelper

**Namespace:** `JC.Web.UI.Helpers`

Static helper methods for building `SelectListItem` collections from various data sources.

### Methods

#### ToDropdownEntry(string text, string value, bool selected = false)

**Returns:** `SelectListItem`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `text` | `string` | — | The display text. |
| `value` | `string` | — | The option value. |
| `selected` | `bool` | `false` | Whether this item is selected. |

Creates a single `SelectListItem`.

---

#### FromEnum\<T\>(T? selected = null)

**Returns:** `List<SelectListItem>`

**Constraint:** `T : struct, Enum`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `selected` | `T?` | `null` | The currently selected value, if any. |

Converts all values of an enum to dropdown items. Display text is generated using `EnumExtensions.ToDisplayName`.

---

#### FromCollection\<T\>(IEnumerable\<T\> items, Func\<T, string\> textSelector, Func\<T, string\> valueSelector, Func\<T, bool\>? selectedPredicate = null)

**Returns:** `List<SelectListItem>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `items` | `IEnumerable<T>` | — | The source collection. |
| `textSelector` | `Func<T, string>` | — | Function to extract display text. |
| `valueSelector` | `Func<T, string>` | — | Function to extract the option value. |
| `selectedPredicate` | `Func<T, bool>?` | `null` | Optional predicate to determine selected items. |

Converts a collection to dropdown items using custom selectors.

---

#### FromDictionary(Dictionary\<string, string\> items, string? selected = null)

**Returns:** `List<SelectListItem>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `items` | `Dictionary<string, string>` | — | The source dictionary (key = value, value = display text). |
| `selected` | `string?` | `null` | The key of the currently selected item. |

Converts a dictionary to dropdown items.

---

#### GetCountryDropdown(string? selected = null)

**Returns:** `List<SelectListItem>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `selected` | `string?` | `null` | The ISO alpha-2 code of the selected country. |

Builds a country dropdown using `CountryHelper`. Comparison is case-insensitive.

---

#### WithPlaceholder(this List\<SelectListItem\> items, string text = "Please select...", string value = "")

**Returns:** `List<SelectListItem>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `items` | `List<SelectListItem>` | — | The existing dropdown items. |
| `text` | `string` | `"Please select..."` | The placeholder display text. |
| `value` | `string` | `""` | The placeholder value. |

Extension method that inserts a placeholder item at index 0.

---

## QrCodeHelper

**Namespace:** `JC.Web.UI.Helpers`

Helper for generating QR codes in SVG or Base64 PNG format using QRCoder.

### Constants

| Constant | Type | Value | Description |
|----------|------|-------|-------------|
| `Base64ImgPrefix` | `string` | `"data:image/png;base64,"` | The data URI prefix for base64-encoded PNG images. |

### Constructors

#### QrCodeHelper()

Creates a QR code helper with default settings: SVG format, 10 pixels per module, ECC level M (15% error correction).

---

#### QrCodeHelper(QrCodeFormat format, int pixelsPerModule, QRCodeGenerator.ECCLevel eccLevel = QRCodeGenerator.ECCLevel.M)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `format` | `QrCodeFormat` | — | Output format (SVG or Base64 PNG). |
| `pixelsPerModule` | `int` | — | Size of each QR module in pixels. Clamped to 10 if zero or negative. |
| `eccLevel` | `QRCodeGenerator.ECCLevel` | `M` | Error correction level: L (7%), M (15%), Q (25%), H (30%). |

### Methods

#### GenerateQrCode(string content)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `content` | `string` | — | The data to encode in the QR code. |

Generates a QR code from the provided content. Returns an SVG markup string or a base64-encoded PNG data URI depending on the configured format. Throws `ArgumentException` if content is empty.

---

## QrCodeFormat

**Namespace:** `JC.Web.UI.Helpers`

Specifies the output format for generated QR codes.

| Member | Value | Description |
|--------|-------|-------------|
| `Svg` | `0` | SVG markup string. |
| `Base64` | `1` | Base64-encoded PNG data URI. |

---

## ModelStateWrapper

**Namespace:** `JC.Web.UI.Helpers`

Wraps `ModelStateDictionary` with automatic key prefixing for cleaner error access in Razor Pages and MVC scenarios where model properties are nested under a prefix (e.g. `"Input."`).

### Constructor

#### ModelStateWrapper(ModelStateDictionary modelState, string? prefix = null, bool ignorePrefix = false)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `modelState` | `ModelStateDictionary` | — | The model state dictionary to wrap. |
| `prefix` | `string?` | `null` | The key prefix. Defaults to `"Input."`. A trailing `.` is appended automatically if missing. |
| `ignorePrefix` | `bool` | `false` | Set to `true` to disable prefixing entirely. |

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `IsValid` | `bool` | get; | Whether the underlying model state is valid. |

### Indexer

#### this[string key]

**Returns:** `string`

Gets the first error message for the specified key (with prefix applied), or an empty string if no errors.

### Methods

#### AddModelError(string key, string errorMessage)

**Returns:** `void`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `key` | `string` | — | The property name (without prefix). |
| `errorMessage` | `string` | — | The error message. |

Adds a model error with the prefix applied to the key.

---

#### HasError(string key)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `key` | `string` | — | The property name (without prefix). |

Checks whether the key (with prefix) has any validation errors.

---

#### GetErrors(string key)

**Returns:** `IEnumerable<string>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `key` | `string` | — | The property name (without prefix). |

Gets all error messages for the key. Returns an empty collection if none.

---

#### GetAllErrors()

**Returns:** `Dictionary<string, string[]>`

Gets all validation errors across all keys as a dictionary mapping full keys to their error message arrays.
