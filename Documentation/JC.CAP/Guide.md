# JC.CAP — Guide

Covers signing in and out, what a CAP sign-in puts on the principal, the session and its silent refresh, enforcing account state, publishing roles, calling CAP's API, linking into CAP's pages, and extending the principal. See [Setup](Setup.md) for registration and every option.

Reading the current user, establishing an identity outside a request, and the account rules themselves belong to the shared runtime. See the [JC.Identity.Shared guide](../JC.Identity.Shared/Guide.md). This guide covers what is specific to signing in through CAP.

## Signing in and out

### A sign-in link

```html
<a href="/cap/signin?returnUrl=@Url.Page("/Orders/Index")">Sign in</a>
```

`/cap/signin` issues the authorize request to CAP and returns to `returnUrl` once the session exists. An `[Authorize]` page needs no link at all: the cookie is the default scheme, so an unauthenticated request is redirected to the same endpoint with the page's URL as the return URL.

What happens between the two: CAP shows its login page if the browser holds no CAP session, or issues a code immediately if it does, so a user moving between CAP applications is never prompted twice. CAP then sends the browser to `/signin-oidc` with the code, JC.CAP exchanges it for tokens, builds the cookie and redirects to the return URL. From there every request authenticates on the cookie alone.

**The return URL must be local.** Anything else is replaced with `/`, on every endpoint, so none of them can be used as an open redirect.

### A sign-out button

```html
<form method="post" action="/cap/signout">
    <input type="hidden" name="returnUrl" value="/" />
    <button type="submit">Sign out</button>
</form>
```

POST only, so a link cannot sign a visitor out. The form tag helper adds the antiforgery token, which the endpoint validates. The cookie is cleared first, then CAP is asked to end its own session with the id token as the hint, and CAP returns the browser through `/signout-callback-oidc` to the return URL. `returnUrl` is read from the query first, then the form.

**A request with no session simply redirects** to the return URL. Nothing is sent to CAP, since there is no CAP session to end.

### What lands on the principal

CAP's tokens speak OIDC; the cookie speaks ASP.NET Identity. `CapClaimsPrincipalFactory` translates between them at sign-in and on every refresh:

| On the cookie | From CAP |
|---------------|----------|
| `ClaimTypes.NameIdentifier` | `sub`, the CAP account id, stable for life |
| `ClaimTypes.Name` | `preferred_username`, which in CAP is the email, falling back to `email` then `sub` |
| `ClaimTypes.Email` | `email` |
| `ClaimTypes.Role`, one per role | `role`, bare keys as published, for this application only |
| `is_enabled`, `email_confirmed`, `display_name`, `phone_number`, `phone_number_confirmed`, `two_factor_enabled`, `last_login_utc`, `registration_utc` | The same names, copied as they are |
| `display_name` | `name`, only where CAP sent no `display_name` |

The identity is built with `ClaimTypes.Name` and `ClaimTypes.Role` as its name and role claim types, so everything below works with no configuration:

```csharp
[Authorize(Roles = AppRoles.Editor)]
public class EditModel(IUserInfo userInfo) : PageModel
{
    public bool CanPublish => User.IsInRole(AppRoles.Publisher) || userInfo.IsInRole(AppRoles.Publisher);
}
```

**Compare roles by the constant.** CAP keeps the casing a key was first published with and every check is ordinal, so `nameof` constants and the token agree by construction as long as the constant never changes case.

**`GetCapRoles()` from CAP.SSO returns nothing against this cookie.** It reads the bare `role` claim type, which is right for a raw token and wrong for the translated cookie. Use `User.IsInRole` or `IUserInfo.IsInRole`.

### Reading the current user

```csharp
public class AccountModel(IUserInfo userInfo) : PageModel
{
    public string Greeting => $"Welcome back, {userInfo.DisplayName ?? userInfo.Username}";
    public bool IsCapUser => userInfo.Authority == IdentityAuthority.CAP;
}
```

`IUserInfo` is projected from the cookie once per request, exactly as under JC.Identity. `TenantId` is empty unless an enricher supplies it, `RequiresPasswordChange` is always false, and the lockout members hold their defaults, because CAP does not release those. Everything else is populated from the claims above.

### Nuances and gotchas

**The cookie is a snapshot, refreshed every fifteen minutes.** A role granted in CAP, an authenticator enrolled, or a display name changed shows up at the next refresh, not at once. Where a change must take effect now, send the user through `/cap/refresh`, described next.

**A CAP session and an application session are different things.** Signing out of the application ends both. Signing out at CAP directly, on CAP's own pages, leaves the application's cookie alive until its next refresh fails, up to fifteen minutes later.

**Dropping `cap_identity` from the scopes locks every user out.** `IUserInfo.IsEnabled` defaults to false and is set only from the `is_enabled` claim that scope releases. The validator refuses to start without it for that reason.

## The session and its refresh

### How the refresh works

The cookie carries CAP's access token, its expiry, the id token and the refresh token. Inside cookie authentication, one minute before the access token expires, JC.CAP exchanges the refresh token with CAP, reads userinfo, rebuilds the principal through the claims factory and the enrichers, replaces the tokens, and renews the cookie. Nothing in the application sees any of it; the request continues with the refreshed principal.

CAP re-runs its gates on every refresh: the application still enabled, the account still enabled, the membership still live. That is what makes a withdrawal in CAP end an application session within fifteen minutes.

### Forcing a refresh now

```csharp
public IActionResult OnPostAcceptInvitation()
{
    // ... grant the role in CAP through its own tooling ...

    return LocalRedirect($"/cap/refresh?returnUrl={Url.Page("/Orders/Index")}");
}
```

`/cap/refresh` refreshes immediately and returns to the return URL. Use it after telling a user their access has changed, so the new cookie is in place before they land on the page that needs it.

### When CAP refuses

A refused refresh, because the account or membership was withdrawn or the refresh token has expired, ends the session at once: the principal is rejected and the cookie deleted. The next protected request challenges CAP, which either signs the user back in silently, if the refusal was only an expired token and their CAP session is alive, or shows them its branded denied page.

### When CAP cannot be reached

The session stands and the refresh is retried on the next request, for up to five minutes past the access token's expiry. After that it ends. Failing closed at once would sign every user out on a network blip; failing open indefinitely would let a withdrawal ride out an outage. `Session.RefreshFailureGrace` sets the period.

### Sessions without a refresh token

Remove `offline_access` from the scopes and no refresh token is issued. The session then ends when the access token does, fifteen minutes after sign-in, and the next request signs in again silently through CAP. That makes `offline_access` the switch between a fifteen-minute session and a long one.

### Nuances and gotchas

**Two requests arriving at expiry both refresh.** CAP's refresh tokens roll, but its server keeps a short reuse leeway for exactly this, so the second exchange succeeds and the last cookie written wins. No client-side lock is taken.

**A refresh mid-request rewrites the principal for that request.** `IUserInfo` is projected after authentication, so it sees the refreshed claims. Anything that read `HttpContext.User` before the projection ran, which nothing in the pipeline order `UseCap` sets does, would see the old ones.

**`Session.Persistent` decides whether the browser keeps the cookie**, not how long the session lasts. A session cookie ends with the browser, and a user whose CAP session persists is signed back in silently anyway.

## Enforcing account state

The three rules, disabled account, forced password change, two-factor, are the shared runtime's. JC.CAP points them at endpoints of its own rather than at pages, because the cookie is a snapshot and a page cannot refresh it.

### Enforcing two-factor

```csharp
builder.Services.AddCap(builder.Configuration,
    configureMiddleware: options => options.Default.EnforceTwoFactor = true);
```

A user without an authenticator is sent to `/cap/two-factor`, which refreshes the session first. If CAP now says they are enrolled, they go straight back to the page they were on. If not, they are handed over to CAP's enrolment page, branded for the application. When they return, the rule fires again, the endpoint refreshes again, the claim is true, and they are through. Without that refresh they would be bounced back to CAP for up to fifteen minutes after enrolling.

**CAP can enforce two-factor too**, per application, on its own settings page. That enforcement happens at CAP before a token is ever issued, so it needs nothing here. The switch above is the application's own, on top of whatever CAP requires.

**The user comes back on their own.** CAP's enrolment page does not know where they came from, so after enrolling they navigate back to the application themselves.

### Disabled accounts

A disabled account is sent to `/cap/denied`, which refreshes first. CAP refuses a refresh for a disabled account, so the session is ended and the user handed to CAP's denied page. If an operator has re-enabled them since the cookie was written, the refresh succeeds instead and they are back in where they were.

In practice this rarely fires from the rule: CAP never issues a token to a disabled account, so a cookie saying `is_enabled` is false comes only from a custom factory or enricher. The everyday path is the silent refresh refusing and the next challenge landing on CAP's denied page.

### Role refusals

A user who is enabled but lacks a role is forbidden, and `CapOptions.AccessDenied` decides what that becomes. The default is a plain 403, styled as the application's own:

```csharp
app.UseStatusCodePagesWithReExecute("/Error/{0}");
```

The re-execute keeps the original verb, so a refused POST reaches the error page as a POST; give the page an `OnPost` as well as an `OnGet`, or it renders with no handler run.

To send the user to a page of your own instead, with the URL they were refused in `returnUrl`:

```csharp
builder.Services.AddCap(builder.Configuration,
    configure: options =>
    {
        options.AccessDenied = CapAccessDenied.LocalPath;
        options.AccessDeniedPath = "/AccessDenied";
    });
```

`CapAccessDenied.CapDeniedPage` hands over to CAP's denied page instead. It is branded for the application but written for a refused sign-in, and the user leaves the application with no way back, so read [Setup](Setup.md#role-refusals) before choosing it.

**Do not route a role refusal to `/cap/denied`.** That endpoint refreshes and, finding an enabled account, sends the user straight back to the page that refused them.

### Nuances and gotchas

**The return URL comes from the rules.** The shared middleware appends the request's URL to the route as `returnUrl`, so both endpoints return the user where they were once the rule is satisfied.

**Do not point the rule routes at CAP's pages.** Both loop, for the reason above. The endpoints are the defaults and the routes are exposed only so they can be moved, not replaced.

**The forced-password rule is off and its route is absolute.** CAP never issues a token to an account owing a password change, so the claim is absent. The route points at CAP's forced-password page anyway, so nothing is left aimed at Identity UI, and because it is on another host the middleware does not append a return URL to it.

**Serving a second audience** works as it does under JC.Identity: add a rule set with a condition rather than exempting paths. The [shared guide](../JC.Identity.Shared/Guide.md#serving-more-than-one-audience) covers it.

## Roles and the catalogue

### Declaring roles

```csharp
public class AppRoles : SystemRoles
{
    public const string Editor = nameof(Editor);
    public const string EditorDesc = "Can create and edit content.";

    public const string PageEditor = nameof(PageEditor);
    public const string PageEditorDesc = "Can edit page content only.";
}
```

`SystemRoles.GetAllRoles<AppRoles>()` reflects over the public `const string` fields, pairs each with its `Desc`, and `SystemRoles.ToCatalogue` turns the result into what CAP publishes: the key, a display name derived from it, `Page Editor` for `PageEditor`, and the description.

### Publishing at startup

```csharp
var app = builder.Build();

app.UseCap();
app.MapCap();

await app.SyncCapRolesAsync<AppRoles>();
```

The application waits for CAP's answer and refuses to start if the publish fails, which is deliberate: a wrong secret or an unreachable CAP is found before the first user rather than by them. Pass `throwOnFail: false` to log and continue instead.

### Reading the result

```csharp
var sync = await app.SyncCapRolesAsync<AppRoles>();

if (sync is { Recased.Count: > 0 })
    app.Logger.LogWarning("Role keys differ in case from what CAP holds: {Recased}", sync.Recased);
```

`CatalogueSync` carries what CAP did: created, updated, unchanged and marked-stale counts, and `Recased`, mapping any key you sent to the form CAP holds where the two differ. The job already logs all of that, warning per recased key and when anything was marked stale, so reading the result is only needed to act on it.

### Publishing on a schedule

```csharp
builder.Services.AddCap(builder.Configuration);
builder.Services.AddHangfireJob<CapRoleSyncJob<AppRoles>>(options => options.Cron = "0 3 * * *");
```

`CapRoleSyncJob<AppRoles>` is the same code the startup call runs, as a JC.Core `IBackgroundJob`. Register it through JC.BackgroundJobs and drop the startup call, or keep both; a second publish of the same catalogue changes nothing.

### Publishing on demand

```csharp
public class RolesAdminModel(CapApiClient cap) : PageModel
{
    public async Task<IActionResult> OnPostRepublishAsync()
    {
        var catalogue = SystemRoles.ToCatalogue(SystemRoles.GetAllRoles<AppRoles>());
        var sync = await cap.PublishRolesAsync(catalogue);

        TempData["Message"] = sync.Display;
        return RedirectToPage();
    }
}
```

### Nuances and gotchas

**Send the full set every time.** Anything CAP holds that a publish does not name is marked stale, never deleted, so a renamed role keeps its assignments until a CAP operator decides. An empty catalogue is a valid publish meaning "no roles", so publishing an empty list by accident marks everything stale.

**A stale role is still issued.** Stale means "the application stopped publishing this, so an operator should decide", not "withdrawn". Tolerate receiving a role you no longer declare rather than treating it as impossible.

**Casing is permanent.** CAP keeps the form a key was first published with, and every check is ordinal. Publish `Editor` today and `editor` tomorrow and you get no error, a `recased` entry in the response, and tokens that keep carrying `Editor` while your constant says `editor`. Correct the constant, not CAP.

**Only `const string` fields count.** A `static readonly` is not a literal and is skipped, as is anything non-public or not a string. A name ending in `Desc` is a description, never a role.

## Calling CAP's API

`CapApiClient` calls CAP as the application, with a client-credentials token it obtains, caches and renews itself. Every endpoint identifies the caller from that token, so none takes a client id.

### What CAP is configured as for you

```csharp
public class LoginModel(CapApiClient cap, CapLinks links) : PageModel
{
    public bool OfferRegistration { get; private set; }
    public string RegisterUrl => links.Register;

    public async Task OnGetAsync()
    {
        var application = await cap.GetApplicationAsync();
        OfferRegistration = application.Registration == UserRegistration.Standard_Registration;
    }
}
```

`Registration` is the useful field: offer a register link only on standard registration, since CAP refuses self-registration in the other modes. Reading it rather than hardcoding it is what keeps the link right when an operator changes the setting.

### The application's members

```csharp
public class MembersModel(CapApiClient cap) : PageModel
{
    public IReadOnlyList<ApplicationUserDto> Members { get; private set; } = [];

    public async Task OnGetAsync(string? search)
    {
        Members = await cap.GetUsersAsync(search);              // live account and membership
        var all = await cap.GetAllUsersAsync(search);           // every member regardless
        var one = await cap.GetUserAsync("cap-account-id");     // null where not a member
    }
}
```

`GetUsersAsync` returns members whose account and membership are both live; pass `enabledAccounts: false` for the rest. `GetAllUsersAsync` is the only way to get everyone. `GetUserAsync` answers `null` for a 404, which CAP returns both for a stranger and for a member the filter excludes.

### Members from the cache

Most pages that name a member, a dropdown of assignees, a list of who can be granted something, want the members without a round trip to CAP each time. `CapUserCache` holds every live member for a configurable window:

```csharp
public class AuditModel(CapUserCache members) : PageModel
{
    public IReadOnlyList<CapUser> Members { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Members = await members.GetUsersAsync();

        var author = await members.GetUserAsync("cap-account-id");
        var name = author?.DisplayName ?? "Unknown";
    }
}
```

Every member is a cache entry of its own, so `GetUserAsync` is a single lookup, and the whole set is read from CAP again, in one call and under a lock, whenever the window has passed or any entry has been evicted. A fresh set that does not name an id answers `null` without going to CAP, so a lookup for a stranger, or for a member removed since the last refresh, costs nothing until the next window.

```csharp
await members.RefreshAsync();   // read everyone from CAP now
members.Invalidate();           // drop everything held, so the next read goes to CAP
```

The cache holds live members only, those whose account and membership are both enabled, through `GetUsersAsync`, and each `CapUser` carries the member's `Roles` as CAP returned them. A disabled account or a suspended membership is absent, so `GetUserAsync` answers `null` for them exactly as it does for a stranger; read them from `CapApiClient` directly where a page needs them.

**Five minutes is the default window**, on `Cache:UserLifetime`, and `Cache:Enabled` turns the cache off altogether for an application that wants every read live. See [Setup](Setup.md#capcacheoptions).

### Handling a refusal

```csharp
try
{
    var sync = await cap.PublishRolesAsync(catalogue);
}
catch (CapApiException ex) when (ex.IsApplicationUnavailable)
{
    // CAP is not serving this application: disabled, deleted, or its tenant has lapsed.
    // Nothing here will clear it. Tell an operator, and do not retry in a loop.
    logger.LogError(ex, "CAP has withdrawn this application.");
}
catch (CapApiException ex)
{
    logger.LogError(ex, "CAP's API answered {Status} ({Reason}).", ex.StatusCode, ex.Reason);
}
```

`Reason` is CAP's machine-readable half: `InvalidScope` means the token lacks `cap_api`, which is configuration; `ApplicationUnavailable` means CAP is not serving the application, which needs an operator. `Message` is prose for a log and must not be matched on. `StatusCode` is `0` and `OidcError` is set when the failure was obtaining the token in the first place, for instance `invalid_client` for a wrong secret.

### Nuances and gotchas

**The API is client-credentials only.** A user's access token is never sent to it, and `cap_api` is never requested on a user's sign-in. The token is the application's, cached for the process and renewed thirty seconds before it expires.

**A 401 is retried once** with a fresh token, in case the held one was revoked. A second 401 surfaces as a `CapApiException`.

**Calls time out after thirty seconds.**

## Linking into CAP

`CapLinks` builds absolute URLs into CAP's account pages, branded for the application by carrying its client id, from the host and client id you already configured:

```html
@inject CapLinks Links

<a href="@Links.Manage">My account</a>
<a href="@Links.Security">Password and two-factor</a>
<a href="@Links.EnableAuthenticator">Set up an authenticator</a>
<a href="@Links.ForgotPassword">Forgotten password</a>
```

`Register` is meaningful only when CAP reports standard registration, as shown [above](#what-cap-is-configured-as-for-you). `For(route)` takes any constant from CAP.SSO's `SsoEndpoints` and brands it the same way; `Discovery` is the discovery document, unbranded.

**Link to the branded form.** CAP remembers the application for the rest of the visit once a branded link has been followed, but the first link has to carry the client id, which is what these do.

## Extending the principal

### Enriching the principal

An `ICapClaimsEnricher` adds claims after the translation, at sign-in and on every refresh, so anything it stamps stays current. The tenant is the canonical example:

```csharp
public class TenantEnricher(AppDbContext db) : ICapClaimsEnricher
{
    public async Task EnrichAsync(CapPrincipalContext context, CancellationToken cancellationToken = default)
    {
        var tenantId = await db.UserTenants
            .Where(t => t.CapUserId == context.UserId)
            .Select(t => t.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenantId is not null)
            context.Identity.AddClaim(new Claim(DefaultClaims.TenantId, tenantId));
    }
}
```

```csharp
builder.Services.AddCap(builder.Configuration);
builder.Services.AddScoped<ICapClaimsEnricher, TenantEnricher>();
```

Nothing more is needed: the shared projection already reads `tenant_id`, so `IUserInfo.TenantId` and JC.Tenancy's `ITenantInfo` follow. Because enrichers run on refresh, a changed tenant reaches the session within fifteen minutes rather than at the next sign-in.

`context.UserId` is the CAP account id, `context.CapPrincipal` the raw principal CAP returned if you need a claim the translation did not carry, and `context.IsRefresh` says whether this is a rebuild rather than a sign-in.

### Replacing the claims factory

```csharp
public class AppClaimsPrincipalFactory(
    IEnumerable<ICapClaimsEnricher> enrichers,
    ILogger<CapClaimsPrincipalFactory> logger)
    : CapClaimsPrincipalFactory(enrichers, logger)
{
    public override async Task<ClaimsPrincipal> CreateAsync(ClaimsPrincipal capPrincipal, bool isRefresh,
        CancellationToken cancellationToken = default)
    {
        var principal = await base.CreateAsync(capPrincipal, isRefresh, cancellationToken);
        ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim("signed_in_via", "cap"));

        return principal;
    }
}
```

```csharp
builder.Services.AddCap(builder.Configuration);
builder.Services.AddScoped<ICapClaimsPrincipalFactory, AppClaimsPrincipalFactory>();
```

Registered after `AddCap`, so it replaces the default. An enricher is the lighter tool for adding claims; replace the factory when the translation itself has to change.

### Carrying extra properties on IUserInfo

```csharp
public class AppUserInfo : CapUserInfo
{
    public string? DepartmentId { get; set; }
}
```

```csharp
builder.Services.AddCap<AppUserInfo>(builder.Configuration);
```

Populate the addition after the standard projection has run, registering the pipeline by hand in place of `UseCap`:

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
app.MapCap();
```

The claim itself comes from an enricher, as above.

### Working outside a request

A background job that acts for a CAP user projects one of the API's records:

```csharp
public class NightlyDigest(IServiceProvider services, CapApiClient cap)
{
    public async Task RunAsync(string capUserId)
    {
        var member = await cap.GetUserAsync(capUserId)
            ?? throw new InvalidOperationException("Not a member.");

        await using var scope = services.CreateAsyncScopeForUser(new CapUser(member), member.Roles);

        var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        await repo.SaveAsync(order);   // audited against that user, Authority CAP
    }
}
```

`CapUser` is the `IApplicationUser` shape of an `ApplicationUserDto`, and `CreateAsyncScopeForUser` is the shared runtime's, which stamps `Authority` from the projection options. `new CapUserInfo(member)` builds the same projection directly, roles included, where a scope is not wanted. Both leave `TenantId` alone; pass it to the scope yourself where the job knows it.

## Next steps

- [Setup](Setup.md): registration, options and their defaults.
- [API Reference](API.md)
- [JC.Identity.Shared — Guide](../JC.Identity.Shared/Guide.md): reading the current user, background-job identity, the account rules.
