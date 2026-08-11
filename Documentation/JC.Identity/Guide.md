# JC.Identity — Guide

Covers extending the user and role entities, what a sign-in puts on the principal, enforcing account state, seeding roles and an administrator, working with the DbContext, and composing with tenancy. See [Setup](Setup.md) for registration.

Reading the current user, establishing an identity outside a request, and the account rules themselves belong to the shared runtime — see the [JC.Identity.Shared guide](../JC.Identity.Shared/Guide.md). This guide covers what is specific to local ASP.NET Core Identity.

## Users and roles

### Adding your own properties

```csharp
public class AppUser : BaseUser
{
    public string? JobTitle { get; set; }
    public string? DepartmentId { get; set; }
}

public class AppRole : BaseRole
{
    public int SortOrder { get; set; }
}
```

Nothing else is needed — `IdentityDataDbContext<AppUser, AppRole>` maps the derived types, so a migration picks the new columns up.

### Managing users through UserManager

```csharp
public class StaffService(UserManager<AppUser> users)
{
    public async Task<IdentityResult> CreateAsync(string email, string password, string? jobTitle)
    {
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            JobTitle = jobTitle,
            RegistrationUtc = DateTime.UtcNow
        };

        return await users.CreateAsync(user, password);
    }
}
```

`UserManager<TUser>` is ASP.NET Identity's, registered by `AddIdentity`. This package adds no user-management service of its own — password hashing, validation, lockout and token generation are all Identity's, unchanged.

**`IsEnabled` defaults to `true`**, so a newly constructed user is usable without being explicitly enabled. `RegistrationUtc` is not set for you outside the admin seeder — set it yourself if you want it populated.

### Nuances and gotchas

**`LastLoginUtc` is never written by this package.** It is projected onto `IUserInfo` and emitted as a claim, but nothing sets it. Stamp it in your sign-in path:

```csharp
var result = await signInManager.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure: true);

if (result.Succeeded)
{
    user.LastLoginUtc = DateTime.UtcNow;
    await users.UpdateAsync(user);
}
```

**Do not make your user entity implement `IMultiTenancy`.** `BaseUser` carries a `TenantId` column and is tenant-*aware*, but it is deliberately not tenant-*filtered*. A global query filter on the user entity breaks `UserManager` and `SignInManager`, because authentication resolves a user before any tenant scope exists — you would lose the ability to log in at all.

**`IdentityTenantId` and `TenantId` are the same column.** `IdentityTenantId` is `[NotMapped]` and reads and writes `TenantId`; it exists so code holding an `IApplicationUser` can reach the tenant without referencing ASP.NET Identity. Assigning either assigns both.

## Signing in

### What lands on the principal

`DefaultClaimsPrincipalFactory` replaces Identity's default factory and adds thirteen claims from the user entity on top of the identifier, username and role claims Identity already writes:

```csharp
// After sign-in, the principal carries these alongside the standard Identity claims
User.FindFirst(DefaultClaims.DisplayName)?.Value;
User.FindFirst(DefaultClaims.TenantId)?.Value;
User.FindFirst(DefaultClaims.IsEnabled)?.Value;
```

You rarely read them directly. `UseUserInfo()` projects the whole set onto `IUserInfo`, which is what the rest of the suite consumes:

```csharp
public class DashboardModel(IUserInfo userInfo)
{
    public string Greeting => $"Welcome back, {userInfo.DisplayName ?? userInfo.Username}";
}
```

The projection and everything `IUserInfo` exposes are covered in the [shared guide](../JC.Identity.Shared/Guide.md#reading-the-current-user).

### Adding claims of your own

Derive from the factory and extend it:

```csharp
public class AppClaimsPrincipalFactory(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    IOptions<IdentityOptions> options)
    : DefaultClaimsPrincipalFactory<AppUser, AppRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("department_id", user.DepartmentId ?? string.Empty));

        return identity;
    }
}
```

Register it after `AddIdentity`, so it replaces the one this package registered:

```csharp
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, AppClaimsPrincipalFactory>();
```

To read your claim back on `IUserInfo`, derive a user info type too — see [Carrying extra properties on IUserInfo](#carrying-extra-properties-on-iuserinfo).

### Nuances and gotchas

**Claims are minted at sign-in, not read live.** Changing `DisplayName` or `IsEnabled` in the database does not change the cookie already issued. The user sees the old value until the security stamp is revalidated or they sign in again. Where a change must take effect immediately, call `UserManager.UpdateSecurityStampAsync(user)`, which invalidates the existing cookie.

**A null value becomes an empty claim, not a missing one.** Every one of the thirteen is always present on the principal, so `FindFirst` never returns null for them — test the value, not its presence.

**Reconfigure `IdentityOptions.ClaimsIdentity` rather than the projection.** The claim types the projection reads are copied from Identity's own configuration, so changing them in one place keeps both in step. See [Setup](Setup.md#claim-types-and-authority).

## Enforcing account state

The three rules — disabled account, forced password change, optional two-factor — are evaluated by the shared runtime. This package supplies the data they read.

### Disabling an account

```csharp
public async Task SuspendAsync(AppUser user)
{
    user.IsEnabled = false;
    await users.UpdateAsync(user);
    await users.UpdateSecurityStampAsync(user);
}
```

`IsEnabled = false` alone does not eject a signed-in user, because their cookie already says otherwise. `UpdateSecurityStampAsync` is what invalidates it, so the next request re-reads the account and the rule fires.

### Forcing a password change

```csharp
user.RequirePasswordChange = true;
await users.UpdateAsync(user);
await users.UpdateSecurityStampAsync(user);
```

Set `BaseUser.RequirePasswordChange`. That flows through the claim into `IUserInfo.RequiresPasswordChange`, which the rule reads. Clear it once the new password is accepted, or the user is redirected forever:

```csharp
var result = await users.ChangePasswordAsync(user, currentPassword, newPassword);

if (result.Succeeded)
{
    user.RequirePasswordChange = false;
    await users.UpdateAsync(user);
}
```

### Requiring two-factor

Two-factor enforcement is a switch on `IdentityMiddlewareOptions`, off by default; see [Setup](Setup.md#addidentity--standard-registration). The per-user state is ASP.NET Identity's own `TwoFactorEnabled`, and the setup screen is built with `IdentityHelper` — covered in the [shared guide](../JC.Identity.Shared/Guide.md#two-factor-setup-screens).

### Nuances and gotchas

**Three similarly named members are easy to confuse.** `BaseUser.RequirePasswordChange` is the persisted column you set. `IUserInfo.RequiresPasswordChange` is the projected value the rule reads. `IdentityMiddlewareOptions.RequirePasswordChange` is the switch that turns the rule on at all — leave it enabled, or setting the column does nothing.

**Routes must exist.** The rules redirect to `/Identity/Account/Manage/SetPassword`, `/Identity/Account/Manage/EnableAuthenticator` and `/Identity/Account/AccessDenied` by default. An application not using the Identity UI scaffolding must point these at its own pages, or every redirect lands on a 404.

## Seeding roles and an administrator

### Both together

```csharp
var app = builder.Build();

var admin = await app.ConfigureAdminAndRolesAsync<AppUser, AppRole, AppRoles>();
```

Roles first, then the administrator — which matters, because roles are assigned by name and a missing role fails the assignment.

### Reacting to the result

```csharp
var admin = await app.ConfigureAdminAndRolesAsync<AppUser, AppRole, AppRoles>();

if (admin is null)
{
    app.Logger.LogCritical("Administrator could not be created — check the Admin configuration section.");
    return;
}
```

`null` means creation was attempted and failed; the reason is already logged. An account that *already existed* is returned rather than nulled, which is what makes anything you chain afterwards idempotent.

### Assigning application roles at seed time

```csharp
var admin = await app.SeedDefaultAdminAsync<AppUser>(
    assignAdminRole: true,
    additionalRoles: [AppRoles.Editor, AppRoles.Viewer]
);
```

### Nuances and gotchas

**Seeding is matched on email, then username.** An existing account matching either is returned untouched — no roles are added, no properties updated. Changing `Admin:Email` in configuration therefore creates a *second* administrator rather than renaming the first.

**A failed role assignment does not fail the seed.** Each is logged and the rest continue, so an administrator can end up with some of their roles. Check the startup log rather than assuming success from a non-null return.

**`SeedRolesAsync` never updates an existing role.** A role whose description you have since changed keeps the old one, because only missing roles are created.

## Working with the DbContext

### Adding your own entities

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options, IUserInfo userInfo)
    : IdentityDataDbContext<AppUser, AppRole>(options, userInfo)
{
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ProductMap());
    }
}
```

**Call `base.OnModelCreating` first.** It configures the Identity model and the audit mapping; skipping it loses both.

### The audit trail

Entities extending JC.Core's `AuditModel` are stamped automatically on save, attributed to `IUserInfo.UserId`:

```csharp
public class Product : AuditModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public required string Name { get; set; }
}
```

Nothing further is required — `IdentityDataDbContext.SaveChangesAsync` runs the audit service before and after the write.

### Nuances and gotchas

**`SaveChangesAsync` returns the first save's row count.** Where new entities were created, a second save writes their audit entries, and those rows are not included in the returned figure. Do not treat the return value as "everything written".

**Audit attribution comes from the ambient `IUserInfo`.** In a request that is the signed-in user. In a background job it is whatever you established — or the system user if you established nothing. See [establishing an identity outside a request](../JC.Identity.Shared/Guide.md#establishing-an-identity-outside-a-request).

## Carrying extra properties on IUserInfo

```csharp
public class AppUserInfo : UserInfoBase
{
    public string? DepartmentId { get; set; }
}
```

```csharp
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext, AppUserInfo>();
```

Populate the addition after the standard projection has run:

```csharp
app.UseAuthentication();
app.UseUserInfo();

app.Use(async (context, next) =>
{
    if (context.RequestServices.GetRequiredService<IUserInfo>() is AppUserInfo appUserInfo)
        appUserInfo.DepartmentId = context.User.FindFirst("department_id")?.Value;

    await next();
});

app.UseAuthorization();
app.UseIdentityMiddleware();
```

Registering the pipeline by hand like this is the reason `UseIdentity()` has individual equivalents — see [Setup](Setup.md#middleware--individual-registration).

**Derive from `UserInfoBase`, not from `UserInfo`,** unless you want the `BaseUser`-shaped constructors as well. `UserInfo` adds only those constructors; everything else comes from the base.

## Composing with tenancy

`BaseUser.TenantId` is where a user's tenant is stored, and it reaches the runtime by claim rather than by query filter:

```text
BaseUser.TenantId  →  tenant_id claim  →  IUserInfo.TenantId  →  ITenantInfo (JC.Tenancy)
```

That chain is why the user entity needs no filter of its own, and why identity works before any tenant scope exists.

### Assigning a tenant to a user

```csharp
user.TenantId = tenant.Id;
await users.UpdateAsync(user);
await users.UpdateSecurityStampAsync(user);
```

The security stamp update matters here too — until the cookie is reissued the old tenant claim is still on the principal, so the user keeps operating in their previous tenant.

### Nuances and gotchas

**Never scope a query by `IUserInfo.TenantId`.** It is the tenant assigned to the *user*; the tenant an operation runs against is `ITenantContext.TenantId`, and the two differ whenever a job or an administrator works elsewhere. Let the query filters do the scoping.

**Wiring tenant filtering is a per-context job.** `IdentityDataDbContext` applies no filters, so an application that adds JC.Tenancy must declare its context tenant-scoped explicitly — see [Setup](Setup.md#adding-tenant-filtering).

## Next steps

- [Setup](Setup.md) — registration, options and their defaults.
- [API Reference](API.md)
- [JC.Identity.Shared — Guide](../JC.Identity.Shared/Guide.md) — reading the current user, background-job identity, the account rules and two-factor helpers.
- [JC.Tenancy — Setup](../JC.Tenancy/Setup.md) — if you need tenant filtering.
