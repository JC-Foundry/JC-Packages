# JC.Identity.Shared — Guide

Covers reading the current user, establishing an identity outside an HTTP request, projecting identity from your own authority, applying the account rules directly, roles, and two-factor setup screens. See [Setup](Setup.md) for registration and option defaults.

## Reading the current user

### Basic usage

Inject `IUserInfo` anywhere. It is scoped, so every service in a request or job scope sees the same instance:

```csharp
public class OrderService(IUserInfo userInfo, IRepositoryContext<Order> orders)
{
    public async Task<Order> PlaceAsync(string productId, int quantity)
    {
        var order = new Order
        {
            ProductId = productId,
            Quantity = quantity,
            PlacedByUserId = userInfo.UserId,
            PlacedByName = userInfo.DisplayName ?? userInfo.Username
        };

        return await orders.AddAsync(order);
    }
}
```

`DisplayName` is nullable — an authority need not supply one. `Username` always holds something, because it falls back to a constant rather than to null.

### Checking roles

```csharp
public class ReportPage(IUserInfo userInfo)
{
    // Role names come from whichever authority is registered. On JC.Identity these are
    // SystemRoles.Admin and SystemRoles.SystemAdmin.
    public bool CanExport => userInfo.IsInRole("Admin");

    public bool CanManageTenants => userInfo.IsInRole("SystemAdmin");
}
```

`IsInRole` looks in `Roles` first, then falls back to any claim of type `ClaimTypes.Role`. Both comparisons are **ordinal and case-sensitive**, so `"admin"` does not match `"Admin"`. An empty or null role name always returns `false`.

**Nuance:** if your authority uses a custom `RoleClaimType`, the projection still populates `Roles` from it, so `IsInRole` keeps working. Only the claim fallback is fixed to `ClaimTypes.Role`.

### Reading the user's tenant

```csharp
if (userInfo.HasTenant)
    _logger.LogInformation("Acting for tenant {TenantId}", userInfo.TenantId);
```

`HasTenant` is derived from `TenantId` and has no setter, so it cannot disagree with the value it describes.

**Nuance:** `IUserInfo.TenantId` is the tenant assigned to the user. It is *not* the tenant the current operation is scoped to — that is `ITenantContext.TenantId`, and the two differ whenever a job or an administrator deliberately works in another tenant. Never read `IUserInfo.TenantId` to decide what data to load; read the operational tenant, or let the query filters do it.

### Knowing which authority signed the user in

```csharp
var banner = userInfo.Authority switch
{
    IdentityAuthority.Local => "Signed in locally",
    IdentityAuthority.CAP => "Signed in through the Central Admin Portal",
    IdentityAuthority.Custom => "Signed in through a custom provider",
    _ => "Not signed in"
};
```

`Authority` answers *who supplied this identity*, not how they established it. Somebody who signs into a portal with an external provider and is then passed through to your application has an authority of `CAP`, whatever they used to reach the portal.

### Nuances and gotchas

**An unpopulated instance is not blank.** `UserId`, `Username` and `Email` start at the system-user constants — `"System__ID"`, `"System"`, `"<SYSTEM@EMAIL>"`. Code that checks `if (userInfo.UserId is null)` never fires. Check `IsSetup` instead:

```csharp
if (!userInfo.IsSetup)
{
    // Nothing has projected an identity into this scope yet
}
```

**Three distinct "no user" values exist, and they mean different things:**

| Value | Meaning |
|-------|---------|
| `SYSTEM_USER_ID` (`System__ID`) | No principal at all — the request or job carries no identity |
| `UNKNOWN_USER_ID` (`Unknown__ID`) | A principal exists but is not authenticated, or a claim was missing |
| `MissingUserInfoId` (`<NONE>`) | `IUserInfo` could not be resolved from the container at all |

The third only appears when the shared services were never registered. Register them and an unattributed write is stamped `System__ID`; leave them out and it is stamped `<NONE>`.

**Constructing an `IUserInfo` does not make it ambient.** The instance is scoped and populated in place, so this achieves nothing:

```csharp
// Wrong — nothing that injects IUserInfo will ever see this
var userInfo = IUserInfo.SystemUser<UserInfoBase>();
await DoWorkAsync(userInfo);
```

Use the scope helpers below instead.

## Establishing an identity outside a request

### Basic usage

A background job that needs its work attributed to a real user:

```csharp
public class NightlyInvoiceJob(IServiceProvider services, IUserBackedJobSource source) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        foreach (var (user, roles) in await source.GetSchedulesAsync(cancellationToken))
        {
            await using var scope = services.CreateAsyncScopeForUser(user, roles, user.IdentityTenantId);

            var invoices = scope.ServiceProvider.GetRequiredService<IRepositoryContext<Invoice>>();
            await invoices.AddAsync(new Invoice { UserId = user.Id }, cancellationToken: cancellationToken);
        }
    }
}
```

Every audited write inside that scope is attributed to `user`, with no fake principal and no HTTP context. `CreateScopeForUser` is the synchronous equivalent where nothing in the scope needs asynchronous disposal.

### Populating the scope you are already in

```csharp
public class ImpersonationService(IServiceProvider scopedServices)
{
    public IUserInfo ActAs(IApplicationUser user, IEnumerable<string?> roles)
        => scopedServices.SetUserInfoForUser(user, roles, user.IdentityTenantId);
}
```

**This replaces the authenticated user for the remainder of the scope.** Inside a live request that is impersonation, and it should be a deliberate, audited decision rather than a convenience. Prefer creating a fresh scope wherever you can.

### Nuances and gotchas

**`tenantId` is a separate argument for a reason.** All three helpers take it independently of the user record:

```csharp
// The two coincide here, and saying so at the call site makes that explicit
await using var scope = services.CreateAsyncScopeForUser(user, roles, user.IdentityTenantId);

// A job deliberately working in another tenant
await using var scope = services.CreateAsyncScopeForUser(user, roles, "acme");
```

`IApplicationUser.IdentityTenantId` is the tenant that owns the identity *record*. `IUserInfo.TenantId` is the user's tenant *inside this application*. For a local ASP.NET Identity user they are the same value; for an externally supplied identity they need not be.

**These require the shared services to be registered.** `SetUserInfoForUser` resolves `IOptions<IdentityProjectionOptions>` to stamp `Authority`, so a container without `AddSharedIdentityServices` throws.

**Roles are filtered, not validated.** Null and empty entries are discarded; anything else is taken at face value. Passing a role that does not exist in your store still makes `IsInRole` return `true` for it.

**Order does not matter when you also set a tenant.** A job needing both an actor and a tenant scope can establish them either way round:

```csharp
await using var scope = services.CreateAsyncScopeForUser(user, roles, user.IdentityTenantId);
scope.ServiceProvider.SetTenantInfoForTenant("acme");   // only if you need a different tenant
```

JC.Tenancy reads `IUserInfo.TenantId` on every access rather than capturing it, and an explicit tenant is an override that wins from whenever it was set. Establishing the user first is convention, not a requirement.

## Projecting identity from your own authority

This is what you implement if you are supplying identity from something other than JC.Identity.

### From a claims principal

```csharp
public class PortalAuthenticationHandler(
    IUserInfo userInfo,
    IOptions<IdentityProjectionOptions> projection,
    ILogger<PortalAuthenticationHandler> logger)
{
    public void Apply(ClaimsPrincipal principal)
        => userInfo.PopulateFrom(principal, projection.Value, logger);
}
```

Three branches, and all three set `IsSetup = true`:

| Condition | Result |
|-----------|--------|
| `principal` or its `Identity` is `null` | System-user constants |
| Identity present but not authenticated | Unknown-user constants |
| Authenticated | Every field projected from claims, and `Authority` stamped |

`Authority` is stamped **only** on the authenticated branch, so an anonymous request keeps `None` however you have configured the options.

### From a user record

```csharp
// PortalUser implements JC.Core's IApplicationUser, which is what the base constructor takes
public class PortalUserInfo : UserInfoBase
{
    public PortalUserInfo(PortalUser user, IEnumerable<string?> roles)
        : base(user, roles)
    {
        TenantId = user.IdentityTenantId;
        Authority = IdentityAuthority.CAP;
    }
}
```

The base constructor projects everything except `TenantId` and `Authority`. It leaves those two alone deliberately — the tenant because the two tenant concepts differ, the authority because only the registering package knows it. A derived type that knows the answers sets them, exactly as above.

You can also project onto an existing instance rather than constructing one:

```csharp
userInfo.PopulateFrom(user, roles);
```

### Emitting the claims the projection reads

Your authority must write claims under the names in `DefaultClaims`, or those fields stay at their defaults:

```csharp
var identity = new ClaimsIdentity(authenticationType: "Portal");

identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
identity.AddClaim(new Claim(ClaimTypes.Email, user.Email ?? string.Empty));

identity.AddClaim(new Claim(DefaultClaims.DisplayName, user.DisplayName ?? string.Empty));
identity.AddClaim(new Claim(DefaultClaims.IsEnabled, user.IsEnabled.ToString()));
identity.AddClaim(new Claim(DefaultClaims.TenantId, user.IdentityTenantId ?? string.Empty));
identity.AddClaim(new Claim(DefaultClaims.RequirePasswordChange, user.RequirePasswordChange.ToString()));
```

The first two use the configurable claim types; everything else uses the fixed constants. The full list is in [Setup](Setup.md#defaultclaims).

### Nuances and gotchas

**Boolean claims are compared against `"true"`, case-insensitively.** Anything else — including `"1"`, `"yes"` or an empty string — reads as `false`. `bool.ToString()` produces `"True"`, which matches.

**Date claims need a parseable format.** They go through `DateTime.TryParse` and fall back to `null`. Round-trip format (`"O"`) is what JC.Identity emits and is the safe choice.

**An empty tenant claim does not clear the tenant.** `TenantId` is assigned only when the claim carries a value, so an empty claim leaves whatever was already on the instance. If you need to clear it, assign `null` yourself.

**`AccessFailedCount` falls back to `0`** when the claim is missing or unparseable, not to null — it is a non-nullable `int`.

## Applying the account rules

### Basic usage

In an ASP.NET Core application, `UseIdentityMiddleware()` does this for you. Nothing further is needed.

### Applying them yourself

Where there is no middleware pipeline — a minimal API filter, a Blazor circuit, a desktop shell — call the rules directly:

```csharp
public class AccountGate(IUserInfo userInfo, IOptions<IdentityMiddlewareOptions> options)
{
    public string? RequiredRedirect(string path, bool isAuthenticated)
        => IdentityRules.GetRedirect(userInfo, path, isAuthenticated, options.Value);
}
```

A `null` return means the request may proceed; anything else is the route to send the caller to.

### Returning a status code instead of a redirect

An API has no use for a redirect, but the same rules still apply:

```csharp
var redirect = IdentityRules.GetRedirect(userInfo, context.Request.Path, isAuthenticated: true, options.Value);

if (redirect is not null)
    return Results.Problem(
        title: "Account action required",
        detail: $"Complete the action at {redirect} before continuing.",
        statusCode: StatusCodes.Status403Forbidden);
```

### Nuances and gotchas

**The order of the checks is deliberate.** Disabled accounts are caught first, before password change and two-factor. A disabled account should not be routed to a page it has no business completing.

**Rules 2 and 3 guard against a redirect loop; rule 1 does not need to.** The password-change and two-factor checks are skipped when the path already starts with their own route. The disabled check has no such guard because `AccessDeniedRoute` is one of the `ExcludedPaths` and never reaches the rules at all.

**Static files are matched by extension, not by middleware order.** `.css`, `.js`, `.jpg`, `.jpeg`, `.png`, `.gif`, `.svg`, `.ico`, `.woff`, `.woff2`, `.ttf`, `.eot`, `.map`, `.json`, `.xml`. A `.json` API endpoint whose path ends in that extension is skipped along with them.

**Pass `isAuthenticated` honestly.** The rules return `null` immediately when it is `false`, so passing `true` for an anonymous caller runs every check against system-user defaults — `IsEnabled` is `false` on an unpopulated instance, so you would redirect them to access-denied.

## Roles

### Where roles come from

This package defines none. Roles belong to whichever authority issued them — JC.Identity supplies `SystemRoles`, and another authority brings its own. What is shared is the shape they arrive in:

```csharp
public class ContentPolicy(IUserInfo userInfo)
{
    // Role names, whatever authority issued them
    public bool CanPublish => userInfo.IsInRole("Editor");

    public IReadOnlyList<string> Assigned => userInfo.Roles;
}
```

`IUserInfo.Roles` holds names in the consuming application's own authorisation domain. Code that only reads roles therefore works unchanged whichever authority is registered, provided it matches on names rather than on one authority's constants.

### Nuances and gotchas

**Prefer an authority's constants where you have exactly one.** An application on JC.Identity should write `userInfo.IsInRole(SystemRoles.Admin)` rather than `"Admin"` — the constant is compile-checked, and there is no ambiguity to avoid. Reach for bare names only in code meant to serve more than one authority.

**Do not mix administrative roles from two domains.** An external authority's own administrative roles are a separate security domain from your application's roles. If such roles are ever surfaced to a consuming application they must be distinguishable — separately prefixed or carried in differentiated claims — so that a check against `IUserInfo.Roles` cannot accidentally match one.

**Role matching is ordinal and case-sensitive**, in `IsInRole` and in anything else comparing these names, including JC.Tenancy's bypass configuration.

## Two-factor setup screens

### Basic usage

```csharp
public class EnableAuthenticatorModel(UrlEncoder urlEncoder)
{
    public string AuthenticatorUri { get; private set; } = string.Empty;
    public string SharedKey { get; private set; } = string.Empty;

    public void Build(string appName, string email, string unformattedKey)
    {
        var helper = new IdentityHelper(urlEncoder);
        (AuthenticatorUri, SharedKey) = helper.Generate2faKey(appName, email, unformattedKey);
    }
}
```

`AuthenticatorUri` goes into a QR code; `SharedKey` is displayed beside it for anyone typing the code in by hand.

### The two halves separately

```csharp
var uri = helper.Generate2faQrCodeUri("MyApp", user.Email!, unformattedKey);
var key = helper.Format2faKey(unformattedKey);
```

`Format2faKey` lowercases the key and groups it in fours. `Generate2faKey` is just both calls in one.

### A custom URI format

```csharp
var helper = new IdentityHelper(urlEncoder, "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=8");
```

The placeholders are the issuer name, the URL-encoded email and the secret, in that order. The issuer appears twice in the default format, which is why it is `{0}` in both positions.

### Nuances and gotchas

**It is not registered in the container.** Construct it where you need it — it holds only a `UrlEncoder` and a format string.

**It formats; it does not generate.** The shared secret comes from your authority — for ASP.NET Core Identity, `UserManager.GetAuthenticatorKeyAsync`. This helper never creates or validates a key.

**Only the email is URL-encoded.** The issuer name is interpolated as given, so a name containing `:` or `/` will produce a malformed URI.

## How this fits with the rest of the suite

### Audit attribution

JC.Core's repository layer resolves `IUserInfo` and stamps `CreatedById`, `LastModifiedById` and the audit trail from `UserId`. Establishing an identity is therefore the whole of what makes a background job's writes attributable:

```csharp
await using var scope = services.CreateAsyncScopeForUser(user, roles);

var products = scope.ServiceProvider.GetRequiredService<IRepositoryContext<Product>>();
await products.AddAsync(product);   // CreatedById is user.Id
```

Every write method also takes an explicit `userId` that wins over the ambient one, which is the right tool for a one-off system attribution:

```csharp
await products.UpdateAsync(product, userId: "data-migration-job");
```

### Tenancy

`IUserInfo.TenantId` is where JC.Tenancy takes its default operational tenant from, read live rather than captured. Populating the user is therefore enough to put a scope in the right tenant — you only call `SetTenantInfoForTenant` when you need a *different* one.

The two remain distinct concepts, and tenant-aware queries follow the operational tenant, never `IUserInfo` directly.

## Next steps

- [Setup](Setup.md) — registration, options and their defaults.
- [API Reference](API.md)
- [JC.Identity — Setup](../JC.Identity/Setup.md) — the local ASP.NET Core Identity authority built on this package.
