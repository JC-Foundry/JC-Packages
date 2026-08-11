# JC Package Extraction Audit

**Purpose:** catalogue generic, reusable functionality currently **hand-rolled inside the UltimateMonopoly (`monappoly.com`) web app** that should be lifted **up into the JC.\* packages** — either as new features of an existing package, a new sub-module of an existing package, or a brand-new package.

This is deliberately the *inverse* of a "package features we aren't using" audit. Every item below is something the app **already implements and proves in production**, so each has a working reference implementation to extract from.

**Scope note:** the app is a companion web app for a physical Monopoly-variant board game (real-time SignalR play, social/friends, DMs, in-app notifications, per-game & lifetime stats, custom board designer, admin area, email, GitHub-synced bug reports, Hangfire jobs, Syncfusion UI, MySQL). It is a heavy, mature consumer of the JC.\* suite (`JC.Core`, `JC.Web`, `JC.Identity`, `JC.Github`, `JC.Communication`, `JC.BackgroundJobs`, `JC.MySql`, `JC.SqlServer.Hangfire`). The items here are the *remaining* generic pieces the app had to build itself.

**Legend — effort:** 🟢 quick · 🟡 medium · 🔴 larger.
**Legend — priority:** ⭐ flagship (highest reuse + cleanest lift).

---

## Contents

- [Decisions taken](#decisions-taken)
- [Summary table](#summary-table)
- [Add to existing packages](#add-to-existing-packages)
  - [JC.Communication.Email](#jccommunicationemail)
  - [JC.Identity](#jcidentity)
  - [JC.Core](#jccore)
  - [JC.Web](#jcweb)
  - [JC.Github](#jcgithub)
  - [JC.BackgroundJobs](#jcbackgroundjobs)
- [New packages](#new-packages)
  - [JC.Caching](#jccaching-new)
  - [JC.SignalR](#jcsignalr-new--proposed)
  - [JC.Content.Moderation](#jccontentmoderation-new--optional)
- [Appendix: source-file index](#appendix-source-file-index)

---

## Decisions taken

Recorded from review discussion so the intent is captured with the catalogue:

| Item | Decision |
|------|----------|
| **JC.Caching** | ✅ Approved as a **new standalone package**. |
| **JC.Web.Seo** | ✅ Roll into **JC.Web as a sub-module** (a peer of `JC.Web.Cookies` / `JC.Web.Headers` / `JC.Web.UI`), **not** an independent package. |
| **JC.SignalR** | 🤔 Worth doing, but **scope tightly to the generic ~20%** (presence + base hub + notifier). See section for the full analysis. Not urgent unless a second real-time app is on the horizon. |
| **JC.Content.Moderation** | Optional / new — no existing home today. |

---

## Summary table

| # | What the app built | Where | Target package | Effort | Priority |
|---|--------------------|-------|----------------|--------|----------|
| 1 | `EmailBuilder` — fluent plain+HTML body builder | `Helpers/Email/EmailBuilder.cs` | JC.Communication.Email | 🟡 | ⭐ |
| 2 | `AccountEmail` — confirm/reset/change templates | `Helpers/Email/AccountEmail.cs` | JC.Communication.Email | 🟢 | |
| 3 | Live session/claim refresh middleware | `Areas/Admin/Middleware/AuthRefresh*.cs` | JC.Identity | 🟡 | |
| 4 | Redirect-authenticated-away-from-auth-pages filter | `Authorization/RedirectAuthenticatedFilter.cs` | JC.Identity | 🟢 | |
| 5 | Audit-trail **viewer** (paged/searchable/filtered) | `Areas/Admin/Services/AuditTrailService.cs` | JC.Core | 🔴 | ⭐ |
| 6 | Intentional admin **action log** | `Areas/Admin/Services/AdminLogService.cs` | JC.Core | 🟡 | |
| 7 | `JoinCodeGenerator` — short unique code | `Helpers/JoinCodeGenerator.cs` | JC.Core | 🟢 | |
| 8 | `FileSizeFormatter` | `Helpers/FileSizeFormatter.cs` | JC.Core | 🟢 | |
| 9 | **SEO** — sitemap/robots/meta/OG/JSON-LD | `Program.cs`, `Pages/Shared/_Layout.cshtml` | JC.Web (new `JC.Web.Seo`) | 🔴 | ⭐ |
| 10 | `<avatar>` initials/colour badge | `TagHelpers/ProfileCircleTagHelper.cs` | JC.Web.UI | 🟢 | |
| 11 | Screenshot capture in `<bug-reporter>` | JC.Web tag helper + app | JC.Web + JC.Github | 🟡 | |
| 12 | Bug-report screenshot storage + failed-sync retry | `Pages/BugReport.cshtml.cs` | JC.Github | 🟡 | |
| 13 | Memory-cache get-or-create + invalidate pattern | `Services/Cache/*.cs` | **JC.Caching** (new) | 🟡 | ⭐ |
| 14 | Presence tracking + base hub + notifier | `Services/Friends/Presence*.cs`, `Hubs/*` | **JC.SignalR** (new) | 🔴 | |
| 15 | Profanity/blocked-words moderation | `Services/ProfanityService.cs` + helpers | **JC.Content.Moderation** (new) | 🟡 | |

---

## Add to existing packages

### JC.Communication.Email

#### 1. `EmailBuilder` → email body builder ⭐ 🟡

**What the app built.** A fluent builder that produces a **plain-text body and an HTML body from one set of section calls**, so the two never drift. The HTML is wrapped in a branded, email-client-safe shell (table-based layout, inline hex styles, gradient header bar carrying the app/game name + a per-email caption). All caller text is HTML-encoded inside the builder, so call-sites pass raw strings and cannot leak unescaped user input.

Section API today: `Paragraph(text, emphasis)`, `Quote(text)`, `Button(text, url)` (gradient CTA + plain-text fallback link), `Divider()`, `SignOff(text)`, `Reference(code)`, `Footer(text)`, terminating in `Build() → (string Plain, string Html)`.

**Source:** `UltimateMonopoly/Helpers/Email/EmailBuilder.cs`.

**Why it belongs in the package.** JC.Communication.Email today only accepts **raw** `plainBody` / `htmlBody` strings (`IEmailService.SendAsync(...)`, `EmailMessage`). It has **no answer to "how do I actually produce a nice, consistent HTML+plain body?"** — every consuming app will re-solve this, badly, with string concatenation (as this app originally did before the builder). This is the missing composition layer directly above the send API.

**Proposed package shape.**
- `EmailBuilder` (or `EmailBodyBuilder`) in `JC.Communication.Email` returning `(string Plain, string Html)`, feeding straight into `EmailMessage` / `SendAsync`.
- **Themeable shell**: brand colours, header text/caption, and footer should come from `EmailOptions` (e.g. `EmailOptions.Branding`) or a per-build override, rather than the hard-coded hex in the app's copy.
- Keep the "encode everything, sections stay in lockstep" guarantees — that's the core value.
- Consider an `EmailMessage.FromBuilder(EmailBuilder)` convenience.

**Migration.** The app's `IssueContactService` and `Guides/Index` contact form already consume this builder; they'd switch to the package type with the branding sourced from config.

---

#### 2. `AccountEmail` → default Identity email templates 🟢

**What the app built.** Ready-made `(Plain, Html)` bodies for the ASP.NET Identity account flows: `ConfirmAccount(callbackUrl)`, `ResetPassword(callbackUrl)`, `ConfirmEmailChange(callbackUrl)` — each composed via `EmailBuilder` with a CTA button and a footer.

**Source:** `UltimateMonopoly/Helpers/Email/AccountEmail.cs` (consumed by `Areas/Identity/Pages/Account/{Register,ForgotPassword,ResendEmailConfirmation,ExternalLogin}.cshtml.cs` and `Manage/Index.cshtml.cs`).

**Why it belongs in the package.** JC.Communication.Email is already the email layer paired with JC.Identity flows. Shipping **themeable default templates** for confirm/reset/change-email means downstream apps get branded account emails for free instead of hand-writing the `"click here"` anchors the ASP.NET scaffold ships with.

**Proposed package shape.** A small `AccountEmails` helper (or extension on the email service) in JC.Communication.Email — or, if there's a tighter coupling, in a JC.Identity ⇄ JC.Communication bridge — that renders these three (plus 2FA-related) bodies from the shared branding used by `EmailBuilder`.

---

### JC.Identity

#### 3. Live session / claim refresh on server-side user change 🟡

**What the app built.** `AuthRefreshService` + `AuthRefreshMiddleware` propagate **admin-driven role / enable-disable / account changes to the affected user's own live session** — i.e. when an admin changes a user, that user's next request re-issues their auth cookie (`RefreshSignInAsync`) so their claims/roles update without a manual re-login. Registered right after `UseIdentity()` so `IUserInfo` is populated (`Program.cs`).

**Source:** `UltimateMonopoly/Areas/Admin/Middleware/AuthRefreshService.cs`, `AuthRefreshMiddleware.cs`.

**Why it belongs in the package.** The JC.Identity guide explicitly calls out "refresh after property change" as a nuance the consumer must handle — but ships nothing for it. This app built the middleware to close that gap. It's a universal Identity concern (any app with an admin that mutates users needs it).

**Proposed package shape.** A `UseIdentityLiveRefresh()` (or an option on `UseIdentity()`) + a service that flags a user id as "needs refresh" (e.g. a versioned security-stamp / small store) which the middleware consumes on that user's next request. Slots naturally into the existing `UseIdentity()` pipeline.

---

#### 4. Redirect-authenticated-away-from-auth-pages filter 🟢

**What the app built.** `RedirectAuthenticatedFilter` bounces already-signed-in users off `/Identity/Account` login/register flows to account management, **with a skip-list** for pages a signed-in user legitimately uses (`Manage/*`, `Logout`, `ConfirmEmail`, `ConfirmEmailChange`, `AccessDenied`, `Disabled`). Registered via a page convention in `Program.cs`.

**Source:** `UltimateMonopoly/Authorization/RedirectAuthenticatedFilter.cs`.

**Why it belongs in the package.** Near-universal Identity UX. The skip-list is the fiddly part and is entirely generic (the excluded routes are ASP.NET Identity's own).

**Proposed package shape.** Ship the filter + a one-liner registration convention in JC.Identity, with the skip-list configurable.

---

### JC.Core

#### 5. Audit-trail **viewer** ⭐ 🔴

**What the app built.** A full read UI over JC.Core's audit data: `AuditTrailService` / `AuditDashboardService` + `AuditEntryViewModel` + admin Razor pages — paged, searchable, filterable by action (`AuditAction`) and by table/entity, with `ActionData` diff rendering.

**Source:** `UltimateMonopoly/Areas/Admin/Services/AuditTrailService.cs` (+ `AuditDashboardService.cs`, `Areas/Admin/.../Audit` pages).

**Why it belongs in the package.** JC.Core generates and persists the audit trail (`AuditEntry`, `AuditEntries` DbSet) but ships **no way to read it** — a fact the app's own code comments note. Every JC.Core app with an admin surface will re-build this viewer (this app effectively built it three times across audit / admin-log / issue viewers). Clearest "built exactly what the package should have provided."

**Proposed package shape.**
- A `JC.Core` (or `JC.Core.Admin` / `JC.Web.UI`-adjacent) **audit-viewer service**: paged query over `AuditEntry` with filters (action, table, entity key, user, date range) returning ready-to-render view models, including `ActionData` from/to diff parsing.
- Optionally a **Razor Class Library** with drop-in pages/partials so an app gets `/admin/audit` for free.
- Belongs logically with the audit data it reads; if UI is split out, keep the query service in JC.Core and the Razor in a `JC.Core.Web`/`JC.Web.UI` component.

---

#### 6. Intentional admin **action log** 🟡

**What the app built.** `AdminActionLog` (a `LogModel`) + `AdminLogService` — ~30 templated `LogXxx(...)` methods that record deliberate admin actions (contacted reporter, enabled/disabled user, deleted game, etc.) with a human-readable `Detail`, plus the paged/searchable viewer.

**Source:** `UltimateMonopoly/Areas/Admin/Services/AdminLogService.cs` (entity `Areas/Admin/Models/AdminActionLog.cs`).

**Why it belongs in the package.** This is distinct from JC.Core's *entity-diff* audit trail: it's a curated "**who did what deliberate action, and why**" log — an operator concern every admin app needs, and one JC.Core doesn't offer.

**Proposed package shape.** A generic `IActionLog.Record(action, targetType, targetId, detail)` backed by a `LogModel` entity, with a templated-detail helper and (reusing #5) the same viewer. Action names would be an app-supplied enum/const set.

---

#### 7. `JoinCodeGenerator` → short unique-code helper 🟢

**What the app built.** Generates short, human-friendly, collision-checked join codes (e.g. `MP4K7Q2`).

**Source:** `UltimateMonopoly/Helpers/JoinCodeGenerator.cs`.

**Why it belongs in the package.** A short-code / invite-code generator (configurable alphabet, length, ambiguity-avoidance, uniqueness callback) is generic infra that sits naturally alongside JC.Core's existing string helpers (`ToSlug`, `Mask`, `Truncate`).

---

#### 8. `FileSizeFormatter` 🟢

**What the app built.** Human-readable byte-size formatting (KB/MB/…).

**Source:** `UltimateMonopoly/Helpers/FileSizeFormatter.cs`.

**Why it belongs in the package.** Trivial but universal; belongs with JC.Core's format helpers (`ToRelativeTime`, `ToFriendlyDate`, etc.). Low value on its own — bundle it, don't package it alone.

---

### JC.Web

#### 9. SEO → new `JC.Web.Seo` sub-module ⭐ 🔴

**Decision:** roll into **JC.Web** as a sub-module (peer of `JC.Web.Cookies` / `JC.Web.Headers` / `JC.Web.UI`), not a standalone package.

**What the app built (all hand-rolled, in two awkward spots).**
1. `/sitemap.xml` — an inline minimal-API `MapGet` in `Program.cs` that string-builds a `<urlset>` from a hardcoded path list (`["/", "/Rules", "/Guides"]`), `AllowAnonymous`, absolute URLs from the request host.
2. `Pages/Shared/_Layout.cshtml` — canonical `<link>`, `<meta name="description">`, the Open Graph set, and a hand-written `application/ld+json` `WebApplication` block (with fiddly `@@context` / `@@type` Razor escaping). Pages feed intent via `ViewData["Canonical"]` / `["Description"]` (`Pages/Rules.cshtml`, `Pages/Guides/Index.cshtml`), and canonical/publisher base URLs come from config (`Routes:WebUrl`, `Routes:PublisherUrl`).
3. There is still **no `robots.txt`**.

**Source:** `UltimateMonopoly/Program.cs` (sitemap endpoint), `UltimateMonopoly/Pages/Shared/_Layout.cshtml` (meta/OG/JSON-LD), `Pages/Rules.cshtml`, `Pages/Guides/Index.cshtml`.

**Why it belongs in JC.Web.** SEO is exactly the cross-cutting concern JC.Web already centralises for security headers and UI. It fits JC.Web's three established patterns: **options-based registration**, **tag helpers**, and **fluent builders**.

**Proposed `JC.Web.Seo` shape.**

| Concern | API shape | Replaces in-app |
|---------|-----------|-----------------|
| Sitemap | `AddSeo(o => { o.CanonicalHost = "https://www.monappoly.com"; })` + `MapSitemap()` endpoint + an `ISitemapSource` DI contract so pages/areas contribute their own URLs (with `lastmod` / `changefreq` / `priority`); host from request or configured canonical host; `AllowAnonymous` baked in | the `Program.cs` sitemap endpoint + hardcoded array |
| robots.txt | `MapRobots()` auto-pointing a `Sitemap:` line at the sitemap route; `Disallow: /` in non-prod environments | the current gap (no robots) |
| Meta / OG / canonical | a `<seo-meta>` (or `<meta-tags>`) tag helper reading canonical / description / title / OG from typed properties or a `ViewData` convention, emitting `<link rel="canonical">`, `<meta name="description">`, and the OG set (optional `og:image` / Twitter card) — mirrors the existing `<alert>` / `<bug-reporter>` tag helpers | the `_Layout.cshtml` meta block |
| Structured data | typed JSON-LD builders (`JsonLd.WebApplication(...)`, `.Organization(...)`, `.BreadcrumbList(...)`) or a `<json-ld>` tag helper that serialises a valid `application/ld+json` script | the hand-escaped `@@context` block |

**Net effect.** Pages keep supplying *intent* (canonical/description/structured-data values); JC.Web.Seo owns all rendering, HTML/JSON escaping, host resolution, and endpoint wiring — exactly how `AddWebDefaults` / `UseWebDefaults` already own security headers. JC.Web already bundles QRCoder/UAParser, so an SEO area sits there naturally.

---

#### 10. `<avatar>` initials/colour badge → JC.Web.UI 🟢

**What the app built.** `ProfileCircleTagHelper` renders an avatar circle with a user-chosen background colour and **automatic contrast** for the text/initials via `ColourHelper.FontColour`.

**Source:** `UltimateMonopoly/TagHelpers/ProfileCircleTagHelper.cs`.

**Why it belongs in the package.** The initials-badge-with-auto-contrast pattern is fully generic UI and already depends only on `JC.Core.ColourHelper`. Fits `JC.Web.UI`'s existing tag-helper family (`<alert>`, `<pagination>`, `<breadcrumb>`).

**Proposed shape.** A `<avatar>` / `<initials-badge>` tag helper in JC.Web.UI (text/initials, background colour, size, optional image src) with the auto-contrast built in. The app's game-piece/avatar specifics stay in the app.

---

#### 11 & 12. Bug-reporter screenshot capture + storage/retry → JC.Web + JC.Github 🟡

**What the app built / what's missing.** JC.Web ships the `<bug-reporter>` tag helper and JC.Github ships `ReportedIssue` (which already has an unused `Image` byte[] field). The app wires them together (`Pages/BugReport.cshtml.cs`) but **never captures or stores a screenshot**, and although the admin dashboard **counts** `ReportSent == false` sync failures, nothing re-pushes them.

**Source:** `UltimateMonopoly/Pages/BugReport.cshtml.cs`; the tag helper lives in JC.Web; `ReportedIssue.Image` in JC.Github.

**Proposed shape.**
- **JC.Web:** add optional **screenshot capture** (e.g. html2canvas) to the `<bug-reporter>` tag helper, POSTing a base64 image alongside the existing metadata payload.
- **JC.Github:** have `BugReportService.RecordIssue(...)` accept/persist that image into `ReportedIssue.Image` and expose it in the issue view model; add a **failed-sync retry** (background job or admin action) that re-pushes `ReportSent == false` records and back-fills `ExternalId` / `ReportSent`.
- Also generic: the app's **"View Issue in App" deep-link append + `StripReportLink`** convention (`Pages/BugReport.cshtml.cs`) could be a JC.Github option.

Rationale: a board-game/UI bug is far easier to triage from a screenshot than from metadata JSON, and local-only reports that failed to reach GitHub currently vanish silently.

---

### JC.BackgroundJobs

No net-new extraction — but the app's generic **`PurgeDeleted<T>` retention pattern** (per-entity soft-delete sweeps in the game/snapshot cleanup jobs) is evidence that JC.Core's `SoftDeleteCleanupJob` needs a **per-entity retention policy / blacklist** so apps don't hand-roll bespoke purge jobs. Track that as a `SoftDeleteCleanupJob` enhancement rather than a new extraction. (Separately, wiring `ExecutionTimeout` + `[AutomaticRetry]` on the app's own purge jobs is an app-side fix, not a package change.)

---

## New packages

### JC.Caching (new) ⭐ 🟡

**Decision:** ✅ approved as a standalone package.

**What the app built.** Five services repeat the same shape: `IMemoryCache.GetOrCreateAsync` + a keyed cache string + an expiry/`Priority.NeverRemove` policy + a role-gated `Invalidate`. The only real variations are **per-user key suffixing** (`$"{CacheKey}__{userId}"`) and a role check inside `Invalidate`.

**Source:**
- `UltimateMonopoly/Services/Cache/BoardCacheService.cs`
- `UltimateMonopoly/Services/Cache/CardCacheService.cs`
- `UltimateMonopoly/Services/Cache/BlockedWordsCacheService.cs`
- `UltimateMonopoly/Services/Cache/GameCacheService.cs`
- `UltimateMonopoly/Services/Cache/PlayerCacheService.cs`

**Proposed package shape.**
- A `CachedSet<T>` / `GetOrCreate<T>(key, loader, policy)` abstraction over `IMemoryCache` (and pluggable for `IDistributedCache` later).
- First-class **scoped keys** (per-user / per-tenant suffixing) and **scoped invalidation** (invalidate one user's entry, or admin-gated flush-all).
- Standard policy presets (never-remove seed data vs. sliding/absolute expiry).

**Why standalone (not under JC.Web/JC.Core).** Caching is layer-agnostic infra used by services, not tied to web or data specifically — a standalone `JC.Caching` keeps it dependency-light and reusable everywhere.

---

### JC.SignalR (new — proposed) 🔴

**Verdict:** worth doing, but **scope tightly to the generic ~20%**; most of the app's real-time code is game-specific and stays put. Not urgent unless a second real-time app appears.

**Generic, high-value parts to extract:**
- **Presence tracking (the flagship).** Multi-connection **reference counting** per user, last-active tracking, dirty/missed-flush bookkeeping, and batched drain-to-DB via a Hangfire job. This is the hard, universal part everyone re-solves badly.
  - Source: `UltimateMonopoly/Services/Friends/PresenceService.cs`, `PresenceFlushJob.cs`, `Hubs/PresenceHub.cs`.
- **A base hub** — connection lifecycle + group-name conventions.
  - Source: `UltimateMonopoly/Hubs/GameBaseHub.cs`.
- **A typed notifier abstraction** — "push a typed message to a group/user **from outside the hub**" (broadcast off the game pump).
  - Source: `UltimateMonopoly/Services/GameEngine/SignalrEngineNotifier.cs` (generalise the `IEngineNotifier` idea).

**Leave in the app:** anything touching the game engine/turn pump, and the `GamePlayHub` / `GameSetupHub` / `MessagingHub` payloads.

**Important overlap.** JC.Communication.Web already renders chat/notification UI over SignalR and would benefit from presence. Make **JC.SignalR the low-level primitive** (presence registry, base hub, group/notifier helpers) and let **JC.Communication.Web depend on it**, rather than duplicating connection tracking in two packages. (Alternatively, presence could live as `JC.Web.Presence` — but a dedicated real-time package reads cleaner given the base-hub + notifier pieces.)

---

### JC.Content.Moderation (new — optional) 🟡

**What the app built.** A complete, app-agnostic content-safety stack:
- `ProfanityService` — orchestrates a detection library + a DB-backed extra-terms list.
- `ProfanityNormaliser` — canonicaliser handling leetspeak, diacritics, and repeated-run collapsing (the part that actually makes profanity filtering work).
- `BlockedWordsCacheService` — caches the term list.
- `BlockedWordImportService` — seeds the list from a plain-text file at startup.
- `BlockedWord` entity.

**Source:** `UltimateMonopoly/Services/ProfanityService.cs`, `UltimateMonopoly/Helpers/ProfanityNormaliser.cs`, `UltimateMonopoly/Services/Cache/BlockedWordsCacheService.cs`, `UltimateMonopoly/Services/Imports/BlockedWordImportService.cs`.

**Why a new package.** Nothing here is Monopoly-specific — it's a general "is this display name / message clean?" module. It's only "new" because there's no existing JC package that fits (it isn't web, identity, or comms). Would also let JC.Communication (messaging) and JC.Identity (display names) call into it. Note: if `JC.Caching` lands, this module's cache layer should be built on it.

---

## Appendix: source-file index

Confirmed paths (relative to repo root `UltimateMonopoly/`) for the reference implementations:

| Item | File(s) |
|------|---------|
| EmailBuilder | `Helpers/Email/EmailBuilder.cs` |
| AccountEmail | `Helpers/Email/AccountEmail.cs` |
| Auth refresh | `Areas/Admin/Middleware/AuthRefreshService.cs`, `Areas/Admin/Middleware/AuthRefreshMiddleware.cs` |
| Redirect-authenticated filter | `Authorization/RedirectAuthenticatedFilter.cs` |
| Audit viewer | `Areas/Admin/Services/AuditTrailService.cs` (+ `AuditDashboardService.cs`) |
| Admin action log | `Areas/Admin/Services/AdminLogService.cs` (entity `Areas/Admin/Models/AdminActionLog.cs`) |
| Join-code generator | `Helpers/JoinCodeGenerator.cs` |
| File-size formatter | `Helpers/FileSizeFormatter.cs` |
| SEO (sitemap) | `Program.cs` (`MapGet("/sitemap.xml", …)`) |
| SEO (meta/OG/JSON-LD) | `Pages/Shared/_Layout.cshtml`; page intent in `Pages/Rules.cshtml`, `Pages/Guides/Index.cshtml` |
| Avatar badge | `TagHelpers/ProfileCircleTagHelper.cs` |
| Bug reporter wiring | `Pages/BugReport.cshtml.cs` |
| Caching services | `Services/Cache/{Board,Card,BlockedWords,Game,Player}CacheService.cs` |
| Presence / hubs | `Services/Friends/PresenceService.cs`, `Services/Friends/PresenceFlushJob.cs`, `Hubs/{PresenceHub,GameBaseHub,GamePlayHub,GameSetupHub,MessagingHub}.cs`, `Services/GameEngine/SignalrEngineNotifier.cs` |
| Moderation | `Services/ProfanityService.cs`, `Helpers/ProfanityNormaliser.cs`, `Services/Cache/BlockedWordsCacheService.cs`, `Services/Imports/BlockedWordImportService.cs` |

---

*Generated from a multi-agent audit of the codebase against the JC.\* package docs (`docs/pckg-docs/`). Every item has a working in-repo reference implementation, so extraction is a lift-and-generalise, not a green-field build.*