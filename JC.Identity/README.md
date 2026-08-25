# JC.Identity

ASP.NET Core Identity wired into the JC suite. Identity and the audit trail share one `DbContext`, so every change is attributed to the signed-in user, and a claims factory plus middleware project that user onto `IUserInfo` — the contract the rest of the suite depends on.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.Identity/JC.Identity.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

`JC.Identity.Shared` and `JC.Identity.Shared.Web` arrive with this package and need no separate reference.

## Prerequisites

- .NET 9.0 SDK, and an ASP.NET Core project
- **JC.Core**, registered with `AddCore<AppDbContext>()`
- A `DbContext` extending `IdentityDataDbContext<TUser, TRole>`
- User and role types extending `BaseUser` and `BaseRole`

## Quick start

### Data — `AppDbContext`

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options, IUserInfo userInfo)
    : IdentityDataDbContext<AppUser, AppRole>(options, userInfo);

public class AppUser : BaseUser;
public class AppRole : BaseRole;
```

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>();
```

### Middleware — `Program.cs`

```csharp
var app = builder.Build();

// Authentication → IUserInfo → authorisation → identity rules, in that order
app.UseIdentity();

// Optional: seed system roles and a default administrator from configuration
var admin = await app.ConfigureAdminAndRolesAsync<AppUser, AppRole, AppRoles>();
```

### Configuration — `appsettings.json`

Only needed when seeding an administrator:

```json
{
  "Admin": {
    "Username": "admin",
    "Email": "admin@example.com",
    "Password": "YourSecurePassword123!",
    "DisplayName": "System Administrator"
  }
}
```

## Where things live

This package is the **local ASP.NET Identity authority**. The parts that any identity authority needs — the `IUserInfo` implementation, the claims projection, the account rules and their options, and the two-factor helper — live in `JC.Identity.Shared`, with its ASP.NET Core middleware in `JC.Identity.Shared.Web`. A future authority reuses those without reimplementing them.

Tenancy is a separate package. `JC.Identity` has no reference to `JC.Tenancy` and applies no tenant filters; the consuming application joins the two.

## Feature areas

### IUserInfo

The reason this package matters to the others. The claims middleware reads the authenticated principal into a scoped `IUserInfo`, which JC.Core uses to attribute audit rows, JC.Tenancy reads to derive the operational tenant, and JC.FileStorage and JC.Communication use to scope and attribute their own work.

```csharp
public class DashboardModel(IUserInfo userInfo) : PageModel
{
    public void OnGet()
    {
        var id = userInfo.UserId;
        var authority = userInfo.Authority;   // Local
    }
}
```

`IUserInfo` is a JC.Core contract, so packages consume it without referencing any identity package. Where no implementation is registered at all, the suite falls back to a placeholder identifier — valid, but unattributed.

### Claims

`DefaultClaimsPrincipalFactory` projects thirteen fields from `BaseUser` onto the principal — email and phone confirmation, two-factor and lockout state, access failures, tenant, display name, last login, registration, enabled state and whether a password change is due. The shared middleware reads them back, so `IUserInfo` costs no database round trip per request.

The claim type constants are on `DefaultClaims`, in `JC.Identity.Shared`. The identifier, email and role claim types are whatever `IdentityOptions.ClaimsIdentity` says, copied at registration rather than assumed.

**Claims are minted at sign-in.** Changing `IsEnabled`, `RequirePasswordChange` or `TenantId` in the database does not change a cookie already issued — call `UserManager.UpdateSecurityStampAsync` where the change must take effect immediately.

### Identity rules middleware

Enforces account state on every authenticated request, skipping static files and the excluded paths of the rule set that matched:

- Disabled accounts are redirected to the access-denied route
- Users flagged for a password change are redirected until they complete it
- Two-factor enrolment can be required, off by default

```csharp
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>(
    configureMiddleware: options =>
    {
        options.Default.RequirePasswordChange = true;
        options.Default.EnforceTwoFactor = false;
    });
```

`Default` is the rule set applied when nothing else matches, and is all a single-audience application needs. An application serving a second audience adds a rule set with a condition, giving it its own routes rather than exempting it from enforcement.

The rules themselves are `IdentityRules` in `JC.Identity.Shared`, expressed as a function returning a route or nothing, so a host with no HTTP pipeline reaches the same behaviour.

### Roles and seeding

Extend `SystemRoles` with a name and a matching `{Name}Desc` constant per role:

```csharp
public class AppRoles : SystemRoles
{
    public const string Editor = nameof(Editor);
    public const string EditorDesc = "Can create and edit content.";
}
```

`SeedRolesAsync` discovers them by reflection — public `const` strings only. `SeedDefaultAdminAsync` creates the administrator alone, and `ConfigureAdminAndRolesAsync` does both, returning the administrator it created *or found*. All three are idempotent.

Roles belong to this package rather than the shared runtime: an authority's administrative roles are its own security domain, and another authority brings its own.

### Multi-tenancy

`BaseUser` carries a `TenantId` column, and that tenant reaches the runtime by claim:

```text
BaseUser.TenantId → tenant_id claim → IUserInfo.TenantId → ITenantInfo (JC.Tenancy)
```

Filtering itself is [JC.Tenancy](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Tenancy/Setup.md)'s job, opted into per `DbContext`. `IdentityDataDbContext` applies no filters, which is what lets a single-tenant application skip that package entirely.

`BaseUser` is deliberately **not** filtered by tenant, and must not be. A global query filter on the user entity breaks `UserManager` and `SignInManager`, because authentication resolves a user before any tenant scope exists.

Assign the seeded administrator a tenant by joining the two packages at the call site:

```csharp
var admin = await app.ConfigureAdminAndRolesAsync<AppUser, AppRole, AppRoles>();

if (admin is not null)
    await app.Services.SeedDefaultTenantAsync<AppUser, AppDbContext>(admin.Id);
```

### Custom IUserInfo

Need more on it? Derive from `UserInfoBase` and use the four-type-parameter overload:

```csharp
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext, AppUserInfo>();
```

### Identity already registered

Where the application registers ASP.NET Core Identity itself — for external providers, say — `AddIdentityServices` adds only the JC services and leaves Identity and its cookie configuration alone.

## Defaults

| Default | Value |
|---------|-------|
| Login / logout / access denied paths | `/Identity/Account/Login`, `/Logout`, `/AccessDenied` |
| Password change enforcement | Enabled, routing to `/Identity/Account/Manage/SetPassword` |
| Two-factor enforcement | Disabled |
| `IUserInfo` implementation | Built-in `UserInfo`, scoped |
| `IUserInfo.Authority` | `Local` once authenticated, `None` otherwise |
| Claims factory | `DefaultClaimsPrincipalFactory` — thirteen custom claims |
| `UseIdentity` order | Authentication → `UseUserInfo` → authorisation → identity rules |
| Admin roles when seeded | `SystemAdmin`, plus `Admin` unless `assignAdminRole` is `false` |
| Tenant filtering | None. Opt in with JC.Tenancy |

## Documentation

- [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Identity/Setup.md) — registration, cookie and rule options, role and administrator seeding
- [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Identity/Guide.md) — extending the user model, claims, account state, seeding, tenancy composition
- [API Reference](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Identity/API.md)
- [JC.Identity.Shared](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Identity.Shared/Setup.md) — the shared identity runtime this package builds on

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
