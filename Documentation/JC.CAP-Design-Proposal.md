# JC.CAP: design proposal

> **Status:** proposal for review. Nothing here is agreed until John says so.
> **Owner:** John · **Drafted by:** Claude · **Date:** 2026-09-03
> **Companions:** CAP's `docs/design-docs/sso/sso.md` (the identity provider's design), `implementation-plan.md` (its sequencing, phase 7 is this package), and the `CAP.SSO` README (the wire contract). Paths are relative to the Admin-Portal repository.

JC.CAP is the consuming half of CAP single sign-on: the package an application takes so that its users sign in at CAP and arrive back as an ordinary ASP.NET Core session, with `IUserInfo` populated and the rest of the JC suite none the wiser. It is the second identity authority built on JC.Identity.Shared, beside JC.Identity.

This document sets out what the package would contain and why, marks what is already decided, and ends with the questions that change the build. Section 4 answers the question that stopped the first attempt: why an application talking to CAP has any sign-in endpoints of its own when CAP.SSO already names CAP's login page.

---

## 0. How to read this

Three kinds of statement appear below, and they are labelled:

- **Decided.** Either recorded in CAP's design documents, or settled in conversation on 2026-09-03. Not reopened here.
- **Proposed.** Claude's recommendation, with the reasoning. Open to change.
- **Open.** A choice that changes the work materially and needs John's call. Collected in section 12, each with a recommendation.

Five source files already exist under `JC.CAP/` from the interrupted first attempt (section 11). They encode proposals, not decisions, and will be reshaped to whatever this review settles.

---

## 1. What JC.CAP is, and is not

**Decided (sso.md §4.2.1, §10).** JC.CAP wraps `OpenIddict.Client.AspNetCore` rather than Microsoft's OpenID Connect handler, so both ends of the wire sit on `OpenIddict.Abstractions` and CAP.SSO can define the vocabulary by reference. It supplies an `IUserInfo` on top of JC.Identity.Shared so the application's audit trail names the real end user, stamped `IdentityAuthority.CAP`.

**Decided (2026-09-03).**

1. The session is token-backed. The cookie carries CAP's tokens, the access token is refreshed silently as it expires, and a refresh CAP refuses ends the session. This is what makes a withdrawal in CAP end an application session within fifteen minutes, which CAP's design was built to achieve.
2. No Razor. JC.CAP ships services, helpers and endpoint mappings, never pages. Every page a user sees is either CAP's or the application's own.
3. Tenancy is a separate package. **JC.CAP.Tenancy** (John's) references JC.Tenancy, owns a table mapping a CAP user id to a tenant id, and populates `IUserInfo.TenantId`. JC.CAP's job is to give it a hook (section 5.6).
4. The cookie looks like ASP.NET Identity's. Same claim types for the identifier, username, email and roles, plus the `DefaultClaims` block, so the shared middleware, `[Authorize(Roles = ...)]`, `User.IsInRole` and any application code written against a JC.Identity principal keep working without configuration.

**Proposed.** JC.CAP is to CAP what JC.Identity is to ASP.NET Core Identity: it owns the authentication scheme, the cookie, the `IUserInfo`, and the account-rule defaults for its audience. What it does not own is anything CAP owns: credentials, the second factor, registration, account management, and the decision of who may sign in.

What it deliberately does not contain:

| Not in JC.CAP | Why |
|---|---|
| A user table or `DbContext` | CAP holds the account. The application holds only what it needs against the `sub`, and JC.CAP.Tenancy is the first example |
| Login, register or manage pages | They are CAP's, branded per application through `SsoEndpoints.ForApplication` |
| Tenant resolution | JC.CAP.Tenancy |
| CAP's cookie or scheme names | CAP's own, and CAP.SSO's audit already keeps them out of the contract |
| A second OIDC provider | One registration, for CAP. An application wanting Google as well adds it itself |

---

## 2. The wire CAP presents

Everything below is defined in CAP.SSO and verified against CAP's `ConnectController` and `IdentityRegistration`. Only the parts that shape the package are restated.

**Flow.** Authorization code with PKCE, S256 only. Refresh tokens. Client credentials for the API and nothing else. Access token **15 minutes**, refresh token **14 days**, rolling (OpenIddict's default: each refresh issues a new refresh token and retires the old one, with a short reuse leeway for concurrent requests).

**Gates.** CAP re-checks the application, the account and its membership at authorize, at every token exchange including refresh, and at userinfo. The token is a snapshot; userinfo is live. Disabling an application revokes its stored tokens and authorisations.

**Claims a user token carries.**

| Claim | Meaning | Scope that releases it |
|---|---|---|
| `sub` | CAP account id, stable for life | always |
| `preferred_username` | the username, which in CAP is the email | `profile` or `cap_identity` |
| `name` | the person's full name | `profile` or `cap_identity` |
| `email` | the email address | `email` or `cap_identity` |
| `role` | one claim per role, bare key, this application only | `roles` |
| eight `DefaultClaims` names | `is_enabled`, `email_confirmed`, `display_name`, `phone_number`, `phone_number_confirmed`, `two_factor_enabled`, `last_login_utc`, `registration_utc` | `cap_identity` |

Deliberately absent: `tenant_id`, `require_password_change`, the lockout trio. Booleans are the literal strings `true`/`false`; dates are ISO 8601 round-trip with a `Z`.

**Every CAP application is registered** confidential, implicit consent, with permission for the authorize, token and end-session endpoints, the code, refresh and client-credentials grants, and the `profile`, `email`, `roles`, `cap_identity` and `cap_api` scopes. Redirect and post-logout URIs are whatever the operator enters on CAP's settings page, whose placeholder is `https://example.com/signin-oidc`.

**The API** at `/sso/api` takes a client-credentials token carrying `cap_api`: application info, users (all, filtered, by id) and the role-catalogue publish. Refusals are `ApiError` with a reason of `invalid_scope` (the client's configuration) or `application_unavailable` (needs a CAP operator, do not retry).

**Two traps CAP.SSO documents that this package must close by construction:** the handler's role claim type (or `[Authorize(Roles)]` silently refuses), and `is_enabled` (absent reads as disabled). Section 6.1 is how.

---

## 3. Where JC.Identity.Shared already does the work

JC.CAP adds nothing to the shared runtime; it feeds it. For orientation, the pieces it leans on:

| Shared piece | What JC.CAP does with it |
|---|---|
| `UserInfoBase` | `CapUserInfo` derives from it and adds nothing |
| `UserInfoExtensions.PopulateFrom(principal, options)` | Reads the cookie principal per request. Because the cookie carries Identity's claim types, `IdentityProjectionOptions` stays at its defaults; only `Authority = CAP` is set |
| `IdentityMiddlewareOptions.Default` | JC.CAP configures CAP-appropriate routes before the application's callback runs, so the application overrides rather than starts from `/Identity/Account/...` |
| `UserInfoMiddleware`, `IdentityMiddleware` | Registered by `UseCap` in the same order `UseIdentity` uses |

CAP.SSO's `OIDC.Projection` preset (`sub`/`email`/`role`) is **not** applied to the projection options under this design, because the cookie is translated into Identity's vocabulary first (section 6.1). The preset remains the map the translation follows.

---

## 4. The sign-in, end to end

This is the section that answers "why define a login path when CAP.SSO handles that".

### 4.1 Two different things called login

CAP.SSO's `SsoEndpoints.SsoLoginPath` is `/sso/user/login` **on CAP's host**: the page where a person types a password. JC.CAP never serves it, replaces it, or links to it directly for a sign-in.

An OpenID Connect relying party cannot send a user to that page and get a session back. A sign-in begins with an **authorize request** to `/connect/authorize` carrying the client id, redirect URI, scopes, a `state`, a `nonce` and a PKCE code challenge, all generated and remembered by the OpenIddict client on the application's side. CAP answers that request by showing its login page if the browser holds no SSO session, or by issuing a code immediately if it does (silent SSO, sso.md §4.3). Either way CAP then sends the browser to the **application's redirect URI** with the code, and the application's OpenIddict client exchanges it for tokens.

So the application has two protocol duties of its own, neither of which is a login page:

1. **Start the challenge.** Something has to call `ChallengeAsync` on the OpenIddict client scheme, with the URL to return to afterwards.
2. **Receive the callback.** The redirect URI CAP is configured with must resolve to an endpoint in the application that reads the OpenIddict result and issues the session cookie.

The first attempt named the trigger `LoginPath` and defaulted it to `/cap/login`. The name was wrong: it invites exactly the confusion John raised. What it is, is the **challenge trigger**, and section 4.3 asks whether it should exist at all.

### 4.2 The sequence

```
Browser                Application (JC.CAP)                    CAP (SSO host)
  |  GET /orders          |                                        |
  |---------------------->|  [Authorize] finds no cookie           |
  |  302 (challenge)      |  OpenIddict client builds authorize    |
  |<----------------------|  request, sets correlation cookie      |
  |  GET /connect/authorize?client_id=...&code_challenge=...&state=...
  |--------------------------------------------------------------->|
  |                       |                  no SSO session: login page shown
  |                       |                  SSO session: gates run, code issued
  |  302 to redirect URI with code + state                          |
  |<---------------------------------------------------------------|
  |  GET /signin-oidc?code=...&state=...                            |
  |---------------------->|  OpenIddict validates state, exchanges |
  |                       |  code (PKCE), reads userinfo, merges   |
  |                       |  claims. Passthrough to JC.CAP's       |
  |                       |  callback endpoint, which translates   |
  |                       |  the claims, runs enrichers, stores    |
  |                       |  tokens, signs in the JC.CAP cookie    |
  |  302 /orders + cookie |                                        |
  |<----------------------|                                        |
```

From here every request authenticates on the cookie alone. CAP is not contacted again until the access token nears expiry (section 6.3).

### 4.3 Starting the challenge: two ways

**A. Cookie first.** The JC.CAP cookie is the default scheme, including for challenges. An unauthenticated `[Authorize]` request redirects to the cookie's `LoginPath` with `ReturnUrl`, and a small JC.CAP endpoint there issues the OpenIddict challenge with `RedirectUri = ReturnUrl`. This is the shape of OpenIddict's own web samples and of ASP.NET Identity (whose `LoginPath` is a page; here it is a redirect). It gives the application a stable local URL a "Sign in" link can point at.

**B. Direct.** The OpenIddict client scheme is the default challenge scheme. `[Authorize]` challenges CAP with no local hop, and no trigger endpoint exists. A "Sign in" link points at any protected page.

The difference that matters is the return URL. The authorisation middleware challenges with no `RedirectUri`; the cookie handler always supplies one (the current URL) before redirecting to `LoginPath`, whereas whether the OpenIddict client defaults a missing `RedirectUri` to the current request is **unverified** (the source check was not completed). If it does not, option B lands every user on `/` after signing in.

**Proposed:** A, with the trigger renamed so it cannot be read as a login page: `/cap/signin` (open question 12.1). B stays available to an application by pointing the default challenge scheme at OpenIddict itself.

### 4.4 The other endpoints, and whether each earns its place

| Endpoint | Verbs | Role | Required? |
|---|---|---|---|
| callback (`/signin-oidc`) | GET, POST | CAP returns the code here. Reads the OpenIddict result, builds the cookie principal, stores tokens, signs in, redirects to the return URL | **Yes.** Without it there is no session |
| sign-out (`/cap/signout`) | POST | Clears the JC.CAP cookie, then asks OpenIddict to end the CAP session with the id token as hint, returning to the post-logout callback | **Yes**, unless the application accepts a local-only sign-out that leaves the CAP session alive |
| post-logout callback (`/signout-callback-oidc`) | GET, POST | CAP returns here after ending its session; redirects to the return URL stored at sign-out | Yes, if sign-out ends the CAP session |
| challenge trigger (`/cap/signin`) | GET | Issues the OpenIddict challenge with the return URL | Under option A |
| re-check (`/cap/refresh`) | GET | Refreshes the tokens now and re-reads live state from CAP, then redirects to the return URL | Proposed, section 7.2 explains why |
| denied (`/cap/denied`) | GET | Where the identity rules send a disabled account: re-check, and if still refused, sign out and hand over to CAP's branded denied page | Proposed, section 7.2 |
| two-factor (`/cap/two-factor`) | GET | Where the rules send an account owing enrolment: re-check, and if still unenrolled, hand over to CAP's enrolment page | Proposed, section 7.2 |

Minimal endpoints, mapped by one `MapCap()` call, rather than a controller: a controller in a class library is only discovered if the application has registered MVC controllers and mapped them, which a Razor Pages application need not have done. Endpoints work in every hosting shape and let the redirection endpoint carry `AllowAnonymous` and `DisableAntiforgery` metadata explicitly. This is also the reason the paths are `/cap/*` rather than `/Identity/Account/*`: there is no Identity UI here to collide with, and a short prefix reads as JC.CAP's.

The two callback paths keep Microsoft's conventional names because CAP's settings page shows `signin-oidc` as its placeholder, so an operator sees the same string in both places (open question 12.2).

---

## 5. Proposed package surface

### 5.1 Registration

```csharp
// Code-configured
builder.Services.AddCap(options =>
{
    options.Issuer = "https://sso.cap.example";
    options.ClientId = "evbfqxmh";
    options.ClientSecret = builder.Configuration["CAP:ClientSecret"]!;
    options.RoleCatalogue = CapRoles.GetCatalogue<AppRoles>();
});

// Bound from the "CAP" configuration section, with optional code on top
builder.Services.AddCap(builder.Configuration);
```

Full signature, mirroring `AddIdentity`:

```csharp
AddCap(Action<CapOptions> configure,
       Action<IdentityMiddlewareOptions>? configureMiddleware = null,
       Action<CookieAuthenticationOptions>? configureCookie = null,
       Action<OpenIddictClientBuilder>? configureClient = null)

AddCap(IConfiguration configuration, Action<CapOptions>? configure = null, /* same three */)

AddCap<TUserInfo>(...)   // a derived CapUserInfo, as AddIdentity<..., TUserInfo> allows
```

`configureClient` hands the application the raw OpenIddict builder after JC.CAP's configuration, for the things a package should not decide for everyone: production certificates, a resilience pipeline, re-enabling token storage where an OpenIddict database exists.

What `AddCap` registers:

| Registration | Lifetime | Notes |
|---|---|---|
| `CapOptions` with validation on start | singleton | Issuer absolute http(s), client id and secret present, `openid` in scopes, local paths start with `/` |
| `IdentityMiddlewareOptions` CAP defaults | transient `IConfigureOptions` | Registered before the application's callback so the application's settings win (section 7.1) |
| Everything `AddSharedIdentityServices<CapUserInfo>` registers | | Projection options left at Identity's claim types; `Authority = CAP` |
| Authentication: default scheme `JC.CAP`, cookie handler under that name | | Cookie options built from `CapOptions`, then the application's `configureCookie` |
| `CapCookieEvents` | scoped | `ValidatePrincipal` performs the silent refresh (section 6.3) |
| `CapSessionRefresher` | scoped | The refresh itself, shared by the cookie events and the re-check endpoints |
| `ICapClaimsPrincipalFactory` → `CapClaimsPrincipalFactory` | scoped, `TryAdd` | Section 6.1; replaceable |
| OpenIddict client: code + refresh + client-credentials flows, one registration for CAP, ASP.NET Core integration with both passthroughs, System.Net.Http, Data Protection for state tokens, ephemeral keys | | Section 10 |
| `CapApiClient` typed `HttpClient`, `CapAccessTokenProvider` | transient / singleton | Section 9 |
| `CapLinks` | singleton | Section 5.5 |
| `CapRoleCataloguePublisher` | hosted | Runs once at startup when `RoleCatalogue` is set (section 8) |
| `TimeProvider.System` | singleton, `TryAdd` | So the refresh timing is testable |

### 5.2 Options

`CapOptions`, bound from the `CAP` section when the `IConfiguration` overload is used:

| Option | Default | Why it exists |
|---|---|---|
| `Issuer` | required | CAP's `SSO:BaseUrl`, the OIDC authority. The one URL that must never change |
| `ClientId`, `ClientSecret` | required | From CAP's settings page. The secret is shown once |
| `Scopes` | `openid`, `roles`, `cap_identity`, `offline_access` | `cap_identity` is self-sufficient for name and email, so `profile`/`email` are not defaulted (open question 12.6). `offline_access` is what makes the refresh model possible. Binding from configuration adds to this set rather than replacing it, a known binder behaviour worth documenting |
| `CallbackPath` | `/signin-oidc` | Must match a redirect URI registered at CAP |
| `PostLogoutCallbackPath` | `/signout-callback-oidc` | Must match a post-logout URI registered at CAP |
| `SignInPath` | `/cap/signin` | The challenge trigger (12.1). Was `LoginPath` in the first attempt |
| `SignOutPath` | `/cap/signout` | POST only |
| `RefreshPath`, `DeniedPath`, `TwoFactorPath` | `/cap/refresh`, `/cap/denied`, `/cap/two-factor` | Section 7.2 |
| `RoleCatalogue` | `null` | `null` publishes nothing. An empty list is a valid publish meaning "no roles" |
| `AllowInsecureHttp` | `false` | Development only: lets the callbacks answer over http |
| `Session.Lifetime` | 14 days | Cookie lifetime, sliding. CAP's refresh lifetime is the ceiling in practice |
| `Session.Persistent` | `false` | Whether the cookie outlives the browser (12.3) |
| `Session.RefreshSkew` | 1 minute | Refresh this far ahead of access-token expiry |
| `Session.RefreshFailureGrace` | 5 minutes | How long a session survives past expiry when CAP is unreachable (12.4) |

### 5.3 Middleware

```csharp
app.UseCap();   // UseAuthentication, UseUserInfo, UseAuthorization, UseIdentityMiddleware
app.MapCap();   // the endpoints in section 4.4
```

Same order as `UseIdentity`, for the same reasons: the projection must follow authentication and precede the rules. `MapCap` is separate because `IApplicationBuilder` cannot map endpoints; `WebApplication` is both, so a typical `Program.cs` calls the two in succession.

### 5.4 Services

**`CapApiClient`** (section 9), **`CapAccessTokenProvider`** (the cached client-credentials token behind it), **`CapSessionRefresher`** (section 6.3), **`CapRoleCataloguePublisher`** (section 8).

### 5.5 `CapLinks`

The absolute, branded URLs into CAP's account surface, built from the issuer and client id the application already holds:

```csharp
links.Manage              // {issuer}/sso/user/manage/{clientId}
links.Security            // .../manage/security/{clientId}
links.PersonalData
links.EnableAuthenticator
links.Register            // meaningful only when CAP says registration is standard
links.ForgotPassword
links.Denied
links.Discovery           // {issuer}/.well-known/openid-configuration
links.For(route)          // any SsoEndpoints constant
```

CAP.SSO's `SsoEndpoints.ForApplication(route, clientId)` gives the path; composing it onto the issuer needs the consumer's configuration, which is why this lives in JC.CAP rather than CAP.SSO. The issuer is normalised to a trailing slash before combining, so an issuer with a path segment is not truncated.

### 5.6 Hooks

**`ICapClaimsPrincipalFactory`.** `Task<ClaimsPrincipal> CreateAsync(ClaimsPrincipal capPrincipal, bool isRefresh, CancellationToken)`. The default does the translation in section 6.1 then runs the enrichers. Registered with `TryAdd`, so an application can replace it the way JC.Identity lets one replace `IUserClaimsPrincipalFactory`.

**`ICapClaimsEnricher`.** `Task EnrichAsync(CapPrincipalContext context, CancellationToken)`, resolved as `IEnumerable<>` from the request scope, run in registration order at sign-in **and at every refresh**. The context carries the identity being built, the raw CAP principal, the user id, and whether this is a refresh.

This is the hook for JC.CAP.Tenancy: look up the tenant for `context.UserId`, add a `DefaultClaims.TenantId` claim. The shared projection already reads that claim when present, so `IUserInfo.TenantId` and JC.Tenancy's `ITenantInfo` follow with no further wiring. That keeps JC.Identity's shape, where the tenant reaches the runtime by claim, and because enrichers run on refresh a tenant change propagates within fifteen minutes rather than at the next sign-in.

### 5.7 Helpers

**`CapRoles`.** The application's role declarations, on JC.Identity's `SystemRoles` convention (section 8.2).

### 5.8 File layout

Mirrors JC.Identity:

```
JC.CAP/
  Authentication/   CapDefaults, CapEndpoints, CapClaimsPrincipalFactory, ICapClaimsEnricher,
                    CapCookieEvents, CapSessionRefresher, CapTokens (internal)
  Extensions/       ServiceCollectionExtensions (AddCap), ApplicationBuilderExtensions (UseCap),
                    EndpointRouteBuilderExtensions (MapCap)
  Helpers/          CapRoles, LocalUrl (internal)
  Models/           CapUserInfo, Options/CapOptions, Options/CapOptionsValidator
  Services/         CapApiClient, CapAccessTokenProvider, CapApiException, CapLinks,
                    CapRoleCataloguePublisher
  README.md
```

---

## 6. The session cookie

### 6.1 Claims: CAP's vocabulary in, Identity's vocabulary out

**Decided:** the cookie carries the claim types an ASP.NET Identity cookie carries. **Proposed** mapping, applied by `CapClaimsPrincipalFactory` at sign-in and on every refresh:

| From CAP (merged id token + userinfo) | Onto the cookie | Note |
|---|---|---|
| `sub` | `ClaimTypes.NameIdentifier` | Required; a principal without it is refused |
| `preferred_username`, else `email`, else `sub` | `ClaimTypes.Name` | The username. `Identity.Name` is what the projection reads as `Username`, and CAP's `name` is the *full* name, which is why it is not used here |
| `email` | `ClaimTypes.Email` | |
| each `role` | `ClaimTypes.Role` | The identity is constructed with `ClaimTypes.Name` and `ClaimTypes.Role` as its name and role claim types, so `User.IsInRole` and `[Authorize(Roles)]` read them |
| the eight `DefaultClaims` present | same names, verbatim | CAP already emits them under JC's names and in the formats the projection parses |
| `name` | `display_name` | Only where `cap_identity` was not requested and `display_name` is therefore absent |
| (enrichers) | `tenant_id` and anything else | JC.CAP.Tenancy's contribution |

Absent from the cookie: Identity's security stamp (there is no local store to validate against; CAP's refresh gate plays that role), the lockout trio and `require_password_change` (absent reads as false, which is what JC.Identity's `"False"` reads as too), and OpenIddict's own WS-Federation claim mapping, which is switched off so the translation is the only source of `ClaimTypes.*` values.

This is why both CAP.SSO traps close by construction: `is_enabled` arrives under the name the projection reads, and the role claim type on the identity is the one the framework checks.

`ClaimsPrincipalExtensions.GetCapRoles()` from CAP.SSO reads the bare `role` type and so returns nothing against this cookie. It is written for a raw token principal; in a JC.CAP application `User.IsInRole` and `IUserInfo.IsInRole` are the calls.

### 6.2 What the cookie stores besides claims

The tokens, in the authentication properties under OpenIddict's own names: the backchannel access token, its expiry (ISO round-trip, which is what OpenIddict writes), the id token (needed as the hint at sign-out) and the refresh token. Roughly three to four kilobytes; the cookie handler chunks. An application that objects can set a server-side `SessionStore` through `configureCookie`.

### 6.3 Silent refresh

In `CookieAuthenticationEvents.ValidatePrincipal`, which runs inside authentication and therefore before the projection middleware sees the principal:

1. If the stored expiry is more than `RefreshSkew` away, do nothing.
2. Otherwise call OpenIddict's `AuthenticateWithRefreshTokenAsync` with the stored refresh token. OpenIddict exchanges it, validates the new tokens and, because userinfo is not disabled, reads userinfo, so the merged principal reflects CAP's **live** state: roles granted since sign-in appear, a newly enrolled authenticator appears.
3. Rebuild the cookie principal through the factory (enrichers run again), replace the principal on the context, store the new tokens including the rolled refresh token, and set `ShouldRenew` so the handler reissues the cookie.

If no refresh token was stored (the application dropped `offline_access`), the session ends at access-token expiry. That makes `offline_access` the switch between a fifteen-minute session and a long one.

### 6.4 When refresh fails

Two kinds of failure, treated differently:

- **CAP refused** (`OpenIddictExceptions.ProtocolException`, typically `invalid_grant`): the account, membership or application was withdrawn, or the refresh token expired. Reject the principal and sign the cookie out. This is the design's whole point.
- **CAP could not be reached** (anything else): keep the current principal and retry on the next request, but only while the access token expired less than `RefreshFailureGrace` ago. Past that, sign out. Failing closed immediately would sign users out on any network blip; failing open indefinitely would let a withdrawal ride out an outage. Five minutes is the compromise offered; open question 12.4.

### 6.5 Concurrency

Two requests arriving as the token expires both refresh. CAP's refresh tokens roll, but OpenIddict's server keeps a reuse leeway (30 seconds by default, to confirm against CAP's configuration) precisely for this, so the second exchange succeeds and the last cookie written wins. No client-side lock is proposed for v1; if double refreshes show up in CAP's token table they are cosmetic, and a keyed lock can be added later.

### 6.6 Sign-out

POST to the sign-out endpoint: read the cookie for the id token, sign the cookie out, then `SignOut` on the OpenIddict scheme with the id token hint and the return URL. OpenIddict redirects to CAP's end-session endpoint, CAP clears its SSO cookie and returns to the post-logout callback, which redirects to the return URL. If the request carries no session, it simply redirects.

POST only, following CAP's own logout ("a link cannot sign a visitor out"). Antiforgery is validated when the application has `IAntiforgery` registered, which every Razor Pages or MVC application does; a plain minimal-API host without it skips the check, and the documentation says so.

---

## 7. Identity rules for a CAP application

### 7.1 Defaults

`IdentityMiddlewareOptions.Default` as JC.CAP sets it before the application's own callback:

| Setting | JC.CAP default | Why |
|---|---|---|
| `Name` | `CAP` | Log readability |
| `RequirePasswordChange` | `false` | CAP never issues a token to an account still owing one; the claim is absent, and the rule would only ever redirect off a stale cookie |
| `ChangePasswordRoute` | CAP's forced-password page, absolute | Never fires; set so the option is not left pointing at Identity UI |
| `EnforceTwoFactor` | `false` | The application's decision, as in JC.Identity |
| `TwoFactorRoute` | `/cap/two-factor` | Section 7.2 |
| `AccessDeniedRoute` | `/cap/denied` | Section 7.2 |
| `LogoutRoute` | `/cap/signout` | Excluded from enforcement, as the rules require |
| `ErrorRoute` | `/Error` | Unchanged, the application's |
| `AdditionalExcludedPaths` | the sign-in trigger, both callbacks, `/cap/refresh` | So a session mid-repair is never judged by the rules it is trying to satisfy |

### 7.2 The two loops, and why the denied and two-factor routes are local

The obvious defaults are CAP's pages: send a disabled account to `/sso/user/denied/{clientId}` and an unenrolled one to `/sso/user/manage/security/enable-authenticator/{clientId}`. Both loop.

The cookie is a snapshot. A user sent to CAP to enrol an authenticator comes back with a cookie that still says `two_factor_enabled=false`, so the rule sends them straight back to CAP, for up to fifteen minutes. A user re-enabled by an operator is stuck at the denied page for the same reason. CAP's session notes record this exact family of rule-set loop three times on its own surface.

So the two routes point at small local endpoints that **refresh first**:

- `/cap/two-factor`: refresh the tokens (live state); if `two_factor_enabled` is now true, redirect home; otherwise redirect to CAP's enrolment page. On the way back the rule fires again, the endpoint refreshes again, the claim is true, and the user is through.
- `/cap/denied`: refresh; CAP refuses a disabled account at the token endpoint, so the refresh fails, the session is signed out and the user is sent to CAP's branded denied page. If an operator has since re-enabled them, the refresh succeeds and they are back in.
- `/cap/refresh?returnUrl=`: the same mechanism for the application's own use, for instance after telling a user their roles have changed.

The redirect targets are fixed by the package from the issuer and client id, never taken from the query string, so none of these is an open redirect.

---

## 8. Roles

### 8.1 Publishing

**Decided (sso.md §3.1):** the application publishes its role catalogue to CAP over JC.CAP, on startup or on demand; CAP marks anything not republished stale rather than deleting it.

**Proposed:** a hosted service publishes `CapOptions.RoleCatalogue` once at startup with the client-credentials token, logs the `CatalogueSync` counts, and warns when `recased` is non-empty, quoting each key CAP holds in a different case, because that is a bug in the application's source rather than noise. A publish failure is logged and the application keeps starting; `application_unavailable` is never retried in a loop, per the contract. `CapApiClient.PublishRolesAsync` is the on-demand path.

### 8.2 Declaring roles

JC.Identity's convention, extended by nothing:

```csharp
public class AppRoles : CapRoles
{
    public const string Editor = nameof(Editor);
    public const string EditorDesc = "Can create and edit content.";
}

options.RoleCatalogue = CapRoles.GetCatalogue<AppRoles>();
```

`GetCatalogue<T>()` reflects over public `const string` fields, skips those ending `Desc`, and produces `ApplicationRoleDto` with `Key` = the constant's value, `DisplayName` = the field name with spaces before capitals ("PageEditor" becomes "Page Editor"), and `Description` = the matching `Desc` constant. An application wanting other display names builds the list itself; the DTO is a plain class.

`CapRoles` defines no roles of its own. The application owns what its roles mean.

### 8.3 Casing

CAP keeps the casing a key was first published with and every role check is ordinal, so `nameof` constants and the token agree by construction as long as the constant never changes case. The `recased` warning is the safety net for the day one does.

---

## 9. The API client

`CapApiClient`, a typed `HttpClient` against the issuer:

| Method | Endpoint | Returns |
|---|---|---|
| `GetApplicationAsync` | `ApplicationApi.InfoPath` | `ApplicationInfoDto`. `Registration` is what decides whether to show a register link |
| `GetUsersAsync(search, enabledAccounts = true)` | `UsersApi.UsersPath` | `IReadOnlyList<ApplicationUserDto>` |
| `GetAllUsersAsync(search)` | `UsersApi.AllUsersPath` | every member |
| `GetUserAsync(userId, enabledAccounts = true)` | `UsersApi.UserPath` | the member, or `null` on 404 |
| `PublishRolesAsync(roles)` | `RolesApi.RoleCatalogueSyncPath` | `CatalogueSync` |

Query parameter names come from `ApiEndpoints` constants, never literals. `enabledAccounts` is always sent as `true` or `false`, per the contract's "never empty".

**Token.** `CapAccessTokenProvider` holds one client-credentials token for the process, refreshing it under a lock thirty seconds before expiry. A 401 from the API invalidates it and the call retries once.

**Errors.** A non-success response throws `CapApiException` carrying the status code, the `ApiError.Reason` when CAP sent one, and the prose `Error` for logs. A body with no reason is a fault; the framework's `ProblemDetails` is recognised and its title used. A refused token issuance surfaces the OIDC error string. `application_unavailable` is documented as "stop and tell an operator".

---

## 10. Packaging

**Dependencies** (all 7.6.1, already in the local cache): `OpenIddict.Client.AspNetCore`, `OpenIddict.Client.SystemNetHttp`, `OpenIddict.Client.DataProtection`, plus `CAP.SSO` 1.0.0 and the two JC.Identity.Shared halves the csproj already references. Versions go in `Directory.Packages.props`.

**Keys.** Verified in OpenIddict's source: the client refuses to start without at least one signing and one encryption credential once a redirection endpoint is configured. Proposed: ephemeral keys to satisfy that, with state tokens formatted through ASP.NET Core Data Protection (`UseDataProtection()`), so they survive a restart and work across a farm through the application's existing key ring rather than a certificate every consumer must provision. An application can add certificates through `configureClient`.

**Token storage.** The OpenIddict client stores the tokens it receives in a database by default, which needs `AddCore()` and OpenIddict's tables in every consuming application. Proposed: `DisableTokenStorage()`, because the tokens JC.CAP needs live in the cookie and CAP holds the authoritative copies. OpenIddict describes the option as "generally not recommended"; the cost is that the client cannot revoke its own stored copies, which this design never relies on. An application with an OpenIddict database can turn storage back on through `configureClient` (open question 12.5).

**Version.** `7.1.0`: a new package is a minor release, as JC.Content was in 6.1.0. The suite-wide bump to 7.1.0 is John's release step, not part of this build.

**Documentation to write**, per the writing guide: `JC.CAP/README.md`, `Documentation/JC.CAP/{Setup,Guide,API}.md`, the root README's table, dependency tree and quick start, and a `Release-Notes/Minor/v7.1.0.md`.

---

## 11. What exists on disk today

From the interrupted first attempt, all encoding **proposals**:

| File | Encodes | Will change if |
|---|---|---|
| `Authentication/CapDefaults.cs` | scheme `JC.CAP`, cookie `.JC.CAP.Session`, registration id `cap` | 12.7 |
| `Authentication/CapEndpoints.cs` | the seven default paths, including `LoginPath = /cap/login` | 12.1, 12.2 |
| `Models/Options/CapOptions.cs`, `CapOptionsValidator.cs` | section 5.2 as written, with the `LoginPath` name | 12.1 to 12.4 |
| `Models/CapUserInfo.cs` | an empty derivation of `UserInfoBase` | unlikely |
| `JC.CAP.csproj` | placeholder version, CAP.SSO and JC.Identity.Shared references, no OpenIddict yet | section 10 |

---

## 12. Open questions

Each with a recommendation. None is blocking a decision on the rest.

**12.1 The challenge trigger.** Keep a local trigger endpoint (option A, section 4.3) or challenge CAP directly from `[Authorize]` (option B)? Recommendation: A, renamed `/cap/signin` with the option `SignInPath`, because the return URL is guaranteed and the application gets a stable link. If John would rather have no local endpoint, B needs the OpenIddict default-`RedirectUri` behaviour verified first.

**12.2 Callback path names.** `/signin-oidc` and `/signout-callback-oidc` (Microsoft's convention, and CAP's settings-page placeholder) or `/cap/callback` and `/cap/signout-callback` (everything under one prefix)? Recommendation: the former, so the operator entering the URI at CAP sees the same string the placeholder shows.

**12.3 Persistent cookie.** Default the cookie to survive the browser closing? Recommendation: no. A session cookie ends with the browser, and a user whose CAP SSO session persists is signed back in silently anyway, so little is lost and a shared machine is safer. `Session.Persistent` stays available.

**12.4 CAP unreachable.** Keep a session alive through a CAP outage for a grace period, or fail closed at expiry? Recommendation: the five-minute grace in section 6.4. The alternative is simpler and stricter, and if John prefers strict the option disappears.

**12.5 Token storage.** Disable OpenIddict's client-side token storage by default, so consumers need no OpenIddict database? Recommendation: yes, for the reasons in section 10, with re-enabling documented.

**12.6 Default scopes.** Add `profile` and `email` to the defaults alongside `cap_identity`, or leave them out because `cap_identity` already releases name, username and email? Recommendation: leave them out; fewer scopes in the authorize URL and nothing lost.

**12.7 Scheme name.** `JC.CAP` as the cookie scheme, or reuse `IdentityConstants.ApplicationScheme` (`Identity.Application`) so code that names Identity's scheme keeps working? Recommendation: `JC.CAP`. `[Authorize]` uses the default scheme without naming it, and borrowing Identity's constant would mislead anyone reading the cookie jar.

**12.8 Role declaration.** The `CapRoles` const convention with `Desc` suffixes, or only an explicit `ApplicationRoleDto` list? Recommendation: both, as section 8.2 has it; the convention is what JC.Identity applications already know.

**12.9 The re-check endpoints.** Are `/cap/refresh`, `/cap/denied` and `/cap/two-factor` worth three endpoints, or should the rule routes point straight at CAP's pages and accept the fifteen-minute loop? Recommendation: keep them. They share one function, and the loop is the kind of defect that is invisible until the first user enrols.

---

## 13. Out of scope for this package

- Mapping a CAP user to a tenant (JC.CAP.Tenancy).
- More than one OIDC registration, or any provider other than CAP.
- Calling CAP with a *user's* access token. CAP's API is client-credentials only, and the access token is stored solely so its expiry can be tracked.
- Reference access tokens and introspection (CAP's 08-28 notes defer this to a JC.CAP validation side that does not yet exist).
- Blazor Server or WebAssembly hosting. The cookie model is the target; anything else is a later package.

---

## 14. Suggested build order

Each step is provable against a running CAP before the next starts.

1. **Registration and sign-in.** `AddCap`, `UseCap`, `MapCap` with the callback and trigger, the claims factory, the cookie. Verify: an `[Authorize]` page signs in through CAP and `IUserInfo` shows the real user with `Authority = CAP`, `IsEnabled = true`, and roles readable by both `IUserInfo.IsInRole` and `[Authorize(Roles)]`.
2. **Sign-out.** Both directions. Verify: the CAP SSO cookie is gone afterwards and the return URL is honoured.
3. **Silent refresh.** Cookie events and the refresher. Verify against a running CAP: a role assigned in CAP appears within fifteen minutes; a disabled membership signs the user out at the next refresh; CAP stopped mid-session behaves per 12.4.
4. **Rule-set defaults and the re-check endpoints.** Verify the two-factor loop does not occur.
5. **API client and role publishing.** Verify: the catalogue appears in CAP for assignment, `recased` warns on a deliberately mis-cased key, `application_unavailable` is logged once and not retried.
6. **Enricher hook**, proven with a throwaway enricher stamping a fixed `tenant_id`, so JC.CAP.Tenancy has a verified seam to build on.
7. **Documentation** per section 10.
