# JC.Admin — Design Considerations

> **Status:** Exploratory / not yet built. Captured from a design discussion while building the *Portfolio* (JC Foundry) app.
> **Purpose:** Record the problem, the constraints discovered, and a proposed shape for a future `JC.Admin` package plus a centralised JC Foundry admin application.

---

## 1. The problem

JC Foundry apps fall into two camps:

- **Apps that already need JC.Identity to function** (e.g. *Monappoly* — users must log in to play, large cross-cutting schema, heavy audit / JC.Communication / JC.Github use). These bolt an admin area **in-app**, because Identity is already present and the domain justifies it.
- **Lean public sites with no user accounts of their own** (personal portfolio, client brochure/marketing sites). These should **not** carry JC.Identity — scaffolding and hardening Microsoft ASP.NET Identity into every site just to read a few log tables is wasteful and violates DRY.

Lean sites still need *some* gated surface, though:

- The **Hangfire dashboard** (`/hangfire`) from JC.BackgroundJobs must be protected.
- Admin-only reads such as **email logs** (JC.Communication stores these in the app's own DB).
- Potentially light content management later.

The requirement therefore is: **gate a small number of admin surfaces on a lean site, driven by a single central identity, without adding full Identity to each site.** The gate must be **role-independent** as a mechanism.

## 2. Near-term guidance (do not over-build)

For the *Portfolio* app right now, the only surface that genuinely needs gating is `/hangfire`. Given the operator already has **private network access** to the server:

- Hangfire's dashboard defaults to **local-requests-only** and exposes a single `IDashboardAuthorizationFilter` hook.
- The cheapest **correct** solution today is **network-level restriction**: bind the dashboard to the admin host and/or an IP allowlist, and reach it over the private network.

For a purely operational surface like the Hangfire dashboard, network-level isolation is a legitimate and common pattern in its own right — it does not require JC.Admin to exist. Reserve the package for when there is a genuine need to reach admin-gated pages from an ordinary browser over the public internet; a dashboard alone does not justify it.

## 3. Key realisation: most of this is already built in

What was originally described as a "custom auth token system" is, for the most part, **ASP.NET Core cookie authentication** (`AddAuthentication().AddCookie()`):

- Built-in, **zero extra dependencies**.
- Issues a **Data-Protection-encrypted cookie** carrying a `ClaimsPrincipal`.
- No users, no roles, no registration, no external logins — exactly the "mirror the Identity auth cookie but nothing else" behaviour desired.

The **only** thing vanilla cookie auth does not provide is **server-side revocation** — a self-contained auth cookie stays valid until it expires, even if you want to kill it early.

**Conclusion:** cookie authentication for the *mechanism*, plus a **server-side token record (DB/cache) purely for expiry + revocation control**. That is a far smaller and safer thing to own than a bespoke auth system.

## 4. Constraints that shape the design

### 4.1 Cross-domain cookies (the big one)

A cookie scoped to `.jcfoundry.co.uk` flows to `www.`, `home.`, `admin.` subdomains — fine for the JC Foundry umbrella. It will **never** reach a client app hosted on its **own** domain (e.g. a restaurant on `restaurant.com`).

This forces an early scope decision for JC.Admin:

| Intended reach | Viable mechanism |
|---|---|
| **JC Foundry umbrella only** (`*.jcfoundry.co.uk`) | Shared cookie is fine |
| **Any hosted app, including client-owned domains** | Cookie sharing is dead — requires a **redirect flow** (app → `admin.jcfoundry.co.uk` to authenticate → redirect back with a token). This is effectively **OIDC-lite / a mini identity provider**. |

This single choice determines how big the package is. Decide it before writing code.

### 4.2 Data Protection key sharing

For app B to **decrypt** a cookie that app A issued, both must share the **Data Protection key ring** (shared key storage + a common `SetApplicationName`).

The **opaque-token** approach sidesteps this entirely: if the cookie carries only an **opaque random token** that B validates **server-side** against the shared store, B never decrypts anything meaningful. The store is the source of truth; cookie encryption becomes defence-in-depth rather than the trust mechanism.

### 4.3 Authentication vs authorisation

- **Authentication** (who) stays role-independent — a valid session is a valid session.
- **Authorisation** (what it may reach) lives in the **token record** as a **scope** (which apps / areas this session may touch), checked server-side.

This keeps the protocol role-free (as required) while still allowing, later, a client owner to log in and reach **only** their own site — without bolting roles onto the token mechanism.

## 5. Proposed shape of JC.Admin

If/when built, hold the design to:

- **Opaque, cryptographically-random token (≥ 256-bit), stored *hashed* at rest** — treat it like an API key so a DB leak does not hand over live sessions (mirrors how Identity stores security stamps).
- **Validate via an introspection endpoint on the admin app, not a shared DB connection** — an HTTP "is this token valid, and what is its scope?" call, **cached with a short TTL**. This decouples database topology and is the *only* option that also works for client apps on their own domains.
- **Server-side revocation + sliding expiry** — the reason the token record exists at all.
- **Standard hygiene:** `HttpOnly` / `Secure` / `SameSite` cookie flags, anti-forgery on state-changing POSTs, rotation on privilege change.
- **First consumer:** ship an `IDashboardAuthorizationFilter` implementation so Hangfire's dashboard is gated out of the box by validating the JC.Admin session.

### The hard line

Keep JC.Admin to **authentication + revocable sessions (+ coarse scope)** only. The day real **users, roles, or registration** are needed, that is **JC.Identity** — do not let JC.Admin grow into a second identity system.

## 6. Brief outline — centralised JC Foundry admin app

A single ASP.NET app that administers **all** JC Foundry apps which deliberately do **not** carry their own Identity/admin.

- **Holds the one hardened Identity** (or the JC.Admin session issuer) — authentication and hardening are done **once** and reused across every site.
- **One area + DbContext (+ scope/roles) per administered app.** Example: a restaurant wants a public brochure site — that site stays lean and admin-free; a new area is added to the admin app to manage it.
- **Reads administered apps' data with no schema duplication.** Log entities such as `EmailLog` are defined in the **JC.Communication package**, not per app — so the admin app reads them by pointing an `IEmailDbContext` DbContext at the target app's database. Each app remains the **sole owner of its own migrations**.
- **Issues / validates JC.Admin sessions** for the gated surfaces on those lean apps (log views, light content management), via the introspection endpoint described above. Operational background jobs and the Hangfire dashboard live in the admin app itself (see §7), **not** on the lean apps.
- **Scope-aware:** the admin operator sees everything; a client owner (future) sees only the area(s) scoped to their app.

## 7. Background jobs — centralise in the admin app

Rather than each lean app hosting **JC.BackgroundJobs** + a Hangfire storage provider just to run cleanup jobs (audit, email-log), the admin app should host **one** Hangfire instance and run the cleanup jobs for **all** administered apps.

Benefits:

- Lean apps drop **JC.BackgroundJobs** and **JC.SqlServer.Hangfire** entirely.
- A **single Hangfire dashboard** to gate — in the app that already has auth. This removes the per-app dashboard-gating problem that motivated much of this document.
- All recurring jobs are visible and manageable in one place.

### Blocker — the cleanup jobs are single-context today

`AddCore<TContext>` registers exactly **one** ambient `IDataDbContext` (via `TryAdd`, first-registration-wins), and `IRepositoryManager` / `IRepositoryContext<T>` all resolve against it. `IRepositoryContext<T>` is keyed **only by entity type `T`**, not by context, so two apps' contexts cannot coexist behind the repository layer. The built-in `AuditCleanupJob` (JC.Core) and `EmailLogCleanupJob` (JC.Communication) resolve their repositories through that single ambient manager — so **as written they can only ever clean one database**, whichever context won registration.

### Required enhancement — make cleanup jobs multi-context

**JC.Core / JC.BackgroundJobs / JC.Communication** need the cleanup jobs to become **context-typed**, so a single host can run them per-app:

- Generic jobs over the DbContext, e.g. `AuditCleanupJob<TContext>` / `EmailLogCleanupJob<TContext>`, resolving a **specific** context (e.g. via `IDbContextFactory<TContext>`) rather than the ambient `IRepositoryManager`.
- Or a context-scoped repository-manager factory, so a job can be registered bound to a named/typed context.
- Registration in the admin app would then be one per administered context, e.g. `AddHangfireJob<EmailLogCleanupJob<PortfolioDbContext>>(...)`. The admin app references each app's **shared data library** (e.g. `Portfolio.Data`) to obtain its `DbContext` and points it at that app's database.
- Group / identify jobs per app via Hangfire `JobId` prefix + `Queue` (e.g. `portfolio:email-log-cleanup`). Note: the **OSS** dashboard has no native folder/tag grouping (that is Hangfire Pro) — naming conventions + per-app queues are the pragmatic substitute.

Until this enhancement exists, lean apps either keep jobs in-app (rejected — defeats the purpose) or **defer cleanup entirely**. For low-traffic sites this is acceptable: `AuditEntries` and email logs will not grow problematically in the near term.

### Decision log / open questions

- [ ] **Reach of JC.Admin:** umbrella-only (cookie) vs client domains too (redirect / introspection). *Determines package size.*
- [ ] **Token store location:** central admin DB queried via introspection endpoint (preferred) vs per-app replication.
- [ ] **Whether JC.Admin is even needed yet** — operational surfaces can be handled by network-level isolation, which may cover the near-term need without the package.
- [ ] **Make cleanup jobs multi-context** — JC.Core `AuditCleanupJob` and JC.Communication `EmailLogCleanupJob` (and the JC.BackgroundJobs registration surface) must be context-typed before the admin app can run them per administered DbContext. *Blocks centralising background jobs.*
- [ ] **Confirm Hangfire grouping approach** — `JobId` prefix + per-app queue is sufficient without Hangfire Pro tagging.