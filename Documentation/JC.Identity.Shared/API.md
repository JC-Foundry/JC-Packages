# JC.Identity.Shared — API reference

Every public type and member in JC.Identity.Shared. See [Setup](Setup.md) for registration and [Guide](Guide.md) for usage.

> **Note:** Registration extensions (`IServiceCollection`, `IServiceProvider`, `IApplicationBuilder`) and options classes are documented in [Setup](Setup.md), not here. That covers `AddSharedIdentityServices`, `IdentityMiddlewareOptions` and `IdentityProjectionOptions`.
>
> `UserInfoExtensions` is documented in full below despite three of its methods extending `IServiceProvider`. Those establish an ambient identity at run time rather than registering anything, so excluding them would leave the package's non-web surface undocumented.

## Models

### UserInfoBase

**Namespace:** `JC.Identity.Shared.Models`

The `IUserInfo` implementation carrying the property surface every identity authority populates, and nothing specific to any one of them. `IUserInfo` itself is declared in `JC.Core.Models`.

Each authority derives its own type and registers it as the scoped `IUserInfo`. Nothing downstream names the concrete type — consumers inject `IUserInfo`.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Authority` | `IdentityAuthority` | `None` | get; set; | Which authority supplied this identity. Stamped by the claims projection on the authenticated branch only. |
| `UserId` | `string` | `IUserInfo.SYSTEM_USER_ID` | get; set; | The current user's identifier. Non-nullable, so an unpopulated instance reads as the system user rather than null. |
| `Username` | `string` | `IUserInfo.SYSTEM_USER_NAME` | get; set; | The current user's username. Non-nullable. |
| `Email` | `string` | `IUserInfo.SYSTEM_USER_EMAIL` | get; set; | The current user's email address. Non-nullable. |
| `EmailConfirmed` | `bool` | `false` | get; set; | Whether the email address has been confirmed. |
| `PhoneNumber` | `string?` | `null` | get; set; | The current user's phone number, if any. |
| `PhoneNumberConfirmed` | `bool` | `false` | get; set; | Whether the phone number has been confirmed. |
| `TwoFactorEnabled` | `bool` | `false` | get; set; | Whether two-factor authentication is enabled on the account. |
| `LockoutEnabled` | `bool` | `false` | get; set; | Whether lockout applies to the account. |
| `LockoutEnd` | `DateTime?` | `null` | get; set; | When the current lockout ends, if the account is locked out. |
| `AccessFailedCount` | `int` | `0` | get; set; | Consecutive failed access attempts. |
| `TenantId` | `string?` | `null` | get; set; | The tenant assigned to this user within the consuming application. Not the tenant the current operation is scoped to. |
| `DisplayName` | `string?` | `null` | get; set; | The user's display name, if the authority supplies one. |
| `LastLoginUtc` | `DateTime?` | `null` | get; set; | When the user last signed in. |
| `RegistrationUtc` | `DateTime?` | `null` | get; set; | When the user registered. |
| `IsEnabled` | `bool` | `false` | get; set; | Whether the account is enabled. `false` on an unpopulated instance. |
| `RequiresPasswordChange` | `bool` | `false` | get; set; | Whether the user must change their password before continuing. |
| `IsSetup` | `bool` | `false` | get; set; | Whether an identity has been projected onto this instance. The claims middleware skips any instance where this is already `true`. |
| `HasTenant` | `bool` | Derived | get; | `true` when `TenantId` is neither null nor empty. Has no setter, so it cannot disagree with the value it describes. |
| `Roles` | `IReadOnlyList<string>` | `[]` | get; set; | Role names in the consuming application's authorisation domain. |
| `Claims` | `IReadOnlyList<Claim>` | `[]` | get; set; | Every claim on the principal the instance was projected from. |

#### Constructors

##### UserInfoBase()

Initialises an unpopulated instance holding the system-user defaults. This is the constructor dependency injection activates; the claims projection fills the instance in per scope.

##### UserInfoBase(IApplicationUser user, IEnumerable&lt;string?&gt; roles)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `user` | `IApplicationUser` | — | The authoritative user record to project. |
| `roles` | `IEnumerable<string?>` | — | The user's role names. Null and empty entries are discarded. |

Projects a user record onto the new instance by delegating to `UserInfoExtensions.PopulateFrom`, so constructing an instance and seeding an existing one share a single projection. Sets `IsSetup` to `true`.

Sets neither `TenantId` nor `Authority`. `IApplicationUser.IdentityTenantId` means the tenant owning the identity record, which is not inherently the user's tenant inside the consuming application; the authority is known only to the package doing the registering. A derived type that knows either answer assigns it after calling this constructor.

#### Methods

##### IsInRole(string role)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `role` | `string` | — | The role name to test. |

Returns `true` when `role` appears in `Roles`, or when `Claims` contains a claim of type `ClaimTypes.Role` whose value matches. Both comparisons are ordinal and case-sensitive.

Returns `false` immediately when `role` is null or empty, before either collection is examined.

The claim fallback is fixed to `ClaimTypes.Role` regardless of the configured `RoleClaimType`. This does not narrow the check in practice, because the projection populates `Roles` from whichever claim type is configured.

## Helpers

### DefaultClaims

**Namespace:** `JC.Identity.Shared.Authentication`

The custom claim type constants the JC identity claims pipeline uses. An authority writes these onto the `ClaimsIdentity`; the claims projection reads them back onto `IUserInfo`.

These are the claims whose names are fixed. The identifier, email and role claim types are configurable through `IdentityProjectionOptions` instead.

#### Fields

| Field | Type | Value | Access | Populates |
|-------|------|-------|--------|-----------|
| `EmailConfirmed` | `const string` | `email_confirmed` | public | `IUserInfo.EmailConfirmed` |
| `PhoneNumber` | `const string` | `phone_number` | public | `IUserInfo.PhoneNumber` |
| `PhoneNumberConfirmed` | `const string` | `phone_number_confirmed` | public | `IUserInfo.PhoneNumberConfirmed` |
| `TwoFactorEnabled` | `const string` | `two_factor_enabled` | public | `IUserInfo.TwoFactorEnabled` |
| `LockoutEnabled` | `const string` | `lockout_enabled` | public | `IUserInfo.LockoutEnabled` |
| `LockoutEnd` | `const string` | `lockout_end` | public | `IUserInfo.LockoutEnd` |
| `AccessFailedCount` | `const string` | `access_failed_count` | public | `IUserInfo.AccessFailedCount` |
| `TenantId` | `const string` | `tenant_id` | public | `IUserInfo.TenantId` |
| `DisplayName` | `const string` | `display_name` | public | `IUserInfo.DisplayName` |
| `LastLoginUtc` | `const string` | `last_login_utc` | public | `IUserInfo.LastLoginUtc` |
| `RegistrationUtc` | `const string` | `registration_utc` | public | `IUserInfo.RegistrationUtc` |
| `IsEnabled` | `const string` | `is_enabled` | public | `IUserInfo.IsEnabled` |
| `RequirePasswordChange` | `const string` | `require_password_change` | public | `IUserInfo.RequiresPasswordChange` |

### IdentityRules

**Namespace:** `JC.Identity.Shared.Helpers`

Static class evaluating the identity business rules — disabled accounts, required password changes and optional two-factor — against the path being requested. Holds the whole of the account-rule logic; `IdentityMiddleware` in JC.Identity.Shared.Web supplies the path and performs the redirect.

#### Methods

##### GetRedirect(IUserInfo userInfo, string path, bool isAuthenticated, IdentityMiddlewareOptions options, ILogger? logger = null)

**Returns:** `string?`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userInfo` | `IUserInfo` | — | The current user. |
| `path` | `string` | — | The path being requested. |
| `isAuthenticated` | `bool` | — | Whether the caller is authenticated. |
| `options` | `IdentityMiddlewareOptions` | — | The configured routes and enforcement switches. |
| `logger` | `ILogger?` | `null` | Records why a caller was redirected. Nothing is logged when a request passes. |

Returns the route the caller should be sent to, or `null` where the request may proceed.

Returns `null` immediately when the caller is unauthenticated, when the path matches one of `options.ExcludedPaths` by prefix, or when the path ends in one of the static-file extensions: `.css`, `.js`, `.jpg`, `.jpeg`, `.png`, `.gif`, `.svg`, `.ico`, `.woff`, `.woff2`, `.ttf`, `.eot`, `.map`, `.json`, `.xml`. All three comparisons are case-insensitive.

Otherwise three rules are evaluated in order, and the first that matches returns its route:

1. `IUserInfo.IsEnabled` is `false`, returning `options.AccessDeniedRoute` and logging a warning. Evaluated first deliberately — a disabled account should not be routed to a password-change or two-factor page it has no business completing.
2. `options.RequirePasswordChange` is `true` and `IUserInfo.RequiresPasswordChange` is `true`, returning `options.ChangePasswordRoute` and logging at information level.
3. `options.EnforceTwoFactor` is `true` and `IUserInfo.TwoFactorEnabled` is `false`, returning `options.TwoFactorRoute` and logging at information level.

Rules 2 and 3 are skipped when `path` already starts with the route they would return, so the target page remains reachable. Rule 1 needs no such guard because `AccessDeniedRoute` is one of `options.ExcludedPaths` and is filtered out before the rules run.

### IdentityHelper

**Namespace:** `JC.Identity.Shared.Helpers`

Formats the two halves of an authenticator-app two-factor setup screen: the `otpauth://` URI a QR code encodes, and the human-readable grouping of the shared key shown beside it.

Not registered in the container. It holds only a `UrlEncoder` and a format string, and is constructed where needed.

It formats only. Generating and validating the shared secret belong to the identity authority.

#### Constructors

##### IdentityHelper(UrlEncoder urlEncoder)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `urlEncoder` | `UrlEncoder` | — | The encoder used to escape the email in the generated URI. |

Uses the standard authenticator URI format, `otpauth://totp/{0}:{1}?secret={2}&issuer={0}`.

##### IdentityHelper(UrlEncoder urlEncoder, string authenticatorUriFormat)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `urlEncoder` | `UrlEncoder` | — | The encoder used to escape the email in the generated URI. |
| `authenticatorUriFormat` | `string` | — | A composite format string taking the issuer name, the encoded email and the shared secret, in that order. |

#### Methods

##### Generate2faQrCodeUri(string name, string email, string unformattedKey)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The issuer name, usually the application name. |
| `email` | `string` | — | The user's email address, used as the account label. |
| `unformattedKey` | `string` | — | The shared secret, unformatted. |

Formats the configured URI template with the invariant culture. Only `email` is passed through the URL encoder; the issuer name is interpolated as supplied, so a name containing a reserved character produces a malformed URI.

##### Format2faKey(string unformattedKey)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `unformattedKey` | `string` | — | The shared secret, unformatted. |

Splits the key into space-separated groups of four and lowercases the result. The final group is whatever remains, which may be four characters or fewer; no trailing separator is emitted.

##### Generate2faKey(string name, string email, string secret)

**Returns:** `(string AuthenticatorUri, string FormattedKey)`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The issuer name, usually the application name. |
| `email` | `string` | — | The user's email address, used as the account label. |
| `secret` | `string` | — | The shared secret, unformatted. |

Calls `Generate2faQrCodeUri` and `Format2faKey` and returns both results as a named tuple.

## Extensions

### UserInfoExtensions

**Namespace:** `JC.Identity.Shared.Extensions`

Projects an authoritative user record or a claims principal onto an `IUserInfo`, and establishes that identity as the ambient one for work happening outside an HTTP request.

`IUserInfo` is registered scoped and populated in place, so constructing one and handing it around does not make it ambient — nothing that injects `IUserInfo` would observe it. Every method here that establishes an identity resolves the scope's own instance and fills that in.

#### Methods

##### PopulateFrom&lt;T&gt;(this T userInfo, IApplicationUser user, IEnumerable&lt;string?&gt; roles)

**Returns:** `T`

**Constraint:** `where T : class, IUserInfo`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userInfo` | `T` | — | The instance to populate. |
| `user` | `IApplicationUser` | — | The authoritative user record to project. |
| `roles` | `IEnumerable<string?>` | — | The user's role names. Null and empty entries are discarded. |

Copies the identifier, username, email and its confirmation, phone number and its confirmation, two-factor state, lockout state and end, access failure count, display name, last login, registration timestamp, enabled state and password-change requirement from the record, and assigns the filtered `roles` to `Roles`.

`IApplicationUser.RequirePasswordChange` is projected onto `IUserInfo.RequiresPasswordChange`, and `LockoutEnd` is narrowed from `DateTimeOffset?` to `DateTime?`. A null `UserName` or `Email` on the record falls back to the **unknown**-user constant rather than the system-user default the property started at.

Sets `IsSetup` to `true`. Returns the same instance, for chaining.

Sets neither `TenantId` nor `Authority`, for the reasons given under `UserInfoBase`.

##### PopulateFrom&lt;T&gt;(this T userInfo, ClaimsPrincipal? principal, IdentityProjectionOptions options, ILogger? logger = null)

**Returns:** `T`

**Constraint:** `where T : class, IUserInfo`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userInfo` | `T` | — | The instance to populate. |
| `principal` | `ClaimsPrincipal?` | — | The principal to project. Null is accepted. |
| `options` | `IdentityProjectionOptions` | — | The claim types to read and the authority to stamp. |
| `logger` | `ILogger?` | `null` | Records the projection outcome at debug level. |

Takes one of three branches, and sets `IsSetup` to `true` in all of them before returning the same instance:

- `principal` is null, or its `Identity` is null: `UserId`, `Username` and `Email` are set to the system-user constants and nothing else is touched.
- The identity is present but not authenticated: the same three are set to the unknown-user constants.
- The identity is authenticated: `Authority` is stamped from `options`; `Username` comes from `Identity.Name`; `Email` and `UserId` come from the claim types named in `options`, each falling back to its unknown-user constant; the remaining fields come from the fixed `DefaultClaims` names; `Claims` is set to the principal's full claim collection; and `Roles` is set to the values of every claim whose type matches `options.RoleClaimType`.

Boolean claims are matched against the literal `"true"`, case-insensitively; any other value, including absence, yields `false`. Date claims are parsed with `DateTime.TryParse` and fall back to `null`. `AccessFailedCount` is parsed with `int.TryParse` and falls back to `0`.

`TenantId` is assigned only when the tenant claim carries a non-empty value, so an empty claim leaves any existing value in place rather than clearing it.

##### SetUserInfoForUser(this IServiceProvider scopedServices, IApplicationUser user, IEnumerable&lt;string?&gt; roles, string? tenantId = null)

**Returns:** `IUserInfo`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `scopedServices` | `IServiceProvider` | — | The scope's service provider. |
| `user` | `IApplicationUser` | — | The authoritative user record to project. |
| `roles` | `IEnumerable<string?>` | — | The user's role names. Null and empty entries are discarded. |
| `tenantId` | `string?` | `null` | The user's tenant within this application, or null for the null tenant partition. |

Resolves the scope's `IUserInfo`, projects `user` and `roles` onto it, assigns `tenantId`, and stamps `Authority` from the registered `IdentityProjectionOptions` so the authority is stated in exactly one place. Returns the populated instance.

`tenantId` is a separate parameter rather than being taken from `user.IdentityTenantId`, because the tenant owning an identity record and the user's application tenant are different concepts.

Resolves `IUserInfo` and `IOptions<IdentityProjectionOptions>` with `GetRequiredService`, so a container without the shared services registered throws.

Calling this inside a live request scope replaces the authenticated user for the remainder of that request, which is impersonation.

##### CreateScopeForUser(this IServiceProvider services, IApplicationUser user, IEnumerable&lt;string?&gt; roles, string? tenantId = null)

**Returns:** `IServiceScope`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `services` | `IServiceProvider` | — | The root or parent service provider. |
| `user` | `IApplicationUser` | — | The authoritative user record to project. |
| `roles` | `IEnumerable<string?>` | — | The user's role names. Null and empty entries are discarded. |
| `tenantId` | `string?` | `null` | The user's tenant within this application, or null for the null tenant partition. |

Creates a service scope and calls `SetUserInfoForUser` on it. Returns the scope, which the caller disposes.

##### CreateAsyncScopeForUser(this IServiceProvider services, IApplicationUser user, IEnumerable&lt;string?&gt; roles, string? tenantId = null)

**Returns:** `AsyncServiceScope`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `services` | `IServiceProvider` | — | The root or parent service provider. |
| `user` | `IApplicationUser` | — | The authoritative user record to project. |
| `roles` | `IEnumerable<string?>` | — | The user's role names. Null and empty entries are discarded. |
| `tenantId` | `string?` | `null` | The user's tenant within this application, or null for the null tenant partition. |

As `CreateScopeForUser`, but creates an asynchronously disposable scope for work whose scoped services implement `IAsyncDisposable`.

## Next steps

- [Setup](Setup.md) — registration, options and their defaults.
- [Guide](Guide.md) — usage, scenarios and nuances.
