# JC.Identity — API reference

Complete reference of all public types, properties, and methods in JC.Identity. See [Setup](Setup.md) for registration and [Guide](Guide.md) for usage examples.

> **Note:** Registration extensions (`IServiceCollection`, `IServiceProvider`, `IApplicationBuilder`) and options classes are documented in [Setup](Setup.md), not here.

---

# Models

## BaseUser

**Namespace:** `JC.Identity.Models`

Base user entity extending ASP.NET Core `IdentityUser` with multi-tenancy, display name, login tracking, and account management properties. All standard `IdentityUser` properties (e.g. `Id`, `UserName`, `Email`, `PasswordHash`) are inherited and not re-documented here.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `TenantId` | `string?` | `null` | get; set; | The tenant identifier this user belongs to. |
| `DisplayName` | `string?` | `null` | get; set; | The user's display name. |
| `LastLoginUtc` | `DateTime?` | `null` | get; set; | UTC timestamp of the user's last login. |
| `IsEnabled` | `bool` | `true` | get; set; | Whether the user account is enabled. |
| `RequirePasswordChange` | `bool` | `false` | get; set; | Whether the user must change their password on next login. |

---

## BaseRole

**Namespace:** `JC.Identity.Models`

Base role entity extending ASP.NET Core `IdentityRole` with a description field. All standard `IdentityRole` properties (e.g. `Id`, `Name`, `NormalizedName`) are inherited and not re-documented here.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Description` | `string?` | `null` | get; set; | An optional description of the role's purpose. Used by `SystemRoles.GetAllRoles` for role seeding. |

---

## Tenant, TenantSettings and IMultiTenancy — moved to JC.Core

**Namespace:** `JC.Core.Models.MultiTenancy`

These three types are no longer part of JC.Identity. They live in **JC.Core**, so that any package with domain models can implement `IMultiTenancy` without taking a dependency on JC.Identity. See the JC.Core API reference:

- [Tenant](../JC.Core/API.md#tenant)
- [TenantSettings](../JC.Core/API.md#tenantsettings)
- [IMultiTenancy](../JC.Core/API.md#imultitenancy)

JC.Identity still owns the *mechanism* that makes them work. `IdentityDataDbContext<TUser, TRole>` applies the global query filter that scopes every `IMultiTenancy` entity to the current tenant, and `QueryExtensions.AllTenants` is the only sanctioned way to bypass it. Both are documented below.

---

## UserInfo

**Namespace:** `JC.Identity.Models`

Default `IUserInfo` implementation populated per-request by `UserInfoMiddleware`. Provides system and unknown user constants for unauthenticated and fallback scenarios. Registered as scoped. Inject via `IUserInfo`.

For the `IUserInfo` interface definition (properties and `IsInRole` method), see the [JC.Core API reference](../JC.Core/API.md#iuserinfo).

### Constants

| Constant | Type | Value | Description |
|----------|------|-------|-------------|
| `SYSTEM_USER_ID` | `string` | `"System__ID"` | User ID assigned for unauthenticated requests. |
| `SYSTEM_USER_NAME` | `string` | `"System"` | Username assigned for unauthenticated requests. |
| `SYSTEM_USER_EMAIL` | `string` | `"<SYSTEM@EMAIL>"` | Email assigned for unauthenticated requests. |
| `UNKNOWN_USER_ID` | `string` | `"Unknown__ID"` | User ID assigned when an identity is present but not authenticated. |
| `UNKNOWN_USER_NAME` | `string` | `"Unknown"` | Username assigned when an identity is present but not authenticated. |
| `UNKNOWN_USER_EMAIL` | `string` | `"<UNKNOWN@EMAIL>"` | Email assigned when an identity is present but not authenticated. |

### Constructors

#### UserInfo()

Parameterless constructor. `UserId`, `Username`, and `Email` default to the system identity (`SYSTEM_USER_ID`, `SYSTEM_USER_NAME`, `SYSTEM_USER_EMAIL`); all other properties default to empty values. Populated later by `UserInfoMiddleware`.

---

#### UserInfo(BaseUser user, IEnumerable\<string?\> roles)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `user` | `BaseUser` | — | The user entity to populate properties from. |
| `roles` | `IEnumerable<string?>` | — | The role names to assign. |

Creates a `UserInfo` populated directly from a `BaseUser` entity and a list of role name strings.

---

#### UserInfo(BaseUser user, IEnumerable\<BaseRole\> roles)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `user` | `BaseUser` | — | The user entity to populate properties from. |
| `roles` | `IEnumerable<BaseRole>` | — | The role entities to extract names from. |

Creates a `UserInfo` populated directly from a `BaseUser` entity and a list of role entities.

### Methods

#### IsInRole(string role)

**Returns:** `bool`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `role` | `string` | — | The role name to check. |

Returns `false` immediately if `role` is null or empty. Otherwise checks both the `Roles` list (populated from role claims) and the full `Claims` list for any claim with type `ClaimTypes.Role` matching the value.

---

# Services

## DefaultClaimsPrincipalFactory\<TUser, TRole\>

**Namespace:** `JC.Identity.Authentication`

Custom claims principal factory that extends the default ASP.NET Core Identity claims with 12 additional claims from `BaseUser` properties. Registered as `IUserClaimsPrincipalFactory<TUser>` during service registration.

**Constraints:** `TUser : BaseUser`, `TRole : BaseRole`

**Extends:** `UserClaimsPrincipalFactory<TUser, TRole>`

### Methods

#### GenerateClaimsAsync(TUser user)

**Returns:** `Task<ClaimsIdentity>`

**Access:** `protected override`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `user` | `TUser` | — | The user entity to generate claims for. |

Calls the base implementation to generate standard identity claims (name, user ID, roles, security stamp), then adds the following 12 custom claims from the user entity:

| Claim type | Source property | Serialisation |
|------------|----------------|---------------|
| `email_confirmed` | `EmailConfirmed` | `ToString()` |
| `phone_number` | `PhoneNumber` | Value or `""` |
| `phone_number_confirmed` | `PhoneNumberConfirmed` | `ToString()` |
| `two_factor_enabled` | `TwoFactorEnabled` | `ToString()` |
| `lockout_enabled` | `LockoutEnabled` | `ToString()` |
| `lockout_end` | `LockoutEnd` | ISO 8601 (`"O"`) or `""` |
| `access_failed_count` | `AccessFailedCount` | `ToString()` |
| `tenant_id` | `TenantId` | Value or `""` |
| `display_name` | `DisplayName` | Value or `""` |
| `last_login_utc` | `LastLoginUtc` | ISO 8601 (`"O"`) or `""` |
| `is_enabled` | `IsEnabled` | `ToString()` |
| `require_password_change` | `RequirePasswordChange` | `ToString()` |

---

# Helpers

## DefaultClaims

**Namespace:** `JC.Identity.Authentication`

Defines the custom claim type constants used by the JC.Identity claims pipeline.

### Constants

| Constant | Type | Value | Description |
|----------|------|-------|-------------|
| `EmailConfirmed` | `string` | `"email_confirmed"` | Whether the user's email has been confirmed. |
| `PhoneNumber` | `string` | `"phone_number"` | The user's phone number. |
| `PhoneNumberConfirmed` | `string` | `"phone_number_confirmed"` | Whether the phone number has been confirmed. |
| `TwoFactorEnabled` | `string` | `"two_factor_enabled"` | Whether 2FA is enabled. |
| `LockoutEnabled` | `string` | `"lockout_enabled"` | Whether lockout is enabled. |
| `LockoutEnd` | `string` | `"lockout_end"` | UTC lockout expiry timestamp. |
| `AccessFailedCount` | `string` | `"access_failed_count"` | Failed access attempt count. |
| `TenantId` | `string` | `"tenant_id"` | The user's tenant identifier. |
| `DisplayName` | `string` | `"display_name"` | The user's display name. |
| `LastLoginUtc` | `string` | `"last_login_utc"` | UTC last login timestamp. |
| `IsEnabled` | `string` | `"is_enabled"` | Whether the account is enabled. |
| `RequirePasswordChange` | `string` | `"require_password_change"` | Whether a password change is required. |

---

## IdentityHelper

**Namespace:** `JC.Identity.Helpers`

Builds the authenticator URI and formatted shared key used when setting up TOTP-based two-factor authentication (for example, to render an authenticator-app QR code and its manual-entry fallback). Not registered in DI — construct directly, supplying a `UrlEncoder`. It does not manage the authenticator secret itself; obtain the unformatted key from ASP.NET Core Identity's `UserManager`.

### Constructors

#### IdentityHelper(UrlEncoder urlEncoder)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `urlEncoder` | `UrlEncoder` | — | Used to URL-encode the email when building the authenticator URI. |

Uses the default authenticator URI format `otpauth://totp/{0}:{1}?secret={2}&issuer={0}`, where `{0}` is the issuer/app name, `{1}` is the URL-encoded email, and `{2}` is the unformatted key.

---

#### IdentityHelper(UrlEncoder urlEncoder, string authenticatorUriFormat)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `urlEncoder` | `UrlEncoder` | — | Used to URL-encode the email when building the authenticator URI. |
| `authenticatorUriFormat` | `string` | — | A custom composite format string overriding the default. Uses the same placeholder positions: `{0}` = issuer/app name, `{1}` = URL-encoded email, `{2}` = unformatted key. |

### Methods

#### Generate2faQrCodeUri(string name, string email, string unformattedKey)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The issuer/app name. Inserted at `{0}` — both the label prefix and the `issuer` parameter. |
| `email` | `string` | — | The account email. URL-encoded via the supplied `UrlEncoder` and inserted at `{1}`. |
| `unformattedKey` | `string` | — | The raw authenticator secret. Inserted verbatim at `{2}`. |

Formats the authenticator URI from the configured format string using `CultureInfo.InvariantCulture`. Only the email is URL-encoded; `name` and `unformattedKey` are inserted as-is.

---

#### Format2faKey(string unformattedKey)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `unformattedKey` | `string` | — | The raw authenticator secret to format for display. |

Groups the key into space-separated blocks of four characters and lowercases the result, producing the human-readable shared key shown as a manual-entry fallback beneath the QR code.

---

#### Generate2faKey(string name, string email, string secret)

**Returns:** `(string AuthenticatorUri, string FormattedKey)`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `name` | `string` | — | The issuer/app name. |
| `email` | `string` | — | The account email. |
| `secret` | `string` | — | The raw authenticator secret. |

Convenience method that returns both the authenticator URI (via `Generate2faQrCodeUri`) and the formatted shared key (via `Format2faKey`) in a single call.

---

## SystemRoles

**Namespace:** `JC.Identity.Authentication`

Defines built-in system roles. Designed to be extended by consuming applications (e.g. `class AppRoles : SystemRoles`). Role descriptions follow the naming convention `{RoleName}Desc` and are discovered automatically by `GetAllRoles`.

### Constants

| Constant | Type | Value | Description |
|----------|------|-------|-------------|
| `SystemAdmin` | `string` | `"SystemAdmin"` | Full system administrator with access to tenant management and assignment. |
| `SystemAdminDesc` | `string` | `"Full system administrator with access to tenant management and assignment."` | Description for `SystemAdmin`. |
| `Admin` | `string` | `"Admin"` | Administrator with access to all features within their tenant. |
| `AdminDesc` | `string` | `"Administrator with access to all features within their tenant."` | Description for `Admin`. |

### Methods

#### GetAllRoles\<T\>()

**Returns:** `List<(string Role, string Description)>`

**Constraint:** `T : SystemRoles`

Discovers all `const string` fields on `T` (including inherited fields from `SystemRoles`) using reflection. Fields ending in `"Desc"` are treated as descriptions, not roles. Each role field is paired with its description by looking for a corresponding `{FieldName}Desc` constant. Returns a list of tuples containing the role name and its description (empty string if no description field exists).

---

# Extensions

## QueryExtensions

**Namespace:** `JC.Identity.Extensions`

Static extension methods for multi-tenancy query filtering.

### Methods

#### AllTenants\<T\>(this IQueryable\<T\> query, IUserInfo userInfo)

**Returns:** `IQueryable<T>`

**Constraint:** `T : class`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `query` | `IQueryable<T>` | — | The source queryable. |
| `userInfo` | `IUserInfo` | — | The current user information, used to check the `SystemAdmin` role. |

If the user has the `SystemAdmin` role, calls `IgnoreQueryFilters()` on the queryable to bypass tenant scoping. Otherwise returns the queryable unchanged. Use this to allow system administrators to query across all tenants.

---

#### ApplyTenantQueryFilters(this ModelBuilder modelBuilder, DbContext context)

**Returns:** `ModelBuilder`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `modelBuilder` | `ModelBuilder` | — | The model builder to apply filters to. |
| `context` | `DbContext` | — | The `DbContext` instance whose `CurrentTenantId` property is referenced in the filter expression. |

Iterates all entity types in the model and applies a global query filter to those implementing `IMultiTenancy`. The filter logic: if `CurrentTenantId` is null or empty, only entities with `TenantId == null` are returned; otherwise, only entities matching `CurrentTenantId` are returned.

---

# Data

## IdentityDataDbContext\<TUser, TRole\>

**Namespace:** `JC.Identity.Data`

Identity-aware data context extending `IdentityDbContext<TUser, TRole, string>` and implementing `IDataDbContext`. Configures core entities (`AuditEntry`), tenant entities, and applies multi-tenancy global query filters to all entities implementing `IMultiTenancy`.

**Constraints:** `TUser : BaseUser`, `TRole : BaseRole`

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `CurrentTenantId` | `string?` | get; | The current user's tenant identifier, read from `IUserInfo.TenantId`. Referenced by global query filters. |
| `AuditEntries` | `DbSet<AuditEntry>` | get; set; | The set of audit trail records. |
| `Tenants` | `DbSet<Tenant>` | get; | The set of tenants. |

Overrides `SaveChangesAsync` to automatically create audit trail entries via the change tracker, identically to `DataDbContext` in JC.Core — see the [JC.Core API reference](../JC.Core/API.md#datadbcontext) for audit behaviour details.

---

# Middleware

## UserInfoMiddleware

**Namespace:** `JC.Identity.Middleware`

Middleware that populates the scoped `IUserInfo` instance from the current `ClaimsPrincipal` on first access per request. Skips population if `IUserInfo.IsSetup` is already `true`.

### Methods

#### InvokeAsync(HttpContext context)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `HttpContext` | — | The HTTP context for the current request. |

Resolves `IUserInfo` from the request's service provider. If not already set up:

For requests with **no identity** (`context.User.Identity` is null): assigns `SYSTEM_USER_ID`, `SYSTEM_USER_NAME`, and `SYSTEM_USER_EMAIL`.

For requests with an **identity present but not authenticated**: assigns `UNKNOWN_USER_ID`, `UNKNOWN_USER_NAME`, and `UNKNOWN_USER_EMAIL`.

For **authenticated requests**: reads the user ID, username, and email from the claims principal using the claim types configured in `IdentityOptions.ClaimsIdentity`. Reads all 12 custom claims defined by `DefaultClaims` and parses them into the corresponding `IUserInfo` properties. Populates `Roles` by filtering claims with the configured `RoleClaimType`, and `Claims` with the full claim list. Sets `MultiTenancyEnabled` to `true` if `TenantId` is non-empty.

Sets `IsSetup = true` after population and invokes the next middleware.

---

## IdentityMiddleware

**Namespace:** `JC.Identity.Middleware`

Middleware that enforces identity business rules for authenticated requests. Evaluates checks in order: disabled account, password change, 2FA. Skips static files (`.css`, `.js`, `.jpg`, `.jpeg`, `.png`, `.gif`, `.svg`, `.ico`, `.woff`, `.woff2`, `.ttf`, `.eot`, `.map`, `.json`, `.xml`), unauthenticated requests, and excluded paths.

### Methods

#### InvokeAsync(HttpContext context, IUserInfo userInfo)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `HttpContext` | — | The HTTP context for the current request. |
| `userInfo` | `IUserInfo` | — | The current user information, injected by the DI container. |

Evaluates the following checks in order, redirecting on the first failure:

1. **Disabled account** — if `userInfo.IsEnabled` is `false`, redirects to `IdentityMiddlewareOptions.AccessDeniedRoute`.
2. **Password change** — if `RequirePasswordChange` is enabled in options and `userInfo.RequiresPasswordChange` is `true`, redirects to `ChangePasswordRoute`. Skipped if the current path already starts with `ChangePasswordRoute`.
3. **Two-factor authentication** — if `EnforceTwoFactor` is enabled in options and `userInfo.TwoFactorEnabled` is `false`, redirects to `TwoFactorRoute`. Skipped if the current path already starts with `TwoFactorRoute`.

If all checks pass, invokes the next middleware.
