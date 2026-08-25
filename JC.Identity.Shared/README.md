# JC.Identity.Shared

The identity runtime shared by every JC identity authority — the `IUserInfo` implementation, the claims projection, the account rules and the two-factor helper. No dependency on ASP.NET Core or ASP.NET Identity, so a worker service can take user attribution without the ASP.NET Core runtime.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.Identity.Shared/JC.Identity.Shared.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

**Most consumers never reference this directly.** [JC.Identity](https://github.com/JC-Foundry/JC-Packages/blob/master/JC.Identity/README.md) references it and calls everything here on your behalf — if you are using local ASP.NET Core Identity, start there.

Reference it yourself when you are supplying identity from somewhere other than JC.Identity, or when you want user attribution in a host with no HTTP pipeline. For an ASP.NET Core application, add `JC.Identity.Shared.Web` as well — it holds the middleware and is the only half carrying a framework reference.

## Prerequisites

- .NET 9.0 SDK
- **JC.Core** — `IUserInfo` is a JC.Core contract
- ASP.NET Core is **not** required

## Quick start

### An IUserInfo implementation

```csharp
public class AppUserInfo : UserInfoBase
{
    public string? DepartmentId { get; set; }
}
```

### Services — `Program.cs`

```csharp
builder.Services.AddSharedIdentityServices<AppUserInfo>(
    configureProjection: options =>
    {
        options.UserIdClaimType = "sub";
        options.EmailClaimType = "email";
        options.RoleClaimType = "roles";
        options.Authority = IdentityAuthority.Custom;
    });
```

### Middleware — a web host

```csharp
var app = builder.Build();

app.UseAuthentication();     // whatever establishes the principal
app.UseUserInfo();           // projects its claims onto IUserInfo
app.UseAuthorization();
app.UseIdentityMiddleware(); // enforces disabled accounts, password changes, two-factor
```

`UseUserInfo` and `UseIdentityMiddleware` come from `JC.Identity.Shared.Web`.

### A worker or console host

No middleware. Establish the identity for a unit of work explicitly:

```csharp
await using var scope = services.CreateAsyncScopeForUser(user, roles, user.IdentityTenantId);

var orders = scope.ServiceProvider.GetRequiredService<IRepositoryContext<Order>>();
await orders.AddAsync(order);   // audited against that user
```

## Feature areas

### UserInfoBase

Implements every `IUserInfo` member, so an authority derives its own type and adds whatever it carries beyond the shared surface. Consumers still inject `IUserInfo` — nothing downstream names the concrete type.

An unpopulated instance is **not** blank: `UserId`, `Username` and `Email` hold the system-user constants, so `if (userInfo.UserId is null)` never fires. Check `IsSetup` instead.

### Claims projection

```csharp
userInfo.PopulateFrom(principal, projectionOptions, logger);
```

Three branches — no principal, an unauthenticated one, and an authenticated one — all of which mark the instance populated. Only the authenticated branch stamps `Authority`, so an anonymous request keeps `None`.

`IdentityProjectionOptions` names the identifier, email and role claim types plus the authority to stamp. Everything else comes from the fixed constants on `DefaultClaims`, so an authority wanting those fields populated must emit claims under exactly those names.

There is also a `PopulateFrom` overload taking an `IApplicationUser`, for projecting a stored record rather than a principal.

### Identity outside a request

`IUserInfo` is scoped and populated **in place**, so constructing one and passing it around changes nothing that injects it. These resolve the scope's own instance and fill it in:

```csharp
services.SetUserInfoForUser(user, roles, tenantId);          // the current scope
services.CreateScopeForUser(user, roles, tenantId);          // a new scope
services.CreateAsyncScopeForUser(user, roles, tenantId);     // the same, async-disposable
```

`tenantId` is passed separately rather than taken from `user.IdentityTenantId`, because the tenant owning an identity record and the user's tenant inside the application are different concepts.

Calling any of these inside a live request replaces the authenticated user for the rest of it — that is impersonation, and should be deliberate.

### Account rules

`IdentityRules.GetRedirect` evaluates disabled accounts, forced password changes and optional two-factor against a request path, returning a route or `null`:

```csharp
var redirect = IdentityRules.GetRedirect(userInfo, path, isAuthenticated, options);
```

Plain code rather than middleware, so a minimal API filter, a Blazor circuit or a desktop shell reaches the same behaviour. `IdentityMiddleware` in `JC.Identity.Shared.Web` is a wrapper that supplies the request and performs the redirect.

Disabled accounts are checked first, deliberately — a disabled account should not be routed to a password-change page it has no business completing.

### Rule sets

Which rules run, and where each sends the caller, is a property of an `IdentityRuleSet` rather than of the application. `IdentityMiddlewareOptions` holds an ordered list of them and a `Default` that catches anything unmatched:

```csharp
options.Default.EnforceTwoFactor = true;

options.AddForPathPrefix("/sso", ruleSet =>
{
    ruleSet.EnforceTwoFactor = false;
    ruleSet.TwoFactorRoute = "/sso/security/authenticator";
    ruleSet.AccessDeniedRoute = "/sso/denied";
});
```

The first set whose condition matches wins, and its routes are also what supply the excluded paths. An application serving a second audience gives it its own routes rather than exempting it from enforcement, so a disabled user is still stopped, at the right page.

A condition is any `Func<IdentityRuleContext, bool>`, and the context carries the path, the user and the request's services, so a rule can depend on something only knowable per request.

### Two-factor setup

```csharp
var helper = new IdentityHelper(UrlEncoder.Default);
var (authenticatorUri, formattedKey) = helper.Generate2faKey("MyApp", user.Email!, unformattedKey);
```

Not registered in the container — construct it where you need it. It formats only; generating and validating the shared secret belong to the authority.

### Roles

This package defines none. An authority's administrative roles are its own security domain, so each brings its own structure — JC.Identity supplies `SystemRoles`.

What is shared is the shape they arrive in: `IUserInfo.Roles` holds names in the consuming application's authorisation domain, and `IsInRole` matches against it ordinally and case-sensitively. JC.Tenancy likewise matches cross-tenant permissions by role **name**, precisely so it need not know whose roles they are.

## Defaults

| Default | Value |
|---------|-------|
| `IUserInfo` implementation | Your `TUserInfo`, registered scoped with `TryAdd` |
| `UserIdClaimType` | `ClaimTypes.NameIdentifier` |
| `EmailClaimType` | `ClaimTypes.Email` |
| `RoleClaimType` | `ClaimTypes.Role` |
| `Authority` | `None` — an authority that never declares itself cannot pass as local |
| Password change enforcement | Enabled, routing to `/Identity/Account/Manage/SetPassword` |
| Two-factor enforcement | Disabled, routing to `/Identity/Account/Manage/EnableAuthenticator` |
| Access denied / logout / error routes | `/Identity/Account/AccessDenied`, `/Identity/Account/Logout`, `/Error` |
| Authentication and authorisation | **Not** registered — establishing that a principal is authenticated belongs to the authority |

## Documentation

- [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Identity.Shared/Setup.md) — registration, projection and rule options, and the ASP.NET Core middleware
- [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Identity.Shared/Guide.md) — reading the current user, background-job identity, building a custom authority
- [API Reference](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Identity.Shared/API.md)
- [JC.Identity](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Identity/Setup.md) — the local ASP.NET Core Identity authority built on this package

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
