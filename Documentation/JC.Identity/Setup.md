# JC.Identity — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project with JC.Core registered
- A database provider — see [JC.SqlServer](../../JC.SqlServer/README.md) or [JC.MySql](../../JC.MySql/README.md)
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

JC.Identity is one of two identity authorities built on the same runtime. Anything authority-agnostic — the `IUserInfo` implementation, the claims projection, the account rules and their options, and the two-factor helper — lives in **JC.Identity.Shared** and is documented in [JC.Identity.Shared — Setup](../JC.Identity.Shared/Setup.md). This document covers only what is specific to local ASP.NET Core Identity, including its roles.

## 0. Add the package

Add a project reference to `JC.Identity`:

```xml
<ProjectReference Include="path/to/JC.Identity/JC.Identity.csproj" />
```

`JC.Identity` references `JC.Identity.Shared` and `JC.Identity.Shared.Web`, so both arrive with it and neither needs adding separately.

Tenancy is **not** included. `JC.Identity` has no reference to `JC.Tenancy`; see [Adding tenant filtering](#adding-tenant-filtering) below.

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### Entities

Your user and role entities extend `BaseUser` and `BaseRole`:

```csharp
public class AppUser : BaseUser;
public class AppRole : BaseRole;
```

`BaseUser` extends `IdentityUser` and implements JC.Core's `IApplicationUser`, adding `DisplayName`, `LastLoginUtc`, `RegistrationUtc`, `IsEnabled`, `RequirePasswordChange` and a `TenantId` column. `BaseRole` extends `IdentityRole` with a `Description`.

### DbContext

Your `DbContext` extends `IdentityDataDbContext<TUser, TRole>`, which provides the ASP.NET Identity tables and the JC.Core audit trail:

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options, IUserInfo userInfo)
    : IdentityDataDbContext<AppUser, AppRole>(options, userInfo)
{
    public DbSet<Product> Products { get; set; }
}
```

The `IUserInfo` parameter is what attributes audit entries to the signed-in user.

### Services — `Program.cs`

```csharp
// Your DbContext, registered through whichever provider package you use
builder.Services.AddSqlServerDatabase<AppDbContext>(builder.Configuration, "YourApp");

builder.Services.AddCore<AppDbContext>();

// Registers ASP.NET Core Identity, EF stores, token providers, the application cookie,
// and the shared identity runtime
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>();
```

### Middleware — `Program.cs`

```csharp
var app = builder.Build();

// Authentication, user info projection, authorisation, identity rules — in that order
app.UseIdentity();

// Optional: seed system roles and a default administrator from configuration
var admin = await app.ConfigureAdminAndRolesAsync<AppUser, AppRole, AppRoles>();
```

### Configuration — `appsettings.json`

Only required if you call `ConfigureAdminAndRolesAsync` or `SeedDefaultAdminAsync`:

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

Called with no arguments, `AddIdentity` gives you:

| Default | Value |
|---------|-------|
| Login path | `/Identity/Account/Login` |
| Logout path | `/Identity/Account/Logout` |
| Access denied path | `/Identity/Account/AccessDenied` |
| `IUserInfo` implementation | `UserInfo` (built-in) |
| `IUserInfo.Authority` | `IdentityAuthority.Local` once authenticated, `None` otherwise |
| Claim types read | ASP.NET Identity's own, copied from `IdentityOptions.ClaimsIdentity` |
| Claims factory | `DefaultClaimsPrincipalFactory` — adds 13 claims from `BaseUser` |
| Account rule routes and switches | `IdentityMiddlewareOptions` defaults — see [JC.Identity.Shared](../JC.Identity.Shared/Setup.md#identitymiddlewareoptions) |
| Tenant filtering | **None.** Opt in with JC.Tenancy |

`AddIdentity` registers:

| Registration | Lifetime | Description |
|--------------|----------|-------------|
| ASP.NET Core Identity | — | `UserManager<TUser>`, `RoleManager<TRole>`, EF Core stores, default token providers |
| Authentication and authorisation | — | `AddAuthentication()` and `AddAuthorization()` with no scheme configured beyond the Identity cookie |
| `IUserClaimsPrincipalFactory<TUser>` → `DefaultClaimsPrincipalFactory<TUser, TRole>` | Scoped | Adds the 13 `DefaultClaims` values to the principal |
| `IConfigureOptions<IdentityProjectionOptions>` | Singleton | Points the projection at ASP.NET Identity's claim types and states the authority |
| Everything `AddSharedIdentityServices` registers | — | The scoped `IUserInfo` and both options types — see [JC.Identity.Shared](../JC.Identity.Shared/Setup.md#addsharedidentityservices) |

`UseIdentity` registers middleware in this order:

1. `UseAuthentication()` — ASP.NET Core authentication
2. `UseUserInfo()` — projects the principal's claims onto `IUserInfo`
3. `UseAuthorization()` — ASP.NET Core authorisation
4. `UseIdentityMiddleware()` — enforces disabled accounts, password changes and two-factor

The order matters in both directions: `UseUserInfo` must follow authentication because it reads claims, and must precede `UseIdentityMiddleware` because that enforces rules against what it produced.

Both middlewares come from JC.Identity.Shared.Web, and the rules they apply — which paths are skipped, which checks run and in what order — are described in [JC.Identity.Shared](../JC.Identity.Shared/Setup.md#enforcing-the-account-rules-without-middleware).

## 2. Full configuration

### AddIdentity — standard registration

The recommended entry point. Registers ASP.NET Core Identity with EF Core stores and default token providers, configures the application cookie, and registers the JC.Identity and shared services.

```csharp
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>(
    configureMiddleware: options =>
    {
        options.RequirePasswordChange = true;
        options.EnforceTwoFactor = false;
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
|----------------|-----------|-------------|
| `TUser` | `BaseUser` | Your user entity |
| `TRole` | `BaseRole` | Your role entity |
| `TContext` | `IdentityDataDbContext<TUser, TRole>` | The context holding the Identity stores |

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configureMiddleware` | `Action<IdentityMiddlewareOptions>?` | `null` | Passed straight through to the shared runtime. Every property and default is in [JC.Identity.Shared](../JC.Identity.Shared/Setup.md#identitymiddlewareoptions) |
| `configureCookie` | `Action<CookieAuthenticationOptions>?` | `null` | Configures the application cookie. When `null`, the three JC.Identity path defaults below are applied |

**The cookie callback replaces the defaults rather than adding to them.** Supplying `configureCookie` means JC.Identity applies none of its own paths, so set every path you need inside the callback.

There is no `configureProjection` parameter here — JC.Identity fills `IdentityProjectionOptions` in itself, as described next.

#### CookieAuthenticationOptions

**Namespace:** `Microsoft.AspNetCore.Authentication.Cookies`

| Property | Type | Default applied by JC.Identity | Description |
|----------|------|-------------------------------|-------------|
| `LoginPath` | `string` | `/Identity/Account/Login` | Where unauthenticated users are redirected |
| `LogoutPath` | `string` | `/Identity/Account/Logout` | Where the sign-out handler is mapped |
| `AccessDeniedPath` | `string` | `/Identity/Account/AccessDenied` | Where users are redirected on 403 |

This is ASP.NET Core's own options type, applied through `ConfigureApplicationCookie` — every property on it is available, not only the three JC.Identity sets by default.

### Claim types and authority

`IdentityProjectionOptions` tells the shared projection which claims to read and which authority to stamp. Its properties and their own defaults are documented in [JC.Identity.Shared](../JC.Identity.Shared/Setup.md#identityprojectionoptions); what follows is what JC.Identity does with it.

JC.Identity registers an `IConfigureOptions<IdentityProjectionOptions>` that:

- copies `UserIdClaimType`, `EmailClaimType` and `RoleClaimType` from `IdentityOptions.ClaimsIdentity`, so the projection reads whatever ASP.NET Identity is actually configured to write;
- sets `Authority` to `IdentityAuthority.Local`.

It is registered as `IConfigureOptions<>` rather than copied at registration time deliberately. `IdentityOptions` has not been configured when `AddIdentity` runs — the consuming application's own `Configure<IdentityOptions>` calls may come afterwards — so copying eagerly would capture the defaults and silently discard the customisation.

**There is deliberately no `configureProjection` hook on `AddIdentity` or `AddIdentityServices`.** The claim types are not this package's to choose — ASP.NET Identity's claims factory writes whatever `IdentityOptions.ClaimsIdentity` says, and a separate hook would let the projection read a claim nothing is writing.

To change a claim type, change it where Identity itself reads it. The projection follows automatically:

```csharp
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.ClaimsIdentity.UserIdClaimType = "sub";
});
```

`Authority` is not configurable here at all. It is `Local` by definition for this package; an application supplying identity from elsewhere registers that authority's own runtime instead — see [JC.Identity.Shared](../JC.Identity.Shared/Setup.md#addsharedidentityservices), which does expose `configureProjection`.

### AddIdentity with a custom IUserInfo

To carry extra properties on the current user, derive from `UserInfoBase` and use the four-type-parameter overload:

```csharp
public class AppUserInfo : UserInfoBase
{
    public string? DepartmentId { get; set; }
}

builder.Services.AddIdentity<AppUser, AppRole, AppDbContext, AppUserInfo>();
```

| Type parameter | Constraint | Description |
|----------------|-----------|-------------|
| `TUserInfo` | `class, IUserInfo` | Registered as the scoped `IUserInfo` instead of `UserInfo` |

`TUser`, `TRole` and `TContext` carry the same constraints as the three-parameter overload, and both callbacks behave identically.

`UserInfoBase`, its defaults and its projection behaviour are covered in [JC.Identity.Shared](../JC.Identity.Shared/Setup.md#userinfobase). The built-in `UserInfo` derives from it and adds the two `BaseUser`-shaped constructors, which also set `TenantId` from `BaseUser.TenantId` and stamp `Authority` as `Local` — the only two things the shared base deliberately leaves alone.

### AddIdentityServices — when ASP.NET Core Identity is already registered

Registers only the JC.Identity half. Use it when Identity is configured elsewhere, for instance alongside external authentication providers.

```csharp
// Two type parameters — uses the built-in UserInfo
builder.Services.AddIdentityServices<AppUser, AppRole>();

// Three type parameters — uses a custom IUserInfo
builder.Services.AddIdentityServices<AppUser, AppRole, AppUserInfo>();
```

Both overloads take an optional `configureMiddleware` callback, passed through to the shared runtime.

It registers:

- `AddAuthorization()` and `AddAuthentication()`
- `AddSharedIdentityServices<TUserInfo>` — see [JC.Identity.Shared](../JC.Identity.Shared/Setup.md#addsharedidentityservices)
- The `IConfigureOptions<IdentityProjectionOptions>` described above
- `DefaultClaimsPrincipalFactory<TUser, TRole>` as `IUserClaimsPrincipalFactory<TUser>`

It does **not** call `AddEntityFrameworkStores`, `AddDefaultTokenProviders` or `ConfigureApplicationCookie`. There is no `configureCookie` parameter — the cookie belongs to whichever code registered ASP.NET Core Identity.

### Middleware — individual registration

If you need control over ordering, register each component instead of calling `UseIdentity()`:

```csharp
app.UseAuthentication();
app.UseUserInfo();           // after UseAuthentication — reads the principal's claims
app.UseAuthorization();
app.UseIdentityMiddleware(); // after UseUserInfo — enforces rules against what it produced
```

`UseUserInfo` and `UseIdentityMiddleware` belong to JC.Identity.Shared.Web and are documented in [JC.Identity.Shared](../JC.Identity.Shared/Setup.md#adding-the-aspnet-core-middleware). `UseIdentity` lives in `JC.Identity.Extensions` and composes all four.

### Admin and role seeding

#### ConfigureAdminAndRolesAsync — combined seeding

Seeds the system roles, then the default administrator. Call after `app.Build()`.

```csharp
var admin = await app.ConfigureAdminAndRolesAsync<AppUser, AppRole, AppRoles>(
    assignAdminRole: true,
    usernameConfigKey: "Admin:Username",
    emailConfigKey: "Admin:Email",
    passwordConfigKey: "Admin:Password",
    displayNameConfigKey: "Admin:DisplayName",
    additionalRoles: null
);
```

| Type parameter | Constraint | Description |
|----------------|-----------|-------------|
| `TUser` | `BaseUser, new()` | Your user entity — needs a parameterless constructor |
| `TRole` | `BaseRole, new()` | Your role entity — needs a parameterless constructor |
| `TRoles` | `SystemRoles` | Your roles class extending `SystemRoles` |

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `assignAdminRole` | `bool` | `true` | Whether the administrator also receives `Admin` alongside `SystemAdmin` |
| `usernameConfigKey` | `string` | `"Admin:Username"` | Configuration key for the username |
| `emailConfigKey` | `string` | `"Admin:Email"` | Configuration key for the email address |
| `passwordConfigKey` | `string` | `"Admin:Password"` | Configuration key for the password |
| `displayNameConfigKey` | `string` | `"Admin:DisplayName"` | Configuration key for the display name |
| `additionalRoles` | `IEnumerable<string>?` | `null` | Further roles to assign beyond the system ones |

**Returns** the administrator — newly created, or the existing account where one already matched the configured email or username. `null` only where creation was attempted and failed, in which case the reason is logged.

Returning the existing account rather than nothing is what keeps follow-on setup idempotent: a first run that created the user but failed a later step corrects itself on the next start.

`Username`, `Email` and `Password` are required — a missing key throws `InvalidOperationException` naming the key. `DisplayName` is optional and falls back to `"System Administrator"`.

The administrator is created with `EmailConfirmed = true`, `IsEnabled = true` and `RegistrationUtc` set to the current UTC time. Roles are assigned in order: `SystemAdmin`, then `Admin` when `assignAdminRole` is `true`, then each entry in `additionalRoles`. A role that fails to assign is logged and does not stop the rest.

Assigning a tenant to the administrator is JC.Tenancy's job — see [Adding tenant filtering](#adding-tenant-filtering).

#### SeedRolesAsync — roles only

```csharp
await app.SeedRolesAsync<AppRoles, AppRole>();
```

| Type parameter | Constraint | Description |
|----------------|-----------|-------------|
| `TRoles` | `SystemRoles` | The roles class to read constants from |
| `TRole` | `BaseRole, new()` | Your role entity |

Creates any role discovered on `TRoles` that does not already exist, with its description. Roles that exist are left untouched. Returns the `IApplicationBuilder` for chaining.

#### SeedDefaultAdminAsync — administrator only

Creates the administrator without seeding roles. Same parameters as `ConfigureAdminAndRolesAsync`, minus the `TRole` and `TRoles` type parameters.

```csharp
var admin = await app.SeedDefaultAdminAsync<AppUser>(
    assignAdminRole: true,
    additionalRoles: ["Editor", "Reviewer"]
);
```

Roles are assigned by name, so seed them first or the assignment fails and is logged.

### Defining roles

**Namespace:** `JC.Identity.Authentication`

`SystemRoles` supplies two roles:

| Role | Description |
|------|-------------|
| `SystemAdmin` | Full system administrator with access to tenant management and assignment. |
| `Admin` | Administrator with access to all features within their tenant. |

Extend it with a `const string` per role and a matching `{Name}Desc` for the description:

```csharp
public class AppRoles : SystemRoles
{
    public const string Editor = nameof(Editor);
    public const string EditorDesc = "Can create and edit content.";

    public const string Viewer = nameof(Viewer);
    public const string ViewerDesc = "Read-only access to content.";
}
```

`SystemRoles.GetAllRoles<AppRoles>()` returns `List<(string Role, string Description)>` for the class and everything it inherits, so `SeedRolesAsync` gets the whole set from one call. It reads public static literal strings only — a `static readonly string` is skipped — and ignores any name ending in `Desc`. A role with no matching description gets an empty string.

These roles belong to this package rather than to the shared runtime. An authority with its own administrative plane brings its own role structure, and those roles are a separate security domain that must not be mixed into an application's own authorisation roles.

JC.Tenancy matches cross-tenant bypass permissions by role **name** rather than by referencing this class, which is what keeps the two packages independent while still letting an application nominate `SystemRoles.SystemAdmin`.

### Claims written to the principal

`DefaultClaimsPrincipalFactory<TUser, TRole>` extends whatever ASP.NET Identity already generates with all 13 `DefaultClaims` values, read from the `BaseUser` properties of the same name. `LockoutEnd`, `LastLoginUtc` and `RegistrationUtc` are written in round-trip format; a null value is written as an empty string rather than omitted, so every claim is always present.

The claim type strings and what each one populates on `IUserInfo` are listed in [JC.Identity.Shared](../JC.Identity.Shared/Setup.md#defaultclaims).

One naming trap worth knowing: `BaseUser.RequirePasswordChange` is the persisted column, and it is what you set to force a user through the change-password flow. `IUserInfo.RequiresPasswordChange` is the projected value the rule reads, and `IdentityMiddlewareOptions.RequirePasswordChange` is the switch that turns the rule on at all.

### IdentityDataDbContext — what it provides

`IdentityDataDbContext<TUser, TRole>` extends `IdentityDbContext<TUser, TRole, string>` and implements JC.Core's `IDataDbContext`:

- Every ASP.NET Core Identity table — users, roles, claims, tokens, logins
- `DbSet<AuditEntry> AuditEntries`, with the JC.Core audit mapping applied
- An overridden `SaveChangesAsync` performing the two-phase audit save: pending changes are processed, saved, then any create entries are written and saved again
- `TUser.TenantId` mapped with a maximum length of 36

The constructor takes `DbContextOptions` and `IUserInfo`. The user info is what audit entries are attributed to; where none resolves, JC.Core falls back to `IUserInfo.MissingUserInfoId`.

**It does not filter by tenant.** Tenant filtering is opt-in and arrives with JC.Tenancy, which is what lets a single-tenant application avoid that package entirely.

`BaseUser` carries a `TenantId` column and exposes it as `IApplicationUser.IdentityTenantId`, but does **not** implement `IMultiTenancy` — and must not. A global query filter on the user entity breaks `UserManager` and `SignInManager`, because authentication resolves a user before any tenant scope exists.

### Adding tenant filtering

**Opt-in.** Tenant filtering requires [JC.Tenancy](../JC.Tenancy/Setup.md); JC.Identity neither references it nor configures it.

Register the engine against the context that owns tenant storage:

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>();

builder.Services.AddTenancy<AppDbContext>(options =>
{
    options.AllowBypassForRole(SystemRoles.SystemAdmin);
});
```

Then declare the context tenant-scoped and install the filters:

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options, IUserInfo userInfo, ITenantInfo tenantInfo)
    : IdentityDataDbContext<AppUser, AppRole>(options, userInfo), ITenantScopedContext, ITenantDbContext
{
    public string? CurrentTenantId => tenantInfo.TenantId;

    public DbSet<Tenant> Tenants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyTenancyMappings();
        modelBuilder.ApplyTenantFilters(this);
    }
}
```

Every line is required, and `ApplyTenantFilters` must come **last** — it reads the model as it stands when called, so any tenant-scoped entity registered afterwards never receives a filter.

Assign the seeded administrator a tenant by joining the two packages at the call site:

```csharp
var admin = await app.ConfigureAdminAndRolesAsync<AppUser, AppRole, AppRoles>();

if (admin is not null)
    await app.Services.SeedDefaultTenantAsync<AppUser, AppDbContext>(admin.Id);
```

`ITenantDbContext` and `ApplyTenancyMappings()` belong to the one context that owns the tenant table. Other contexts participate in filtering by implementing `ITenantScopedContext` and calling `ApplyTenantFilters(this)` alone. See [JC.Tenancy — Setup](../JC.Tenancy/Setup.md) for the full options.

## 3. Apply migrations

JC.Identity introduces the ASP.NET Core Identity tables (users, roles, user claims, user roles, user logins, user tokens, role claims) and JC.Core's `AuditEntries` table.

It introduces no tenant table — that arrives with JC.Tenancy, and only in the context you nominate through `AddTenancy<TContext>`.

```bash
dotnet ef migrations add InitialIdentity --project YourApp
dotnet ef database update --project YourApp
```

Or generate the migration and apply it at start-up with JC.Core's helper:

```bash
dotnet ef migrations add InitialIdentity --project YourApp
```

```csharp
await app.Services.MigrateDatabaseAsync<AppDbContext>();
```

## 4. Verify

1. Run the application and navigate to a page marked `[Authorize]` — you should be redirected to `/Identity/Account/Login`.
2. Sign in with the seeded administrator credentials from `appsettings.json`.
3. Inject `IUserInfo` anywhere and confirm `UserId`, `Username` and `Authority` are populated — `Authority` should read `Local`.

## Next steps

- [Guide](Guide.md) — using `IUserInfo`, custom claims and two-factor flows.
- [API Reference](API.md)
- [JC.Identity.Shared — Setup](../JC.Identity.Shared/Setup.md) — the shared runtime: options, projection, account rules, roles and the two-factor helper.
- [JC.Tenancy — Setup](../JC.Tenancy/Setup.md) — if you need tenant filtering.
