# JC.CAP — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project
- An application registered at CAP by a CAP operator. That gives you a client id and a client secret, shown once, and is where the two callback URIs in [Quick setup](#1-quick-setup) are entered
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

JC.CAP is one of two identity authorities built on the same runtime. Anything authority-agnostic, the `IUserInfo` implementation, the claims projection, the account rules and their options, lives in **JC.Identity.Shared** and is documented in [JC.Identity.Shared — Setup](../JC.Identity.Shared/Setup.md). This document covers what is specific to signing in through CAP.

The wire contract, endpoint paths, scope and claim names, the API's DTOs and the redirect URI rules, is the **CAP.SSO** package, which arrives with this one. Its README is the reference underneath everything here.

## 0. Add the package

Add a project reference to `JC.CAP`:

```xml
<ProjectReference Include="path/to/JC.CAP/JC.CAP.csproj" />
```

`JC.CAP` references `JC.Identity.Shared`, `JC.Identity.Shared.Web`, `CAP.SSO`, `Flurl.Http` and the three OpenIddict client packages, so all of them arrive with it and none needs adding separately.

Tenancy is **not** included. `JC.CAP` has no reference to `JC.Tenancy`; a tenant reaches `IUserInfo.TenantId` through the enricher hook in [Enriching the principal](#enriching-the-principal).

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### Roles

Declare the application's roles on a class extending `JC.CAP.Authentication.SystemRoles`, a `const string` per role and a matching `{Name}Desc` for the description:

```csharp
public class AppRoles : SystemRoles
{
    public const string Editor = nameof(Editor);
    public const string EditorDesc = "Can create and edit content.";

    public const string Viewer = nameof(Viewer);
    public const string ViewerDesc = "Read-only access to content.";
}
```

Unlike JC.Identity's `SystemRoles`, this one declares nothing: there is no `SystemAdmin` or `Admin`. A CAP application defines its whole catalogue and publishes it; CAP operators assign from that list and cannot invent a role the code does not know.

### Services in `Program.cs`

```csharp
// Binds the SSO section, registers the session cookie as the default scheme, the OpenIddict client
// for CAP, the API client, and the shared identity runtime
builder.Services.AddCap(builder.Configuration);

// Strongly recommended, not required: JC.Core's data services. See below.
builder.Services.AddCore<AppDbContext>();
```

**`AddCore` is not a requirement of JC.CAP.** Signing in, the session, the rules, the roles and CAP's API all work without it, and `IUserInfo` is populated either way. What needs it is everything in JC.Core: a `DbContext` extending `DataDbContext` with its audit trail, `IRepositoryManager`, and the background-job options. An application with a database should register it, since attributing that work to the signed-in CAP user is the reason to sign one in; an application with no database can leave it out. What `AddCore<TContext>` registers and what the audit trail does with `IUserInfo` are in [JC.Core — Setup](../JC.Core/Setup.md#addcore--service-registration).

### Middleware and endpoints in `Program.cs`

```csharp
var app = builder.Build();

// Authentication, user info projection, authorisation, identity rules, in that order
app.UseCap();

// The sign-in trigger, both callbacks CAP returns to, sign-out, and the three re-check endpoints
app.MapCap();

// Publishes the roles on AppRoles to CAP once, and fails startup if CAP refuses
await app.SyncCapRolesAsync<AppRoles>();

app.Run();
```

### Configuration in `appsettings.json`

```json
{
  "SSO": {
    "BaseUrl": "https://sso.example.com",
    "ClientId": "evbfqxmh",
    "ClientSecret": "the-secret-CAP-showed-once"
  }
}
```

`SSO` is `CapDictionary.ConfigSection` from CAP.SSO and `BaseUrl` is `CapDictionary.BaseUrlKey`, the same key CAP itself is configured with, so the SSO host is named once on both sides. **Startup fails** with a message naming the missing key if the host, the client id or the secret is absent, or if the host is not an absolute http or https URL.

### Registering the callbacks at CAP

CAP sends the browser back to the application at two URIs, and it accepts only URIs an operator has registered against the application. Give the operator:

| Purpose | URI |
|---------|-----|
| Sign-in callback | `https://your-app/signin-oidc` |
| Post-logout callback | `https://your-app/signout-callback-oidc` |

The paths are `CapOptions.CallbackPath` and `CapOptions.PostLogoutCallbackPath`, and `/signin-oidc` is the placeholder CAP's settings page shows. CAP accepts `http` only for a loopback address, so a development application on `http://localhost:5000` registers `http://localhost:5000/signin-oidc`, and sets `AllowInsecureHttp` as described in [Development over http](#development-over-http).

### Defaults

Called with only configuration, `AddCap` gives you:

| Default | Value |
|---------|-------|
| Configuration section | `SSO` |
| Scopes requested at sign-in | `openid`, `roles`, `cap_identity`, `offline_access` |
| Sign-in trigger | `/cap/signin`, also the cookie's login path |
| Sign-out | `/cap/signout`, POST only, also the cookie's logout path |
| Callbacks | `/signin-oidc` and `/signout-callback-oidc` |
| Re-check endpoints | `/cap/refresh`, `/cap/denied`, `/cap/two-factor` |
| Cookie | `.JC.CAP.Session` on scheme `JC.CAP`, HttpOnly, SameSite Lax, secure only, sliding 14 days, not persistent |
| Silent refresh | One minute ahead of access-token expiry; five minutes' grace when CAP is unreachable |
| Role refusal | A plain 403, `AccessDenied` being `Forbid`. CAP's denied page or a local page by configuration |
| Account rules | The shared defaults, with CAP's routes substituted: two-factor enforcement off, password-change enforcement off, disabled accounts to `/cap/denied` |
| `IUserInfo` implementation | `CapUserInfo` (built-in) |
| `IUserInfo.Authority` | `IdentityAuthority.CAP` once authenticated, `None` otherwise |
| Claim types read | ASP.NET Identity's, because the cookie is written in that vocabulary |
| OpenIddict token storage | Disabled. No OpenIddict tables are needed |
| OpenIddict signing and encryption keys | Ephemeral, with state tokens protected by ASP.NET Core Data Protection |
| Member cache | Enabled, five minutes, one entry per member, refreshed together |

`AddCap` registers:

| Registration | Lifetime | Description |
|--------------|----------|-------------|
| `CapOptions` | Singleton | Bound from `SSO` and validated on start |
| `IConfigureOptions<IdentityMiddlewareOptions>` | Singleton | JC.CAP's rule-set defaults, registered before `configureMiddleware` so the application's settings win |
| Everything `AddSharedIdentityServices<TUserInfo>` registers | | The scoped `IUserInfo` and both options types, with `Authority` set to `CAP`. See [JC.Identity.Shared](../JC.Identity.Shared/Setup.md#addsharedidentityservices) |
| `ICapClaimsPrincipalFactory` → `CapClaimsPrincipalFactory` | Scoped | Translates CAP's claims onto the cookie. `TryAdd`, so one registered first is kept |
| `CapSessionRefresher` | Scoped | The token refresh, shared by the cookie events and the re-check endpoints |
| `CapCookieEvents` | Scoped | Runs the refresh inside cookie authentication |
| `CapLinks` | Singleton | Absolute, branded URLs into CAP's account pages |
| `CapAccessTokenProvider` | Singleton | The client-credentials token for CAP's API |
| `CapApiClient` | Singleton | CAP's API |
| `CapRoleSyncJob<>` | Scoped, open generic | The role publish, closed over the application's roles class |
| `CapUserCache` | Singleton | The application's members, one cache entry per member. `AddMemoryCache()` is called for it |
| `TimeProvider` | Singleton | `TimeProvider.System`, `TryAdd`, so the refresh timing is testable |
| Authentication | | Default scheme `JC.CAP` with a cookie handler under that name, and `AddAuthorization()` |
| OpenIddict client | | Code, refresh and client-credentials flows; one registration for CAP; ASP.NET Core integration with both callback passthroughs; System.Net.Http; Data Protection |

`UseCap` registers middleware in this order:

1. `UseAuthentication()`, which is also where the silent refresh runs
2. `UseUserInfo()`, projecting the cookie's claims onto `IUserInfo`
3. `UseAuthorization()`
4. `UseIdentityMiddleware()`, enforcing disabled accounts and, where enabled, two-factor

The order matters in both directions: `UseUserInfo` must follow authentication because it reads claims, and must precede `UseIdentityMiddleware` because that enforces rules against what it produced. Both middlewares come from JC.Identity.Shared.Web and are described in [JC.Identity.Shared](../JC.Identity.Shared/Setup.md#adding-the-aspnet-core-middleware).

`MapCap` maps these endpoints, every one anonymous:

| Path | Verbs | Does |
|------|-------|------|
| `SignInPath` | GET | Challenges CAP, returning to `returnUrl` afterwards. Redirects straight there if a session already exists |
| `CallbackPath` | GET, POST | Receives the code, builds the session principal, writes the cookie, redirects to the return URL |
| `SignOutPath` | POST | Clears the cookie, ends the session at CAP, returns to `returnUrl` |
| `PostLogoutCallbackPath` | GET, POST | Lands on the return URL stored at sign-out |
| `RefreshPath` | GET | Refreshes the tokens now and returns to `returnUrl` |
| `DeniedPath` | GET | Where the rules send a disabled account: re-checks with CAP, then hands over to CAP's denied page |
| `TwoFactorPath` | GET | Where the rules send an account owing two-factor: re-checks with CAP, then hands over to enrolment |

The two callbacks carry `DisableAntiforgery`, since CAP may answer with a form post. Sign-out validates antiforgery itself where the application has `IAntiforgery` registered, which Razor Pages and MVC both do, and answers 400 on a bad token.

## 2. Full configuration

### AddCap from configuration

```csharp
builder.Services.AddCap(
    builder.Configuration,
    configure: options =>
    {
        options.AllowInsecureHttp = false;
        options.Session.Persistent = false;
    },
    configureMiddleware: options =>
    {
        options.Default.EnforceTwoFactor = false;
    },
    configureCookie: cookie =>
    {
        cookie.Cookie.Name = ".JC.CAP.Session";
    },
    configureClient: client =>
    {
        // The raw OpenIddict client builder, after JC.CAP's own configuration
    }
);
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configuration` | `IConfiguration` | required | Binds `CapOptions` from its `SSO` section |
| `configure` | `Action<CapOptions>?` | `null` | Code configuration applied after binding, so it wins |
| `configureMiddleware` | `Action<IdentityMiddlewareOptions>?` | `null` | Passed to the shared runtime after JC.CAP's rule-set defaults. Every property is in [JC.Identity.Shared](../JC.Identity.Shared/Setup.md#identitymiddlewareoptions); what JC.CAP sets is in [The account rules](#the-account-rules) |
| `configureCookie` | `Action<CookieAuthenticationOptions>?` | `null` | Applied after JC.CAP's cookie defaults, so anything you set wins. See [The session cookie](#the-session-cookie) |
| `configureClient` | `Action<OpenIddictClientBuilder>?` | `null` | Handed the OpenIddict client builder last. See [The OpenIddict client](#the-openiddict-client) |

**Binding adds scopes rather than replacing them.** `CapOptions.Scopes` is a getter-only set, so an `SSO:Scopes` array in configuration reads as "these as well", never "these instead". To remove a default scope, do it in `configure`.

### AddCap from code

```csharp
builder.Services.AddCap(
    options =>
    {
        options.BaseUrl = "https://sso.example.com";
        options.ClientId = "evbfqxmh";
        options.ClientSecret = builder.Configuration["SSO:ClientSecret"]!;
    },
    configureMiddleware: null,
    configureCookie: null,
    configureClient: null
);
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configure` | `Action<CapOptions>` | required | Configures `CapOptions`. The host, client id and secret must be set |

The remaining three parameters are identical to the configuration overload. Validation runs on start for both.

### AddCap with a custom IUserInfo

Both overloads have a generic form that registers a derived `IUserInfo` in place of `CapUserInfo`:

```csharp
public class AppUserInfo : CapUserInfo
{
    public string? DepartmentId { get; set; }
}

builder.Services.AddCap<AppUserInfo>(builder.Configuration);
```

| Type parameter | Constraint | Description |
|----------------|-----------|-------------|
| `TUserInfo` | `class, IUserInfo` | Registered as the scoped `IUserInfo` instead of `CapUserInfo` |

Derive from `CapUserInfo` to keep its constructors, or from `UserInfoBase` for the bare surface. Populating the extra property is shown in the [Guide](Guide.md#carrying-extra-properties-on-iuserinfo).

### CapOptions

**Namespace:** `JC.CAP.Models.Options`

Bound from the `SSO` section by the configuration overload, or set in `configure`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConfigSection` | `const string` | `SSO` | `CapDictionary.ConfigSection`. The section the configuration overload binds |
| `BaseUrl` | `string` | required | CAP's SSO host as an absolute http or https URL: the OIDC issuer, and the origin the discovery document is read from. Bound from `SSO:BaseUrl`, which is `CapDictionary.BaseUrlKey` |
| `ClientId` | `string` | required | The client id CAP allocated to the application |
| `ClientSecret` | `string` | required | The secret CAP showed once. Keep it out of source |
| `Scopes` | `HashSet<string>` | `openid`, `roles`, `cap_identity`, `offline_access` | The scopes requested at sign-in. Getter-only; binding adds to it. `openid` and `cap_identity` must be present |
| `CallbackPath` | `string` | `/signin-oidc` | Where CAP returns the code. Register the absolute form at CAP |
| `PostLogoutCallbackPath` | `string` | `/signout-callback-oidc` | Where CAP returns after ending its session. Register the absolute form at CAP |
| `SignInPath` | `string` | `/cap/signin` | The sign-in trigger, and the cookie's login path |
| `SignOutPath` | `string` | `/cap/signout` | The sign-out endpoint, and the cookie's logout path and the rules' logout route |
| `RefreshPath` | `string` | `/cap/refresh` | The on-demand refresh endpoint |
| `DeniedPath` | `string` | `/cap/denied` | Where the rules send a disabled account |
| `TwoFactorPath` | `string` | `/cap/two-factor` | Where the rules send an account owing two-factor enrolment |
| `AccessDenied` | `CapAccessDenied` | `Forbid` | What a role refusal becomes: a plain 403, CAP's denied page, or a local page. See [Role refusals](#role-refusals) |
| `AccessDeniedPath` | `string?` | `null` | The local page a role refusal redirects to. Read only when `AccessDenied` is `LocalPath`, and then required |
| `AllowInsecureHttp` | `bool` | `false` | Lets the callbacks answer over plain http and the cookie travel over it. Development only |
| `Session` | `CapSessionOptions` | see below | How the cookie and its tokens behave |
| `Cache` | `CapCacheOptions` | see below | How long what is read from CAP's API is kept in memory |

Every path must be local and start with `/`. The defaults are also constants on `JC.CAP.Authentication.CapEndpoints`; read the option rather than the constant when building a link, since the option is what was mapped.

Dropping `offline_access` from `Scopes` means no refresh token, so the session ends when the access token does, fifteen minutes after sign-in.

#### CapSessionOptions

**Namespace:** `JC.CAP.Models.Options`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Lifetime` | `TimeSpan` | 14 days | The cookie's sliding lifetime. CAP's refresh token lifetime is the same, so a session whose refresh CAP refuses ends sooner regardless. Must be positive |
| `Persistent` | `bool` | `false` | Whether the cookie survives the browser closing |
| `RefreshSkew` | `TimeSpan` | 1 minute | How far ahead of access-token expiry a refresh is attempted. Cannot be negative |
| `RefreshFailureGrace` | `TimeSpan` | 5 minutes | How long past expiry a session survives when CAP cannot be reached. A refusal from CAP ends the session at once whatever this says. Cannot be negative |

#### CapCacheOptions

**Namespace:** `JC.CAP.Models.Options`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Whether members read from CAP are cached at all. Off, every read of `CapUserCache` goes to CAP |
| `UserLifetime` | `TimeSpan` | 5 minutes | How long the set of members is held before the next read refreshes it. Must be positive |

```json
{
  "SSO": {
    "BaseUrl": "https://sso.example.com",
    "ClientId": "evbfqxmh",
    "ClientSecret": "the-secret-CAP-showed-once",
    "Scopes": [ "profile" ],
    "CallbackPath": "/signin-oidc",
    "PostLogoutCallbackPath": "/signout-callback-oidc",
    "SignInPath": "/cap/signin",
    "SignOutPath": "/cap/signout",
    "RefreshPath": "/cap/refresh",
    "DeniedPath": "/cap/denied",
    "TwoFactorPath": "/cap/two-factor",
    "AccessDenied": "Forbid",
    "AllowInsecureHttp": false,
    "Session": {
      "Lifetime": "14.00:00:00",
      "Persistent": false,
      "RefreshSkew": "00:01:00",
      "RefreshFailureGrace": "00:05:00"
    },
    "Cache": {
      "Enabled": true,
      "UserLifetime": "00:05:00"
    }
  }
}
```

### Validation at startup

`AddCap` validates `CapOptions` on start, so a misconfiguration stops the application rather than failing at the first redirect to CAP. Every failure is reported together, naming the configuration key:

- `SSO:BaseUrl` must be an absolute http or https URL
- `SSO:ClientId` and `SSO:ClientSecret` are required
- `SSO:Scopes` must include `openid` and `cap_identity`. Without `cap_identity` the `is_enabled` claim never arrives, and an absent claim reads as a disabled account, so the application would lock out every user
- Every path must start with `/`, and `SSO:AccessDeniedPath` must be present and start with `/` when `SSO:AccessDenied` is `LocalPath`
- `Session:Lifetime` must be positive; `Session:RefreshSkew` and `Session:RefreshFailureGrace` cannot be negative
- `Cache:UserLifetime` must be positive

### The session cookie

JC.CAP configures the cookie handler on scheme `JC.CAP` through `IConfigureOptions`, then applies `configureCookie`, so anything set there wins.

| Property | Default applied by JC.CAP | Description |
|----------|---------------------------|-------------|
| `Cookie.Name` | `.JC.CAP.Session` | `CapDefaults.CookieName` |
| `Cookie.HttpOnly` | `true` | |
| `Cookie.SameSite` | `Lax` | Not Strict: CAP returns the browser by a top-level redirect from another origin, and Strict would withhold the cookie on that first request back |
| `Cookie.SecurePolicy` | `Always`, or `SameAsRequest` when `AllowInsecureHttp` | |
| `ExpireTimeSpan` | `Session.Lifetime` | |
| `SlidingExpiration` | `true` | |
| `EventsType` | `CapCookieEvents` | The silent refresh. See below |
| `LoginPath` | `SignInPath` | An `[Authorize]` challenge lands on the sign-in trigger |
| `LogoutPath` | `SignOutPath` | |
| `ReturnUrlParameter` | `returnUrl` | `CapEndpoints.ReturnUrlParameter` |
| `AccessDeniedPath` | `AccessDeniedPath`, only when `AccessDenied` is `LocalPath` | Otherwise not consulted. The framework fills an empty path with `/Account/AccessDenied`, which is why the other two modes are decided in the events instead. See [Role refusals](#role-refusals) |

```csharp
builder.Services.AddCap(builder.Configuration,
    configureCookie: cookie =>
    {
        cookie.Cookie.Name = ".JC.CAP.Session";
        cookie.SlidingExpiration = true;
    });
```

**Setting `Events` or `EventsType` in `configureCookie` replaces JC.CAP's events, and with them the silent refresh and the role-refusal behaviour.** Derive from `CapCookieEvents` and call the base `ValidatePrincipal` and `RedirectToAccessDenied` if you need events of your own.

**The cookie carries the tokens**, roughly three to four kilobytes, which the cookie handler chunks. An application that objects can set a server-side `SessionStore` through `configureCookie`.

### The account rules

JC.CAP sets the shared rule set's defaults before `configureMiddleware` runs, so what follows is what `options.Default` holds when your callback sees it. Every property is described in [JC.Identity.Shared](../JC.Identity.Shared/Setup.md#identityruleset).

| Property | Default applied by JC.CAP | Why |
|----------|---------------------------|-----|
| `Name` | `CAP` | Log readability |
| `RequirePasswordChange` | `false` | CAP never issues a token to an account still owing a password change, so the claim is absent and the rule could only fire off a stale cookie |
| `ChangePasswordRoute` | CAP's forced-password page, absolute and branded | Never fires; set so the route does not point at Identity UI |
| `EnforceTwoFactor` | `false` | The application's decision, as in JC.Identity |
| `TwoFactorRoute` | `TwoFactorPath` | A local endpoint that re-checks with CAP before handing over to enrolment |
| `AccessDeniedRoute` | `DeniedPath` | A local endpoint that re-checks with CAP before handing over to CAP's denied page |
| `LogoutRoute` | `SignOutPath` | Excluded from enforcement, as the rules require |
| `ErrorRoute` | `/Error` | Unchanged, the application's |
| `ReturnUrlParameter` | `returnUrl` | So the re-check endpoints can return the user where they were |
| `AdditionalExcludedPaths` | `SignInPath`, `CallbackPath`, `PostLogoutCallbackPath`, `RefreshPath` | A session mid-repair is never judged by the rules it is trying to satisfy |

```csharp
builder.Services.AddCap(builder.Configuration,
    configureMiddleware: options =>
    {
        options.Default.EnforceTwoFactor = true;
    });
```

**The two rule routes are endpoints, not pages.** CAP's own pages would loop: a user sent to CAP to enrol comes back with a cookie that still says `two_factor_enabled` is false, and the rule would send them straight back, for up to fifteen minutes. The endpoints refresh first, so the claim is current before the rule is judged. The [Guide](Guide.md#enforcing-two-factor) walks through both flows.

**A role refusal is not a rule refusal.** The rules send a *disabled* account to `DeniedPath`. A user who is enabled but lacks a role is refused by authorisation, which is a different outcome with its own setting, described next. Pointing that setting at `DeniedPath` would refresh an enabled account and send it straight back to the page that refused it.

### Role refusals

An authenticated user reaching a page whose `[Authorize(Roles = ...)]` they do not satisfy is forbidden, and the cookie handler decides what a forbid becomes. `CapOptions.AccessDenied` chooses:

| Value | What the user gets |
|-------|--------------------|
| `Forbid` | A plain 403, the default. The application styles it, for instance with `UseStatusCodePagesWithReExecute("/Error/{0}")` |
| `CapDeniedPage` | A redirect to CAP's denied page, branded for the application. The user leaves the application and there is no return URL |
| `LocalPath` | A redirect to `AccessDeniedPath`, carrying the return URL as `returnUrl`. The path must be local and is validated at startup |

```csharp
builder.Services.AddCap(builder.Configuration,
    configure: options =>
    {
        options.AccessDenied = CapAccessDenied.Forbid;
        options.AccessDeniedPath = null;
    });
```

A request carrying `X-Requested-With: XMLHttpRequest` gets a 403 in every mode, with a `Location` header in the two redirecting ones, which is the framework's own convention for a fetch.

**Why a setting rather than the cookie's path.** The framework never answers a forbid with a bare status: it always builds a redirect to the cookie's `AccessDeniedPath` and hands it to the events, and its post-configure fills an empty path with `/Account/AccessDenied`. So `Forbid` and `CapDeniedPage` are implemented in `CapCookieEvents`, and only `LocalPath` uses the cookie's path.

**`CapDeniedPage` and CAP's copy.** CAP's denied page is written for a refused sign-in, no membership or a disabled account, and a user refused a single role is neither. Prefer `Forbid` or `LocalPath` until that page has copy that covers a role refusal.

### The OpenIddict client

`AddCap` registers `OpenIddict.Client` with:

- the authorization code, refresh token and client credentials flows;
- one registration for CAP, id `cap`, provider name `CAP`, with `BaseUrl` as the issuer and the two callback paths as relative redirect URIs. Grant types, response types and the code challenge method are left for the discovery document to decide, which is where CAP advertises S256 only;
- ephemeral signing and encryption keys, which the client insists on once a redirection endpoint exists, with state tokens protected through ASP.NET Core Data Protection so they survive a restart and work across a farm on the application's existing key ring;
- token storage disabled, so no consumer needs OpenIddict's own `AddCore()` or its tables. The tokens JC.CAP needs live in the cookie and CAP holds the authoritative copies;
- WS-Federation claim mapping disabled, so `CapClaimsPrincipalFactory` is the only thing writing `ClaimTypes.*` onto the cookie;
- ASP.NET Core integration with passthrough on both callbacks, so JC.CAP's own endpoints finish them;
- System.Net.Http, with this package's assembly as the product information.

`configureClient` is handed the builder after all of that:

```csharp
builder.Services.AddCap(builder.Configuration,
    configureClient: client =>
    {
        client.AddEncryptionCertificate(certificate)
              .AddSigningCertificate(certificate);
    });
```

Use it for what a package should not decide for everyone: production certificates in place of the ephemeral keys, a resilience pipeline on the HTTP integration, or re-enabling token storage where an OpenIddict database already exists.

### Replacing the claims factory

`ICapClaimsPrincipalFactory` is registered with `TryAdd`, so a registration made **before** `AddCap` is kept, and one made after replaces it:

```csharp
builder.Services.AddCap(builder.Configuration);
builder.Services.AddScoped<ICapClaimsPrincipalFactory, AppClaimsPrincipalFactory>();
```

Derive from `CapClaimsPrincipalFactory` and override `CreateAsync` to keep the standard translation, or implement the interface outright. What the default writes is in [the API reference](API.md#capclaimsprincipalfactory).

### Enriching the principal

An `ICapClaimsEnricher` adds claims after the translation, at sign-in and on every refresh. Register as many as you need; they run in registration order:

```csharp
builder.Services.AddCap(builder.Configuration);
builder.Services.AddScoped<ICapClaimsEnricher, TenantEnricher>();
```

This is the seam a tenant reaches `IUserInfo.TenantId` through: an enricher that adds a `DefaultClaims.TenantId` claim needs no further wiring, because the shared projection already reads it. Writing one is in the [Guide](Guide.md#enriching-the-principal).

### Roles

#### SyncCapRolesAsync, once at startup

```csharp
var app = builder.Build();

var sync = await app.SyncCapRolesAsync<AppRoles>(throwOnFail: true);
```

| Type parameter | Constraint | Description |
|----------------|-----------|-------------|
| `TRoles` | `SystemRoles` | The roles class to read constants from |

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `throwOnFail` | `bool` | `true` | Whether a failed publish stops the application starting |

**Returns** `CatalogueSync`, what CAP did with the catalogue, or `null` where the publish failed and `throwOnFail` is `false`, in which case the failure has been logged.

Reflects over `TRoles` with `SystemRoles.GetAllRoles`, projects the result with `SystemRoles.ToCatalogue`, and publishes it through `CapApiClient`, waiting for CAP's answer. With the default a failure propagates out of `Program.cs`, so a wrong secret or an unreachable CAP is found at startup rather than at the first sign-in. That is also the validation: an application that starts has proved it can reach CAP as itself.

It is the counterpart of JC.Identity's `SeedRolesAsync`. It is not needed where the job below runs on a schedule, though calling both is harmless.

#### CapRoleSyncJob, on a schedule

`CapRoleSyncJob<TRoles>` is the same work as a JC.Core `IBackgroundJob`, registered by `AddCap` as an open generic so any of JC.BackgroundJobs' registrations closes it over your roles class:

```csharp
builder.Services.AddCap(builder.Configuration);
builder.Services.AddHangfireJob<CapRoleSyncJob<AppRoles>>(options => options.Cron = "0 3 * * *");
```

or, with the hosted-service runner, `AddBackgroundJob<CapRoleSyncJob<AppRoles>>()`. A failure is then handled by that package's error behaviour rather than stopping the application. See [JC.BackgroundJobs — Setup](../JC.BackgroundJobs/Setup.md).

#### What a publish does

The full set is sent every time. Anything CAP holds that the publish does not name is marked **stale**, never deleted, so a renamed role does not silently strip anyone's access; a CAP operator decides. An empty catalogue is a valid publish meaning the application defines no roles. CAP keeps the casing a key was first published with, and every role check is case-sensitive, so a key later published in a different case is reported back as `recased` and logged as a warning to correct in source.

Display names are derived from the key by `ToDisplayName` in JC.Core, so `PageEditor` is shown to operators as `Page Editor`. Descriptions come from the `{Name}Desc` constant, or are omitted where there is none.

### Development over http

CAP accepts an `http` redirect URI only for a loopback address, so run the application on `localhost` or `127.0.0.1` in development and set:

```csharp
builder.Services.AddCap(builder.Configuration,
    configure: options => options.AllowInsecureHttp = true);
```

That lets the callbacks answer over plain http, which OpenIddict otherwise refuses, and sends the cookie over http. CAP's own SSO host may be http in development too; the client does not require https of the issuer. The application's server-side calls to CAP, the token exchange, userinfo and the API, resolve the SSO host through the operating system rather than the browser, so a host that only the browser knows needs a hosts entry.

## 3. Verify

1. Run the application and open a page marked `[Authorize]`. You should be sent to CAP's login page, branded for your application, and returned to the page after signing in.
2. Inject `IUserInfo` anywhere and confirm `UserId`, `Username` and `Authority` are populated, with `Authority` reading `CAP`.
3. Check the startup log for `Published N roles from AppRoles to CAP`, and confirm the roles appear for assignment on CAP's administration pages.

## Next steps

- [Guide](Guide.md): signing in and out, the session and its refresh, the rules, roles, CAP's API, extending the principal.
- [API Reference](API.md)
- [JC.Identity.Shared — Setup](../JC.Identity.Shared/Setup.md): the shared runtime: options, projection, account rules.
