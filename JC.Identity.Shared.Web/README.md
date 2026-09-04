# JC.Identity.Shared.Web

The ASP.NET Core half of [JC.Identity.Shared](https://github.com/JC-Foundry/JC-Packages/blob/master/JC.Identity.Shared/README.md). Two middlewares and their builder extensions — one projecting the current principal onto `IUserInfo`, one enforcing the account rules — and nothing else.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.Identity.Shared.Web/JC.Identity.Shared.Web.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

It references `JC.Identity.Shared`, so adding this alone is enough for a web host. [JC.Identity](https://github.com/JC-Foundry/JC-Packages/blob/master/JC.Identity/README.md) references both, so an application on local ASP.NET Core Identity needs neither directly.

## Prerequisites

- .NET 9.0 SDK, and an ASP.NET Core project
- **JC.Identity.Shared**, registered with `AddSharedIdentityServices<TUserInfo>()`
- An authentication scheme — this package projects a principal, it does not establish one

## Quick start

```csharp
var app = builder.Build();

app.UseAuthentication();     // whatever establishes the principal
app.UseUserInfo();           // projects its claims onto IUserInfo
app.UseAuthorization();
app.UseIdentityMiddleware(); // enforces disabled accounts, password changes, two-factor
```

The order matters in both directions. `UseUserInfo` must follow authentication because it reads claims, and must precede `UseIdentityMiddleware` because that enforces rules against what it produced.

## Feature areas

### UserInfoMiddleware

Populates the scoped `IUserInfo` from the current principal, **once per scope**. An instance already populated — by a background job, or by impersonation — is left alone, which is what `IsSetup` is for.

The projection itself is a `PopulateFrom` overload in JC.Identity.Shared. This middleware resolves the scoped instance and the projection options, hands over `HttpContext.User`, and calls the next middleware.

### IdentityMiddleware

Evaluates the account rules against the request path and either redirects or continues:

- Disabled accounts are redirected to the access-denied route
- Users flagged for a password change are redirected until they complete it
- Two-factor enrolment can be required, off by default

Skips unauthenticated requests, static files by extension, and the excluded paths of whichever rule set matched. The rules themselves, and the choice of rule set, are `IdentityRules.GetRedirect` in JC.Identity.Shared, which returns a route or nothing — this middleware supplies the request and performs the redirect.

It passes `HttpContext.RequestServices` through, so a rule set's condition can resolve services to decide whether it applies, and the request's full local URL, which a local route carries back as the set's `ReturnUrlParameter` so the page can send the user back.

Configure the rule sets and their routes through `IdentityMiddlewareOptions`, on `AddSharedIdentityServices`.

### Why the split

Both middlewares are thin wrappers. Everything they decide lives in `JC.Identity.Shared`, which carries no framework reference, so a worker service or an authority with no HTTP pipeline reaches identical behaviour without requiring the ASP.NET Core shared runtime.

This package holds the only `FrameworkReference` on `Microsoft.AspNetCore.App` in the shared identity runtime.

## Defaults

| Default | Value |
|---------|-------|
| `UseIdentity` order (from JC.Identity) | Authentication → `UseUserInfo` → authorisation → identity rules |
| Population | Once per scope, skipped where `IUserInfo.IsSetup` is already `true` |
| Rule routes and switches | The defaults on `IdentityMiddlewareOptions.Default`, with no conditional rule sets — see JC.Identity.Shared |
| Static file extensions skipped | `.css`, `.js`, `.jpg`, `.jpeg`, `.png`, `.gif`, `.svg`, `.ico`, `.woff`, `.woff2`, `.ttf`, `.eot`, `.map`, `.json`, `.xml` |

## Documentation

Documented alongside the package it wraps:

- [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Identity.Shared/Setup.md#adding-the-aspnet-core-middleware) — registering the middleware, and the options it reads
- [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Identity.Shared/Guide.md) — reading the current user, and applying the rules without middleware
- [API Reference](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Identity.Shared/API.md)

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
