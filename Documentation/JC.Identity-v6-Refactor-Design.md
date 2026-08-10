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

Likely responsibilities:

- `IUserInfo`;
- `IApplicationUser`;
- `IdentityAuthority`;
- `IMultiTenancy`;
- possibly `ITenantInfo`;
- possibly `ITenantContext`.

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

# 8. Future JC.CAP.Base — Boundary Only

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

# 9. IUserInfo — Runtime Authenticated Identity

`IUserInfo` already belongs in `JC.Core`.

It should remain the authoritative **current server-side identity context**.

Meaning:

> **Who is the user executing this operation, and what does this consuming application currently know about them?**

`IUserInfo` is not a persisted user record.

It is a runtime projection.

Its existing broad set of properties can remain. Properties unavailable from a particular identity authority may remain null/default.

This avoids introducing multiple competing current-user abstractions.

---

# 10. IdentityAuthority

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

# 11. IApplicationUser — Authoritative User Record Contract

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

# 12. IApplicationUser Shape

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

# 13. BaseUser

`BaseUser` remains in `JC.Identity`.

It remains an ASP.NET Identity persistence model.

Conceptually:

```text
BaseUser
    : IdentityUser
    : IApplicationUser
    : IMultiTenancy
```

Explicit `IMultiTenancy` participation is preferred over property-name discovery.

`BaseUser` must not move to `JC.Core`.

A future CAP SSO consuming application should not need `BaseUser`.

---

# 14. IdentityTenantId

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

# 15. IUserInfo.TenantId

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

# 16. CAP Tenant vs Application Tenant

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

# 17. Future CAP Application Tenant Mapping

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

# 18. Null Tenant Semantics

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

# 19. IMultiTenancy

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

# 20. Remove Concrete Tenant Navigation from IMultiTenancy

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

# 21. Tenant and TenantSettings Move

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

# 22. Existing Tenant Model

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

# 23. TenantSettings

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

# 24. ITenantInfo

Introduce/formalise a read-only runtime tenant-information abstraction.

Potentially:

```text
ITenantInfo
├── Id
├── Name
├── Description
├── Domain
├── MaxUsers
├── ExpiryDateUtc
├── GetSetting(...)
└── GetSettings(...)
```

Exact fields should follow the actual current Tenant model.

Audit fields probably do not belong unless a runtime requirement appears.

`Tenant` may implement `ITenantInfo`.

---

# 25. ITenantContext

Introduce a separate operational tenant scope.

Meaning:

> **Which application tenant is this operation currently running against?**

It must not require an authenticated user.

Valid callers include:

- authenticated HTTP requests;
- background jobs;
- administrative operations;
- system processes;
- tests;
- maintenance tooling.

The context cares about tenant scope only.

Possible shape:

```text
ITenantContext
└── Current : ITenantInfo?
```

or equivalent.

Exact API remains open.

---

# 26. IUserInfo vs ITenantContext

These concepts must remain distinct.

```text
IUserInfo.TenantId
= tenant assigned to the current authenticated user

ITenantContext
= tenant this operation is currently scoped to
```

Typical request:

```text
IUserInfo.TenantId
    ↓
initialises
    ↓
ITenantContext
```

But they are not permanently coupled.

Examples where context may differ:

- SystemAdmin operates another tenant;
- background job runs for a chosen tenant;
- infrastructure task runs cross-tenant;
- app explicitly changes tenant scope.

Tenant-aware EF filtering should use `ITenantContext`, not `IUserInfo` directly.

---

# 27. Default Tenant Scope

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

# 28. Tenant Scope Initialisation

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

# 29. DbContext Integration

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

# 30. No Automatic Tenant DbSet Everywhere

`DataDbContext` should not automatically expose a Tenant DbSet.

That would:

- force tenant persistence into apps not using tenancy;
- couple Core to concrete tenancy;
- create surprising migrations;
- blur tenant filtering and tenant ownership.

The owning context should be explicitly configured through `JC.Tenancy`.

Do not implement "Tenant table may not exist, silently no-op" magic.

---

# 31. ITenantStore

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

# 32. Tenant Mutation Rule

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

# 33. Tenant Caching

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

# 34. Tenant Cache Invalidation

`ITenantStore` should invalidate affected cache entries when:

- Tenant added;
- Tenant updated;
- Tenant removed;
- Tenant settings changed.

Subsequent reads refresh the cache.

Direct/out-of-band DB writes are not immediately detected and may remain stale until TTL expiry.

That is acceptable and documented.

---

# 35. Tenant Source Is Local Application Data

`JC.Tenancy` should not be designed around CAP as a tenant source.

Application tenants belong to the application.

CAP has its own organisation tenancy.

A CAP-authenticated application that uses JC.Tenancy still uses a local application `ITenantStore`.

---

# 36. Tenant Scope Switching

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

# 37. Cross-Tenant Access

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

# 38. Query-Level Bypass

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

# 39. Scope-Level Bypass

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

# 40. Configurable Tenant Filtering

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

# 41. Safe Bypass Authorisation

If tenancy mechanics move out of `JC.Identity`, `JC.Tenancy` should ideally not hard-code:

```text
SystemAdmin
```

Potential future mechanisms:

- configurable predicate;
- `ITenantBypassAuthorizer`;
- application policy;
- Identity-supplied default adapter.

This remains to be designed.

The safe API must remain safe.

The unsafe API explicitly bypasses this mechanism.

---

# 42. SystemRoles

`SystemRoles` remain valid in `JC.Identity`.

They define generic high-level roles for an application that owns local Identity.

Examples:

- `SystemAdmin`;
- `Admin`.

They should not become universal Core roles.

Future CAP administrative roles are a separate security domain.

---

# 43. Application Roles vs CAP Roles

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

# 44. BaseUser Remains Tenant-Aware

`BaseUser` should keep tenancy.

The refactor is not removing tenancy capability from local Identity users.

Instead:

- `BaseUser` remains tenant-aware;
- JC.Identity integrates with JC.Tenancy;
- filtering/persistence mechanics move out of Identity;
- tenancy contracts remain reusable elsewhere.

---

# 45. Likely JC.Identity → JC.Tenancy Dependency

Likely direction:

```text
JC.Identity
    ↓
JC.Tenancy
    ↓
JC.Core
```

This is acceptable even if an app uses only the null tenant.

The architecture matters more than avoiding one package dependency.

Exact activation/configuration remains to be designed.

---

# 46. IdentityDataDbContext Refactor

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

# 47. DataDbContext Refactor

`DataDbContext` should be able to participate in tenancy independently from Identity.

Potential work:

- model/configuration hooks;
- tenant-filter installation;
- access to operational tenant context;
- preserve v4 multi-DbContext support;
- no mandatory Tenant table.

---

# 48. Multi-DbContext Compatibility

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

# 49. Background Jobs

Tenant context must work without authenticated HTTP users.

A job should be able to establish:

```text
TenantId = X
```

and then use normal tenant-aware repositories/DbContexts.

No fake `IUserInfo` or fake SystemAdmin should be required merely to scope a job.

Cross-tenant jobs should use explicit bypass APIs.

---

# 50. Authentication and Tenant Scope

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

# 51. Future CAP SSO — Explicitly Out of Scope

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

# 52. Authentication Refresh — Future Verification

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

# 53. CAP Compile-Time Integration Constraint

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

# 54. One Authoritative Tenant Store

Many DbContexts may be tenant-filtered.

Only one DbContext may own the authoritative Tenant store for a given application tenancy model.

Do not support multiple independent Tenant tables for one application tenancy domain.

---

# 55. Persistence / Migration Considerations

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

# 56. Backward Data Safety

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

# 57. Tenant Foreign Keys

Removing concrete Tenant navigation from `IMultiTenancy` changes relationship assumptions.

Potential behaviour:

- no FK if Tenant is in another context;
- optional FK configuration when Tenant is in the same model;
- consuming entities may define their own navigation;
- relationship may be configured from TenantId alone.

Do not force impossible cross-context relationships.

Document migration implications.

---

# 58. Tenant Expiry / Domain / MaxUsers

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

# 59. Tenant Settings Runtime API

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

# 60. Cache Configuration

Potential initial options:

```text
TenantCacheOptions
├── Enabled
└── TimeToLive
```

Keep initial implementation focused.

Distributed caching/invalidation should not be introduced unless a real deployment requires it.

---

# 61. Direct Database Changes

Document clearly:

> Direct EF/database changes to Tenant bypass `ITenantStore` guarantees.

Possible consequence:

- stale tenant info until TTL expiry.

That is acceptable.

Do not increase framework complexity solely to protect callers from intentionally bypassing the abstraction.

---

# 62. Testing Requirements

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

# 63. Documentation Required for v6

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

# 64. Re-Audit Every Current JC.Identity Type

Before implementation, classify every current public/internal type/member:

```text
Stay in JC.Identity
Move to JC.Core
Move to JC.Tenancy
Replace/deprecate
Needs discussion
```

Specifically verify:

- `BaseUser`;
- `BaseRole`;
- `IdentityDataDbContext`;
- current Tenant DbSet;
- current automatic query filters;
- `AllTenants`;
- user-info implementation;
- claims middleware;
- roles;
- seeding;
- options;
- DI extensions;
- 2FA support;
- password-change support;
- account enable/disable handling;
- tenant-aware Identity services;
- docs/examples.

---

# 65. Re-Audit Real Consumers

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

# 66. Likely Breaking Changes

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

# 67. What Should Not Change Without Separate Reason

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

# 68. Roles — Intentionally Open

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

# 69. Future JC.CAP Compatibility Goal

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

# 70. Proposed v6 Architecture

```text
JC.Core
│
├── IUserInfo
├── IApplicationUser
├── IdentityAuthority
├── IMultiTenancy
├── ITenantInfo?       [placement to confirm]
└── ITenantContext?    [placement to confirm]
        │
        ├──────────────────────────┐
        ▼                          ▼
JC.Tenancy                   JC.Identity
│                            │
├── Tenant                   ├── BaseUser
├── TenantSettings           ├── BaseRole
├── ITenantStore             ├── ASP.NET Identity
├── tenant cache             ├── local roles
├── tenant context impl      ├── password/2FA/security
├── EF tenant filters        ├── Identity DbContext integration
├── tenant switching         └── populates IUserInfo
└── safe/unsafe bypass
        │
        └──────── future ────────→ JC.CAP / JC.CAP.Base
```

Conceptual only; final package references must be validated.

---

# 71. Current Decisions

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

---

# 72. Open Questions

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

---

# 73. Suggested Implementation Order

## Phase 1 — Inventory

- classify every JC.Identity type/member;
- identify all tenancy mechanics;
- find current consumers;
- record current EF/schema assumptions.

## Phase 2 — Core contracts

- `IdentityAuthority`;
- `IApplicationUser`;
- refine `IMultiTenancy`;
- decide tenant-contract placement.

## Phase 3 — JC.Tenancy

- create package;
- move Tenant/TenantSettings;
- add ITenantStore;
- add tenant cache;
- add tenant context;
- move filtering mechanics;
- support multiple participating DbContexts;
- support one Tenant-storage owner.

## Phase 4 — JC.Identity adaptation

- BaseUser implements new contracts;
- preserve TenantId storage;
- populate IdentityAuthority.Local;
- remove ownership of tenancy mechanics;
- preserve local Identity behaviour.

## Phase 5 — Consumer migration

- migrate real apps;
- validate null tenancy;
- validate multiple DbContexts;
- validate CAP itself as a JC.Identity consumer.

## Phase 6 — Hardening

- tests;
- migration review;
- docs;
- release notes;
- remove/deprecate old APIs.

---

# 74. Design Principle for v6

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

# 75. Definition of Success

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

# 76. Final Note

This is an **initial architecture/refactor design**, not a locked specification.

Implementation should challenge it.

If current EF behaviour, real consumers, migrations, or code review exposes a cleaner model, update this document.

The important outcome is the boundary:

> **Local Identity, application tenancy, runtime user context, and future CAP identity integration are related concerns — but they are not the same concern and should not live in the same package merely because they currently intersect.**
