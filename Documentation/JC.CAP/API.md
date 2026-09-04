# JC.CAP — API reference

Every public type and member in JC.CAP. See [Setup](Setup.md) for registration and [Guide](Guide.md) for usage.

> **Note:** Registration extensions (`IServiceCollection`, `IApplicationBuilder`, `IEndpointRouteBuilder`) and options classes are documented in [Setup](Setup.md), not here. That covers `AddCap`, `UseCap`, `MapCap`, `SyncCapRolesAsync`, `CapOptions` and `CapSessionOptions`.
>
> The authority-agnostic runtime, `UserInfoBase`, `UserInfoExtensions`, `IdentityRules` and `DefaultClaims`, belongs to JC.Identity.Shared and is documented in [its API reference](../JC.Identity.Shared/API.md). The wire contract, `SsoEndpoints`, `ApiEndpoints`, `OIDC`, `CapDictionary` and the DTOs, belongs to CAP.SSO and is documented in that package's README.

## Models

### CapUser

**Namespace:** `JC.CAP.Models`

A CAP account as JC.Core's `IApplicationUser`, built from the `ApplicationUserDto` CAP's users API returns. What `BaseUser` is to JC.Identity, minus the persistence: there is no store behind it, and writing to it changes nothing at CAP.

#### Constructor

##### CapUser(ApplicationUserDto dto)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `dto` | `ApplicationUserDto` | required | The member, as returned by CAP's users API. |

Copies `UserId`, `Username`, `Email`, `EmailConfirmed`, `DisplayName`, `PhoneNumber`, `PhoneNumberConfirmed`, `IsEnabled`, `TwoFactorEnabled`, `LastLoginUtc`, `RegistrationUtc` and `Roles` onto the matching properties.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `string` | from the DTO | get; set; | The CAP account id, the `sub` claim. |
| `UserName` | `string?` | from the DTO | get; set; | The username, which in CAP is the email address. |
| `Email` | `string?` | from the DTO | get; set; | The email address. |
| `EmailConfirmed` | `bool` | from the DTO | get; set; | Whether the address has been confirmed. |
| `PhoneNumber` | `string?` | from the DTO | get; set; | The phone number, where CAP holds one. |
| `PhoneNumberConfirmed` | `bool` | from the DTO | get; set; | Whether the number has been confirmed. |
| `TwoFactorEnabled` | `bool` | from the DTO | get; set; | Whether the account holds an authenticator. Enrolment is account-wide at CAP. |
| `LockoutEnabled` | `bool` | `false` | get; set; | Not released by CAP; holds the default. |
| `LockoutEnd` | `DateTimeOffset?` | `null` | get; set; | Not released by CAP; holds the default. |
| `AccessFailedCount` | `int` | `0` | get; set; | Not released by CAP; holds the default. |
| `DisplayName` | `string?` | from the DTO | get; set; | The person's full name. |
| `IsEnabled` | `bool` | from the DTO | get; set; | The account as a whole. Membership of the application is a separate switch at CAP. |
| `RequirePasswordChange` | `bool` | `false` | get; set; | Always false: CAP never issues a token to an account still owing a password change. |
| `LastLoginUtc` | `DateTime?` | from the DTO | get; set; | When they last signed in to CAP, not to the application. |
| `RegistrationUtc` | `DateTime?` | from the DTO | get; set; | When the CAP account was created. |
| `IdentityTenantId` | `string?` | `null` | get; set; | Not released by CAP. CAP's tenancy is not the application's. |
| `Roles` | `IReadOnlyList<string>` | from the DTO | get; set; | The role keys held in this application, as CAP returned them. Empty where they hold none. |

### CapUserInfo

**Namespace:** `JC.CAP.Models`

The CAP `IUserInfo`. Extends `UserInfoBase`, whose members are documented in the [JC.Identity.Shared API reference](../JC.Identity.Shared/API.md#userinfobase); this type adds only constructors. Registered as the scoped `IUserInfo` by `AddCap` unless a different implementation is supplied, and populated per request from the session cookie by the shared claims middleware, which stamps `Authority` as `IdentityAuthority.CAP`.

#### Constructors

##### CapUserInfo()

Initialises an unpopulated instance for dependency injection to activate. The claims middleware fills it in per request.

##### CapUserInfo(CapUser user, IEnumerable&lt;string?&gt; roles)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `user` | `CapUser` | required | The user record to project. |
| `roles` | `IEnumerable<string?>` | required | The user's role keys. Null and empty entries are discarded. |

Projects the user through the base constructor, then sets `Authority` to `IdentityAuthority.CAP`. Leaves `TenantId` alone: the tenant is not CAP's to supply.

##### CapUserInfo(ApplicationUserDto user)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `user` | `ApplicationUserDto` | required | The member, as returned by CAP's users API. |

Wraps the DTO in a `CapUser` and delegates to the constructor above with the DTO's `Roles`, so behaviour is identical.

### CapPrincipalContext

**Namespace:** `JC.CAP.Authentication`

What an `ICapClaimsEnricher` is given: the identity being built and where it came from. Sealed.

#### Constructor

##### CapPrincipalContext(ClaimsIdentity identity, ClaimsPrincipal capPrincipal, string userId, bool isRefresh)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `identity` | `ClaimsIdentity` | required | The session identity under construction. |
| `capPrincipal` | `ClaimsPrincipal` | required | The principal as CAP returned it. |
| `userId` | `string` | required | The CAP account id. |
| `isRefresh` | `bool` | required | Whether this is a rebuild after a token refresh. |

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Identity` | `ClaimsIdentity` | | get; | The session identity under construction. Claims added here reach the cookie. |
| `CapPrincipal` | `ClaimsPrincipal` | | get; | The merged principal OpenIddict built from CAP's tokens and userinfo, before translation. |
| `UserId` | `string` | | get; | The CAP account id, the `sub` claim. |
| `IsRefresh` | `bool` | | get; | `true` when rebuilding after a token refresh rather than at sign-in. |

### CapRefreshResult

**Namespace:** `JC.CAP.Models`

The result of a token refresh. A sealed positional record.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Outcome` | `CapRefreshOutcome` | required | get; init; | What the refresh came to. |
| `Principal` | `ClaimsPrincipal?` | `null` | get; init; | The rebuilt principal. Set only when `Outcome` is `Refreshed`. |
| `Error` | `Exception?` | `null` | get; init; | What went wrong. Set only when `Outcome` is `Refused` or `Unavailable`. |

#### Static members

| Member | Description |
|--------|-------------|
| `NoRefreshToken` | The shared instance for a session holding no refresh token. |
| `Refreshed(ClaimsPrincipal principal)` | A result carrying the rebuilt principal. |
| `Refused(Exception error)` | A refusal, carrying the exception CAP's answer became. |
| `Unavailable(Exception error)` | An unreachable CAP, carrying the exception. |

### CapApiException

**Namespace:** `JC.CAP.Models`

Thrown when CAP's API refused or failed a call, or when the access token for it could not be obtained. Extends `Exception`. `Message` is prose for a log and must not be matched on; the machine-readable parts are the properties.

#### Constructor

##### CapApiException(string message, int statusCode = 0, ApiErrorReason? reason = null, string? oidcError = null, Exception? innerException = null)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `message` | `string` | required | Prose describing the failure. |
| `statusCode` | `int` | `0` | The HTTP status CAP answered with, or `0` where the failure was obtaining the token. |
| `reason` | `ApiErrorReason?` | `null` | CAP's reason, when it sent one. |
| `oidcError` | `string?` | `null` | The OIDC error code, when the token endpoint refused. |
| `innerException` | `Exception?` | `null` | The underlying exception. |

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `StatusCode` | `int` | `0` | get; | The HTTP status CAP answered with, or `0` when the failure was obtaining the access token. |
| `Reason` | `ApiErrorReason?` | `null` | get; | Why CAP refused, when it said. `InvalidScope` is the caller's configuration; `ApplicationUnavailable` needs a CAP operator. |
| `OidcError` | `string?` | `null` | get; | The OIDC error code, such as `invalid_client`, when CAP's token endpoint refused to issue an access token. |
| `IsApplicationUnavailable` | `bool` | | get; | Whether `Reason` is `ApplicationUnavailable`: CAP is not currently serving this application, and nothing the caller does will clear it. |

## Enums

### CapRefreshOutcome

**Namespace:** `JC.CAP.Enums`

What a token refresh against CAP came to.

| Value | Description |
|-------|-------------|
| `Refreshed` | CAP issued new tokens and the principal was rebuilt from its live state. |
| `Refused` | CAP refused: access was withdrawn, or the refresh token has expired. The session ends. |
| `Unavailable` | CAP could not be reached, or answered with a server error. Nothing is known about the account. |
| `NoRefreshToken` | The session holds no refresh token, so it cannot be re-checked before it ends. |

### CapAccessDenied

**Namespace:** `JC.CAP.Enums`

What a role refusal becomes: an authenticated user reaching a page whose `[Authorize(Roles = ...)]` they do not satisfy. Distinct from the identity rules' denied route, which handles a disabled account. Read by `CapCookieEvents.RedirectToAccessDenied`.

| Value | Description |
|-------|-------------|
| `Forbid` | A plain 403 for the application to style. The default. |
| `CapDeniedPage` | A redirect to CAP's denied page, `CapLinks.Denied`, branded for the application. |
| `LocalPath` | A redirect to `CapOptions.AccessDeniedPath`, carrying the return URL. |

## Services

### CapClaimsPrincipalFactory

**Namespace:** `JC.CAP.Authentication`

The default `ICapClaimsPrincipalFactory`: CAP's vocabulary in, ASP.NET Identity's out, so the shared projection, `[Authorize(Roles = ...)]` and `User.IsInRole` all read the cookie unchanged. Registered as `ICapClaimsPrincipalFactory` with `TryAdd`, so a registration made before `AddCap` is kept and one made after replaces it. Inject `ICapClaimsPrincipalFactory`.

#### Constructor

##### CapClaimsPrincipalFactory(IEnumerable&lt;ICapClaimsEnricher&gt; enrichers, ILogger&lt;CapClaimsPrincipalFactory&gt; logger)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `enrichers` | `IEnumerable<ICapClaimsEnricher>` | required | Every registered enricher, in registration order. |
| `logger` | `ILogger<CapClaimsPrincipalFactory>` | required | Records each principal built, at debug level. |

#### Methods

##### CreateAsync(ClaimsPrincipal capPrincipal, bool isRefresh, CancellationToken cancellationToken = default)

**Returns:** `Task<ClaimsPrincipal>`

**Access:** `public virtual`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `capPrincipal` | `ClaimsPrincipal` | required | The merged principal OpenIddict built from CAP's tokens and userinfo. |
| `isRefresh` | `bool` | required | `true` when rebuilding after a token refresh rather than at sign-in. |
| `cancellationToken` | `CancellationToken` | `default` | Cancels the enrichers. |

Reads the `sub` claim and throws `InvalidOperationException` if it is missing or empty. Builds a new `ClaimsIdentity` with authentication type `JC.CAP`, name claim type `ClaimTypes.Name` and role claim type `ClaimTypes.Role`, then adds, in order: `ClaimTypes.NameIdentifier` from `sub`; `ClaimTypes.Name` from `preferred_username`, falling back to `email`, then to `sub`; `ClaimTypes.Email` from `email` where present; one `ClaimTypes.Role` per distinct `role` claim, compared ordinally; each of the eight claims in `OIDC.UserClaims.All` under its own name where present and non-empty; and `display_name` from `name` only where no `display_name` was copied. An empty claim value counts as absent throughout.

Then constructs a `CapPrincipalContext` and runs each enricher in turn, awaiting each before the next. Returns a new `ClaimsPrincipal` over the identity. Nothing from `capPrincipal` other than the claims named above reaches the result.

### ICapClaimsEnricher

**Namespace:** `JC.CAP.Authentication`

A hook adding claims to the session principal after the CAP translation, at sign-in and on every refresh. Standalone interface: JC.CAP registers no implementation. Resolved as `IEnumerable<ICapClaimsEnricher>` from the request scope and run in registration order by `CapClaimsPrincipalFactory`.

#### Methods

##### EnrichAsync(CapPrincipalContext context, CancellationToken cancellationToken = default)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `CapPrincipalContext` | required | The identity being built, the raw CAP principal, the user id and whether this is a refresh. |
| `cancellationToken` | `CancellationToken` | `default` | Cancels the work. |

Adds claims to `context.Identity`. An exception propagates out of the callback or the refresh that ran it.

### CapSessionRefresher

**Namespace:** `JC.CAP.Services`

Exchanges the session's refresh token with CAP and rebuilds the principal from CAP's live state. Shared by `CapCookieEvents`, which runs it as the access token nears expiry, and the re-check endpoints, which run it on demand. Scoped.

#### Constructor

##### CapSessionRefresher(OpenIddictClientService client, ICapClaimsPrincipalFactory factory, ILogger&lt;CapSessionRefresher&gt; logger)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `client` | `OpenIddictClientService` | required | OpenIddict's client, which performs the exchange and reads userinfo. |
| `factory` | `ICapClaimsPrincipalFactory` | required | Rebuilds the principal from the refreshed one. |
| `logger` | `ILogger<CapSessionRefresher>` | required | Records refusals at information level and unreachability at warning level. |

#### Methods

##### RefreshAsync(AuthenticationProperties properties, CancellationToken cancellationToken = default)

**Returns:** `Task<CapRefreshResult>`

**Access:** `public virtual`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `properties` | `AuthenticationProperties` | required | The session's authentication properties, carrying the tokens. Updated in place on success. |
| `cancellationToken` | `CancellationToken` | `default` | Cancels the exchange. |

Reads the refresh token from `properties` and returns `NoRefreshToken` where there is none. Otherwise asks OpenIddict to authenticate with it against the `cap` registration, which exchanges it at CAP's token endpoint and reads userinfo.

An `OpenIddictExceptions.ProtocolException` whose error is not `server_error`, `temporarily_unavailable` or `slow_down` is a refusal, returned as `Refused` carrying the exception. Any other exception, including a protocol exception with one of those three errors, which is how OpenIddict reports a transport failure, an unparseable response or a 5xx, is returned as `Unavailable`. An `OperationCanceledException` propagates.

On success, rebuilds the principal through the factory with `isRefresh` true, then stores the new access token, its expiry, the id token and the refresh token on `properties`, keeping the previous id token or refresh token where CAP did not reissue one. Returns `Refreshed` carrying the rebuilt principal.

### CapCookieEvents

**Namespace:** `JC.CAP.Services`

The cookie handler's events: the silent token refresh as the access token nears expiry, and what a role refusal becomes. Extends `CookieAuthenticationEvents`. Set as the cookie's `EventsType` by `AddCap` and resolved from the request scope, so the refresh runs inside cookie authentication and the projection middleware only ever sees the refreshed principal. Scoped.

#### Constructor

##### CapCookieEvents(CapSessionRefresher refresher, CapLinks links, IOptions&lt;CapOptions&gt; options, TimeProvider clock, ILogger&lt;CapCookieEvents&gt; logger)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `refresher` | `CapSessionRefresher` | required | Performs the refresh. |
| `links` | `CapLinks` | required | Supplies CAP's denied page for `CapAccessDenied.CapDeniedPage`. |
| `options` | `IOptions<CapOptions>` | required | Read for `Session.RefreshSkew`, `Session.RefreshFailureGrace` and `AccessDenied`. |
| `clock` | `TimeProvider` | required | The current time, so the timing is testable. |
| `logger` | `ILogger<CapCookieEvents>` | required | Records why a session was ended, at information level. |

#### Methods

##### ValidatePrincipal(CookieValidatePrincipalContext context)

**Returns:** `Task`

**Access:** `public override`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `CookieValidatePrincipalContext` | required | The cookie handler's validation context. |

Reads the access token's expiry from the ticket's properties. Returns at once where none is stored, which means the principal was signed in by something other than the callback, or where the current time is earlier than the expiry less `RefreshSkew`.

Otherwise refreshes and acts on the outcome. `Refreshed`: replaces the principal and sets `ShouldRenew`, so the handler reissues the cookie with the updated properties. `Refused`: rejects the principal and signs the cookie out. `NoRefreshToken`: rejects and signs out only once the access token has expired. `Unavailable`: rejects and signs out only once the current time is past the expiry plus `RefreshFailureGrace`; before that the principal stands and the next request tries again.

##### RedirectToAccessDenied(RedirectContext&lt;CookieAuthenticationOptions&gt; context)

**Returns:** `Task`

**Access:** `public override`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `context` | `RedirectContext<CookieAuthenticationOptions>` | required | The redirect the handler built to the cookie's `AccessDeniedPath`, carrying the return URL. |

Called by the cookie handler for every forbid, since the framework never answers one with a bare status. Acts on `CapOptions.AccessDenied`. `Forbid`: sets the response status to 403 and returns. `CapDeniedPage`: for a request carrying `X-Requested-With: XMLHttpRequest`, sets 403 with `CapLinks.Denied` as the `Location` header; otherwise redirects to it. `LocalPath`: defers to the base implementation, which redirects to the cookie's `AccessDeniedPath`, set from `CapOptions.AccessDeniedPath` by `AddCap`, or answers 403 with a `Location` for the same header.

### CapLinks

**Namespace:** `JC.CAP.Services`

The absolute, branded URLs into CAP's account surface, built from the host and client id in `CapOptions`. CAP.SSO's `SsoEndpoints` supplies the paths and `ForApplication` appends the client id; composing them onto the host needs the application's configuration, which is why this lives here. Singleton.

#### Constructor

##### CapLinks(IOptions&lt;CapOptions&gt; options)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `options` | `IOptions<CapOptions>` | required | Read for `BaseUrl` and `ClientId`. |

Normalises `BaseUrl` to a trailing slash before it is used as a base, so a host carrying a path segment is not truncated when a route is combined onto it.

#### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Manage` | `string` | | get; | The account home: profile, security and personal data. |
| `Security` | `string` | | get; | Password and two-factor. |
| `PersonalData` | `string` | | get; | Download or delete the personal data CAP holds. |
| `EnableAuthenticator` | `string` | | get; | Enrol an authenticator. Where the two-factor endpoint hands over to. |
| `ForcedPassword` | `string` | | get; | The forced set-password screen. The rules' change-password route. |
| `Register` | `string` | | get; | Self-registration. Meaningful only when CAP reports standard registration for the application. |
| `ForgotPassword` | `string` | | get; | Starts a password reset. |
| `Denied` | `string` | | get; | Where a refused sign-in lands. Where the denied endpoint hands over to. |
| `Discovery` | `string` | | get; | CAP's discovery document. Not branded. |

Every branded property is `For` applied to the matching `SsoEndpoints` constant, computed on each read.

#### Methods

##### For(string route)

**Returns:** `string`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `route` | `string` | required | An `SsoEndpoints` constant, or any path under CAP's account surface. |

Appends the client id through `SsoEndpoints.ForApplication`, combines the result onto the normalised host and returns the absolute URI.

### CapAccessTokenProvider

**Namespace:** `JC.CAP.Services`

The client-credentials token JC.CAP calls CAP's API with: one per process, renewed under a lock shortly before it expires, and discarded on demand so the next call fetches a fresh one. Implements `IDisposable`. Singleton.

#### Constructor

##### CapAccessTokenProvider(OpenIddictClientService client, TimeProvider clock, ILogger&lt;CapAccessTokenProvider&gt; logger)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `client` | `OpenIddictClientService` | required | OpenIddict's client, which performs the client-credentials grant. |
| `clock` | `TimeProvider` | required | The current time, so renewal timing is testable. |
| `logger` | `ILogger<CapAccessTokenProvider>` | required | Records a refused issuance at error level. |

#### Methods

##### GetTokenAsync(CancellationToken cancellationToken = default)

**Returns:** `Task<string>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | Cancels the request to CAP. |

Returns the held token where it is good for at least the next thirty seconds. Otherwise takes the lock, checks again, and requests a new one from CAP with the `cap_api` scope against the `cap` registration. The token and its expiry are held for the process; a result with no expiry is held with a past one, so the next call asks again rather than trusting a guess.

Throws `CapApiException` with `StatusCode` `0` and `OidcError` set when CAP's token endpoint refuses, for instance `invalid_client` for a wrong secret. Any other failure, such as the discovery document being unreachable, propagates as the exception it was.

##### Invalidate()

**Returns:** `void`

Discards the held token. Called by `CapApiClient` after a 401, so the next call fetches a fresh one.

##### Dispose()

**Returns:** `void`

Releases the lock.

### CapApiClient

**Namespace:** `JC.CAP.Services`

CAP's API, called as the application with the client-credentials token. Every endpoint identifies the caller from that token, so none of them takes a client id. Holds one `FlurlClient` against `BaseUrl` with a thirty-second timeout, serialising with `JsonSerializerDefaults.Web`. Singleton.

Every call obtains a token from `CapAccessTokenProvider`, sends, and on a 401 invalidates the token and sends once more with a fresh one. A non-success status is turned into a `CapApiException`: the body is read as CAP's `ApiError` first, giving the exception CAP's prose and `Reason`; then as `ProblemDetails`, giving it the title; otherwise the exception names the status alone. Each is logged at warning level.

#### Constructor

##### CapApiClient(IOptions&lt;CapOptions&gt; options, CapAccessTokenProvider tokens, ILogger&lt;CapApiClient&gt; logger)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `options` | `IOptions<CapOptions>` | required | Read for `BaseUrl`. |
| `tokens` | `CapAccessTokenProvider` | required | Supplies and invalidates the access token. |
| `logger` | `ILogger<CapApiClient>` | required | Records refusals and faults at warning level. |

#### Methods

##### GetApplicationAsync(CancellationToken cancellationToken = default)

**Returns:** `Task<ApplicationInfoDto>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | Cancels the call. |

Reads how CAP is configured for the calling application: its name, its registration mode and whether it enforces two-factor.

##### GetUsersAsync(string? search = null, bool enabledAccounts = true, CancellationToken cancellationToken = default)

**Returns:** `Task<IReadOnlyList<ApplicationUserDto>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `search` | `string?` | `null` | Free text matching an email or display name containing it, or a user id exactly. Omitted from the request when null. |
| `enabledAccounts` | `bool` | `true` | `true` returns members whose account and membership are both live; `false` returns the rest. Always sent, never omitted. |
| `cancellationToken` | `CancellationToken` | `default` | Cancels the call. |

##### GetAllUsersAsync(string? search = null, CancellationToken cancellationToken = default)

**Returns:** `Task<IReadOnlyList<ApplicationUserDto>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `search` | `string?` | `null` | As above. |
| `cancellationToken` | `CancellationToken` | `default` | Cancels the call. |

Every member of the application, enabled or not, and the only way to get all of them.

##### GetUserAsync(string userId, bool enabledAccounts = true, CancellationToken cancellationToken = default)

**Returns:** `Task<ApplicationUserDto?>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string` | required | The CAP account id. Throws `ArgumentException` when null or whitespace. |
| `enabledAccounts` | `bool` | `true` | As for `GetUsersAsync`. |
| `cancellationToken` | `CancellationToken` | `default` | Cancels the call. |

One member by id, or `null` where CAP answers 404, which it does both for an id that is not a member of the calling application and for one the filter excludes. The id is placed in the path as given, unencoded.

##### PublishRolesAsync(IReadOnlyList&lt;ApplicationRoleDto&gt; roles, CancellationToken cancellationToken = default)

**Returns:** `Task<CatalogueSync>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `roles` | `IReadOnlyList<ApplicationRoleDto>` | required | The full catalogue. An empty list is a valid publish meaning no roles. |
| `cancellationToken` | `CancellationToken` | `default` | Cancels the call. |

Posts the catalogue to CAP's role sync endpoint and returns what CAP did with it. Anything CAP holds that the list does not name is marked stale rather than removed.

### CapUserCache

**Namespace:** `JC.CAP.Services`

The application's live members, those whose account and membership are both enabled, read from CAP's users API through `CapApiClient.GetUsersAsync` and kept in memory for `CapCacheOptions.UserLifetime`. A disabled account or a suspended membership is never held, so a lookup for one answers `null`. Each member is an `IMemoryCache` entry of its own under a `jc-cap:user:` prefix, and the set of ids is held with the time it was read, replaced as one so a reader never sees half a refresh. Implements `IDisposable`. Singleton.

When `CapCacheOptions.Enabled` is `false`, every method reads from CAP and nothing is held.

#### Constructor

##### CapUserCache(IMemoryCache cache, CapApiClient client, IOptions&lt;CapOptions&gt; options, TimeProvider clock, ILogger&lt;CapUserCache&gt; logger)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cache` | `IMemoryCache` | required | Holds the per-member entries. |
| `client` | `CapApiClient` | required | Reads the members from CAP. |
| `options` | `IOptions<CapOptions>` | required | Read for `Cache.Enabled` and `Cache.UserLifetime`. |
| `clock` | `TimeProvider` | required | The current time, so the window is testable. |
| `logger` | `ILogger<CapUserCache>` | required | Records each refresh at debug level. |

#### Methods

##### GetUsersAsync(CancellationToken cancellationToken = default)

**Returns:** `Task<IReadOnlyList<CapUser>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | Cancels a read from CAP. |

Assembles every member from the cache where the set was read within the window and every entry is still held. Otherwise refreshes: takes the lock, checks again in case another request refreshed while this one waited, reads every member from CAP, stores each as an entry expiring after the window, records the ids and the time, and returns them. One evicted entry refreshes the whole set.

##### GetUserAsync(string userId, CancellationToken cancellationToken = default)

**Returns:** `Task<CapUser?>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `userId` | `string` | required | The CAP account id. Throws `ArgumentException` when null or whitespace. |
| `cancellationToken` | `CancellationToken` | `default` | Cancels a read from CAP. |

Returns the member's entry where it is held. Otherwise, where the set was read within the window and does not name the id, returns `null` without going to CAP, so a stranger or a member removed since the last refresh costs nothing until the window passes. Otherwise refreshes as `GetUsersAsync` does and returns the member from the result, or `null` where they are not a member.

##### RefreshAsync(CancellationToken cancellationToken = default)

**Returns:** `Task<IReadOnlyList<CapUser>>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | Cancels the read from CAP. |

Reads every member from CAP now, under the lock, replacing what is held, and returns them.

##### Invalidate()

**Returns:** `void`

Drops the set of ids and removes every member's entry, so the next read goes to CAP. Does nothing where nothing is held.

##### Dispose()

**Returns:** `void`

Releases the lock.

### CapRoleSyncJob&lt;TRoles&gt;

**Namespace:** `JC.CAP.Services`

Publishes the roles declared on `TRoles` to CAP. Implements JC.Core's `IBackgroundJob`, so JC.BackgroundJobs can run it on a schedule; `SyncCapRolesAsync` runs it once at startup. Registered by `AddCap` as an open generic, scoped, so any closed form resolves.

| Type parameter | Constraint | Description |
|----------------|-----------|-------------|
| `TRoles` | `SystemRoles` | The application's roles class. |

#### Constructor

##### CapRoleSyncJob(CapApiClient client, ILogger&lt;CapRoleSyncJob&lt;TRoles&gt;&gt; logger)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `client` | `CapApiClient` | required | Performs the publish. |
| `logger` | `ILogger<CapRoleSyncJob<TRoles>>` | required | Records the counts and any warnings. |

#### Methods

##### ExecuteAsync(CancellationToken cancellationToken = default)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | Cancels the publish. |

The `IBackgroundJob` entry point. Calls `SyncAsync` and discards the result.

##### SyncAsync(CancellationToken cancellationToken = default)

**Returns:** `Task<CatalogueSync>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | Cancels the publish. |

Reflects over `TRoles` with `SystemRoles.GetAllRoles`, projects the result with `SystemRoles.ToCatalogue`, and publishes it through `CapApiClient`. Logs the counts at information level, warns when CAP marked any role stale, and warns once per recased key, quoting the form CAP holds. Returns CAP's answer. A failure propagates as the exception it was, typically `CapApiException`.

## Helpers

### SystemRoles

**Namespace:** `JC.CAP.Authentication`

The base a CAP application declares its roles on, and the helpers that read and project them. Declares no roles of its own, unlike JC.Identity's class of the same name: an application signing in through CAP defines its whole catalogue, and CAP operators assign from it. Extend it with a `const string` per role and a matching `{Name}Desc` for the description.

#### Methods

##### GetAllRoles&lt;T&gt;()

**Returns:** `List<RoleRecord>`

**Constraint:** `where T : SystemRoles`

Reflects over `T` for public static literal string fields, flattening the hierarchy. Only `const` fields count; a `static readonly` is skipped, as is any non-public or non-string field. Fields whose name ends in `Desc` are excluded from the results, because they are descriptions. For each remaining field the role is the constant's value, falling back to the field name, and the description is the value of a field named `{FieldName}Desc` on the same type, or an empty string where none exists. Delegates to JC.Identity.Shared's `IdentityHelper.GetAllRoles`.

##### ToCatalogue(IEnumerable&lt;RoleRecord&gt; roles)

**Returns:** `IReadOnlyList<ApplicationRoleDto>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `roles` | `IEnumerable<RoleRecord>` | required | The declarations, typically from `GetAllRoles`. Throws `ArgumentNullException` when null. |

Projects each record onto CAP's catalogue shape: `Key` is the role, `DisplayName` is the role passed through JC.Core's `ToDisplayName`, so `PageEditor` becomes `Page Editor` and an acronym run is kept together, and `Description` is the record's description, or `null` where it is empty or whitespace.

### CapDefaults

**Namespace:** `JC.CAP.Authentication`

The names JC.CAP registers with ASP.NET Core authentication and OpenIddict.

| Field | Type | Value | Description |
|-------|------|-------|-------------|
| `AuthenticationScheme` | `const string` | `JC.CAP` | The cookie scheme a CAP user is signed in on, and the application's default scheme. |
| `CookieName` | `const string` | `.JC.CAP.Session` | The session cookie's name. |
| `RegistrationId` | `const string` | `cap` | The OpenIddict client registration for CAP. One authority, so one registration. |
| `ProviderName` | `const string` | `CAP` | The provider name OpenIddict reports for the registration. |
| `RuleSetName` | `const string` | `CAP` | The name the identity rules log the default rule set under. |

### CapEndpoints

**Namespace:** `JC.CAP.Authentication`

The default local paths `MapCap` serves. Each is the default of the matching `CapOptions` property; read the option rather than the constant when building a link, since the option is what was mapped.

| Field | Type | Value | Description |
|-------|------|-------|-------------|
| `SignInPath` | `const string` | `/cap/signin` | Starts a sign-in by challenging CAP. Not a login page: CAP serves the only one of those. |
| `SignOutPath` | `const string` | `/cap/signout` | Ends the session here and at CAP. POST only. |
| `RefreshPath` | `const string` | `/cap/refresh` | Re-reads the account from CAP now rather than at the next token expiry. |
| `DeniedPath` | `const string` | `/cap/denied` | Where a disabled account is sent: re-checks with CAP, then hands over to CAP's denied page. |
| `TwoFactorPath` | `const string` | `/cap/two-factor` | Where an account owing two-factor is sent: re-checks with CAP, then hands over to enrolment. |
| `CallbackPath` | `const string` | `/signin-oidc` | Where CAP returns the authorization code. Matches the placeholder on CAP's settings page. |
| `PostLogoutCallbackPath` | `const string` | `/signout-callback-oidc` | Where CAP returns after ending its session. |
| `ReturnUrlParameter` | `const string` | `returnUrl` | The query parameter naming a local URL to land on afterwards, on the cookie, the rules and every endpoint. |

## Next steps

- [Setup](Setup.md): registration, options and their defaults.
- [Guide](Guide.md): usage, scenarios and nuances.
- [JC.Identity.Shared — API reference](../JC.Identity.Shared/API.md): the shared runtime this package builds on.
