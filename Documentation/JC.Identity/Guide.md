# JC.Identity — Guide

Covers user information access, custom claims, role management, middleware behaviour, multi-tenancy with tenant query filters, tenant settings, and extending the user model. See [Setup](Setup.md) for registration.

## Accessing user information

### IUserInfo in services

`IUserInfo` is scoped per-request and populated automatically by `UserInfoMiddleware`. Inject it anywhere you need the current user's details:

```csharp
public class DashboardService(IUserInfo userInfo)
{
    public string GetWelcomeMessage()
    {
        return $"Welcome, {userInfo.DisplayName ?? userInfo.Username}";
    }

    public bool CanManageTenants()
    {
        return userInfo.IsInRole(SystemRoles.SystemAdmin);
    }
}
```

### IUserInfo in controllers and Razor pages

```csharp
public class ProfileController(IUserInfo userInfo) : Controller
{
    public IActionResult Index()
    {
        ViewBag.Username = userInfo.Username;
        ViewBag.Email = userInfo.Email;
        ViewBag.DisplayName = userInfo.DisplayName;
        ViewBag.TenantId = userInfo.TenantId;
        ViewBag.Roles = userInfo.Roles;
        ViewBag.LastLogin = userInfo.LastLoginUtc;

        return View();
    }
}
```

### Available properties

`IUserInfo` exposes everything from `BaseUser` plus request-scoped metadata:

| Property | Type | Description |
|----------|------|-------------|
| `UserId` | `string` | Unique user identifier |
| `Username` | `string` | Username |
| `Email` | `string` | Email address |
| `EmailConfirmed` | `bool` | Whether email is confirmed |
| `PhoneNumber` | `string?` | Phone number |
| `PhoneNumberConfirmed` | `bool` | Whether phone is confirmed |
| `TwoFactorEnabled` | `bool` | Whether 2FA is configured |
| `LockoutEnabled` | `bool` | Whether lockout is enabled |
| `LockoutEnd` | `DateTime?` | When lockout expires |
| `AccessFailedCount` | `int` | Consecutive failed login attempts |
| `TenantId` | `string?` | Current tenant identifier |
| `DisplayName` | `string?` | User's display name |
| `LastLoginUtc` | `DateTime?` | UTC timestamp of last login |
| `IsEnabled` | `bool` | Whether the account is active |
| `RequiresPasswordChange` | `bool` | Whether a password change is pending |
| `IsSetup` | `bool` | Whether the middleware has populated this instance |
| `MultiTenancyEnabled` | `bool` | `true` if `TenantId` is populated |
| `Roles` | `IReadOnlyList<string>` | Role names from claims |
| `Claims` | `IReadOnlyList<Claim>` | All claims from the authenticated user |

### Checking roles

```csharp
// Check specific role
if (userInfo.IsInRole("Editor"))
{
    // User has the Editor role
}

// Check built-in system roles
if (userInfo.IsInRole(SystemRoles.SystemAdmin))
{
    // Full system admin
}

if (userInfo.IsInRole(SystemRoles.Admin))
{
    // Tenant-scoped admin
}
```

`IsInRole` checks both the `Roles` collection and role claims on the `Claims` list. Returns `false` for null or empty role names.

### Unauthenticated requests

`UserInfoMiddleware` assigns fallback identities based on the authentication state:

| Scenario | `UserId` | `Username` | `Email` |
|----------|----------|------------|---------|
| No identity present | `"System__ID"` | `"System"` | `"<SYSTEM@EMAIL>"` |
| Identity present, not authenticated | `"Unknown__ID"` | `"Unknown"` | `"<UNKNOWN@EMAIL>"` |

The "no identity present" case occurs when no authentication middleware has run. The "not authenticated" case is the typical anonymous request — authentication middleware has run but the user hasn't logged in. Both cases ensure audit trails and any code reading `IUserInfo` always have a user identity.

**Nuance:** Before the middleware runs (e.g. code executing before `UseUserInfo()`), properties have their initial defaults: `UserId = "Unknown__ID"`, `Username = "Unknown"`, `Email = "<UNKNOWN@EMAIL>"`.

## Custom IUserInfo

### When to extend

Use a custom `IUserInfo` when you need additional per-request properties beyond what the built-in `UserInfo` provides. For example, if your user model has extra fields that should be available throughout the request:

```csharp
public class AppUser : BaseUser
{
    public string? Department { get; set; }
    public string? ProfileImageUrl { get; set; }
}
```

### Creating a custom implementation

Implement `IUserInfo` and add your custom properties:

```csharp
public class AppUserInfo : UserInfo
{
    public string? Department { get; set; }
    public string? ProfileImageUrl { get; set; }
}
```

Register it using the four-type-parameter overload:

```csharp
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext, AppUserInfo>();
```

**Nuance:** `UserInfoMiddleware` populates the standard `IUserInfo` properties from claims. Your custom properties won't be populated automatically — you'll need additional middleware or a service to populate them (e.g. by querying the database using the `UserId`).

## Claims pipeline

### How claims are added

`DefaultClaimsPrincipalFactory` extends the standard ASP.NET Core Identity claims with 12 custom claims from `BaseUser` properties. These are added when the authentication cookie is created (at login):

| Claim type | Source property | Format |
|-----------|----------------|--------|
| `email_confirmed` | `EmailConfirmed` | `"True"` / `"False"` |
| `phone_number` | `PhoneNumber` | String or empty |
| `phone_number_confirmed` | `PhoneNumberConfirmed` | `"True"` / `"False"` |
| `two_factor_enabled` | `TwoFactorEnabled` | `"True"` / `"False"` |
| `lockout_enabled` | `LockoutEnabled` | `"True"` / `"False"` |
| `lockout_end` | `LockoutEnd` | ISO 8601 or empty |
| `access_failed_count` | `AccessFailedCount` | Integer string |
| `tenant_id` | `TenantId` | String or empty |
| `display_name` | `DisplayName` | String or empty |
| `last_login_utc` | `LastLoginUtc` | ISO 8601 or empty |
| `is_enabled` | `IsEnabled` | `"True"` / `"False"` |
| `require_password_change` | `RequirePasswordChange` | `"True"` / `"False"` |

`UserInfoMiddleware` then reads these claims back into `IUserInfo` on each request.

**Nuance:** Claims are baked into the authentication cookie at login time. If you change a user's `IsEnabled`, `TenantId`, or any other property in the database, the change won't take effect until the cookie is refreshed. You can force a refresh without requiring the user to log out and back in by calling `SignInManager<TUser>.RefreshSignInAsync(user)` — this regenerates the claims and rewrites the cookie.

### Claim type constants

The `DefaultClaims` class provides `const string` fields for all 12 custom claim types (e.g. `DefaultClaims.TenantId`, `DefaultClaims.IsEnabled`). You should not need to read claims directly — `IUserInfo` is a scoped service populated per-request by `UserInfoMiddleware`, so inject it wherever you need user data. The constants exist primarily as an implementation detail of the claims pipeline and are documented here for completeness. Boolean claims are stored as `"True"` / `"False"` (from `bool.ToString()`).

## Role management

### Defining application roles

Extend `SystemRoles` with your application-specific roles. Each role needs a `const string` name and a matching `{Name}Desc` description:

```csharp
public class AppRoles : SystemRoles
{
    public const string Editor = nameof(Editor);
    public const string EditorDesc = "Can create and edit content.";

    public const string Viewer = nameof(Viewer);
    public const string ViewerDesc = "Read-only access to content.";

    public const string Moderator = nameof(Moderator);
    public const string ModeratorDesc = "Can manage user-generated content and comments.";
}
```

`SystemRoles` provides two built-in roles:

- `SystemAdmin` — full system administrator with tenant management access
- `Admin` — administrator scoped to their tenant

### Discovering roles at runtime

`SystemRoles.GetAllRoles<T>()` uses reflection to discover all role/description pairs:

```csharp
var roles = SystemRoles.GetAllRoles<AppRoles>();
// [
//   ("SystemAdmin", "Full system administrator with access to tenant management and assignment."),
//   ("Admin", "Administrator with access to all features within their tenant."),
//   ("Editor", "Can create and edit content."),
//   ("Viewer", "Read-only access to content."),
//   ("Moderator", "Can manage user-generated content and comments.")
// ]
```

This is used internally by `SeedRolesAsync` but is available for building role management UIs, dropdowns, etc.

**Nuance:** The discovery relies on naming convention — a role constant `Foo` must have a matching `FooDesc` constant. If the description constant is missing, the role is still discovered but with an empty description.

### Using roles for authorisation

```csharp
// Attribute-based — use your role constants (they're const strings, so valid in attributes)
[Authorize(Roles = $"{AppRoles.Editor},{AppRoles.Admin}")]
public class ArticleController : Controller
{
    // Only users with Editor or Admin role can access
}
```

```csharp
// Programmatic check
if (userInfo.IsInRole(AppRoles.Editor))
{
    // Allow editing
}
```

Using your role class constants avoids magic strings in both cases.

### Admin seeding and role assignment

When `ConfigureAdminAndRolesAsync` runs, the admin user receives different roles depending on tenancy:

- **Without tenancy** (`setupTenancy: false`): admin gets both `SystemAdmin` and `Admin`
- **With tenancy** (`setupTenancy: true`): admin gets only `SystemAdmin`

The logic is that `Admin` is tenant-scoped, and a system admin managing multiple tenants shouldn't be bound to the default tenant's admin role.

## Middleware behaviour

### Enforcement order

`IdentityMiddleware` checks three business rules in a strict order for authenticated users:

1. **Disabled account** — if `IsEnabled` is `false`, redirect to the access denied route. This is checked first because a disabled user shouldn't reach any other page.
2. **Password change** — if `RequirePasswordChange` is enabled in options and the user's `RequiresPasswordChange` is `true`, redirect to the change password route.
3. **Two-factor authentication** — if `EnforceTwoFactor` is enabled and `TwoFactorEnabled` is `false`, redirect to the 2FA setup route.

Password change is enforced before 2FA — the user must set a proper password before being asked to configure two-factor authentication.

### What gets skipped

The middleware automatically passes through:

- **Static files** — requests for `.css`, `.js`, `.jpg`, `.jpeg`, `.png`, `.gif`, `.svg`, `.ico`, `.woff`, `.woff2`, `.ttf`, `.eot`, `.map`, `.json`, `.xml`
- **Unauthenticated requests** — anonymous users are handled by `[Authorize]` attributes and cookie redirects, not the identity middleware
- **Excluded paths** — the access denied route, logout route, and error route (from `IdentityMiddlewareOptions.ExcludedPaths`)

**Nuance:** The excluded paths check uses `StartsWith`, so `/Identity/Account/Logout` also excludes `/Identity/Account/Logout/Confirm` and any sub-paths.

**Nuance:** The password change and 2FA redirects also use `StartsWith` to check if the user is already on the target route. This prevents infinite redirect loops — if the user is already on the password change route you configured, the redirect is skipped.

### Controlling middleware individually

If you need to insert other middleware between the identity components:

```csharp
app.UseAuthentication();

// Your custom middleware that needs authentication but not IUserInfo
app.UseMyCustomMiddleware();

app.UseUserInfo();       // Must come after UseAuthentication
app.UseAuthorization();
app.UseIdentityMiddleware(); // Must come after UseUserInfo
```

## Multi-tenancy

### Making entities tenant-aware

Implement `IMultiTenancy` on any entity that should be scoped to a tenant:

```csharp
public class Project : AuditModel, IMultiTenancy
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // IMultiTenancy
    public string? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
}
```

Once your entity implements `IMultiTenancy`, `IdentityDataDbContext` automatically applies a global query filter. All queries on that entity are scoped to the current user's tenant — no manual filtering needed.

### How the query filter works

The filter uses the `CurrentTenantId` property on the DbContext, which reads from `IUserInfo.TenantId`:

- **User has a tenant** (`TenantId = "abc123"`): queries return only records where `TenantId == "abc123"`
- **User has no tenant** (`TenantId` is null/empty): queries return only records where `TenantId` is null

This means tenant-less records (where `TenantId` is null) act as shared/global data — visible to users without a tenant, but not to tenant-scoped users.

### Querying across tenants

System administrators can bypass tenant filters using `AllTenants`:

```csharp
public class AdminProjectService(
    IRepositoryContext<Project> projects,
    IUserInfo userInfo)
{
    public async Task<List<Project>> GetAllProjectsAcrossTenantsAsync()
    {
        // SystemAdmin users see all tenants; others see only their own
        return await projects.AsQueryable()
            .AllTenants(userInfo)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
```

`AllTenants` calls `IgnoreQueryFilters()` for `SystemAdmin` users. For all other users, the query is returned unmodified (tenant filter still applies).

**Nuance:** `IgnoreQueryFilters()` removes all global query filters, not just the tenant filter. If you have other global filters (e.g. soft-delete), they will also be bypassed. Apply additional `.Where()` clauses to compensate.

### Managing tenants

The `Tenant` entity extends `AuditModel`, so it has full audit trail support and works with the repository pattern:

```csharp
public class TenantService(IRepositoryContext<Tenant> tenants)
{
    public async Task<Tenant> CreateAsync(string name, string? domain = null)
    {
        var tenant = new Tenant
        {
            Name = name,
            Domain = domain,
            Description = $"Tenant for {name}"
        };

        return await tenants.AddAsync(tenant);
    }

    public async Task<List<Tenant>> GetAllAsync()
    {
        return await tenants.GetAllAsync(t => !t.IsDeleted);
    }

    public async Task<Tenant?> GetByDomainAsync(string domain)
    {
        return await tenants.AsQueryable()
            .FilterDeleted(DeletedQueryType.OnlyActive)
            .FirstOrDefaultAsync(t => t.Domain == domain);
    }
}
```

### Tenant properties

```csharp
tenant.Id;            // GUID string, auto-generated
tenant.Name;          // Required tenant name
tenant.Description;   // Optional description
tenant.Domain;        // Optional domain (indexed for lookup)
tenant.MaxUsers;      // Optional user limit (uint)
tenant.ExpiryDateUtc; // Optional expiry date
tenant.Settings;      // JSON string — managed via methods below
```

Because `Tenant` extends `AuditModel`, it also has `CreatedById`, `CreatedUtc`, `LastModifiedById`, `LastModifiedUtc`, `IsDeleted`, and all other audit fields.

### Tenant settings

Tenants have a JSON-based settings store for key-value configuration:

```csharp
// Set individual settings
tenant.SetSetting("theme", "dark");
tenant.SetSetting("max-projects", "50");
tenant.SetSetting("beta-features", "true", isActive: true);

// Read settings
var settings = tenant.GetSettings();
// [
//   TenantSettings { Key = "theme", Value = "dark", IsActive = true },
//   TenantSettings { Key = "max-projects", Value = "50", IsActive = true },
//   TenantSettings { Key = "beta-features", Value = "true", IsActive = true }
// ]

// Replace all settings at once
tenant.SetSettings(new List<TenantSettings>
{
    new() { Key = "theme", Value = "light", IsActive = true },
    new() { Key = "max-projects", Value = "100", IsActive = true }
});
```

`SetSetting` adds a new setting if the key doesn't exist, or updates the existing one if it does. The `isActive` flag allows disabling a setting without removing it.

**Nuance:** Settings are stored as a single JSON string column. After modifying settings, you must save the tenant entity for changes to persist:

```csharp
tenant.SetSetting("theme", "dark");
await tenants.UpdateAsync(tenant); // Persists the JSON change
```

### Assigning users to tenants

Set the `TenantId` on your user entity:

```csharp
public async Task AssignUserToTenantAsync(UserManager<AppUser> userManager, string userId, string tenantId)
{
    var user = await userManager.FindByIdAsync(userId);
    if (user is null) return;

    user.TenantId = tenantId;
    await userManager.UpdateAsync(user);
}
```

**Nuance:** Changing a user's `TenantId` requires a cookie refresh to take effect — see the [claims pipeline](#claims-pipeline) nuance on `RefreshSignInAsync`.

## Extending the user model

### Adding properties to BaseUser

```csharp
public class AppUser : BaseUser
{
    public string? Department { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
}
```

`BaseUser` extends `IdentityUser` and adds: `TenantId`, `DisplayName`, `LastLoginUtc`, `IsEnabled`, and `RequirePasswordChange`. Your custom properties sit alongside these.


### Adding properties to BaseRole

```csharp
public class AppRole : BaseRole
{
    public string? Colour { get; set; }
    public int SortOrder { get; set; }
}
```

`BaseRole` extends `IdentityRole` and adds a `Description` property. Custom properties are available wherever you work with roles.

### Tracking last login

`BaseUser` has a `LastLoginUtc` property that isn't populated automatically — you need to update it in your login flow:

```csharp
public async Task<IActionResult> OnPostAsync(string email, string password)
{
    var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: true);

    if (result.Succeeded)
    {
        var user = await _userManager.FindByEmailAsync(email);
        user!.LastLoginUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    // ...
}
```

The value is then included in the claims via `DefaultClaimsPrincipalFactory` and available as `IUserInfo.LastLoginUtc` for subsequent requests.
