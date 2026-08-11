# JC.Identity.Shared — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- JC.Core registered — `IUserInfo` is a JC.Core contract
- ASP.NET Core is **not** required. This package carries no framework reference and runs from a console application, a worker service or a test host
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

**Most consumers never set this package up directly.** JC.Identity references it and calls everything here on your behalf — if you are using local ASP.NET Core Identity, start at [JC.Identity — Setup](../JC.Identity/Setup.md) instead.

Set this package up yourself when you are supplying identity from somewhere other than JC.Identity, or when you want user attribution in a host that has no HTTP pipeline.

## 0. Add the package

```xml
<ProjectReference Include="path/to/JC.Identity.Shared/JC.Identity.Shared.csproj" />
```

For an ASP.NET Core application, add the middleware companion as well:

```xml
<ProjectReference Include="path/to/JC.Identity.Shared.Web/JC.Identity.Shared.Web.csproj" />
```

`JC.Identity.Shared.Web` references `JC.Identity.Shared`, so adding it alone is enough for a web host. It is the only half that carries a `FrameworkReference` on `Microsoft.AspNetCore.App`, which is what lets a worker service take the framework-free half on its own.

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### An IUserInfo implementation

Derive from `UserInfoBase` and add whatever your authority carries beyond the shared surface:

```csharp
public class AppUserInfo : UserInfoBase
{
    public string? DepartmentId { get; set; }
}
```

Deriving is not required — any `IUserInfo` implementation is accepted — but the base supplies the whole property surface, `IsInRole`, and a parameterless constructor for the container to activate.

### Services — a web host

```csharp
builder.Services.AddSharedIdentityServices<AppUserInfo>(
    configureProjection: options =>
    {
        options.UserIdClaimType = "sub";
        options.EmailClaimType = "email";
        options.RoleClaimType = "roles";
        options.Authority = IdentityAuthority.Custom;
    }
);
```

### Middleware — a web host

```csharp
var app = builder.Build();

app.UseAuthentication();     // your own authentication, whatever establishes the principal
app.UseUserInfo();           // projects that principal's claims onto IUserInfo
app.UseAuthorization();
app.UseIdentityMiddleware(); // enforces disabled accounts, password changes, two-factor
```

`UseUserInfo` and `UseIdentityMiddleware` come from `JC.Identity.Shared.Web.Extensions`.

### Services — a worker or console host

```csharp
builder.Services.AddSharedIdentityServices<AppUserInfo>();
```

There is no middleware to add. Establish the identity for a unit of work explicitly:

```csharp
await using var scope = services.CreateAsyncScopeForUser(user, roles, user.IdentityTenantId);

var repository = scope.ServiceProvider.GetRequiredService<IRepositoryManager>();
// every write in this scope is audited against that user
```

### Defaults

With no configuration callbacks, `AddSharedIdentityServices` gives you:

| Default | Value |
|---------|-------|
| `IUserInfo` implementation | Your `TUserInfo`, registered scoped |
| `UserIdClaimType` | `ClaimTypes.NameIdentifier` |
| `EmailClaimType` | `ClaimTypes.Email` |
| `RoleClaimType` | `ClaimTypes.Role` |
| `Authority` | `IdentityAuthority.None` |
| Password change enforcement | Enabled |
| Password change route | `/Identity/Account/Manage/SetPassword` |
| Two-factor enforcement | Disabled |
| Two-factor route | `/Identity/Account/Manage/EnableAuthenticator` |
| Access denied route | `/Identity/Account/AccessDenied` |
| Logout route | `/Identity/Account/Logout` |
| Error route | `/Error` |

The claim-type defaults match ASP.NET Identity's own. `Authority` deliberately does **not** — it stays `None` so an authority that never declares itself cannot pass as local. Set it in `configureProjection`.

`AddSharedIdentityServices` registers:

| Registration | Lifetime | Description |
|--------------|----------|-------------|
| `IUserInfo` → `TUserInfo` | Scoped | Registered with `TryAddScoped`, so an implementation registered earlier wins |
| `IOptions<IdentityMiddlewareOptions>` | Singleton | Routes and enforcement switches |
| `IOptions<IdentityProjectionOptions>` | Singleton | Claim types and authority |

It deliberately does **not** call `AddAuthentication` or `AddAuthorization`. Establishing *that* a principal is authenticated belongs to the authority; this package only projects the result.

## 2. Full configuration

### AddSharedIdentityServices

**Namespace:** `JC.Identity.Shared.Extensions`

```csharp
builder.Services.AddSharedIdentityServices<AppUserInfo>(
    configureMiddleware: options =>
    {
        options.RequirePasswordChange = true;
        options.ChangePasswordRoute = "/Identity/Account/Manage/SetPassword";
        options.EnforceTwoFactor = false;
        options.TwoFactorRoute = "/Identity/Account/Manage/EnableAuthenticator";
        options.AccessDeniedRoute = "/Identity/Account/AccessDenied";
        options.LogoutRoute = "/Identity/Account/Logout";
        options.ErrorRoute = "/Error";
    },
    configureProjection: options =>
    {
        options.UserIdClaimType = ClaimTypes.NameIdentifier;
        options.EmailClaimType = ClaimTypes.Email;
        options.RoleClaimType = ClaimTypes.Role;
        options.Authority = IdentityAuthority.None;
    }
);
```

| Type parameter | Constraint | Description |
|----------------|-----------|-------------|
| `TUserInfo` | `class, IUserInfo` | The implementation registered as the scoped `IUserInfo` |

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configureMiddleware` | `Action<IdentityMiddlewareOptions>?` | `null` | Configures the account rules and their routes |
| `configureProjection` | `Action<IdentityProjectionOptions>?` | `null` | Configures which claim types are read and which authority is stamped |

When a callback is `null` the corresponding options type is still registered, carrying its own defaults.

### IdentityProjectionOptions

**Namespace:** `JC.Identity.Shared.Models.Options`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `UserIdClaimType` | `string` | `ClaimTypes.NameIdentifier` | Claim the user identifier is read from |
| `EmailClaimType` | `string` | `ClaimTypes.Email` | Claim the email address is read from |
| `RoleClaimType` | `string` | `ClaimTypes.Role` | Claim role membership is read from |
| `Authority` | `IdentityAuthority` | `None` | Stamped onto `IUserInfo.Authority` on the authenticated branch only |

These three claim types are the only ones an authority chooses. Everything else the projection reads comes from the fixed constants in `DefaultClaims`, so an authority that wants those fields populated must emit claims under those exact names.

`Authority` is stamped only when a principal is authenticated, so an anonymous request keeps `None` whatever you configure.

### IdentityMiddlewareOptions

**Namespace:** `JC.Identity.Shared.Models.Options`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RequirePasswordChange` | `bool` | `true` | Redirect users whose `IUserInfo.RequiresPasswordChange` is `true` |
| `ChangePasswordRoute` | `string` | `/Identity/Account/Manage/SetPassword` | Where those users are sent |
| `EnforceTwoFactor` | `bool` | `false` | Redirect users whose `IUserInfo.TwoFactorEnabled` is `false` |
| `TwoFactorRoute` | `string` | `/Identity/Account/Manage/EnableAuthenticator` | Where those users are sent |
| `AccessDeniedRoute` | `string` | `/Identity/Account/AccessDenied` | Where disabled accounts are sent |
| `LogoutRoute` | `string` | `/Identity/Account/Logout` | Logout route — excluded from enforcement |
| `ErrorRoute` | `string` | `/Error` | Error route — excluded from enforcement |
| `ExcludedPaths` | `string[]` | Derived | Read-only. Built from `AccessDeniedRoute`, `LogoutRoute` and `ErrorRoute` on each read |

The routes default to the ASP.NET Core Identity UI paths. If your application does not use that UI, set all of them.

### UserInfoBase

**Namespace:** `JC.Identity.Shared.Models`

Implements every `IUserInfo` member. Two constructors:

| Constructor | Use |
|-------------|-----|
| `UserInfoBase()` | What dependency injection activates. Holds the system-user defaults until something populates it |
| `UserInfoBase(IApplicationUser user, IEnumerable<string?> roles)` | Projects an authoritative user record immediately |

An unpopulated instance is **not** blank. `UserId`, `Username` and `Email` default to `IUserInfo.SYSTEM_USER_ID`, `SYSTEM_USER_NAME` and `SYSTEM_USER_EMAIL` — `"System__ID"`, `"System"` and `"<SYSTEM@EMAIL>"`. Audit entries written before anything populates the instance are therefore attributed to the system user rather than to nothing.

`HasTenant` is derived from `TenantId` and has no setter. `IsSetup` reports whether the instance has been populated; the claims middleware skips any instance where it is already `true`.

`IsInRole(role)` returns `true` when the name appears in `Roles`, or when a claim of type `ClaimTypes.Role` carries it. Comparison is ordinal and case-sensitive, and an empty role name always returns `false`.

The record-projecting constructor deliberately leaves `TenantId` alone: `IApplicationUser.IdentityTenantId` means the tenant owning the identity record, which is not necessarily the user's tenant inside your application. Set it yourself where you know the two coincide.

### Projecting a principal onto IUserInfo

**Namespace:** `JC.Identity.Shared.Extensions`

```csharp
userInfo.PopulateFrom(principal, projectionOptions, logger);
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `principal` | `ClaimsPrincipal?` | — | The principal to project. `null` is accepted |
| `options` | `IdentityProjectionOptions` | — | The claim types to read and the authority to stamp |
| `logger` | `ILogger?` | `null` | Records the projection outcome |

Three branches, and it sets `IsSetup = true` in all of them:

| Condition | Result |
|-----------|--------|
| `principal` or its `Identity` is `null` | The system-user constants |
| Identity present but not authenticated | The unknown-user constants — `"Unknown__ID"`, `"Unknown"`, `"<UNKNOWN@EMAIL>"` |
| Authenticated | Every field projected from claims, and `Authority` stamped |

On the authenticated branch, `TenantId` is assigned **only when the tenant claim carries a value**, so an empty claim leaves whatever was already there rather than clearing it.

This is what `UserInfoMiddleware` calls. Call it directly when your authority has a principal but no HTTP pipeline.

### Projecting a user record onto IUserInfo

```csharp
userInfo.PopulateFrom(user, roles);
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `user` | `IApplicationUser` | The record to project |
| `roles` | `IEnumerable<string?>` | Role names. Null and empty entries are discarded |

Sets `IsSetup = true`. Sets neither `TenantId` nor `Authority` — the first because the two tenant concepts differ, the second because only the registering package knows it.

### Establishing an ambient identity outside a request

`IUserInfo` is scoped and populated **in place**, so constructing one and passing it around changes nothing that injects it. These resolve the scope's own instance and fill it in.

```csharp
// Populate the current scope
var userInfo = services.SetUserInfoForUser(user, roles, user.IdentityTenantId);

// Or create a scope that already has it
using var scope = services.CreateScopeForUser(user, roles, user.IdentityTenantId);
await using var asyncScope = services.CreateAsyncScopeForUser(user, roles, user.IdentityTenantId);
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `user` | `IApplicationUser` | — | The record to project |
| `roles` | `IEnumerable<string?>` | — | Role names. Null and empty entries are discarded |
| `tenantId` | `string?` | `null` | The user's tenant **within this application** |

`tenantId` is passed separately rather than taken from `user.IdentityTenantId` because the tenant owning an identity record and the user's application tenant are different concepts. Where they coincide, pass `user.IdentityTenantId` and say so at the call site.

`SetUserInfoForUser` also stamps `Authority` from `IdentityProjectionOptions`, so the authority is stated in exactly one place. That means it resolves `IOptions<IdentityProjectionOptions>` and requires `AddSharedIdentityServices` to have run.

**Calling any of these inside a live request scope replaces the authenticated user for the rest of that request.** That is impersonation, and should be a deliberate choice rather than a convenience.

### Enforcing the account rules without middleware

**Namespace:** `JC.Identity.Shared.Helpers`

```csharp
var redirect = IdentityRules.GetRedirect(userInfo, path, isAuthenticated, options, logger);

if (redirect is not null)
{
    // send the caller to that route
}
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userInfo` | `IUserInfo` | — | The current user |
| `path` | `string` | — | The path being requested |
| `isAuthenticated` | `bool` | — | Whether the caller is authenticated |
| `options` | `IdentityMiddlewareOptions` | — | The configured routes and switches |
| `logger` | `ILogger?` | `null` | Records why a caller was redirected |

**Returns** the route to redirect to, or `null` to continue.

It returns `null` immediately for unauthenticated callers, excluded paths, and static files matched by extension: `.css`, `.js`, `.jpg`, `.jpeg`, `.png`, `.gif`, `.svg`, `.ico`, `.woff`, `.woff2`, `.ttf`, `.eot`, `.map`, `.json`, `.xml`.

Otherwise it checks, in order:

1. **Disabled account** — `IsEnabled` is `false`, so return `AccessDeniedRoute`. First deliberately: a disabled account should not be routed to a password-change or two-factor page it has no business completing.
2. **Password change** — `RequirePasswordChange` is on and the user's `RequiresPasswordChange` is `true`, so return `ChangePasswordRoute`.
3. **Two-factor** — `EnforceTwoFactor` is on and the user's `TwoFactorEnabled` is `false`, so return `TwoFactorRoute`.

Rules 2 and 3 are skipped when the path already starts with their own route, so the target page stays reachable. Rule 1 needs no such guard, because `AccessDeniedRoute` is one of the `ExcludedPaths`.

This is the whole of `IdentityMiddleware`'s logic. The middleware supplies the path and performs the redirect; everything else is here.

### Adding the ASP.NET Core middleware

Reference `JC.Identity.Shared.Web`, then register both in order:

```csharp
app.UseAuthentication();
app.UseUserInfo();           // after authentication — reads the principal's claims
app.UseAuthorization();
app.UseIdentityMiddleware(); // after UseUserInfo — enforces rules against what it produced
```

`UserInfoMiddleware` resolves the scoped `IUserInfo` and populates it only when `IsSetup` is `false`, so an instance established earlier — by a background job, or by impersonation — is left alone.

`IdentityMiddleware` resolves `IUserInfo` as a method parameter, evaluates `IdentityRules.GetRedirect`, and either redirects or calls the next middleware.

Neither adds behaviour of its own. Both are wrappers, which is what allows an authority with no HTTP pipeline to reach identical results.

### Roles

This package defines no roles. An authority's administrative roles are its own security domain and must not be mixed into another's, so each authority brings its own structure — JC.Identity supplies `SystemRoles`, documented in [JC.Identity — Setup](../JC.Identity/Setup.md#defining-roles).

What is shared is the *reading* of them: `IUserInfo.Roles` holds role names in the consuming application's own authorisation domain, whichever authority supplied them, and `IUserInfo.IsInRole` matches against it. JC.Tenancy likewise matches cross-tenant bypass permissions by role **name** rather than by a constant, precisely so it need not know whose roles they are.

### DefaultClaims

**Namespace:** `JC.Identity.Shared.Authentication`

The claim types the projection reads for everything beyond the three configurable ones. An authority that wants these fields populated must emit claims under exactly these names.

| Constant | Claim type | Populates |
|----------|-----------|-----------|
| `EmailConfirmed` | `email_confirmed` | `IUserInfo.EmailConfirmed` |
| `PhoneNumber` | `phone_number` | `IUserInfo.PhoneNumber` |
| `PhoneNumberConfirmed` | `phone_number_confirmed` | `IUserInfo.PhoneNumberConfirmed` |
| `TwoFactorEnabled` | `two_factor_enabled` | `IUserInfo.TwoFactorEnabled` |
| `LockoutEnabled` | `lockout_enabled` | `IUserInfo.LockoutEnabled` |
| `LockoutEnd` | `lockout_end` | `IUserInfo.LockoutEnd` |
| `AccessFailedCount` | `access_failed_count` | `IUserInfo.AccessFailedCount` |
| `TenantId` | `tenant_id` | `IUserInfo.TenantId` |
| `DisplayName` | `display_name` | `IUserInfo.DisplayName` |
| `LastLoginUtc` | `last_login_utc` | `IUserInfo.LastLoginUtc` |
| `RegistrationUtc` | `registration_utc` | `IUserInfo.RegistrationUtc` |
| `IsEnabled` | `is_enabled` | `IUserInfo.IsEnabled` |
| `RequirePasswordChange` | `require_password_change` | `IUserInfo.RequiresPasswordChange` |

Boolean claims are compared case-insensitively against `"true"`, so anything else reads as `false`. Date claims are parsed with `DateTime.TryParse` and fall back to `null`; `AccessFailedCount` falls back to `0`.

### IdentityHelper

**Namespace:** `JC.Identity.Shared.Helpers`

Formats the two halves of an authenticator setup screen. **Not registered in the container** — construct it where you need it:

```csharp
var helper = new IdentityHelper(UrlEncoder.Default);
var (authenticatorUri, formattedKey) = helper.Generate2faKey("MyApp", user.Email!, unformattedKey);
```

| Member | Returns | Description |
|--------|---------|-------------|
| `Generate2faQrCodeUri(name, email, unformattedKey)` | `string` | The `otpauth://` URI a QR code encodes |
| `Format2faKey(unformattedKey)` | `string` | The key lowercased and grouped in fours for display |
| `Generate2faKey(name, email, secret)` | `(string AuthenticatorUri, string FormattedKey)` | Both of the above in one call |

The default URI format is `otpauth://totp/{0}:{1}?secret={2}&issuer={0}`. A second constructor takes a replacement format string, whose placeholders are the issuer name, the URL-encoded email and the secret, in that order.

## 3. Verify

1. Sign in through whatever authenticates your application.
2. Inject `IUserInfo` and confirm `UserId`, `Username` and `Roles` are populated, and that `Authority` reads whatever you configured.
3. In a non-web host, wrap a unit of work in `CreateScopeForUser` and confirm an audited write is attributed to that user rather than to `System`.

## Next steps

- [Guide](Guide.md) — projection scenarios, impersonation, and supplying identity from a custom authority.
- [API Reference](API.md)
- [JC.Identity — Setup](../JC.Identity/Setup.md) — the local ASP.NET Core Identity authority built on this package.
