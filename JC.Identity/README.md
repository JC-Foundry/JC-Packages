# JC.Identity

ASP.NET Core Identity wired into the JC suite. Identity, tenants and the audit trail share one `DbContext`, so every change is attributed to the signed-in user; multi-tenancy is enforced by global query filters; and middleware projects the user onto `IUserInfo` — the contract the rest of the suite depends on.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.Identity/JC.Identity.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

## Prerequisites

- .NET 9.0 SDK, and an ASP.NET Core project
- **JC.Core**, registered with `AddCore<AppDbContext>()`
- A `DbContext` extending `IdentityDataDbContext<TUser, TRole>`
- User and role types extending `BaseUser` and `BaseRole`

## Quick start

### Data — `AppDbContext`

```csharp
public class AppDbContext(DbContextOptions options, IUserInfo userInfo)
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

// Optional: seed system roles and a default admin from configuration
await app.ConfigureAdminAndRolesAsync<AppUser, AppRole, AppDbContext, AppRoles>(setupTenancy: true);
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

## Feature areas

### IUserInfo

The reason this package matters to the others. Middleware reads the authenticated principal's claims into a scoped `IUserInfo`, which JC.Core uses to attribute audit rows, JC.FileStorage uses to scope files to a tenant, and JC.Communication requires before it will register notifications or messaging at all.

```csharp
public class DashboardModel(IUserInfo userInfo) : PageModel
{
    public void OnGet()
    {
        var id = userInfo.UserId;
        var tenant = userInfo.TenantId;
    }
}
```

Without JC.Identity there is no `IUserInfo` implementation, and the suite falls back to a placeholder identifier — valid, but unattributed.

### Claims

`DefaultClaimsPrincipalFactory` projects twelve fields from `BaseUser` onto the principal — email and phone confirmation, two-factor and lockout state, tenant, display name, last login, enabled state and whether a password change is due. `UserInfoMiddleware` reads them back, so `IUserInfo` costs no database round trip per request.

The claim type constants are on `DefaultClaims`.

### Multi-tenancy

Entities implementing `IMultiTenancy` are filtered globally by the current tenant:

```csharp
public class Order : AuditModel, IMultiTenancy
{
    public string? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
}
```

The filter reads the tenant **per query** rather than caching it when the model is built, so a request for one tenant cannot see another's rows even though the model is compiled once.

`Tenant` and `IMultiTenancy` live in JC.Core, so a package can define a tenant-scoped entity without referencing JC.Identity; the filters that enforce it live here.

### Identity rules middleware

Enforces business rules on every authenticated request, skipping static files and configured paths:

- Disabled accounts are redirected to the access-denied route
- Users flagged for a password change are redirected until they complete it
- Two-factor enrolment can be required, off by default

```csharp
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>(
    configureMiddleware: options =>
    {
        options.RequirePasswordChange = true;
        options.EnforceTwoFactor = false;
    });
```

### Roles and seeding

Extend `SystemRoles` with a name and a matching `{Name}Desc` constant per role:

```csharp
public class AppRoles : SystemRoles
{
    public const string Editor = nameof(Editor);
    public const string EditorDesc = "Can create and edit content.";
}
```

`SeedRolesAsync` discovers them by reflection. `SeedDefaultAdminAsync` creates the administrator alone, and `ConfigureAdminAndRolesAsync` does both. All three are idempotent — an existing user or role is left untouched.

### Custom IUserInfo

Need more on it? Implement `IUserInfo` and use the four-type-parameter overload:

```csharp
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext, CustomUserInfo>();
```

### Identity already registered

Where the application registers ASP.NET Core Identity itself — for external providers, say — `AddIdentityBase` adds only the JC.Identity services and leaves Identity and its cookie configuration alone.

## Defaults

| Default | Value |
|---------|-------|
| Login / logout / access denied paths | `/Identity/Account/Login`, `/Logout`, `/AccessDenied` |
| Password change enforcement | Enabled, routing to `/Identity/Account/Manage/SetPassword` |
| Two-factor enforcement | Disabled |
| `IUserInfo` implementation | Built-in `UserInfo`, scoped |
| Claims factory | `DefaultClaimsPrincipalFactory` — twelve custom claims |
| `UseIdentity` order | Authentication → `UseUserInfo` → authorisation → identity rules |
| Admin roles when seeded | `SystemAdmin` and `Admin`, or `SystemAdmin` alone when tenancy is set up |

## Documentation

- [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Identity/Setup.md) — registration, middleware options, role and admin seeding
- [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Identity/Guide.md) — claims, multi-tenancy, tenant settings, extending the user model
- [API Reference](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Identity/API.md)

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
