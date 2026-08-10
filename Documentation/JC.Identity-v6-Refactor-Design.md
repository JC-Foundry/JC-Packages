# JC.Identity v6 Refactor — Identity & Tenancy Boundary Design

> **Status:** Initial design / working document  
> **Target:** JC Packages v6  
> **Primary scope:** `JC.Identity` architectural refactor and boundary hardening  
> **Secondary scope:** Extraction of reusable identity and tenancy concerns required so a future `JC.CAP` package can integrate cleanly without forcing another foundational major-version migration.  
> **Not a goal:** Designing or implementing the complete `JC.CAP` package, CAP SSO protocol, CAP session model, or the full Central Admin Portal identity architecture.

---

# 1. Purpose

`JC.Identity` currently provides an opinionated integration over ASP.NET Core Identity for applications that **own their identity system locally**.

That responsibility remains valid.

The purpose of the v6 refactor is **not** to turn `JC.Identity` into a generic authentication package used by both local Identity applications and future CAP SSO applications.

Instead, the refactor should:

1. preserve `JC.Identity` as the local ASP.NET Identity implementation;
2. identify concepts currently inside `JC.Identity` that are actually broader than local ASP.NET Identity;
3. move those concepts to more appropriate package boundaries;
4. introduce a dedicated tenancy package where tenancy behaviour currently leaks through `JC.Identity`;
5. harden the distinction between:
   - an authoritative user record;
   - the currently authenticated user context;
   - identity ownership;
   - identity tenancy;
   - application tenancy;
   - current operational tenant scope;
6. create the foundational contracts a future `JC.CAP` package will need without making this document a CAP construction plan;
7. minimise the chance that introducing `JC.CAP` later requires another major-version migration solely because the underlying identity/tenancy boundaries were not corrected during v6.

This refactor may be aggressively breaking where required.

The goal is not to preserve v5/v6 API compatibility at the cost of maintaining incorrect boundaries.

---

# 2. Existing Architectural Intent

## 2.1 JC.Identity remains local ASP.NET Identity

`JC.Identity` should continue to mean:

> **A reusable, opinionated JC Packages implementation around ASP.NET Core Identity for applications that own and configure their identity locally.**

A consuming application using `JC.Identity` owns:

- users;
- roles;
- local authentication;
- ASP.NET Identity persistence;
- password policy;
- lockout policy;
- two-factor authentication;
- account confirmation;
- local identity cookies;
- security rules;
- application-level `SystemAdmin` / `Admin` roles;
- application-specific user extensions;
- local user management.

This is not being replaced by CAP.

A future application using CAP SSO should generally use `JC.CAP`, **not `JC.Identity`**, unless that application itself also has a separate legitimate reason to own a local ASP.NET Identity system.

---

# 3. Why Refactor Before JC.CAP

A future `JC.CAP` package introduces a second way for a consuming application to obtain an authenticated application identity.

Current model:

```text
Application
    ↓
JC.Identity
    ↓
ASP.NET Identity
    ↓
IUserInfo
```

Future CAP model:

```text
Application
    ↓
JC.CAP
    ↓
Central Admin Portal identity authority
    ↓
IUserInfo
```

The important design goal is that both models can populate the same **Core-level runtime identity contracts** without `JC.CAP` depending on:

- ASP.NET Identity;
- `BaseUser`;
- `UserManager`;
- `SignInManager`;
- Identity-specific persistence;
- JC.Identity-specific role mechanics.

The v6 work should therefore extract only the concepts that are genuinely common.

---

# 4. Guiding Boundary

The key architectural rule is:

> **JC.Identity owns local ASP.NET Identity behaviour. JC.Core owns foundational contracts. JC.Tenancy owns tenancy behaviour. Future JC.CAP owns CAP integration.**

Conceptually:

```text
JC.Core
├── IUserInfo
├── IApplicationUser
├── IdentityAuthority
└── IMultiTenancy
      │
      ├───────────────┐
      ▼               ▼
JC.Tenancy       JC.Identity
      │               │
      └────── future JC.CAP / JC.CAP.Base
```

The exact dependency graph must be validated during implementation. Circular dependencies must be avoided.

---

# 5. JC.Core Responsibilities

`JC.Core` should contain **small, foundational contracts** that can be consumed without installing a complete identity or tenancy engine.

Responsibilities:

- `IUserInfo` — **pinned to Core**, see below;
- `IdentityAuthority` — **pinned to Core**, see below;
- `IMultiTenancy`;
- possibly `ITenantInfo`;
- possibly `ITenantContext`.

`IApplicationUser` was originally placed here. It moves to `JC.Identity.Shared` (§8): nothing in Core consumes it, and it is only meaningful to the two identity authorities.

**Two types cannot move out of Core, whatever the boundary argument.**

`IUserInfo` is consumed *inside* Core — `RepositoryContext` resolves it to stamp audit fields, and `AuditService` reads `UserId` and `Username` from it with `IUserInfo.MissingUserInfoId` as the fallback. Moving it would make Core depend on the identity package and invert the dependency graph this whole design rests on.

`IdentityAuthority` follows it. §11 places the enum **on** `IUserInfo`, and a Core type cannot expose a property typed from a package Core does not reference. Either the enum stays in Core, or §11 is wrong about where the property lives — and §11 is right, so the enum stays.

Core should **not** implement:

- ASP.NET Identity;
- tenant storage;
- tenant caching;
- automatic EF tenancy filters;
- CAP authentication;
- CAP API calls.

The placement of `ITenantInfo` and `ITenantContext` remains open: if nothing outside `JC.Tenancy` needs those interfaces without referencing `JC.Tenancy`, they may belong there instead.

---

# 6. JC.Identity Responsibilities

`JC.Identity` remains responsible for:

- ASP.NET Identity integration;
- `BaseUser`;
- `BaseRole`;
- local Identity DbContext integration;
- local role conventions;
- local `SystemAdmin` / `Admin`;
- user/role seeding;
- password policy;
- lockout;
- local 2FA;
- account confirmation;
- account enable/disable state;
- password-change requirements;
- local Identity claims generation;
- creation of `IUserInfo` from locally authenticated identity;
- Identity-specific middleware/services.

It may depend on `JC.Tenancy`, but tenancy mechanics should no longer fundamentally live inside `JC.Identity`.

---

# 7. JC.Tenancy Responsibilities

A new `JC.Tenancy` package should own the **tenancy engine and persisted tenancy domain**.

Likely responsibilities:

- `Tenant`;
- `TenantSettings`;
- `ITenantStore`;
- tenant information resolution;
- tenant caching;
- tenant cache invalidation;
- `ITenantContext` implementation;
- current operational tenant scope;
- automatic EF query filters for `IMultiTenancy`;
- attaching tenant filtering to one or many DbContexts;
- explicit tenant scope selection;
- safe cross-tenant access;
- unsafe cross-tenant access;
- tenant persistence in one configured DbContext;
- configuration and controlled overrides.

`JC.Tenancy` must be usable independently from `JC.Identity`.

A non-Identity application should be able to use:

```text
JC.Core
JC.Tenancy
```

and still have full application tenancy.

---

# 8. JC.Identity.Shared Responsibilities

`JC.Identity` and a future `JC.CAP` both answer the same question — *who is the current user?* — from different authorities. Today the machinery that answers it lives entirely inside `JC.Identity` and is welded to ASP.NET Identity, so `JC.CAP` would have to reimplement it or depend on a package it has no business depending on.

`JC.Identity.Shared` is that machinery, extracted.

> **The shared identity runtime: everything needed to turn an authenticated principal into an `IUserInfo`, independent of who authenticated them.**

It is deliberately **not** an abstractions-only package. It ships working code.

## 8.1 Responsibilities

- `IApplicationUser` — the authoritative user-record contract (§12, §13);
- the default `IUserInfo` implementation;
- the middleware that projects claims onto `IUserInfo`;
- the claim-type constants both authorities emit and read;
- account-state contracts shared by both authorities.

## 8.2 It must not contain

- ASP.NET Identity types — no `IdentityUser`, `UserManager`, `SignInManager`, `IdentityOptions`;
- `BaseUser` or `BaseRole`;
- CAP protocol, session or API concerns;
- tenancy mechanics — those are `JC.Tenancy`'s (§7);
- authentication itself. Establishing *that* a principal is authenticated belongs to `JC.Identity` or `JC.CAP`. This package only projects the result.

## 8.3 Dependency direction

```text
JC.Core
    ↓
JC.Identity.Shared
    ↓                ↓
JC.Identity     future JC.CAP
```

`JC.Identity.Shared` and `JC.Tenancy` are **siblings**. Neither depends on the other. An application can take tenancy without identity, or identity without tenancy.

## 8.4 Why IUserInfo does not move here

`IUserInfo` stays in `JC.Core`, because Core consumes it directly — `RepositoryContext` resolves it to stamp audit fields and `AuditService` reads `UserId` and `Username` from it, falling back to `IUserInfo.MissingUserInfoId`. Moving it would make `JC.Core` depend on `JC.Identity.Shared` and invert the whole graph.

The consequence is that **`IdentityAuthority` also stays in Core** (§11). A type in Core cannot expose a property typed from a package Core does not reference, and §11 places `IdentityAuthority` on `IUserInfo`.

So the split is:

```text
JC.Core                  contract + the enum on it
    IUserInfo
    IdentityAuthority

JC.Identity.Shared       the implementation and the pipeline that fills it
    UserInfo
    UserInfoMiddleware
    DefaultClaims
    IApplicationUser
```

## 8.5 Two extraction problems to solve

Both are recorded from the Phase 1 inventory (§65) and are prerequisites, not consequences.

**The `IUserInfo` implementation is welded to ASP.NET Identity.** Its useful constructors take `BaseUser` and `IEnumerable<BaseRole>`. The property surface is authority-agnostic; the constructors are not. The move is therefore a **split** — the implementation goes to Shared, and the `BaseUser`-shaped construction stays in `JC.Identity` as a mapper or extension.

**The claims middleware needs `IOptions<IdentityOptions>`.** It reads `ClaimsIdentity.EmailClaimType`, `UserIdClaimType` and `RoleClaimType` from ASP.NET Identity to know which claims to look for. Moving it unchanged would drag ASP.NET Identity into Shared and defeat the package. Those three claim-type names must be abstracted behind a small options contract that `JC.Identity` satisfies from `IdentityOptions` and `JC.CAP` satisfies from its own token shape.

## 8.6 Relationship to JC.CAP.Base

`JC.Identity.Shared` and the future `JC.CAP.Base` (§9) are **distinct packages on different axes**:

| | Shared between | Concerns |
|---|---|---|
| `JC.Identity.Shared` | `JC.Identity`, `JC.CAP` | Current-user runtime — contracts, projection, claims |
| `JC.CAP.Base` | CAP, the Portal, consuming `.Data` packages | CAP integration domain — user-to-tenant maps, app role assignment |

`JC.CAP.Base` may depend on `JC.Identity.Shared`. The reverse must never hold.

---

# 9. Future JC.CAP.Base — Boundary Only

This document does **not** design `JC.CAP.Base` completely.

However, the Identity refactor must allow for a future thin shared package containing contracts/domain types required by both:

- Central Admin Portal itself;
- `JC.CAP`;
- consuming application `.Data` packages.

Likely future shared concepts include:

- application user-to-tenant mapping;
- application role assignment;
- application assignment/integration contracts;
- shared identifiers/enums/results.

These types must not require ASP.NET Identity.

---

# 10. IUserInfo — Runtime Authenticated Identity

`IUserInfo` already belongs in `JC.Core`.

It should remain the authoritative **current server-side identity context**.

Meaning:

> **Who is the user executing this operation, and what does this consuming application currently know about them?**

`IUserInfo` is not a persisted user record.

It is a runtime projection.

Its existing broad set of properties can remain. Properties unavailable from a particular identity authority may remain null/default.

This avoids introducing multiple competing current-user abstractions.

---

# 11. IdentityAuthority

Add a simple identity ownership enum.

Conceptually:

```csharp
public enum IdentityAuthority
{
    Local,
    CAP,
    Custom
}
```

Naming may change.

Its meaning is:

> **Who authoritatively owns/supplies this identity to the consuming application?**

It does **not** mean:

- login protocol;
- OAuth/OIDC/SAML provider;
- Microsoft/Google/passkey/password;
- how CAP itself authenticated the person.

Example:

```text
Microsoft Entra
    ↓
authenticates into CAP
    ↓
CAP authenticates into consuming app
```

The consuming app sees:

```text
IdentityAuthority = CAP
```

If authentication-method metadata is ever required, that should be a separate concept.

`IdentityAuthority` should live on `IUserInfo`, not necessarily on the authoritative user-record contract.

---

# 12. IApplicationUser — Authoritative User Record Contract

Introduce a Core-level `IApplicationUser`.

Its responsibility is different from `IUserInfo`.

```text
IApplicationUser
= an authoritative user/account record

IUserInfo
= the current authenticated user's runtime projection
```

`IApplicationUser` may describe **any user**, including one who is not currently signed in.

Example:

```text
Administrator opens user management
    ↓
loads another user
    ↓
IApplicationUser
```

The two contracts can expose overlapping properties while still modelling different responsibilities.

---

# 13. IApplicationUser Shape

The intent is for `IApplicationUser` to represent the broad authoritative user shape currently exposed through `BaseUser` / ASP.NET Identity without forcing consumers to reference ASP.NET Identity.

Likely properties include equivalents of:

- user ID;
- username;
- email;
- email confirmation;
- phone number;
- phone confirmation;
- 2FA state;
- lockout state;
- lockout end;
- access failure count;
- display name;
- enabled state;
- password-change requirement;
- last login;
- identity tenant ID.

The exact property list must be verified against the current `BaseUser` and `IdentityUser` surface.

The interface should be **read-only**.

Concrete persistence models can still expose setters.

This makes it suitable for:

- `BaseUser`;
- future CAP-facing DTO/user models;
- custom identity authorities;
- read-only user-management projections.

---

# 14. BaseUser

`BaseUser` remains in `JC.Identity`.

It remains an ASP.NET Identity persistence model.

Conceptually:

```text
BaseUser
    : IdentityUser
    : IApplicationUser
```

`BaseUser` must not move to `JC.Core`.

A future CAP SSO consuming application should not need `BaseUser`.

## 14.1 BaseUser must NOT implement IMultiTenancy

> **Hard rule. This was tried and reverted, and the reason is not obvious from reading the code.**

An earlier draft of this document proposed `BaseUser : IMultiTenancy`, on the grounds that explicit participation is cleaner than property-name discovery. **That is wrong and must not be reinstated.**

`ApplyTenantQueryFilters` discovers entities by `typeof(IMultiTenancy).IsAssignableFrom(...)` and installs a **global query filter**. Applied to the user entity, that filter applies to every read ASP.NET Identity performs — which means `UserManager` and `SignInManager` cannot resolve a user during authentication, because authentication happens *before* any tenant scope exists. Login breaks, and with it everything that depends on a signed-in user.

Today `BaseUser` therefore carries a plain `TenantId` column and is **not** covered by automatic tenant filtering. This is deliberate, not an oversight.

The tenant reaches the runtime by a different route:

```text
BaseUser.TenantId          persisted column, no query filter
    ↓ claims factory
tenant claim
    ↓ claims middleware
IUserInfo.TenantId
    ↓
ITenantContext             scopes everything else
```

Anyone tidying `BaseUser` toward "correct" interface participation will break every consuming application's login. If a future change makes user-level tenant filtering genuinely necessary, it must come with an explicit exemption for the Identity resolution path, and it must be proven against `SignInManager` before merge.

The same caution applies to any entity ASP.NET Identity itself queries during authentication.

---

# 15. IdentityTenantId

`IApplicationUser` should expose:

```csharp
string? IdentityTenantId { get; }
```

This means:

> **The tenant/organisation that owns the authoritative identity record.**

Existing `BaseUser` persistence should not require a column rename.

Example:

```csharp
public string? TenantId { get; set; }

public string? IdentityTenantId => TenantId;
```

This preserves:

- existing database column;
- existing data;
- migration history;
- clearer public semantics.

Do not create a schema migration merely to rename the existing persisted `TenantId` column unless another technical reason requires it.

---

# 16. IUserInfo.TenantId

`IUserInfo.TenantId` should mean:

> **The tenant assigned to this user inside the current consuming application.**

For local `JC.Identity`, the values commonly align:

```text
BaseUser.TenantId
    ↓
IApplicationUser.IdentityTenantId
    ↓
IUserInfo.TenantId
```

For future CAP SSO they may differ.

Therefore `IApplicationUser.IdentityTenantId` and `IUserInfo.TenantId` must not be treated as interchangeable concepts.

---

# 17. CAP Tenant vs Application Tenant

This distinction must be respected even though full CAP construction is outside this document.

A CAP tenant means:

> **A business/organisation within the Central Admin Portal ecosystem.**

Example:

```text
CAP Tenant: Acme Ltd
├── App A
├── App B
└── App C
```

An application tenant means:

> **A tenant partition defined inside one specific consuming application using JC.Tenancy.**

The two are independent.

Using CAP SSO does **not** automatically mean the consuming application uses CAP's organisation tenant as its own data tenant.

---

# 18. Future CAP Application Tenant Mapping

Included only because v6 contracts must support it.

Future `JC.CAP` may offer two explicit application modes.

## No application tenancy

```text
IUserInfo.TenantId = null
```

## Application uses JC.Tenancy

Future `JC.CAP.Base` may expose:

```text
UserTenantMap
├── UserId
└── AppTenantId
```

Where:

- `UserId` maps to CAP user ID supplied through SSO;
- `AppTenantId` maps to a local tenant in the consuming application.

The relationship is strictly:

```text
one user → one application tenant
```

A missing map resolves to:

```text
IUserInfo.TenantId = null
```

This is valid.

The mapping implementation is future CAP work; v6 only needs to preserve a compatible identity/tenancy boundary.

---

# 19. Null Tenant Semantics

Null tenancy is intentional existing behaviour.

```text
TenantId = null
```

means:

> **The null/default tenant partition.**

It does not automatically mean:

- tenant lookup failed;
- tenant context is uninitialised;
- application misconfiguration.

Applications not actively using multi-tenancy may naturally operate entirely in the null tenant.

Applications that genuinely use tenancy should generally avoid assigning normal business users/data to null, but that is consuming-app policy.

There should be no synthetic persisted "Null Tenant" row.

---

# 20. IMultiTenancy

`IMultiTenancy` should remain in `JC.Core`.

Its responsibility becomes smaller:

> **This entity supports application-tenant ownership/partitioning.**

It should likely expose only the tenant key.

Conceptually:

```csharp
public interface IMultiTenancy
{
    string? TenantId { get; set; }
}
```

Use the current real key type during implementation.

---

# 21. Remove Concrete Tenant Navigation from IMultiTenancy

Moving `Tenant` to `JC.Tenancy` means Core should no longer require:

```text
Tenant Tenant
```

on `IMultiTenancy`.

This does remove the current automatic CLR navigation/foreign-key convenience, but improves the abstraction.

Reasons:

- Core should not depend on concrete Tenant;
- tenant-aware contexts may not own the Tenant table;
- physical FKs cannot cross separate DbContexts/databases;
- tenant awareness fundamentally only requires TenantId.

When Tenant exists in the same EF model, `JC.Tenancy` may optionally configure an FK without requiring a navigation on the Core contract.

---

# 22. Tenant and TenantSettings Move

The concrete persisted tenancy types should move from:

```text
JC.Core
```

to:

```text
JC.Tenancy
```

Likely:

- `Tenant`;
- `TenantSettings`.

This is a breaking package/namespace move and requires migration documentation.

The v5 architectural benefit remains:

> Other packages can still declare tenant-aware entities without depending on JC.Identity.

They do so through `JC.Core.IMultiTenancy`.

Only consumers that need the tenancy engine install `JC.Tenancy`.

---

# 23. Existing Tenant Model

The current Tenant model already includes concepts such as:

- name;
- description;
- domain;
- maximum users;
- expiry;
- generic consumer-defined settings;
- persistence/audit state.

The new design must distinguish:

- persisted Tenant entity;
- runtime Tenant information;
- operational tenant scope.

These do not need to be the same type.

---

# 24. TenantSettings

`TenantSettings` are generic and consumer-defined.

Examples include:

- enforce email domain;
- tenant colour;
- consuming-application feature/configuration flags.

The tenancy framework should not attempt to define all possible settings.

Runtime APIs should support:

- getting one setting;
- getting settings;
- likely typed reads where useful.

Exact API shape remains open.

The runtime model should avoid forcing consumers to manipulate raw persisted JSON/storage unnecessarily.

---

# 25. ITenantInfo

> **Earlier drafts of this document split tenant data (`ITenantInfo`) from operational tenant scope
> (`ITenantContext`). That split was wrong and has been removed. They are one concept, described
> twice.**

There is only ever one tenant a given operation is running against, and everything you would want
to know about it belongs to that same answer. A separate context type wrapping a nullable info type
adds a layer whose only content is a null check.

`ITenantInfo` is therefore both:

> **Which application tenant is this operation running against, and what do we know about it?**

It is registered **scoped**, and is the tenancy counterpart of `IUserInfo`: same lifetime, same
"resolved once per scope" behaviour, same ability to be set explicitly for work that has no user.

Shape:

```text
ITenantInfo
├── TenantId          the operational scope; settable
├── HasTenant         false in the null partition
├── IsSetup
├── Name
├── Description
├── Domain
├── MaxUsers
├── ExpiryDateUtc
├── IsExpired         reported, never enforced
├── SetTenant(Tenant?)
├── GetSetting(key)
├── GetSetting<T>(key, default)
└── GetSettings()
```

Audit fields are deliberately absent — no runtime requirement has appeared for them.

**Resolution is two-tier, and the tiers matter.** `TenantId` is set when the scope is created and
costs nothing, because the EF query filters read it on every single query. Everything else describes
the persisted `Tenant` and is resolved from the cache the first time it is read, so an application
that never reads tenant metadata never pays for the lookup. Assigning `TenantId` discards whatever
was resolved for the previous tenant.

`Tenant` does **not** implement `ITenantInfo`. The persisted entity and the runtime scope are
different things: one is a row, the other is a question about the current operation.

---

# 26. Establishing Tenant Scope

Scope must be establishable with no authenticated user, and by the same mechanism everywhere.

Valid callers include:

- authenticated HTTP requests;
- background jobs;
- administrative operations;
- system processes;
- tests;
- maintenance tooling.

**There is no tenancy middleware.** `ITenantInfo` is registered as a scoped factory that reads
`IUserInfo.TenantId` where an identity package is present, and starts in the null partition where
one is not. That choice is what keeps `JC.Tenancy` free of any ASP.NET Core dependency, and means a
background job and a request establish scope by identical means rather than by parallel mechanisms
that can drift.

Explicit scope, for work with no user or work deliberately crossing tenants:

```text
SetTenantInfoForTenant(tenantId)      set the scope's tenant
SetTenantInfoForTenant(tenant)        set it from an already-loaded record
CreateScopeForTenant(tenantId)        a new scope, already scoped
CreateAsyncScopeForTenant(tenantId)   the same, for async disposal
```

These mirror the identity equivalents in `JC.Identity.Shared` deliberately. A job needing both an
actor and a tenant establishes the user, then the tenant.

---

# 27. IUserInfo vs ITenantInfo

These concepts must remain distinct.

```text
IUserInfo.TenantId
= tenant assigned to the current authenticated user

ITenantInfo
= tenant this operation is currently scoped to
```

Typical request:

```text
IUserInfo.TenantId
    ↓
initialises
    ↓
ITenantInfo
```

But they are not permanently coupled.

Examples where scope may differ:

- SystemAdmin operates another tenant;
- background job runs for a chosen tenant;
- infrastructure task runs cross-tenant;
- app explicitly changes tenant scope.

Tenant-aware EF filtering follows `ITenantInfo`, never `IUserInfo` directly — reaching for the user
would tie filtering to there being a user at all, which §26 exists to avoid.

**But the filters do not read `ITenantInfo` directly either.** They bind to `ITenantScopedContext`
on the DbContext, which delegates to it:

```csharp
public string? CurrentTenantId => _tenantInfo.TenantId;
```

That indirection is an EF Core constraint, not a preference. EF caches the compiled model per
context type, and makes a specific allowance for a captured `DbContext` in a query filter — it
re-reads that context's members against the *active* instance on every query. No such allowance
exists for an arbitrary service. A filter closing over the scoped `ITenantInfo` would bake whichever
tenant happened to warm the model into every later request, silently and across tenants.

---

# 28. Default Tenant Scope

Null is a valid tenant partition.

Therefore:

```text
TenantId = null
```

can be a valid default context.

Filtering remains:

```text
entity.TenantId == currentTenantId
```

including null-to-null matching.

This preserves existing non-tenant/single-partition behaviour.

---

# 29. Tenant Scope Initialisation

Authenticated request:

```text
IUserInfo.TenantId
    ↓
ITenantContext
    ↓
tenant-aware DbContext filters
```

Background job:

```text
explicit tenant ID
    ↓
ITenantContext
    ↓
tenant-aware DbContext filters
```

No fake user should be required.

---

# 30. DbContext Integration

`JC.Tenancy` must plug into:

- `DataDbContext`;
- `IdentityDataDbContext`;
- both;
- other compatible DbContexts.

Two decisions must remain independent.

## Participates in filtering

A context may contain `IMultiTenancy` entities and apply automatic tenant filters.

## Owns Tenant storage

Exactly **one configured context** owns the authoritative Tenant table/store.

Example:

```text
IdentityDataDbContext
├── Users       ← tenant filtered
└── Roles

DataDbContext
├── Tenants     ← authoritative store
├── Orders      ← tenant filtered
└── Documents   ← tenant filtered
```

Or the Tenant table can live in IdentityDataDbContext while normal DataDbContext still participates in filtering.

All participating contexts use the same operational tenant scope.

---

# 31. No Automatic Tenant DbSet Everywhere

`DataDbContext` should not automatically expose a Tenant DbSet.

That would:

- force tenant persistence into apps not using tenancy;
- couple Core to concrete tenancy;
- create surprising migrations;
- blur tenant filtering and tenant ownership.

The owning context should be explicitly configured through `JC.Tenancy`.

Do not implement "Tenant table may not exist, silently no-op" magic.

---

# 32. ITenantStore

`JC.Tenancy` should expose `ITenantStore`.

It is the supported persistence/mutation boundary for Tenant.

Likely responsibilities:

- get tenant;
- list/query tenants;
- add;
- update;
- remove;
- read/update settings;
- coordinate cache invalidation.

Exact CRUD API should follow JC Packages conventions without becoming unnecessarily generic.

---

# 33. Tenant Mutation Rule

Tenant mutations should go through:

```text
ITenantStore
```

If consuming code bypasses the store and updates Tenant directly through EF/database access:

```text
cache invalidation is not guaranteed
```

That is acceptable.

Do not add complex defensive interception solely to compensate for callers bypassing the supported boundary.

Document this explicitly.

---

# 34. Tenant Caching

Tenant runtime information should be automatically cached.

Goals:

- avoid repeated Tenant reads;
- make tenant context establishment cheap;
- centralise Tenant/settings access.

Caching should be configurable.

A **short TTL** is preferred.

Do not hard-code a 24-hour lifetime because Tenant can contain security/business-sensitive state such as expiry, domain rules, or settings.

Exact default TTL remains open.

---

# 35. Tenant Cache Invalidation

`ITenantStore` should invalidate affected cache entries when:

- Tenant added;
- Tenant updated;
- Tenant removed;
- Tenant settings changed.

Subsequent reads refresh the cache.

Direct/out-of-band DB writes are not immediately detected and may remain stale until TTL expiry.

That is acceptable and documented.

---

# 36. Tenant Source Is Local Application Data

`JC.Tenancy` should not be designed around CAP as a tenant source.

Application tenants belong to the application.

CAP has its own organisation tenancy.

A CAP-authenticated application that uses JC.Tenancy still uses a local application `ITenantStore`.

---

# 37. Tenant Scope Switching

The engine should support explicit tenant scope changes.

Potential concepts:

```text
UseTenant(tenantId)
ForTenant(tenantId)
```

Changing scope should keep:

- TenantId;
- resolved ITenantInfo;

consistent.

Switching to a persisted tenant should resolve via the tenancy system/cache.

Switching to null is valid:

```text
TenantId = null
ITenantInfo = null
```

---

# 38. Cross-Tenant Access

Current `AllTenants()` behaviour is safe and role-gated.

v6 should preserve a safe cross-tenant route while making authorisation configurable.

An explicit unsafe route should also exist.

Potential concepts:

```text
AllTenants()
AllTenantsUnsafe()
```

The word `Unsafe` should be unavoidable for APIs bypassing normal permission checks.

---

# 39. Query-Level Bypass

Support query-level access.

Conceptually:

```text
query.AllTenants()
query.AllTenantsUnsafe()
```

Safe route:

- checks configured authorisation.

Unsafe route:

- bypasses tenant filtering without authorisation.

Valid unsafe use cases:

- trusted background jobs;
- system reconciliation;
- infrastructure;
- migration/maintenance tooling.

---

# 40. Scope-Level Bypass

Support scoped/context-level control too.

Potential concepts:

```text
UseTenant(...)
UseTenantUnsafe(...)
```

or explicit suppression.

Scope-level unsafe access is more dangerous because multiple downstream queries may inherit it.

Documentation should make that risk obvious.

---

# 41. Configurable Tenant Filtering

Default behaviour should remain:

> **Every `IMultiTenancy` entity is automatically filtered to the active application tenant.**

But v6 should support controlled configuration such as:

- exclude entity types;
- override particular filters;
- disable automatic filtering for a context;
- customise tenant resolution;
- configure safe bypass authorisation;
- configure the Tenant-storage context;
- configure cache behaviour;
- configure Identity/Data DbContext participation.

Default remains automatic and safe.

---

# 42. Safe Bypass Authorisation

If tenancy mechanics move out of `JC.Identity`, `JC.Tenancy` should ideally not hard-code:

```text
SystemAdmin
```

Potential future mechanisms:

- configurable predicate;
- `ITenantBypassAuthorizer`;
- application policy;
- Identity-supplied default adapter.

**Resolved.** `ITenantBypassAuthoriser` lives in `JC.Tenancy`, with a default implementation that
matches the current user against role *names* held in `TenantOptions.BypassRoles`.

Names rather than a constant, because `JC.Tenancy` and the identity packages are siblings and
neither may reference the other — and because an application on a different identity authority will
have its own word for the same idea. An application on JC.Identity configures `SystemAdmin`; the
decision stays with whoever owns the role.

It denies when no roles are configured and denies when no user resolves, so an application that has
not considered cross-tenant access has not accidentally granted it.

The safe API must remain safe.

The unsafe API explicitly bypasses this mechanism, and is named `AllTenantsUnsafe` so that nobody
reaches it by accident.

---

# 43. SystemRoles

`SystemRoles` now live in `JC.Identity.Shared`, not `JC.Identity` — a future `JC.CAP` needs the same
role vocabulary, and neither package should reimplement it. Applications on local Identity see no
change beyond the namespace.

They define generic high-level roles for an application whose identity comes from one of the JC
identity packages.

Examples:

- `SystemAdmin`;
- `Admin`.

They should not become universal Core roles.

Future CAP administrative roles are a separate security domain.

---

# 44. Application Roles vs CAP Roles

Full CAP role design is intentionally deferred.

Current foundational rule:

> **`IUserInfo.Roles` represents roles in the current consuming application's normal authorisation domain.**

Future CAP administrative roles must not be silently mixed into that standard role collection.

If CAP roles are ever supplied to consuming apps they should be:

- separately namespaced;
- clearly prefixed;
- stored in differentiated claims/contracts;
- difficult to accidentally mix with normal app roles;
- explicitly consumed by app policy.

Whether CAP `SystemAdmin` can deliberately imply application access remains unresolved.

---

# 45. BaseUser Remains Tenant-Aware

`BaseUser` should keep tenancy.

The refactor is not removing tenancy capability from local Identity users.

Instead:

- `BaseUser` remains tenant-aware **in its data**;
- JC.Identity integrates with JC.Tenancy;
- filtering/persistence mechanics move out of Identity;
- tenancy contracts remain reusable elsewhere.

**Tenant-aware and tenant-filtered are different things, and `BaseUser` is only the first.** It carries and persists a tenant, and that tenant flows into claims and `IUserInfo`. It is not subject to automatic tenant query filtering, and must not become so — see §14.1 for why that breaks authentication.

---

# 46. Likely JC.Identity → JC.Tenancy Dependency

Likely direction:

```text
JC.Identity
    ↓                    ↓
JC.Identity.Shared   JC.Tenancy
    ↓                    ↓
         JC.Core
```

`JC.Identity` depends on both. `JC.Identity.Shared` and `JC.Tenancy` are siblings and must not depend on each other — that independence is what lets an application take tenancy without identity, or a future `JC.CAP` take the identity runtime without tenancy.

The `JC.Identity → JC.Tenancy` edge is acceptable even if an app uses only the null tenant.

The architecture matters more than avoiding one package dependency.

Exact activation/configuration remains to be designed.

---

# 47. IdentityDataDbContext Refactor

Re-read and classify current `IdentityDataDbContext` responsibilities.

Likely tenancy pieces to move/delegate:

- tenant query filters;
- current tenant resolution;
- Tenant DbSet assumptions;
- `AllTenants` mechanics;
- tenant model configuration.

Identity-specific behaviour stays.

The resulting context becomes:

> **An Identity DbContext participating in JC.Tenancy**

rather than:

> **The owner of the tenancy implementation.**

---

# 48. DataDbContext Refactor

`DataDbContext` should be able to participate in tenancy independently from Identity.

Potential work:

- model/configuration hooks;
- tenant-filter installation;
- access to operational tenant context;
- preserve v4 multi-DbContext support;
- no mandatory Tenant table.

---

# 49. Multi-DbContext Compatibility

This refactor must respect v4 architecture.

A consuming app may have many contexts:

```text
IdentityDbContext
ApplicationDbContext
ReportingDbContext
OtherDbContext
```

Any relevant context may contain tenant-aware entities.

All participating contexts share the same operational tenant scope.

Exactly one context owns Tenant persistence.

Strong integration tests are required.

---

# 50. Background Jobs

Tenant context must work without authenticated HTTP users.

A job should be able to establish:

```text
TenantId = X
```

and then use normal tenant-aware repositories/DbContexts.

No fake `IUserInfo` or fake SystemAdmin should be required merely to scope a job.

Cross-tenant jobs should use explicit bypass APIs.

---

# 51. Authentication and Tenant Scope

Local Identity:

```text
BaseUser
    ↓
claims/user resolution
    ↓
IUserInfo
    ↓
ITenantContext initialisation
```

Future CAP SSO:

```text
CAP-authenticated application identity
    ↓
IUserInfo
    ↓
ITenantContext initialisation
```

This common pipeline is a core reason tenancy must not live inside `JC.Identity`.

---

# 52. Future CAP SSO — Explicitly Out of Scope

Do **not** fully design or implement the following as part of this refactor:

- CAP SSO protocol;
- CAP login flow;
- token format;
- OIDC/OAuth details;
- CAP session lifetime;
- consuming-app cookie design;
- refresh-token design;
- session revocation;
- CAP API endpoints;
- full CAP admin-role claims;
- complete application role-definition system;
- role-management UI;
- full `JC.CAP.Base`;
- full `JC.CAP`;
- account reassignment/session refresh policy.

These topics may only be referenced where they prove a v6 boundary requirement.

---

# 53. Authentication Refresh — Future Verification

One future CAP issue should be recorded:

> When authoritative identity data changes, when should a consuming app refresh/re-authenticate its `IUserInfo`?

Examples:

- app assignment removed;
- app role changed;
- application tenant changed;
- account disabled;
- email changed;
- display name changed.

This is broader than tenancy.

It should not be solved inside JC.Tenancy.

v6 only needs contracts that can be repopulated cleanly later.

---

# 54. CAP Compile-Time Integration Constraint

CAP integrates consuming applications through development-time/compile-time work.

A CAP-integrated app exposes a `.Data` package containing relevant:

- entities;
- DbContexts;
- business logic/services;
- domain models;
- CAP management integration.

For future CAP-integrated apps using JC.Tenancy, that `.Data` surface must expose what CAP needs to manage:

- application tenants;
- user → application-tenant assignment;
- application role assignments.

This is future CAP integration work.

The v6 Identity/Tenancy refactor only needs to make this possible cleanly.

---

# 55. One Authoritative Tenant Store

Many DbContexts may be tenant-filtered.

Only one DbContext may own the authoritative Tenant store for a given application tenancy model.

Do not support multiple independent Tenant tables for one application tenancy domain.

---

# 56. Persistence / Migration Considerations

Potential breaking migration concerns:

- `Tenant` package/namespace move;
- `TenantSettings` move;
- changed `IMultiTenancy`;
- removal of Tenant navigation requirement;
- IdentityDataDbContext changes;
- tenant filter registration changes;
- `BaseUser` interface changes;
- `IdentityTenantId`;
- Tenant DbSet ownership changes;
- new JC.Tenancy package;
- EF relationship changes;
- startup registration changes.

Migration docs must distinguish:

1. package/API migration;
2. EF schema migration;
3. configuration migration.

Avoid unnecessary database migrations.

In particular:

```text
BaseUser.TenantId
```

should remain persisted as-is unless a real schema reason requires otherwise.

---

# 57. Backward Data Safety

Existing user and tenant data must be preserved.

Principle:

> **Better architecture naming should not automatically become destructive persistence work.**

Preserve where possible:

- existing Tenant IDs;
- BaseUser TenantId column;
- user tenant assignments;
- TenantSettings JSON;
- Identity user data;
- existing table names.

Manually review generated EF migrations.

---

# 58. Tenant Foreign Keys

Removing concrete Tenant navigation from `IMultiTenancy` changes relationship assumptions.

Potential behaviour:

- no FK if Tenant is in another context;
- optional FK configuration when Tenant is in the same model;
- consuming entities may define their own navigation;
- relationship may be configured from TenantId alone.

Do not force impossible cross-context relationships.

Document migration implications.

## 58.1 This has already happened once

`SavedFile` in `JC.FileStorage` carried a `Tenant` navigation with `[ForeignKey(nameof(TenantId))]`,
mapped as:

```csharp
builder.HasOne(f => f.Tenant)
    .WithMany()
    .HasForeignKey(f => f.TenantId)
    .OnDelete(DeleteBehavior.SetNull);
```

Removing the navigation from `IMultiTenancy` removed both. Two consequences worth stating plainly:

- the foreign-key constraint disappears from the schema — a real migration, not a no-op rename;
- `OnDelete(SetNull)` went with it, so deleting a tenant no longer nulls `TenantId` on that tenant's
  saved files. Nothing replaces that behaviour automatically, and orphaned rows now keep pointing at
  a tenant that no longer exists.

Whether `JC.Tenancy` should offer opt-in FK configuration for entities that share a model with
`Tenant` is exactly the question this section raises. `JC.FileStorage` is the first concrete case to
answer it against, and the answer decides whether that delete behaviour comes back.

---

# 59. Tenant Expiry / Domain / MaxUsers

Tenant currently contains fields such as:

- expiry;
- domain;
- max users.

These should be available as tenant information, but it remains open whether JC.Tenancy itself enforces them.

Examples:

- CAP may enforce domain rules;
- another app may ignore them;
- max users may be workflow-specific;
- expiry may be access policy rather than filtering policy.

Do not accidentally turn all Tenant metadata into universal tenancy-engine enforcement.

---

# 60. Tenant Settings Runtime API

Desired direction:

```text
GetSetting(key)
GetSetting<T>(key)
GetSettings()
```

Potential behaviour:

- active settings only by default;
- typed conversion;
- default values;
- optional raw access.

Do not over-design before auditing current real uses.

---

# 61. Cache Configuration

Potential initial options:

```text
TenantCacheOptions
├── Enabled
└── TimeToLive
```

Keep initial implementation focused.

Distributed caching/invalidation should not be introduced unless a real deployment requires it.

---

# 62. Direct Database Changes

Document clearly:

> Direct EF/database changes to Tenant bypass `ITenantStore` guarantees.

Possible consequence:

- stale tenant info until TTL expiry.

That is acceptable.

Do not increase framework complexity solely to protect callers from intentionally bypassing the abstraction.

---

# 63. Testing Requirements

The refactor should introduce strong automated coverage.

## Identity

Verify:

- `BaseUser` still works with ASP.NET Identity;
- `IApplicationUser` mapping;
- `IdentityTenantId`;
- `IdentityAuthority.Local`;
- `IUserInfo` population;
- password behaviour;
- 2FA;
- lockout;
- account enabled/disabled behaviour;
- role seeding;
- SystemAdmin/Admin.

## Tenancy

Verify:

- null tenant filtering;
- non-null tenant filtering;
- multiple DbContexts share scope;
- one context owns Tenant while another only filters;
- Tenant store CRUD;
- cache hits/misses;
- cache invalidation via ITenantStore;
- direct EF mutation has no immediate invalidation guarantee;
- tenant switching;
- safe cross-tenant query;
- unsafe cross-tenant query;
- scope-level switching/bypass;
- configured exclusions;
- background-job/no-user scope;
- optional FK configuration.

## Compatibility

Verify:

- JC.Identity with null tenancy only;
- JC.Tenancy without JC.Identity;
- JC.Identity + JC.Tenancy;
- multiple participating DbContexts.

---

# 64. Documentation Required for v6

Create a dedicated migration guide.

## JC.Identity v5 → v6

Cover:

- package dependency changes;
- BaseUser changes;
- IApplicationUser;
- IdentityAuthority;
- IdentityDataDbContext changes;
- tenancy mechanics moved out;
- startup/config changes.

## JC.Tenancy

Document:

- purpose;
- relationship to Core and Identity;
- Tenant store ownership;
- participating DbContexts;
- null tenant;
- TenantSettings;
- cache;
- safe/unsafe bypass;
- background jobs.

## EF migration guidance

Document:

- expected no-op migrations;
- namespace/package changes that should not alter schema;
- Tenant table ownership;
- FK changes;
- requirement to review generated migrations before deployment.

---

# 65. Re-Audit Every Current JC.Identity Type

This audit is **complete**. Every type in the current `JC.Identity` (14 source files) and every
tenancy type in `JC.Core` has been read and classified into one of:

```text
Stay in JC.Identity
Move to JC.Identity.Shared
Move to JC.Tenancy
Stay in JC.Core
Replace/deprecate
Needs discussion
```

The classification is the source of truth for the implementation order in §74. Where a type is
split rather than moved whole, the split is stated in the notes.

## 65.1 Classification

| Type / member | Currently in | Classification | Notes |
|---|---|---|---|
| `BaseUser` | `Models/BaseUser.cs` | Stay in JC.Identity | Extends `IdentityUser`. Will implement `IApplicationUser` from Shared. **Must not implement `IMultiTenancy`** — see §14.1. |
| `BaseRole` | `Models/BaseRole.cs` | Stay in JC.Identity | Extends `IdentityRole`. Nothing shareable — one added `Description`. |
| `IdentityDataDbContext<TUser, TRole>` | `Data/IdentityDataDbContext.cs` | Stay in JC.Identity | Extends `IdentityDbContext` and cannot leave. The `Tenants` DbSet, the `Tenant` mapping and `CurrentTenantId` all move or change — see the three rows below. |
| `IdentityDataDbContext.Tenants` DbSet | `Data/IdentityDataDbContext.cs` | Move to JC.Tenancy | Tenant storage becomes the tenancy context's job — decisions 17 and 18. |
| `Tenant` mapping in `OnModelCreating` | `Data/IdentityDataDbContext.cs` | Move to JC.Tenancy | Key, lengths and the `Domain` index ship with the entity. |
| `IdentityDataDbContext.CurrentTenantId` | `Data/IdentityDataDbContext.cs` | Replace | Becomes a member of a tenancy contract instead of a property found by name. See the `ApplyTenantQueryFilters` row. |
| `ApplyTenantQueryFilters(ModelBuilder, DbContext)` | `Extensions/QueryExtensions.cs` | Move to JC.Tenancy | Also **replace** its internals: it resolves the tenant via `Expression.Property(contextConstant, "CurrentTenantId")` — a string. A context that misspells the property compiles and silently filters nothing. Must bind to a contract. |
| `AllTenants<T>(IQueryable<T>, IUserInfo)` | `Extensions/QueryExtensions.cs` | Move to JC.Tenancy | Carries an unresolved dependency on `SystemRoles.SystemAdmin` — see §65.2. |
| `UserInfo` | `Models/UserInfo.cs` | **Split** | The `IUserInfo` property surface moves to Shared. The two constructors taking `BaseUser` and `IEnumerable<BaseRole>` weld it to ASP.NET Identity and stay in JC.Identity as a derived type. |
| `UserInfoMiddleware` | `Middleware/UserInfoMiddleware.cs` | Move to JC.Identity.Shared | Blocked on one dependency: it resolves `IOptions<IdentityOptions>` purely to read three claim-type names (`EmailClaimType`, `UserIdClaimType`, `RoleClaimType`). Abstract that source first. |
| `DefaultClaims` | `Authentication/DefaultClaims.cs` | Move to JC.Identity.Shared | Twelve `const string` claim types, no dependencies. Written by the factory, read by the middleware; CAP needs the same names. |
| `DefaultClaimsPrincipalFactory<TUser, TRole>` | `Authentication/DefaultClaimsPrincipalFactory.cs` | Stay in JC.Identity | Derives from `UserClaimsPrincipalFactory<TUser, TRole>` and takes `UserManager`, `RoleManager` and `IdentityOptions`. Local-login only by construction — CAP receives claims, it does not mint them. |
| `SystemRoles` | `Authentication/SystemRoles.cs` | Move to JC.Identity.Shared | Constants plus a reflection helper; no dependencies. See §65.2 for the consequence for JC.Tenancy. |
| `IdentityMiddleware` | `Middleware/IdentityMiddleware.cs` | Move to JC.Identity.Shared | Depends only on `IUserInfo`, its own options and ASP.NET Core HTTP. No ASP.NET Identity reference at all — it moves as-is. |
| `IdentityMiddlewareOptions` | `Models/Options/IdentityMiddlewareOptions.cs` | Move to JC.Identity.Shared | Plain options object. Moves with the middleware it configures. |
| `IdentityHelper` | `Helpers/IdentityHelper.cs` | Move to JC.Identity.Shared | 2FA support: authenticator URI and key formatting. `UrlEncoder` and string building only. Not DI-registered — consumers construct it. |
| `AddIdentity<TUser, TRole, TContext>` (both overloads) | `Extensions/ServiceCollectionExtensions.cs` | Stay in JC.Identity | Calls `AddEntityFrameworkStores` and `AddDefaultTokenProviders`. |
| `AddIdentityBase<TUser, TRole, TUserInfo>` (both overloads) | `Extensions/ServiceCollectionExtensions.cs` | **Split, and rename** | The parts registering `IUserInfo` and the middleware options belong to Shared; the claims-factory registration stays. The name is now actively misleading once a Shared package exists — see §65.2. |
| `UseUserInfo` | `Extensions/ApplicationBuilderExtensions.cs` | Move to JC.Identity.Shared | Moves with `UserInfoMiddleware`. |
| `UseIdentityMiddleware` | `Extensions/ApplicationBuilderExtensions.cs` | Move to JC.Identity.Shared | Moves with `IdentityMiddleware`. |
| `UseIdentity` | `Extensions/ApplicationBuilderExtensions.cs` | Stay in JC.Identity | Composed entry point. Keeps its current order — authentication, user info, authorisation, identity rules. |
| `SeedRolesAsync<TRoles, TRole>` | `Extensions/ApplicationBuilderExtensions.cs` | Stay in JC.Identity | Needs `RoleManager<TRole>`. |
| `SeedDefaultAdminAsync<TUser, TRole, TContext>` | `Extensions/ApplicationBuilderExtensions.cs` | Stay in JC.Identity, **change** | Needs `UserManager<TUser>`, so it stays — but its `setupTenancy` branch writes `Tenant` rows straight through `context.Tenants` and calls `SaveChangesAsync`. That must go through `ITenantStore`, or the seed silently bypasses cache invalidation — decisions 18 and 19. |
| `ConfigureAdminAndRolesAsync<…>` | `Extensions/ApplicationBuilderExtensions.cs` | Stay in JC.Identity | Composes the two seeders above. |
| `IUserInfo` | `JC.Core/Models/IUserInfo.cs` | **Stay in JC.Core** | Cannot move. `RepositoryContext` and `AuditService` consume it inside Core; moving it inverts the dependency. |
| `IdentityAuthority` | new, `JC.Core` | Stay in JC.Core | §11 places the enum **on** `IUserInfo`, so it must sit where `IUserInfo` sits. |
| `IApplicationUser` | new | Move to JC.Identity.Shared | Not Core. It is an identity-store contract, and Core does not consume it (§5, §8.4). |
| `IMultiTenancy` | `JC.Core/Models/MultiTenancy/IMultiTenancy.cs` | Stay in JC.Core | Any package must be able to mark an entity tenant-scoped without referencing JC.Tenancy — decision 11. |
| `Tenant` | `JC.Core/Models/MultiTenancy/Tenant.cs` | Move to JC.Tenancy | Concrete entity with EF-shaped members and JSON settings helpers. |
| `TenantSettings` | `JC.Core/Models/MultiTenancy/Tenant.cs` | Move to JC.Tenancy | Declared in the same file as `Tenant`; moves with it. |

## 65.2 What the audit turned up that the design had not accounted for

Four items came out of the classification rather than going into it.

**`AllTenants` breaks the sibling rule.** It bypasses the tenant filter when
`userInfo.IsInRole(SystemRoles.SystemAdmin)`. `SystemRoles` is classified into Shared;
`AllTenants` is classified into JC.Tenancy. Decision 37 says the two are siblings that must not
depend on each other, so JC.Tenancy cannot reach that constant. The system-admin bypass has to be
expressed on the tenancy side — as configuration, or as a member of the tenant context — not as a
role-name string borrowed from identity.

**`AddIdentityBase` is now the wrong name.** It predates this design and means "register JC.Identity
without registering ASP.NET Identity". With a `JC.Identity.Shared` package in the picture, a reader
will take it for that package's registration call, which it is not. Rename before the package
ships, while it is only a rename.

**`UserInfo` cannot move whole.** Its two constructors take `BaseUser` and `IEnumerable<BaseRole>`.
The properties are the shareable part; the constructors are not.

**`IUserInfo` is mutable and the middleware depends on that.** Every member is `{ get; set; }` and
`UserInfoMiddleware` resolves the scoped instance and assigns to it field by field. The interface is
documented as a read-only contract; it is not one. Any move or reshape has to keep the
populate-then-freeze behaviour working, or replace it deliberately.

---

# 66. Re-Audit Real Consumers

Test against actual consuming applications.

Priority examples:

- Central Admin Portal;
- Portfolio;
- Alwaha;
- Monappoly;
- other JC.Identity consumers;
- multi-DbContext consumers;
- tenant-aware package consumers;
- tenant-aware background jobs.

For each consumer identify:

- where Tenant table currently lives;
- which DbContexts are filtered;
- null tenancy usage;
- SystemAdmin bypass usage;
- direct Tenant EF writes;
- dependency on Tenant navigation;
- startup assumptions that tenancy comes from Identity.

---

# 67. Likely Breaking Changes

Treat this work as intentionally breaking.

Potential breaks:

- new `JC.Tenancy`;
- moved `Tenant`;
- moved `TenantSettings`;
- changed `IMultiTenancy`;
- new `IApplicationUser`;
- new `IdentityAuthority`;
- changed `IUserInfo`;
- changed `BaseUser`;
- changed Identity DbContext behaviour;
- changed DI registration;
- changed tenant filter setup;
- changed `AllTenants`;
- unsafe APIs;
- changed FK/navigation assumptions;
- moved extensions/namespaces.

Do not preserve an incorrect boundary merely to shorten migration notes.

---

# 68. What Should Not Change Without Separate Reason

This refactor is not a redesign of all JC.Identity capabilities.

Unless a real problem exists, preserve:

- ASP.NET Identity implementation;
- BaseUser local persistence;
- local user ownership;
- local role ownership;
- SystemAdmin/Admin conventions;
- password policy;
- 2FA;
- lockout;
- confirmation;
- local Identity configuration;
- overall package purpose.

The primary change is **boundary hardening**, not feature replacement.

---

# 69. Roles — Intentionally Open

Current accepted principles:

1. Local JC.Identity roles belong to the consuming application's own authorisation plane.
2. `IUserInfo.Roles` contains application roles.
3. CAP administrative roles are a separate security domain.
4. If CAP roles are sent to apps, they must use clearly differentiated claims/contracts.
5. `{App}Access` is CAP administrative access, not SSO application access.
6. SSO application assignment is separate from CAP administrative access.
7. Future app-role assignment belongs to CAP integration, not JC.Identity.

Still unresolved:

- whether CAP SystemAdmin may deliberately bypass app roles;
- CAP admin claim representation;
- app role-definition ownership;
- re-authentication after role changes;
- refresh/revocation model.

Do not accidentally resolve these during this v6 refactor.

---

# 70. Future JC.CAP Compatibility Goal

After v6, a future `JC.CAP` should ideally be able to:

- authenticate through CAP;
- represent authoritative user data without ASP.NET Identity;
- populate `IUserInfo`;
- set `IdentityAuthority.CAP`;
- populate app-specific roles;
- optionally map user to local app tenant;
- initialise `ITenantContext`;
- coexist with `JC.Tenancy`;
- operate without referencing `JC.Identity`.

If v6 establishes these foundations, later CAP work may not require another ecosystem-wide identity/tenancy migration.

---

# 71. Proposed v6 Architecture

```text
JC.Core
│
├── IUserInfo            [pinned — Core consumes it]
├── IdentityAuthority    [pinned — lives on IUserInfo]
├── IMultiTenancy
├── ITenantInfo?         [placement to confirm]
└── ITenantContext?      [placement to confirm]
        │
        ├──────────────────────────┐
        ▼                          ▼
JC.Tenancy                   JC.Identity.Shared
│                            │
├── Tenant                   ├── IApplicationUser
├── TenantSettings           ├── UserInfo (IUserInfo impl)
├── ITenantStore             ├── UserInfoMiddleware
├── tenant cache             ├── IdentityMiddleware + options
├── tenant context impl      ├── DefaultClaims, SystemRoles
├── EF tenant filters        └── IdentityHelper (2FA)
├── tenant switching                │
└── safe/unsafe bypass              ├──────────────┐
        │                           ▼              ▼
        │                        JC.Identity    future JC.CAP
        │                        │              │
        │                        ├── BaseUser   ├── CAP SSO
        │                        ├── BaseRole   ├── CAP user DTOs
        │                        ├── ASP.NET Identity
        │                        ├── local roles
        │                        ├── password/2FA/security
        │                        └── Identity DbContext integration
        │                                       │
        └──────────── both may use ─────────────┘

                     future JC.CAP.Base  →  may depend on JC.Identity.Shared
                                            never the reverse
```

`JC.Identity.Shared` and `JC.Tenancy` are siblings on Core. `JC.Identity` depends on both; a future `JC.CAP` depends on `JC.Identity.Shared` and optionally `JC.Tenancy`, and on neither `JC.Identity` nor ASP.NET Identity.

Conceptual only; final package references must be validated.

---

# 72. Current Decisions

Treat these as current working decisions unless implementation proves them wrong.

1. `JC.Identity` remains local ASP.NET Identity.
2. CAP SSO consumers should not normally depend on JC.Identity.
3. `IUserInfo` remains the current authenticated runtime identity.
4. Add simple `IdentityAuthority`.
5. Authority means who owns/supplies identity, not login method.
6. Add read-only `IApplicationUser`.
7. `BaseUser` stays in JC.Identity and implements it.

8. `IApplicationUser.IdentityTenantId` differs from `IUserInfo.TenantId`.
9. Existing BaseUser `TenantId` persistence remains; expose `IdentityTenantId => TenantId`.
10. `IUserInfo.TenantId` means consuming-application tenant.
11. `IMultiTenancy` stays in Core.
12. Concrete `Tenant` and `TenantSettings` move to JC.Tenancy.
13. `IMultiTenancy` no longer requires concrete Tenant navigation.
14. JC.Tenancy owns tenancy implementation.
15. JC.Tenancy works without JC.Identity.
16. Multiple DbContexts can be tenant filtered.
17. Exactly one DbContext owns Tenant storage.
18. Tenant mutations go through `ITenantStore`.
19. Direct EF writes do not guarantee cache invalidation.
20. Tenant info is cached with short/configurable TTL.
21. ITenantStore mutations invalidate cache.
22. Null tenant is a valid default partition.
23. ITenantContext does not require a user.
24. EF filtering uses ITenantContext, not IUserInfo directly.
25. Safe and unsafe cross-tenant APIs should exist.
26. Query-level and scope-level control should both be possible.
27. Default filtering remains automatic/safe.
28. Advanced configuration/backdoors should exist.
29. CAP organisation tenant and application tenant are separate.
30. CAP user→app-tenant mapping is optional and only relevant when the app uses JC.Tenancy.
31. Future app tenant mapping is strictly one user → one app tenant.
32. Missing map means null app tenant.
33. CAP roles and app roles are separate domains.
34. Full JC.CAP construction is outside this refactor.

### Added after the Phase 1 inventory

35. A third package, `JC.Identity.Shared`, holds the identity runtime common to `JC.Identity` and a future `JC.CAP` — `IApplicationUser`, the `IUserInfo` implementation, the claims middleware and the claim-type constants.
36. `JC.Identity.Shared` ships working code, not abstractions only.
37. `JC.Identity.Shared` and `JC.Tenancy` are siblings on Core and must not depend on each other.
38. `JC.Identity.Shared` and `JC.CAP.Base` are distinct packages on different axes. `JC.CAP.Base` may depend on `JC.Identity.Shared`; never the reverse.
39. `IUserInfo` stays in `JC.Core` because Core consumes it internally. It cannot move.
40. `IdentityAuthority` stays in `JC.Core` because it lives on `IUserInfo`.
41. ~~`IApplicationUser` moves to `JC.Identity.Shared`, not `JC.Core`.~~ **Reversed in Phase 5 — see
    decision 63.** It lives in `JC.Core`, because `JC.Tenancy` needs it and may not reference
    `JC.Identity.Shared`.
42. **`BaseUser` must not implement `IMultiTenancy`.** A global query filter on the user entity breaks `UserManager` and `SignInManager`, because authentication resolves a user before a tenant scope exists. See §14.1.
43. The `IUserInfo` implementation must be split on extraction — the properties move, the `BaseUser`-shaped constructors stay in `JC.Identity`.
44. The claims middleware's dependency on `IOptions<IdentityOptions>` must be abstracted before the middleware can move.
45. The tenant filter's string-based `CurrentTenantId` lookup must be replaced with a contract, not relocated.
46. Identity's admin seeding must stop writing `Tenant` rows directly and go through `ITenantStore`.

### Added while implementing Phases 2–4

47. Types moved into `JC.Identity.Shared` take `JC.Identity.Shared.*` namespaces, so a future
    `JC.CAP` never imports a `JC.Identity` namespace. Consumers update their usings; v6 is breaking
    anyway.
48. `IdentityAuthority` lives in `JC.Core.Enums` and its zero value is `None`, so an unset authority
    reads as "no authentication took place" rather than silently claiming to be local.
49. `IUserInfo.Authority` is supplied by `IdentityClaimTypeOptions`, not by the concrete type's
    constructors, and the claims middleware stamps it only on the authenticated branch — an
    anonymous request keeps `None`.
50. The `IUserInfo` implementation splits as `UserInfoBase` (Shared) and `UserInfo` (JC.Identity).
    The derived type keeps the name, so existing consumer code and the default type argument are
    unaffected.
51. `AddIdentityBase` becomes `AddIdentityServices` (JC.Identity) and `AddSharedIdentityServices`
    (Shared). Removed outright — no `[Obsolete]` forwarders.
52. JC.Identity's claim types are copied from `IdentityOptions` by an `IConfigureOptions<>`, never
    eagerly at registration, so a consumer customising `ClaimsIdentity` afterwards is still honoured.
53. Both packages ship a supported way to establish ambient identity and tenant scope outside a
    request — `UserInfoExtensions` and `TenantInfoExtensions`. Constructing an `IUserInfo` or
    `ITenantInfo` and passing it around cannot work, because both are scoped and populated in place.
54. **`ITenantContext` and `ITenantInfo` are one concept.** Merged into `ITenantInfo`. See §25.
55. `JC.Tenancy` owns tenant filtering outright. Core keeps `IMultiTenancy` only — marking an entity
    tenant-scoped stays free, while filtering costs a reference to the package that does the
    filtering.
56. `ITenantInfo` is registered as a scoped factory, not populated by middleware, so `JC.Tenancy`
    takes no ASP.NET Core dependency and scope is established identically in requests, jobs and
    console applications.
57. The filters bind to `ITenantScopedContext` on the DbContext, never to `ITenantInfo` directly.
    An EF Core model-caching constraint, explained in §27.
58. `ApplyTenantFilters` is a no-op where a model holds no tenant-scoped entities, and **throws** at
    model build where it holds some but the context cannot say which tenant is current. Silently
    returning every tenant's rows is not an available outcome.
59. Cross-tenant access goes through `ITenantBypassAuthoriser`, configured by role name. See §42.
60. `AddTenancy<TContext>` is constrained to `ITenantDbContext` and throws on a second registration,
    enforcing one authoritative tenant store per application.
61. `ITenantStore` follows the suite's established CRUD shape — `Try*` methods returning
    `TenantValidationResponse`, over `IRepositoryManager` — and enforces unique tenant name and
    unique domain on add, update and restore.
62. Neither identity package references `JC.Tenancy`, and `JC.Tenancy` references no identity
    package. This resolves the open question about a `JC.Identity → JC.Tenancy` edge: there isn't
    one. Tenant filtering is wired by the consuming application, per DbContext.

### Added while implementing Phase 5

63. **Decision 41 is reversed. `IApplicationUser` lives in `JC.Core`, not `JC.Identity.Shared`.**
    Decision 41 was made before anything outside the identity packages needed the contract. Assigning
    a tenant to a user record does, and `JC.Tenancy` may not reference `JC.Identity.Shared` under the
    sibling rule (decision 37), so Core is the only place both can see it. §5's argument that
    "nothing in Core consumes it" no longer decides the question — what decides it is that two
    siblings need it and only Core is visible to both.
64. **`IApplicationUser` is read/write.** Every member is `{ get; set; }`, reversing the read-only
    intent in §13. It is a description of how the suite *stores* a user, and storage is not a
    one-way concern — the same reasoning that already made `IUserInfo` mutable. `BaseUser`
    satisfies the write side by routing `IdentityTenantId` to its existing `TenantId` column, so the
    read-only projection becomes a two-way one and the schema is unchanged.
65. **`TenantSeeder`, a concrete class with no interface.** Kept separate from `ITenantStore`
    because seeding is startup work and the store is a CRUD boundary, not because any authority
    needs to override it. An earlier draft of this decision justified the split by supposing a
    future `JC.CAP` would substitute a seeder that refuses — that reasoning was wrong and is
    withdrawn. §17 already says it: an application tenant belongs to the application, and CAP has no
    view on it. A CAP-authenticated application seeds its own default tenant exactly as any other
    does. With no second implementation in prospect, there is nothing for an interface to abstract.
    Two overloads: one creating the tenant alone, one also assigning it to a user.
66. **Identity seeds the administrator; tenancy gives it a tenant.** `SeedDefaultAdminAsync` and
    `ConfigureAdminAndRolesAsync` return `TUser?` — the created *or existing* administrator, `null`
    only where creation was attempted and failed. Returning the existing account is what makes the
    follow-on tenant assignment idempotent: a first run that created the user but failed the tenant
    step corrects itself on the next start.
67. **`setupTenancy` is gone, replaced by `assignAdminRole`.** The old flag had drifted into
    controlling something it did not name — with the tenant block commented out it did nothing
    *except* silently suppress the `Admin` role. The replacement says what it does and defaults to
    `true`. `defaultTenantConfigKey` is removed outright; the tenant name is now an argument to
    `SeedDefaultTenantAsync`.
68. **`ConfigureAdminAndRolesAsync` and `SeedDefaultAdminAsync` lose their `TContext` type
    parameter.** It existed only to reach `context.Tenants`. With tenant seeding gone from
    JC.Identity nothing uses it, and an unused constrained type parameter forces every caller to
    name a context for no reason.
69. **`IdentityClaimTypeOptions` is renamed `IdentityProjectionOptions`**, and its `Authority`
    defaults to `None` rather than `Local`. Both were flagged in §73; the name was wrong because the
    type carries an authority as well as claim types, and the default was wrong because it let an
    authority that never declares itself pass as local. JC.Identity now states `Local` in its
    `IConfigureOptions`, and `UserInfo`'s parameterless constructor no longer stamps it — so an
    anonymous request keeps `None`, which is what decision 49 always intended.
70. **`IdentityDataDbContext` does not filter by tenant.** A tenant-scoped application derives from
    it, implements `ITenantScopedContext` and calls `ApplyTenantFilters` itself. This is decision 62
    made concrete, and it is what lets a single-tenant Identity application avoid JC.Tenancy
    entirely.

---

# 73. Open Questions

## Contract placement

- `ITenantInfo`: Core or JC.Tenancy?
- `ITenantContext`: Core or JC.Tenancy?
- Does anything outside JC.Tenancy need them without referencing JC.Tenancy?

## Safe bypass authorisation

- configurable predicate?
- `ITenantBypassAuthorizer`?
- JC.Identity-provided SystemAdmin default?
- custom identity-provider integration?

## Tenant policy

- expiry: metadata or enforced?
- domain: metadata or enforced?
- max users: metadata or enforced?

## Cache

- default TTL;
- exact cache implementation;
- concurrency;
- manual refresh/invalidation;
- multi-instance implications.

## EF integration

- registration API;
- model-builder hooks;
- Tenant-storage owner registration;
- FK behaviour;
- migration ownership.

## IApplicationUser

- exact property list;
- nullability;
- security/account-state fields;
- mapping from IdentityUser;
- DTO compatibility.

## IUserInfo

- exact `IdentityAuthority` naming;
- verify all current fields;
- ensure CAP/custom authorities can leave unsupported values null/default.

## Roles

- deliberately defer full CAP role design.

## Status of the questions above

Several are now answered, and the answers are recorded as decisions in §72 rather than repeated here.

- **Contract placement** — answered. Decision 55: all tenancy contracts in `JC.Tenancy`, only
  `IMultiTenancy` in Core. `ITenantInfo` and `ITenantContext` merged (decision 54).
- **Safe bypass authorisation** — answered. Decision 59 and §42.
- **`IApplicationUser` exact property list** — answered. Fifteen members, verified against
  `BaseUser` and `IdentityUser`; `IdentityTenantId` projects the existing column and is `[NotMapped]`,
  so no migration.
- **`IdentityAuthority` naming** — answered. Decision 48.
- **Cache** — partly answered. `IMemoryCache`, five-minute default lifetime, invalidated by every
  store write. Concurrency and multi-instance behaviour are untouched and remain open.
- **Tenant policy** — unchanged and still open. `ITenantInfo.IsExpired` reports; nothing enforces
  expiry, domain rules or user limits.
- **EF integration** — mostly answered by decisions 55–58 and 60. Migration ownership is untouched.

## Still open, going into the next session

~~**`IdentityClaimTypeOptions.Authority` still defaults to `Local`.**~~ Closed in Phase 5 —
decision 69. Defaults to `None`; JC.Identity states `Local` in its `IConfigureOptions`.

~~**`IdentityClaimTypeOptions` is now misnamed.**~~ Closed in Phase 5 — renamed
`IdentityProjectionOptions` (decision 69).

**Tenant cache concurrency.** Two scopes missing simultaneously will both load. Harmless, but
undecided — and distributed invalidation across instances is entirely unaddressed.

**Soft-delete semantics for tenants.** `TryDeleteAsync` soft-deletes and `TryRestoreAsync`
revalidates, because a name freed by a delete can be claimed while the tenant is away. Whether a
restore *should* be able to fail on a name clash, rather than forcing a rename first, is worth a
second look.

**Documentation is stale.** `Documentation/JC.Identity/API.md`, `Guide.md` and `Setup.md`,
`JC.Identity/README.md` and `Documentation-Writing-Guide.md` all still describe the pre-v6
namespaces and `AddIdentityBase`. Phase 7 work, but the volume is now significant.

**Packaging is incomplete for both new packages.** `JC.Identity.Shared` and `JC.Tenancy` both carry
`Description: TBD`, neither has a `README.md`, and neither has a Central Package Management entry.
`dotnet pack` fails `NU5039` on the missing readme; a build with `UseLocalProjectReferences` off
fails `NU1010` on the missing versions.

## Raised by the Phase 1 inventory

**Claim-type source.** `UserInfoMiddleware` reads `EmailClaimType`, `UserIdClaimType` and
`RoleClaimType` from `IOptions<IdentityOptions>`. Shared cannot take that dependency. Options:
a small options object in Shared with defaults matching ASP.NET Identity's, which JC.Identity
overrides from `IdentityOptions` at registration; or a narrow interface Shared defines and each
authority implements. Which?

**`SystemAdmin` bypass across the sibling boundary.** `AllTenants` needs the bypass check but
JC.Tenancy cannot reference `SystemRoles` in Shared (§65.2). This is the same question as *Safe
bypass authorisation* above, now with a concrete forcing case — the answer chosen there must work
for `AllTenants` on day one, not later.

~~**Is JC.Tenancy a required or optional dependency of JC.Identity?**~~ **Closed in Phase 5:
neither — there is no reference at all.** The middle option won. Tenant seeding moved out to
JC.Tenancy as `TenantSeeder`, Identity's seeder returns the administrator it created, and the
consuming application joins the two. `setupTenancy` and `defaultTenantConfigKey` did not survive,
and nor did the `TContext` type parameter that existed only to reach `context.Tenants`. See
decisions 63–68.

**Does `IUserInfo` stay mutable?** §65.2 records that it is mutable today and that
`UserInfoMiddleware` depends on that. Decide before the middleware moves: keep populate-then-freeze
as-is, or replace it with a factory that constructs a finished instance. The second is cleaner and
breaks every consumer that assigns to `IUserInfo`.

**What is `AddIdentityBase` renamed to?** And do the current names stay as `[Obsolete]` forwarders
for one version, or break outright at 6.0.0?

**What is the split `UserInfo`'s derived type called?** The base moves to Shared; the type in
JC.Identity carrying the `BaseUser` constructors needs a name, and `UserInfo` is taken.

**Does Shared register `IdentityHelper`?** It is not DI-registered today — consumers construct it
with a `UrlEncoder`. Moving it is a chance to register it; leaving it unregistered is also a
defensible answer.

---

# 74. Suggested Implementation Order

## Phase 1 — Inventory — **complete**

- ~~classify every JC.Identity type/member~~ — §65;
- ~~identify all tenancy mechanics~~ — §65.1;
- record current EF/schema assumptions — see the `CurrentTenantId` and `ApplyTenantQueryFilters`
  rows in §65.1;
- find current consumers — §66, still outstanding.

The blocking questions the inventory raised are in §73 under *Raised by the Phase 1 inventory*.
Several of them — the claim-type source, the `SystemAdmin` bypass, `IUserInfo` mutability — must be
answered before Phase 3, not during it.

## Phase 2 — Core contracts

- ~~`IdentityAuthority` in JC.Core~~ — added as `JC.Core.Models.IdentityAuthority`, exposed as
  `IUserInfo.Authority`, defaulting to `Local`;
- ~~refine `IMultiTenancy`~~ — reduced to `TenantId`; the concrete `Tenant` navigation is gone;
- ~~decide tenant-contract placement~~ — all of it in JC.Tenancy, none in Core. `IMultiTenancy`
  stays in Core so an entity can be *marked* tenant-scoped for free; declaring a context tenant-scoped
  costs a JC.Tenancy reference, which is the package you are already using if you want filtering.
  `ITenantInfo` and `ITenantContext` are merged — they were one concept described twice;
- ~~replace the string-based `CurrentTenantId` lookup with a contract the filter binds to~~ —
  `ITenantScopedContext`.

`IApplicationUser` moves out of this phase — it belongs to JC.Identity.Shared (decision 41), so it
lands in Phase 3.

## Phase 3 — JC.Identity.Shared — **complete**

Done before JC.Tenancy. It was the smaller extraction, it has no dependency on tenancy, and it
proves the boundary while JC.Identity is otherwise unchanged.

- ~~create package~~;
- ~~add `IApplicationUser`; `BaseUser` implements it~~ — `IdentityTenantId` projects the existing
  `TenantId` column and is `[NotMapped]`, so no migration;
- ~~move `DefaultClaims`, `SystemRoles`, `IdentityHelper`, `IdentityMiddleware` and its options~~ —
  moved as-is;
- ~~abstract the claim-type source, then move `UserInfoMiddleware`~~ — `IdentityClaimTypeOptions`,
  populated from `IdentityOptions` by an `IConfigureOptions<>` so the copy happens after the
  consuming application has configured Identity;
- ~~split `UserInfo`~~ — `UserInfoBase` in Shared, `UserInfo : UserInfoBase` in JC.Identity;
- ~~move `UseUserInfo` and `UseIdentityMiddleware`; `UseIdentity` stays and composes them~~;
- ~~split and rename `AddIdentityBase`~~ — now `AddSharedIdentityServices` in Shared and
  `AddIdentityServices` in JC.Identity. Removed outright rather than left as `[Obsolete]`
  forwarders; see §73.

Added beyond the original list: `UserInfoExtensions`, giving a supported way to establish an
ambient identity outside an HTTP request — `PopulateFrom`, `SetUserInfoForUser`,
`CreateScopeForUser` and `CreateAsyncScopeForUser`. The projecting constructors already implied
such a path existed (they set `IsSetup`, which suppresses the claims middleware) but nothing
provided one, and because `IUserInfo` is scoped and populated in place, a constructed instance can
never become the ambient one. This is what §50 asks for: a background job can now take attribution
without a fake user.

Namespaces on the moved types were rebranded to `JC.Identity.Shared.*`, so that a future JC.CAP
never imports a `JC.Identity` namespace.

JC.Identity behaves identically and still owns tenancy. Nothing about tenant filtering has moved.

## Phase 4 — JC.Tenancy

- ~~create package~~;
- ~~move Tenant/TenantSettings~~;
- ~~add ITenantStore~~ — `Try*` methods returning `TenantValidationResponse`, over
  `IRepositoryManager`, with unique name and unique domain enforced on add, update and restore;
- ~~add tenant cache~~ — `IMemoryCache`, short default lifetime, invalidated by every store write;
- ~~add tenant context~~ — merged into `ITenantInfo`, registered as a scoped factory rather than
  populated by middleware, so JC.Tenancy needs no ASP.NET Core dependency and behaves identically in
  a request, a background job and a console application. `TenantId` resolves eagerly because the
  filters read it on every query; the rest of the tenant resolves from cache on first read;
- ~~move filtering mechanics, binding to the Phase 2 contract rather than a property name~~ —
  `ApplyTenantFilters` binds to `ITenantScopedContext.CurrentTenantId`. A no-op where the model holds
  no tenant-scoped entities, and a startup failure where it holds some but the context cannot say
  which tenant is current — silently returning every tenant's rows is the one outcome not on offer;
- ~~move `AllTenants`, with the bypass expressed without `SystemRoles`~~ — takes an
  `ITenantBypassAuthoriser`, with `AllTenantsUnsafe()` alongside it. The default authoriser matches
  configured role *names*, so the sibling boundary holds and a non-Identity authority can use its own;
- ~~support multiple participating DbContexts~~ — any context implementing `ITenantScopedContext`
  and calling `ApplyTenantFilters`;
- ~~support one Tenant-storage owner~~ — `AddTenancy<TContext>` constrains to `ITenantDbContext` and
  throws on a second registration.

Note the dependency that did *not* appear: nothing in JC.Tenancy references an identity package, and
nothing in JC.Identity references JC.Tenancy.

## Phase 5 — JC.Identity adaptation — **complete**

- ~~preserve TenantId storage~~ — the column is untouched; `IdentityTenantId` now reads *and writes*
  it, so there is still no migration;
- ~~populate IdentityAuthority.Local~~ — stated once, by JC.Identity's `IConfigureOptions`, rather
  than in `UserInfo`'s constructor. The option now defaults to `None`, so an anonymous request and an
  authority that never declares itself both report "nobody authenticated" instead of "local";
- ~~remove ownership of tenancy mechanics~~ — `JC.Identity/Extensions/QueryExtensions.cs` deleted
  outright, and `IdentityDataDbContext` has lost `CurrentTenantId`, the filter call and the
  commented-out `Tenants` DbSet and `Tenant` mapping. It is now an Identity context with an audit
  trail and nothing tenant-shaped in it;
- ~~route `SeedDefaultAdminAsync`'s tenant creation through `ITenantStore`~~ — **superseded.** This
  bullet predates decision 62 and assumed a `JC.Identity → JC.Tenancy` edge that decision 62 says
  does not exist. Resolved the other way instead: Identity's seeder returns the administrator, and
  JC.Tenancy's `TenantSeeder` gives that administrator a tenant. See decisions 63–68;
- ~~confirm `BaseUser` still does **not** implement `IMultiTenancy`~~ — confirmed. It implements
  `IApplicationUser` only. End-to-end login verification against a consuming application is Phase 6;
- ~~preserve local Identity behaviour~~ — with one deliberate change: the `setupTenancy` flag is
  gone, and with it the branch that silently suppressed the `Admin` role. See decision 67.

## Phase 6 — Consumer migration

- migrate real apps;
- validate null tenancy;
- validate multiple DbContexts;
- validate CAP itself as a JC.Identity consumer.

## Phase 7 — Hardening

- tests;
- migration review;
- docs;
- release notes;
- remove/deprecate old APIs.

---

# 75. Design Principle for v6

> **Do foundational breaking work now when it produces a cleaner identity boundary for years to come.**

Do not preserve v5 architecture merely to avoid a migration if the same incorrect boundary would force v7 as soon as `JC.CAP` arrives.

At the same time:

> **Do not redesign JC.Identity into JC.CAP.**

The intended responsibilities remain:

```text
JC.Identity
= local ASP.NET Identity

JC.Tenancy
= reusable application tenancy engine

JC.Core
= shared foundational contracts

JC.CAP
= future central identity integration
```

---

# 76. Definition of Success

The refactor succeeds if, after v6:

1. Existing local Identity apps can migrate cleanly and retain expected behaviour.
2. Apps can use tenancy without JC.Identity.
3. JC.Identity can use tenancy without owning it.
4. Tenant-aware data can span multiple DbContexts.
5. Tenant storage can live in whichever configured context is appropriate.
6. Null tenancy remains valid.
7. Tenant cache behaviour is explicit and predictable.
8. `IApplicationUser` can represent authoritative user data without ASP.NET Identity coupling.
9. `IUserInfo` remains the authoritative current-user abstraction.
10. Identity tenancy and application tenancy are no longer conflated.
11. A future JC.CAP package can populate the same runtime contracts without depending on JC.Identity.
12. Full CAP-specific session/role design can happen later without moving foundational types again.

---

# 77. Final Note

This is an **initial architecture/refactor design**, not a locked specification.

Implementation should challenge it.

If current EF behaviour, real consumers, migrations, or code review exposes a cleaner model, update this document.

The important outcome is the boundary:

> **Local Identity, application tenancy, runtime user context, and future CAP identity integration are related concerns — but they are not the same concern and should not live in the same package merely because they currently intersect.**
