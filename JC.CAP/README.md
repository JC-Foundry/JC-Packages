# JC.CAP

Single sign-on against CAP, the Central Admin Portal, wired into the JC suite. A user signs in at CAP and arrives back as an ordinary ASP.NET Core cookie session, with `IUserInfo` populated and stamped `IdentityAuthority.CAP`, so the rest of the suite works exactly as it does under JC.Identity. CAP owns the credentials, the second factor and the account pages; the application owns what its roles mean.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.CAP/JC.CAP.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

`JC.Identity.Shared`, `JC.Identity.Shared.Web`, the `CAP.SSO` contract package and the OpenIddict client packages arrive with this package and need no separate reference.

## Prerequisites

- .NET 9.0 SDK, and an ASP.NET Core project
- An application registered at CAP by a CAP operator, which gives you a client id, a client secret shown once, and the two callback URIs below entered against it

## Quick start

### Roles

```csharp
public class AppRoles : SystemRoles
{
    public const string Editor = nameof(Editor);
    public const string EditorDesc = "Can create and edit content.";
}
```

`SystemRoles` here is `JC.CAP.Authentication.SystemRoles`. It declares no roles of its own: the application is the source of truth for the roles it enforces.

### Services and middleware

```csharp
builder.Services.AddCap(builder.Configuration);

// Strongly recommended: JC.Core's data services, so the audit trail and repositories attribute
// their work to the signed-in CAP user. Not needed for the sign-in itself.
builder.Services.AddCore<AppDbContext>();

var app = builder.Build();

// Authentication, IUserInfo, authorisation, identity rules, in that order
app.UseCap();

// The sign-in trigger, the two callbacks CAP returns to, sign-out, and the re-check endpoints
app.MapCap();

// Publish the roles to CAP once, and refuse to start if CAP refuses them
await app.SyncCapRolesAsync<AppRoles>();

app.Run();
```

### Configuration

```json
{
  "SSO": {
    "BaseUrl": "https://sso.example.com",
    "ClientId": "evbfqxmh",
    "ClientSecret": "the-secret-CAP-showed-once"
  }
}
```

The section is the one CAP itself reads, so the host is configured under one key on both sides. Startup fails if the host, the client id or the secret is missing.

Register `https://your-app/signin-oidc` and `https://your-app/signout-callback-oidc` with the CAP operator.

## Where things live

This package is the **CAP authority**, the second identity authority built on `JC.Identity.Shared`. The parts every authority needs, the `IUserInfo` implementation, the claims projection, the account rules and their options, live there, with the ASP.NET Core middleware in `JC.Identity.Shared.Web`. `CAP.SSO` carries the wire contract: endpoint paths, scope and claim names, the API's DTOs and the redirect URI rules.

There is no user table and no `DbContext`. CAP holds the account; the application holds only what it needs against the CAP account id. Tenancy is a separate package, joined through the enricher hook described below.

JC.CAP does not require `AddCore`: signing in and populating `IUserInfo` work without it. Everything in JC.Core does need it, the `DbContext` with its audit trail, `IRepositoryManager` and the background-job options, and an application with a database should register it, because attributing that work to the real user is the reason to sign one in. See [JC.Core](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Core/Setup.md) for what it registers.

## Feature areas

### IUserInfo

The claims middleware reads the session cookie into a scoped `IUserInfo`, which JC.Core uses to attribute audit rows and the other packages read for their own scoping. Nothing downstream knows the user came from CAP.

```csharp
public class DashboardModel(IUserInfo userInfo) : PageModel
{
    public void OnGet()
    {
        var id = userInfo.UserId;             // the CAP account id
        var authority = userInfo.Authority;   // CAP
    }
}
```

### The sign-in

The cookie is the application's default scheme. An `[Authorize]` page with no session redirects to `/cap/signin`, which sends the browser to CAP's authorize endpoint. CAP shows its login page, or issues a code silently to a browser already signed in at CAP. The code comes back to `/signin-oidc`, is exchanged for tokens, and the session cookie is written. A "Sign in" link points at `/cap/signin?returnUrl=/wherever`.

Sign-out is a POST to `/cap/signout`. It clears the cookie, ends the session at CAP, and returns through `/signout-callback-oidc`.

### Claims

CAP's vocabulary arrives on the tokens and is translated onto the cookie in ASP.NET Identity's: the subject becomes `ClaimTypes.NameIdentifier`, the username `ClaimTypes.Name`, each role `ClaimTypes.Role`. So `[Authorize(Roles = AppRoles.Editor)]`, `User.IsInRole` and `IUserInfo.IsInRole` all work with no configuration, and the eight identity claims CAP sends under JC's own names are copied across for the projection to read.

### Session and refresh

The cookie carries CAP's tokens. As the access token nears expiry the session refreshes silently inside cookie authentication, reading CAP's live state, so a role granted in CAP appears within fifteen minutes and an account disabled in CAP is signed out at its next refresh. An unreachable CAP is tolerated for a grace period, then the session ends.

### Identity rules

The shared rules apply, pointed at CAP. A disabled account goes to `/cap/denied` and an account owing two-factor to `/cap/two-factor`, both of which re-check with CAP before handing over to CAP's own page, so a user just back from enrolling is let through rather than sent round again. Two-factor enforcement is off by default; turn it on through `configureMiddleware`.

### Roles

The application publishes its role catalogue to CAP, and CAP operators assign from that list. `SyncCapRolesAsync<AppRoles>` publishes once at startup and fails the application if CAP refuses. `CapRoleSyncJob<AppRoles>` is the same work as a JC.Core `IBackgroundJob`, for an application that schedules it through JC.BackgroundJobs instead. Roles CAP holds that a publish no longer names are marked stale, never deleted.

### CAP's API

`CapApiClient` calls CAP as the application, with a client-credentials token it obtains and renews itself: what CAP is configured as for you, the application's members, one member by id, and the catalogue publish. A refusal arrives as `CapApiException` carrying CAP's reason.

`CapUserCache` keeps the live members in memory for a configurable window, one entry per member, so a page naming a user by id never waits on CAP.

### Links into CAP

`CapLinks` builds the absolute, branded URLs into CAP's account pages from the host and client id you already configured: the account home, profile, security, two-factor enrolment, registration, forgotten password. A link can carry a return URL, so a user who registers at CAP comes back to the application rather than being left on CAP's account pages.

### Extending the principal

An `ICapClaimsEnricher` runs at sign-in and on every refresh, and is how a tenant id reaches `IUserInfo.TenantId`. `ICapClaimsPrincipalFactory` is replaceable for anything larger. A derived `CapUserInfo` is registered through `AddCap<TUserInfo>`.

## Defaults

| Default | Value |
|---------|-------|
| Configuration section | `SSO`, the one CAP.SSO names |
| Scopes requested | `openid`, `roles`, `cap_identity`, `offline_access` |
| Sign-in, sign-out, refresh, denied, two-factor paths | `/cap/signin`, `/cap/signout`, `/cap/refresh`, `/cap/denied`, `/cap/two-factor` |
| Callback paths registered at CAP | `/signin-oidc`, `/signout-callback-oidc` |
| Cookie | `.JC.CAP.Session`, scheme `JC.CAP`, sliding 14 days, not persistent |
| Refresh | 1 minute ahead of access-token expiry; 5 minutes' grace when CAP is unreachable |
| Two-factor enforcement | Disabled |
| Role refusal | A plain 403 by default; CAP's denied page or a local page by configuration |
| `IUserInfo` implementation | Built-in `CapUserInfo`, scoped |
| `IUserInfo.Authority` | `CAP` once authenticated, `None` otherwise |
| OpenIddict token storage | Disabled, so no OpenIddict tables are needed |
| Member cache | Enabled, five minutes, one entry per member |

## Documentation

- [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.CAP/Setup.md): registration, every option and its default, the cookie, the rules, the OpenIddict client
- [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.CAP/Guide.md): signing in and out, the session, the rules, roles, CAP's API, extending the principal
- [API Reference](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.CAP/API.md)
- [JC.Identity.Shared](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Identity.Shared/Setup.md): the shared identity runtime this package builds on

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
