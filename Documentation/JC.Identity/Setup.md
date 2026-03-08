# JC.Identity — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project with JC.Core registered
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

## 0. Add the package

Add a project reference to `JC.Identity`:

```xml
<ProjectReference Include="path/to/JC.Identity/JC.Identity.csproj" />
```

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### DbContext

Your `DbContext` must extend `IdentityDataDbContext<TUser, TRole>`, which provides Identity tables, audit trail, tenant support, and multi-tenancy query filters:

```csharp
public class AppDbContext : IdentityDataDbContext<AppUser, AppRole>
{
    public AppDbContext(DbContextOptions<AppDbContext> options, IUserInfo userInfo) : base(options, userInfo) { }

    public DbSet<Product> Products { get; set; }
}
```

Your user and role models must extend `BaseUser` and `BaseRole`:

```csharp
public class AppUser : BaseUser { }
public class AppRole : BaseRole { }
```

### Services — `Program.cs`

```csharp
// Register ASP.NET Core Identity + JC.Identity services
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>();
```

### Middleware — `Program.cs`

```csharp
var app = builder.Build();

// Registers authentication, user info population, authorisation, and identity middleware — in that order
app.UseIdentity();

// Optional: seed system roles and a default admin user from config
await app.ConfigureAdminAndRolesAsync<AppUser, AppRole, AppDbContext, AppRoles>();
```

### Configuration — `appsettings.json`

Admin seeding requires these keys (only needed if calling `ConfigureAdminAndRolesAsync`):

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

### Defaults

When called with no configuration callbacks, `AddIdentity` sets:

| Default | Value |
|---------|-------|
| Login path | `/Identity/Account/Login` |
| Logout path | `/Identity/Account/Logout` |
| Access denied path | `/Identity/Account/AccessDenied` |
| Password change enforcement | Enabled — users with `RequiresPasswordChange` are redirected |
| Password change route | `/Identity/Account/Manage/SetPassword` |
| Two-factor enforcement | Disabled |
| Two-factor route | `/Identity/Account/Manage/EnableAuthenticator` |
| `IUserInfo` implementation | `UserInfo` (built-in) |
| Claims factory | `DefaultClaimsPrincipalFactory` — adds 12 custom claims from `BaseUser` properties |

`AddIdentity` registers:

| Registration | Lifetime | Description |
|-------------|----------|-------------|
| ASP.NET Core Identity | — | `UserManager<TUser>`, `RoleManager<TRole>`, EF Core stores, default token providers |
| `IUserInfo` → `UserInfo` | Scoped | Current user identity, populated per-request by `UserInfoMiddleware` |
| `IUserClaimsPrincipalFactory<TUser>` → `DefaultClaimsPrincipalFactory` | Scoped | Extends the default claims with 12 custom claims from `BaseUser` |
| `IOptions<IdentityMiddlewareOptions>` | Singleton | Middleware configuration |
| `IRepositoryContext<Tenant>` | Scoped | Repository for multi-tenancy tenants |

`UseIdentity` registers middleware in this order:
1. `UseAuthentication()` — ASP.NET Core authentication
2. `UseUserInfo()` — populates `IUserInfo` from the authenticated user's claims
3. `UseAuthorization()` — ASP.NET Core authorisation
4. `UseIdentityMiddleware()` — enforces disabled account redirects, password change, and 2FA

`IdentityMiddleware` automatically skips static file requests (.css, .js, .jpg, .png, .gif, .svg, .ico, .woff, .woff2, .ttf, .eot, .map, .json, .xml) and unauthenticated requests. For authenticated users it checks, in order:

1. **Disabled accounts** — if `IUserInfo.IsEnabled` is `false`, redirects to the access denied route
2. **Password change** — if `RequirePasswordChange` is enabled and the user's `RequiresPasswordChange` is `true`, redirects to the change password route
3. **Two-factor** — if `EnforceTwoFactor` is enabled and `TwoFactorEnabled` is `false`, redirects to the 2FA setup route

## 2. Full configuration

### AddIdentity — standard registration

Registers ASP.NET Core Identity with EF Core stores, default token providers, the JC.Identity claims factory, `IUserInfo`, and `IdentityMiddlewareOptions`.

```csharp
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>(
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
    configureCookie: cookie =>
    {
        cookie.LoginPath = "/Identity/Account/Login";
        cookie.LogoutPath = "/Identity/Account/Logout";
        cookie.AccessDeniedPath = "/Identity/Account/AccessDenied";
    }
);
```

| Type parameter | Constraint | Description |
|---------------|-----------|-------------|
| `TUser` | `BaseUser` | Your user entity |
| `TRole` | `BaseRole` | Your role entity |
| `TContext` | `DbContext` | Your DbContext (should extend `IdentityDataDbContext<TUser, TRole>`) |

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configureMiddleware` | `Action<IdentityMiddlewareOptions>?` | `null` | Callback to configure identity middleware behaviour. When `null`, all defaults apply |
| `configureCookie` | `Action<CookieAuthenticationOptions>?` | `null` | Callback to configure the authentication cookie. When `null`, sets login/logout/access-denied paths to `/Identity/Account/...` |

#### IdentityMiddlewareOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RequirePasswordChange` | `bool` | `true` | When enabled, users with `RequiresPasswordChange = true` are redirected to the change password route |
| `ChangePasswordRoute` | `string` | `/Identity/Account/Manage/SetPassword` | Route users are redirected to when a password change is required |
| `EnforceTwoFactor` | `bool` | `false` | When enabled, users without 2FA configured are redirected to the 2FA setup route |
| `TwoFactorRoute` | `string` | `/Identity/Account/Manage/EnableAuthenticator` | Route users are redirected to for 2FA setup |
| `AccessDeniedRoute` | `string` | `/Identity/Account/AccessDenied` | Route disabled users are redirected to |
| `LogoutRoute` | `string` | `/Identity/Account/Logout` | Logout route — excluded from middleware enforcement |
| `ErrorRoute` | `string` | `/Error` | Error route — excluded from middleware enforcement |

`ExcludedPaths` is a read-only array automatically built from `AccessDeniedRoute`, `LogoutRoute`, and `ErrorRoute`. Requests to these paths skip all middleware enforcement checks.

#### CookieAuthenticationOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `LoginPath` | `string` | `/Identity/Account/Login` | Where unauthenticated users are redirected |
| `LogoutPath` | `string` | `/Identity/Account/Logout` | Where the sign-out handler is mapped |
| `AccessDeniedPath` | `string` | `/Identity/Account/AccessDenied` | Where users are redirected on 403 |

These are ASP.NET Core's `CookieAuthenticationOptions` — you can set any property on the options object, not just the three above. JC.Identity sets these three defaults if no `configureCookie` callback is provided.

### AddIdentity with custom IUserInfo

If you need additional properties on `IUserInfo`, create a class implementing `IUserInfo` and use the four-type-parameter overload:

```csharp
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext, CustomUserInfo>(
    configureMiddleware: options =>
    {
        options.RequirePasswordChange = true;
        options.EnforceTwoFactor = false;
    },
    configureCookie: cookie =>
    {
        cookie.LoginPath = "/Identity/Account/Login";
    }
);
```

`CustomUserInfo` is registered as the scoped `IUserInfo` implementation instead of the built-in `UserInfo`.

### AddIdentityBase — when ASP.NET Core Identity is already registered

If your project registers ASP.NET Core Identity separately (e.g. with external auth providers), use `AddIdentityBase` to add only the JC.Identity services without re-registering Identity:

```csharp
// Two type parameters — uses built-in UserInfo
builder.Services.AddIdentityBase<AppUser, AppRole>(
    configureMiddleware: options =>
    {
        options.RequirePasswordChange = true;
        options.EnforceTwoFactor = false;
    }
);
```

```csharp
// Three type parameters — uses custom IUserInfo implementation
builder.Services.AddIdentityBase<AppUser, AppRole, CustomUserInfo>(
    configureMiddleware: options =>
    {
        options.RequirePasswordChange = true;
        options.EnforceTwoFactor = false;
    }
);
```

`AddIdentityBase` does **not** accept a `configureCookie` parameter — cookie configuration is the responsibility of whichever code registered ASP.NET Core Identity.

`AddIdentityBase` registers:
- Authorisation and authentication services
- `IUserInfo` (scoped)
- `DefaultClaimsPrincipalFactory` as `IUserClaimsPrincipalFactory<TUser>`
- `IdentityMiddlewareOptions`
- `Tenant` repository context

### Middleware — individual registration

If you need control over middleware ordering, register each component individually instead of calling `UseIdentity()`:

```csharp
app.UseAuthentication();
app.UseUserInfo();           // Must come after UseAuthentication — reads claims from the authenticated user
app.UseAuthorization();
app.UseIdentityMiddleware(); // Must come after UseUserInfo — depends on populated IUserInfo
```

### Admin and role seeding

#### ConfigureAdminAndRolesAsync — combined seeding

Seeds all system roles and creates a default admin user from configuration. Call after `app.Build()`.

```csharp
await app.ConfigureAdminAndRolesAsync<AppUser, AppRole, AppDbContext, AppRoles>(
    setupTenancy: false,
    usernameConfigKey: "Admin:Username",
    emailConfigKey: "Admin:Email",
    passwordConfigKey: "Admin:Password",
    displayNameConfigKey: "Admin:DisplayName",
    additionalRoles: null
);
```

| Type parameter | Constraint | Description |
|---------------|-----------|-------------|
| `TUser` | `BaseUser, new()` | Your user entity — must have a parameterless constructor |
| `TRole` | `BaseRole, new()` | Your role entity — must have a parameterless constructor |
| `TContext` | `IdentityDataDbContext<TUser, TRole>` | Your DbContext |
| `TRoles` | `SystemRoles` | Your roles class extending `SystemRoles` |

| Parameter | Type | Default | Description |
|----------|------|---------|-------------|
| `setupTenancy` | `bool` | `false` | When `true`, finds or creates a "Default Tenant" and assigns it to the admin user |
| `usernameConfigKey` | `string` | `"Admin:Username"` | Configuration key for the admin username |
| `emailConfigKey` | `string` | `"Admin:Email"` | Configuration key for the admin email |
| `passwordConfigKey` | `string` | `"Admin:Password"` | Configuration key for the admin password |
| `displayNameConfigKey` | `string` | `"Admin:DisplayName"` | Configuration key for the admin display name (falls back to "System Administrator") |
| `additionalRoles` | `IEnumerable<string>?` | `null` | Extra roles to assign to the admin beyond the system defaults |

Configuration — `appsettings.json`:

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

`Username`, `Email`, and `Password` are required — throws `InvalidOperationException` if missing. `DisplayName` is optional, defaulting to "System Administrator".

The admin user is created with `EmailConfirmed = true` and `IsEnabled = true`. If `setupTenancy` is `false`, the admin receives both `SystemAdmin` and `Admin` roles. If `setupTenancy` is `true`, the admin receives only `SystemAdmin`.

Seeding is idempotent — if a user with the configured email or username already exists, no changes are made.

#### SeedRolesAsync — roles only

Seeds roles without creating an admin user. Uses reflection to discover all `const string` pairs from your `SystemRoles` subclass (role name matched with `{RoleName}Desc` for descriptions).

```csharp
await app.SeedRolesAsync<AppRoles, AppRole>();
```

#### SeedDefaultAdminAsync — admin only

Creates the admin user without seeding roles. Accepts the same parameters as `ConfigureAdminAndRolesAsync` but without the `TRoles` type parameter.

```csharp
await app.SeedDefaultAdminAsync<AppUser, AppRole, AppDbContext>(
    setupTenancy: false,
    usernameConfigKey: "Admin:Username",
    emailConfigKey: "Admin:Email",
    passwordConfigKey: "Admin:Password",
    displayNameConfigKey: "Admin:DisplayName",
    additionalRoles: ["Editor", "Reviewer"]
);
```

### Defining roles

Extend `SystemRoles` to define application-specific roles. Each role needs a `const string` for the name and a matching `{Name}Desc` constant for the description:

```csharp
public class AppRoles : SystemRoles
{
    public const string Editor = nameof(Editor);
    public const string EditorDesc = "Can create and edit content.";

    public const string Viewer = nameof(Viewer);
    public const string ViewerDesc = "Read-only access to content.";
}
```

`SystemRoles` provides two built-in roles:
- `SystemAdmin` — "Full system administrator with access to tenant management and assignment."
- `Admin` — "Administrator with access to all features within their tenant."

### IdentityDataDbContext — what it provides

`IdentityDataDbContext<TUser, TRole>` extends `IdentityDbContext<TUser, TRole, string>` and implements `IDataDbContext`. It provides:

- All ASP.NET Core Identity tables (users, roles, claims, tokens, logins)
- `DbSet<AuditEntry> AuditEntries` — audit trail with two-phase save (same as `DataDbContext`)
- `DbSet<Tenant> Tenants` — multi-tenancy tenants
- `CurrentTenantId` — read from `IUserInfo.TenantId`, re-evaluated per query by EF Core
- Global query filters on all entities implementing `IMultiTenancy` — automatically scoped to the current tenant
- Entity configuration: `TUser.TenantId` max length 36, `Tenant` with indexed `Domain` (max 256), `Tenant.Name` required (max 256)

The constructor requires `IUserInfo` for tenant query filter resolution:

```csharp
public AppDbContext(DbContextOptions<AppDbContext> options, IUserInfo userInfo) : base(options, userInfo) { }
```

## 3. Apply migrations

JC.Identity introduces tables for Identity (users, roles, claims, tokens, logins), audit entries, and tenants. Generate and apply the initial migration:

```bash
dotnet ef migrations add InitialIdentity --project YourApp
dotnet ef database update --project YourApp
```

Alternatively, generate the migration and apply it programmatically at startup:

```bash
dotnet ef migrations add InitialIdentity --project YourApp
```

```csharp
await app.Services.MigrateDatabaseAsync<AppDbContext>();
```

## 4. Verify

1. Run the application.
2. Navigate to a page protected with `[Authorize]` — you should be redirected to `/Identity/Account/Login`.
3. If admin seeding is configured, log in with the credentials from `appsettings.json`.

## Next steps

- [Guide](Guide.md) — multi-tenancy, custom `IUserInfo`, tenant query filters, and `UserInfoMiddleware` behaviour.
- [API Reference](API.md)
