# JC.Identity / JC.Tenancy v6 Refactor — Implementation Audit

> **Scope:** correctness, completeness, package boundaries and usability of the v6 identity/tenancy
> refactor as implemented through Phase 5.
> **Deliberately excluded:** packaging, `.csproj` metadata, READMEs and user-facing documentation —
> those are tracked separately as Phase 7 work.
> **Method:** every source file in `JC.Core`, `JC.Identity`, `JC.Identity.Shared` and `JC.Tenancy`
> read in full, plus the tenancy-facing surface of `JC.FileStorage`. Findings are ordered by
> severity, and each states the concrete failure rather than the principle.

---

## Summary

The boundary the refactor set out to establish holds. `JC.Tenancy` references no identity package,
neither identity package references `JC.Tenancy`, and the only cross-package mentions are prose in
doc comments. The build is clean. `ITenantScopedContext` genuinely fixed the string-keyed filter
lookup, and `ITenantInfo` merging the old `ITenantContext` was the right call.

## Status

Section 1 has been worked through. Sections 2–4 are reviewed but not yet actioned beyond §2.1.

| # | Finding | Status |
|---|---|---|
| 1.1 | FileStorage tenant source | **Fixed** |
| 1.2 | Filtering lost silently on v5 upgrade | **Deferred** — covered by the v6 change documentation |
| 1.3 | Detached cross-scope user update | **Fixed** |
| 1.4 | Malformed settings JSON throws | **Fixed** |
| 1.5 | Tenant uniqueness race | **Fixed** |
| 1.6 | `IsSetup` always true | **Fixed** — and uncovered §1.6a |
| 1.6a | Tenant snapshotted at construction | **Fixed** — the most serious defect found |
| 1.7 | Soft-deleted tenant metadata | **Withdrawn** — behaviour is correct |
| 2.1 | No scope-level bypass | **Deferred** — cost outweighs the need |
| 2.7 | `AllTenantsUnsafe` remark is wrong | **Open** — found while exploring §2.1 |
| 2.2–2.6, 3.x, 4.x | — | Not yet actioned |

The two headline risks remaining are **§2.5 (no tests)** and **§1.2**, the latter now resting on the
change documentation being read.

---

# 1. Correctness

## 1.1 — Critical — FileStorage writes tenants from the user, reads them from the tenant scope

> **Fixed.** `ITenantContext` added to `JC.Core.Models.MultiTenancy` carrying everything except the
> two members needing concrete types; `ITenantInfo` now extends it and `AddTenancy` forwards
> `ITenantContext` to the same scoped instance. `StorageService` and
> `UploadConstraintsTagHelper` — which had the same defect — now read it instead of `IUserInfo`.
> `Tenant` and `TenantSettings` stayed in JC.Tenancy, so decision 12 is intact.
>
> The `IgnoreQueryFilters` in `GetSavedFile` is **kept by design**: the `*ForTenant` methods are
> FileStorage's explicit unsafe bypass. The safe route is `CreateAsyncScopeForTenant` plus the
> ordinary methods, which keeps reads and writes consistent.

`JC.FileStorage/Services/StorageService.cs` resolves `IUserInfo` and uses `_userInfo?.TenantId` as
the tenant for every default-path save, read and delete (lines 73, 76, 134, 138, 241). Meanwhile
`SavedFile` implements `IMultiTenancy`, so once the consuming application calls `ApplyTenantFilters`
its **reads are filtered by `ITenantInfo.TenantId`** through `ITenantScopedContext`.

Two sources of truth for the same question. They agree in an ordinary authenticated request and
diverge everywhere else:

| Situation | Written under | Filtered as | Result |
|---|---|---|---|
| Background job scoped via `CreateScopeForTenant("acme")`, no user | `null` | `acme` | File saved, then invisible to the job that saved it |
| SystemAdmin re-scoped via `SetTenantInfoForTenant(other)` | their own tenant | `other` | Writes leak into the admin's tenant |
| Request where `IUserInfo` is unresolvable | `null` | current scope | Silently lands in the null partition |

This is decision 24 and §27 — *"tenant-aware EF filtering follows `ITenantInfo`, never `IUserInfo`
directly"* — violated on the write side by a package that predates the rule.

There is a second defect in the same file. `GetSavedFile` escapes the tenant filter like this:

```csharp
//Unable to do role check (JC.Identity) for cross-tenant query
if(!string.Equals(_userInfo?.TenantId, tenantId))
    query = query.IgnoreQueryFilters().Where(f => f.TenantId == tenantId);
```

Any caller naming a tenant other than the user's own gets an unauthorised cross-tenant read. The
comment describes exactly the problem `ITenantBypassAuthoriser` was built to solve in §42 — and
FileStorage does not use it, because it cannot see it.

**Root cause is the boundary, not the file.** `JC.FileStorage` references only `JC.Core`, and
decision 55 put every tenancy contract in `JC.Tenancy`, leaving Core with `IMultiTenancy` — a
*marker*. So a package can declare its entities tenant-scoped but has no supported way to ask which
tenant is current. See §3.1.

**Options, in preference order:**

- Add a minimal read-only contract to Core — `ICurrentTenant { string? TenantId { get; } }` — which
  `ITenantInfo` implements. Packages resolve it optionally (`GetService`), falling back to the null
  partition when tenancy is not registered. Keeps the sibling rule, costs Core one tiny interface,
  and closes the gap for every future tenant-aware package, not just this one.
- Give `JC.FileStorage` a `JC.Tenancy` reference. Simplest, but makes tenancy non-optional for a
  package that currently works without it.
- Leave it, and document that FileStorage ignores operational tenant scope. Not recommended — the
  read/write split means the failure is silent data loss, not a documented limitation.

Whichever is chosen, `AllTenantsUnsafe()` should replace the hand-rolled `IgnoreQueryFilters()`, so
the bypass is at least named.

## 1.2 — High — Upgrading from v5 silently disables tenant filtering

> **Deferred to the v6 change documentation**, which will cover the migration in full. Worth also
> stating in the JC.Tenancy setup guide, since the same silent failure recurs whenever a tenant-scoped
> context is added later and the call is forgotten — not only on upgrade.

In v5, `IdentityDataDbContext.OnModelCreating` called `ApplyTenantQueryFilters` itself. Every
application inheriting from it was filtered whether or not it knew. In v6 that call is gone, and
filtering is opt-in per context.

An application that upgrades, does nothing, and rebuilds will **compile, start, and return every
tenant's rows from every tenant-scoped query.** No exception, no log line, no failed migration.

`ApplyTenantFilters` does throw when a model holds `IMultiTenancy` entities but the context is not
an `ITenantScopedContext` (decision 58) — but only if you call it. *Not* calling it is the silent
path, and it is also the default after upgrade.

**Suggested mitigation:** a startup check. `AddTenancy` can register a hosted service or a
`IStartupFilter` that, on first resolve, walks the registered `DbContext` types, and logs an error
(or throws, behind an option) where a context's model contains `IMultiTenancy` entities but carries
no query filter on them. That converts the worst failure mode in the refactor from silent to loud.

At minimum this needs to be the first item in the migration guide, stated as a data-exposure risk
rather than a configuration change.

## 1.3 — High — `TenantSeeder` updates a detached user across scope boundaries

> **Fixed.** `SeedDefaultTenantAsync<TUser>` now takes a `userId` and loads the user through the
> context that saves it, so EF writes only the tenant column. Added a "no user with that id" branch
> that logs and returns `null`. Taking an id rather than an instance also removes the stale
> caller-reference problem.

`SeedDefaultAdminAsync` creates its own scope, resolves `UserManager<TUser>`, creates the user, and
returns it. That scope is then disposed. `SeedingExtensions.SeedDefaultTenantAsync` opens a *new*
scope, and `TenantSeeder` calls:

```csharp
await manager.GetRepository<T>().UpdateAsync(user, ...);
```

`RepositoryContext.UpdateRangeAsync` calls `_context.UpdateRange(list)`. On an entity this context
has never tracked, EF attaches it in the `Modified` state with **every property marked modified** —
so the UPDATE writes `PasswordHash`, `SecurityStamp` and `ConcurrencyStamp` back alongside the
tenant. It also bypasses `UserManager` entirely, so Identity's concurrency stamp is not regenerated
and none of its validation runs.

It works in the happy path. It is fragile in exactly the ways that are hard to debug later.

**Suggested fix:** have `TenantSeeder` re-load the user inside its own scope and mutate the tracked
instance:

```csharp
var repo = manager.GetRepository<T>();
var tracked = await repo.GetByIdAsync(user.Id) ?? user;
tracked.IdentityTenantId = tenant.Id;
await repo.UpdateAsync(tracked, cancellationToken: cancellationToken);
```

That also keeps the caller's in-memory instance and the database in step, which the current code
does only by coincidence.

Related, lower severity: the seeder's scope has an unpopulated `IUserInfo`, so any audit attribution
falls back to the system/unknown constants. Harmless for `BaseUser` (not an `AuditModel`) but it
will surprise whoever first seeds an audited entity this way.

## 1.4 — Medium — A malformed `Settings` column throws out of every tenant settings read

> **Fixed.** `Tenant.GetSettings()` catches `JsonException` and returns `[]`, matching what it
> already did for a null deserialise.

`Tenant.GetSettings()` calls `JsonSerializer.Deserialize` with no guard, so invalid JSON throws
`JsonException`. `TenantInfo.GetSetting<T>` looks like it handles this:

```csharp
var raw = GetSetting(key);              // <-- outside the try
if (string.IsNullOrEmpty(raw)) return defaultValue;
try { /* conversion only */ }
catch (Exception) { return defaultValue; }
```

The `try` covers type conversion only. The deserialise happens in `GetSetting(key)`, one line above
it. So a single malformed row takes down every settings read for that tenant, including the typed
overload that appears to be defensive.

The comment in the catch — *"a malformed setting value is consuming-application data, not a
framework fault"* — is the right instinct applied one layer too shallow. Guard the deserialise in
`Tenant.GetSettings()` and return `[]`, or move the call inside the `try`.

## 1.5 — Medium — Tenant uniqueness is check-then-act with no database constraint

> **Fixed.** Unique index on `Tenant.Name`. Not on `Domain` — it is nullable, and SQL Server would
> then permit only one tenant without a domain, while MySQL would permit many; a provider-agnostic
> mapping cannot express "unique except nulls". `ValidateAsync` now checks `DeletedQueryType.All`,
> so the code check matches what the index enforces, and clash messages distinguish a live tenant
> from a soft-deleted one.
>
> Two migration notes for the change docs: the index **fails to apply where duplicate names already
> exist**, and soft-deleted tenants now reserve their name permanently. This also dissolves the §73
> question about restore-time name clashes — a deleted name is never freed, so a restore cannot clash.

`TenantStore.ValidateAsync` queries for a clashing name or domain and then inserts. `TenantMap`
declares `HasIndex(t => t.Domain)` — **not unique** — and no index on `Name` at all. Two concurrent
`TryAddAsync` calls both pass validation and both commit.

`TenantSeeder.SeedDefaultTenantAsync` has the same shape (find by name, then add) and runs at
startup, which is precisely when several instances boot at once. A rolling deploy can produce two
"Default Tenant" rows.

The `ValidateAsync` doc comment says case sensitivity is left to the database *"so that the check
agrees with any unique index an application adds over the same columns"* — which reads as a
deliberate decision not to add one. That is defensible for `Domain` (nullable, and applications may
legitimately want duplicates) but leaving `Name` unconstrained while enforcing it in code gives the
worst of both: the check is not authoritative and the database will not save you.

**Suggested:** unique index on `Name`, and catch `DbUpdateException` in `TryAddAsync` to return a
`TenantValidationResponse` rather than surfacing a raw EF exception.

## 1.6 — Low — `ITenantInfo.IsSetup` is always true

> **Fixed.** Renamed `IsOverridden` and made read-only, now meaning "set explicitly rather than
> derived from the user". It became load-bearing rather than cosmetic once §1.6a was found.

`AddTenancy`'s factory constructs `TenantInfo` with `IsSetup = true`, and `SetTenant` sets it true.
Nothing anywhere can observe `false`.

It exists because `IUserInfo.IsSetup` exists — but there it does real work, gating
`UserInfoMiddleware`'s populate-once behaviour. `ITenantInfo` has no middleware (decision 56), so
the flag has no job.

Either remove it, or give it a meaning worth having: `false` until something explicitly establishes
scope, so a caller can distinguish *"null partition because that is where we are"* from *"null
partition because nobody said otherwise"*. The second reading would be genuinely useful to the
startup check proposed in §1.2.

Related: `TenantInfo.TenantId`'s setter returns early when the value is unchanged, so assigning the
same tenant does not set `IsSetup`. Moot while the flag is always true; worth fixing alongside it.

## 1.6a — Critical — The tenant was snapshotted at construction, not derived live

> **Found while resolving §1.6. Fixed.** Not in the original audit — it surfaced from the question
> "why would the tenant ever come from `AddTenancy`?"

`AddTenancy`'s factory read `IUserInfo.TenantId` **eagerly, when `ITenantInfo` was first resolved**.
But `IUserInfo` is scoped and populated *in place* by `UserInfoMiddleware`, so the value was only
correct if nothing touched `ITenantInfo` before that middleware ran.

Something does. In a tenant-scoped application the DbContext takes `ITenantInfo` to implement
`ITenantScopedContext`, and `UseIdentity()` runs `UseAuthentication()` first — cookie auth reaches
`SecurityStampValidator` → `SignInManager` → `UserManager` → `IUserStore` → the DbContext. That
constructs `ITenantInfo` before any claims are projected, so `TenantId` snapshotted as `null` and
the entire request ran in the null partition.

`SecurityStampValidator` revalidates only on an interval (30 minutes by default), so this would have
fired **intermittently** — correct in development, wrong for occasional production requests.

**Fix.** `TenantInfo` takes `IUserInfo?` and reads it on every access, so the tenant is evaluated
when a query filter asks. Assigning `TenantId` or calling `SetTenant` sets an override that wins
from then on, including an explicit `null` to pin the null partition deliberately. Metadata
resolution is keyed on `_resolvedFor`, so a cached `Tenant` is discarded whenever the underlying
identifier changes — by override, or by the middleware populating the user mid-scope.

**Design doc consequence:** §26 and decision 56 describe the snapshot behaviour and are now
inaccurate.

---

## 1.7 — Low — Deleting a tenant leaves scoped rows in a half-resolved state

> **Withdrawn — not a defect.** Query filters compare strings and never touch the tenant table, so
> data access is unaffected; only metadata reads go null. That is the coherent outcome: the tenant
> *record* is gone, the partition key is just a string, and a restore brings everything back intact.
> Every alternative is worse. `TryRestoreAsync` already invalidates the cache, so there is no stale
> window either. Deleting a tenant that still has active users is an application workflow failure,
> not a framework state.

`ITenantStore.TryDeleteAsync` soft-deletes and explicitly does not cascade — reasonable, since
tenant-scoped data can live in contexts this store has never heard of. But `TenantCache.Load`
filters to `DeletedQueryType.OnlyActive`, so after a delete:

- `ITenantInfo.TenantId` still holds the identifier;
- `HasTenant` is still `true`;
- `Name`, `Domain`, `MaxUsers`, `ExpiryDateUtc` and every setting silently become `null`/empty;
- query filters keep scoping rows to a tenant that no longer resolves.

Nothing is wrong, exactly, but nothing says anything either. An application in this state looks
configured and behaves like an unnamed tenant. Worth an explicit decision: either resolve
soft-deleted tenants for metadata purposes, or surface the state (`IsDeleted` on `ITenantInfo`).

This is the concrete form of §58.1's unanswered question, and it now applies to every tenant-scoped
package, not just `JC.FileStorage`.

---

# 2. Gaps against the design's own intent

## 2.1 — No scope-level cross-tenant bypass

> **Deferred.** Re-scoping (`CreateScopeForTenant`) already exists; only *suppression* — a scope
> where every query ignores the filter — is missing. Building it means putting the flag inside the
> filter expression, so every tenant-filtered query in every application pays an extra `OR`
> parameter to enable a rarely-used feature, plus another mandatory member on `ITenantScopedContext`.
> Looping tenants is safer for the main use case, and per-query `AllTenants` covers the rest. If the
> real friction is calling `AllTenants(authoriser)`, §4.2 is the cheaper fix.

§40 asks for scope-level control (`UseTenant` / `UseTenantUnsafe`, or explicit suppression) as a
distinct concept from query-level. Only the query-level pair (`AllTenants` / `AllTenantsUnsafe`)
shipped. Decision 26 lists both as required.

In practice a caller wanting to operate across tenants for a whole unit of work must remember
`AllTenants(...)` on every individual query, which is exactly the error-prone pattern scope-level
control exists to avoid.

## 2.2 — No tenant resolution by domain

`Tenant.Domain` is a first-class field, `TenantMap` indexes it, and the mapping carries the comment
*"tenants are commonly resolved by domain on the way in"*. `ITenantStore.GetByDomainAsync` exists.

Nothing uses any of it. There is no host-header resolver, and the scoped factory in `AddTenancy`
reads only `IUserInfo.TenantId`. Any application doing domain-based multi-tenancy — the case the
index was added for — writes that itself.

Given `JC.Tenancy` deliberately has no ASP.NET Core dependency, this cannot be middleware. It could
reasonably be an opt-in delegate on `TenantOptions`:

```csharp
options.ResolveTenantId = sp => /* host header, header, claim, whatever */;
```

with the factory preferring it over `IUserInfo.TenantId` when set. That keeps the package
framework-free while making the documented use case reachable.

## 2.3 — No optional foreign-key configuration

§58 asks whether `JC.Tenancy` should offer opt-in FK configuration for entities sharing a model with
`Tenant`. `ApplyTenancyMappings` maps `Tenant` and nothing else, so the answer shipped as "no" by
omission rather than by decision. §58.1 records that `JC.FileStorage` lost a real FK and its
`OnDelete(SetNull)` behaviour to this, and nothing replaced it.

## 2.4 — `ITenantStore.GetAllAsync` is unpaged

Returns `List<Tenant>` with no paging, in a suite that ships `IPagination` and `PagedList` in Core.
The tenant list is the one collection an administration screen is guaranteed to want paged. Also
means `TenantSeeder` loads every tenant to find one by name — it should use a targeted query.

## 2.5 — No tests

§63 specifies a substantial matrix across identity, tenancy and compatibility. The solution contains
no test project of any kind.

This matters more than the usual "we should add tests", because the riskiest behaviour in this
refactor happens at **EF model-build time** and is invisible to the compiler:

- `ApplyTenantFilters` throwing when a context has tenant entities but no `ITenantScopedContext`;
- null-to-null partition matching in the generated filter expression;
- the captured-`DbContext` trick in `BuildTenantFilter` surviving EF's model cache across scopes —
  which, if it ever regresses, leaks one tenant's data into another's request and is close to
  undetectable by inspection;
- multiple contexts sharing one operational scope;
- `AddTenancy` throwing on a second registration;
- login working end to end with `BaseUser` not implementing `IMultiTenancy` (§14.1).

The third item is the one to write first. It is the single assumption the entire filtering design
rests on, it is asserted in three separate doc comments, and nothing verifies it.

## 2.7 — `AllTenantsUnsafe`'s remark is factually wrong

> **Open.** Found while exploring §2.1.

Its `<remarks>` states that it *"also drops soft-delete and any other global filters on the entity"*.
The tenant filter is the **only** global query filter in the suite — one `HasQueryFilter` call, in
`DataExtensions`. Soft-delete is `FilterDeleted(DeletedQueryType)`, an explicit `IQueryable`
operator, and is unaffected by `IgnoreQueryFilters`.

The general caution is fair, since a consuming application may add its own global filters, but the
concrete example is incorrect and will mislead.

---

## 2.6 — Vestigial `IUserInfo.MultiTenancyEnabled`

Set by `UserInfoMiddleware` and `SetUserInfoForUser` from whether the user has a tenant. Nothing in
the suite reads it. It also reads like an application-level switch when it is a per-user fact.
Candidate for removal while v6 is still breaking.

---

# 3. Package boundaries

## 3.1 — Core can mark tenancy but cannot answer it

> **Resolved by §1.1** — `ITenantContext` in Core closes the gap for every tenant-aware package, not
> only FileStorage. Retained below because the reasoning explains why the interface exists.

The one place the boundary genuinely bites, and the cause of §1.1.

`IMultiTenancy` lives in Core so any package can declare an entity tenant-scoped for free
(decision 55). But the *operational* tenant lives only in `JC.Tenancy`. The asymmetry is that the
package **owning the entity** and the package **installing the filters** are different — the entity
owner is `JC.FileStorage`, the filter installer is the consuming application. So a package can ship
tenant-scoped entities and have no supported way to behave correctly about them.

Decision 55's reasoning — *"marking an entity tenant-scoped stays free, while filtering costs a
reference to the package that does the filtering"* — is sound for applications and wrong for
packages. A package that marks entities is not the one choosing to filter.

The minimal `ICurrentTenant` in Core proposed in §1.1 resolves this without weakening the sibling
rule: it is a read-only accessor, not the tenancy engine, and `JC.Tenancy` remains the only thing
that can *establish* scope.

## 3.2 — `JC.Identity.Shared` pulls ASP.NET Core into non-web consumers

`JC.Tenancy` deliberately avoids a `FrameworkReference` on `Microsoft.AspNetCore.App`, and the
csproj says so. `JC.Identity.Shared` takes one, because it houses the middleware.

The consequence is that `UserInfoExtensions` — explicitly built for background jobs and non-HTTP
work (§50, decision 53) — lives in a package that cannot be consumed without ASP.NET Core. A worker
service wanting only `SetUserInfoForUser` takes the whole framework reference.

Not urgent, and arguably right for now, but it is an asymmetry with the sibling package that will
look odd to a future `JC.CAP` author. If it is ever worth splitting, the seam is
middleware-and-builder-extensions versus contracts-and-projection.

## 3.3 — Forgetting `BypassRoles` fails closed and silently

`RoleTenantBypassAuthoriser` denies when no roles are configured, which is the right default. But an
application that simply forgets `options.AllowBypassForRole(SystemRoles.SystemAdmin)` gets a
`SystemAdmin` who silently cannot see other tenants, with nothing indicating why.

Failing closed is correct; failing quietly is not. One informational log line at registration when
`BypassRoles` is empty would cost nothing.

## 3.4 — `AddTenancy`'s duplicate-registration guard can misfire

The guard detects an existing `ITenantDbContext` service registration. An application that registers
`ITenantDbContext` itself for any reason — a test double, a decorator — trips it and gets a message
saying tenancy is already registered against another context, which is not what happened. Minor, but
the message asserts more than the check establishes.

---

# 4. Usability

The refactor moved real complexity out of `JC.Identity`, and some of it landed on the consuming
application rather than disappearing. These are the places where the cost is highest.

## 4.1 — Every tenant-scoped application hand-writes the same context wiring

The new minimum for a tenant-scoped identity application:

```csharp
public class AppDbContext(DbContextOptions o, IUserInfo u, ITenantInfo t)
    : IdentityDataDbContext<AppUser, AppRole>(o, u), ITenantScopedContext, ITenantDbContext
{
    public string? CurrentTenantId => t.TenantId;
    public DbSet<Tenant> Tenants { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
        mb.ApplyTenancyMappings();
        mb.ApplyTenantFilters(this);
    }
}
```

Every one of those lines is mandatory, identical across applications, and silently wrong if omitted
(§1.2). This is the single biggest ergonomic regression from v5, where it was one base class.

**Suggestions, cheapest first:**

- Document this block verbatim as the canonical starting point. It is short; the problem is that
  nothing tells you it is required.
- Ship the startup diagnostic from §1.2, so omitting it is loud.
- Consider a small optional glue package (`JC.Identity.Tenancy`) providing
  `TenantScopedIdentityDataDbContext<TUser, TRole>` with all of it pre-wired. It would depend on
  both siblings — which is fine, because *it* is the composition point, and the sibling rule exists
  to stop `JC.Identity` and `JC.Tenancy` depending on each other, not to stop a third package
  depending on both.

## 4.2 — `AllTenants` requires threading an authoriser through every call site

```csharp
var all = query.AllTenants(_bypassAuthoriser);
```

Every class building a cross-tenant query must inject `ITenantBypassAuthoriser` and pass it
manually. The doc comment explains why (an `IQueryable<T>` extension has no service provider), and
the explanation is correct — but it is still friction on the common path, and it inherits the shape
of the v5 `AllTenants(IUserInfo)` it replaced.

Alternatives worth considering: expose `CanAccessAllTenants` on `ITenantInfo` (already scoped and
already injected almost everywhere tenancy matters), or add a scoped `ITenantQueries` helper that
closes over the authoriser once.

## 4.3 — The seeding handoff is discoverable only if you already know about it

```csharp
var admin = await app.ConfigureAdminAndRolesAsync<AppUser, AppRole, AppRoles>();
if (admin is not null)
    await app.Services.SeedDefaultTenantAsync(admin);
```

Correct and well-factored, but nothing in `JC.Identity` can reference the second line, and nothing in
`JC.Tenancy` knows the first exists. A developer who used `setupTenancy: true` in v5 gets a compile
error naming a parameter that no longer exists, with no pointer to the replacement.

Cheap fix: name the replacement in the obsolete-parameter migration notes, and add the two-line
snippet to `ConfigureAdminAndRolesAsync`'s `<remarks>` — which is done — and to `AddTenancy`'s.

## 4.4 — `userContextType` is a `Type`, not a type argument

`TenantSeeder.SeedDefaultTenantAsync(..., Type? userContextType = null, ...)` takes an untyped
`Type`, so a wrong value fails at runtime inside `RepositoryManager.For(Type)` rather than at
compile time. It is placed after two optional string parameters, so most call sites will pass it by
name or not at all.

Given the default (the `AddCore` context) is correct for most applications, this is a reasonable
trade — but a generic overload `SeedDefaultTenantAsync<T, TContext>(...)` alongside it would cost
nothing and give the compiler something to check.

---

# 5. Confirmed correct

Recorded so a later reader does not re-audit them.

- **No circular package references.** Verified by grep across `.cs` and `.csproj`: the only
  cross-package mentions are prose in doc comments.
- **No dependency cycle in DI.** `ITenantInfo`'s factory resolves `TenantCache` and `IUserInfo`
  without touching a `DbContext`; `TenantCache.Load` resolves `ITenantDbContext` lazily, by which
  time `ITenantInfo` is fully constructed. The scoped chain terminates.
- **`BaseUser` does not implement `IMultiTenancy`** (§14.1), so `ApplyTenantFilters` cannot install
  a filter on the user entity even in a fully tenant-scoped application. The hard rule holds.
- **`IdentityTenantId` remains `[NotMapped]`** and projects the existing `TenantId` column in both
  directions. No migration.
- **`Tenant` does not implement `IMultiTenancy`**, so the tenant table is never filtered by itself.
- **`AddCore<TContext>` registers `DbContext` → `TContext`**, so `TenantSeeder`'s default-context
  assumption is correct for any application following the documented setup.
- **The claim-type indirection works.** `IdentityProjectionOptions` is populated by an
  `IConfigureOptions<>`, so a consumer reconfiguring `IdentityOptions.ClaimsIdentity` after
  `AddIdentity` is still honoured.
- **`Authority` now defaults to `None`** and is stated as `Local` only by JC.Identity, so an
  anonymous request and an undeclared authority both report honestly.

---

# 6. Suggested order of work

**Done** — §1.1, §1.3, §1.4, §1.5, §1.6, §1.6a. §1.7 withdrawn; §1.2 and §2.1 deferred.

**Remaining, highest value first**

1. **§2.5** — write the model-cache/tenant-isolation test. Still the single highest-value item in the
   suite, and §1.6a is a reminder of how invisible this class of bug is by inspection.
2. **§2.7** — correct the `AllTenantsUnsafe` remark. One line.
3. **§2.6** — decide `MultiTenancyEnabled`; a cheap removal while v6 is still breaking.
4. **§2.3** — optional FK configuration, and with it the `SavedFile` delete behaviour lost in §58.1.
5. **§4.1** — canonical context snippet, then consider the glue package.
6. **§2.2** — domain resolution hook.
7. **§2.4** — paging on `GetAllAsync`; also stop `TenantSeeder` loading every tenant to find one.
8. **§3.2, §3.3, §3.4, §4.2, §4.3, §4.4** — smaller boundary and ergonomics items.

**For the change documentation**

- §1.2 — filtering is now opt-in per context; an upgraded application loses it silently.
- §1.5 — the unique index fails where duplicate tenant names exist; deleted tenants reserve names.
- §1.6a — §26 and decision 56 of the design document describe the removed snapshot behaviour.
- §1.6 — `IsSetup` is now `IsOverridden` and read-only.
