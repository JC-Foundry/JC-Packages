# JC.Web Section & Extension Plan

## Purpose

This document defines the planned internal structure for `JC.Web`, with a focus on keeping the project organised as a single package while avoiding feature sprawl.

The goal is:

- keep `JC.Web` web-focused
- group related functionality into clear internal sections
- expose granular registration/extensions for precise control
- also expose curated defaults for convenience
- ensure all defaults remain composable, predictable, and overridable

This is **not** intended to turn `JC.Web` into a second framework over ASP.NET Core. The package should stay close to the platform and provide:

- safer defaults
- reduced boilerplate
- focused helpers
- practical wrappers where they add real value

---

## High-Level Internal Sections

Planned internal areas within `JC.Web`:

1. **Security**
2. **Observability**
3. **RateLimiting**
4. **UI**

These are effectively “mini projects inside the project” rather than separate repos or packages.

This keeps the repository manageable while still allowing strong internal structure.

---

## Section 1: Security

### Scope

The `Security` area should cover **web hardening**, not authentication or business authorization.

It should remain focused on HTTP/browser/request concerns.

### Planned Areas

- Security Headers
- Cookies
- Possibly CORS helpers/wrappers
- Possibly Antiforgery helpers
- Potential future additions:
  - safe redirect helpers
  - upload validation helpers
  - forwarded-header / proxy safety helpers

### What belongs here

- response/browser security headers
- cookie creation/reading/deletion/protection helpers
- cookie security defaults
- antiforgery convenience setup/helpers
- thin wrappers over ASP.NET Core CORS where useful
- web request hardening helpers

### What does **not** belong here

- auth workflows
- ASP.NET Identity abstractions
- role/claim systems
- token issuing
- business authorization rules
- a general cryptography framework

### Current direction

The current Security Headers implementation is already a solid v1 slice and belongs here.

### Notes on Cookies

Cookies are currently expected to live under `Security`, but may later be promoted into their own top-level internal section **if** they grow beyond:

- simple helpers
- cookie generation/reading
- security defaults
- Data Protection integration

If the cookie system becomes significantly larger (multiple services, policies, typed storage, lifecycle handling, consent abstractions, etc.), then extracting it into its own section would be reasonable.

---

## Section 2: Observability

### Scope

The `Observability` area should remain **lightweight and request-focused**.

This is **not** intended to become analytics software, tracking infrastructure, or a telemetry platform.

### Planned Areas

- user agent helpers
- IP extraction helpers
- forwarded header helpers
- optional geolocation hooks/enrichment points
- consent-aware request metadata helpers where useful

### What belongs here

- extracting request/client metadata
- parsing or classifying user agents
- resolving real client IPs behind proxies / forwarded headers
- optional pluggable geolocation hooks
- helper services around request/client context

### What does **not** belong here

- analytics storage
- event tracking platforms
- user behaviour analytics
- persistent telemetry systems
- invasive tracking code

### Design intent

This section should provide **request/client context tooling**, not data collection software.

A tighter alternative name could be `RequestMetadata`, but `Observability` is acceptable if the scope stays controlled.

---

## Section 3: RateLimiting

### Scope

The `RateLimiting` area should provide application-level abuse/throttling controls.

It is **not** intended to be marketed as full DDoS protection.

### Planned Areas

- wrappers for ASP.NET Core rate limiting
- middleware helpers
- named policy helpers
- possibly per-IP / per-user / per-endpoint helpers
- retry/rejection helper behaviour where appropriate

### What belongs here

- throttling policies
- request quota helpers
- burst handling
- middleware registration helpers
- safe wrappers around built-in ASP.NET Core rate limiting

### What does **not** belong here

- WAF-style systems
- bot scoring engines
- edge mitigation products
- “full DDoS protection” claims

### Design intent

This section is intended to complement edge protection (e.g. Cloudflare) by handling:

- endpoint abuse
- noisy clients
- accidental or deliberate hammering
- application-level traffic smoothing

---

## Section 4: UI

### Scope

The `UI` area contains reusable web UX helpers/components.

### Existing / Planned Areas

- tag helpers
- UI helpers
- QR generation
- component builders
- reusable web-facing helper constructs

### What belongs here

- practical reusable UI helpers
- component generation/building
- tag helpers
- UX utility helpers that are broadly reusable

### What does **not** belong here

- app-specific UI logic
- one-off project widgets
- random helpers with no clear reuse value

### Design intent

UI should remain useful and broadly reusable, not become a miscellaneous bucket.

---

## Extension Method Strategy

`JC.Web` should follow a **layered registration model**:

1. **Granular feature registration**
2. **Section-level defaults**
3. **Package-level defaults**

This gives consumers multiple entry points without duplicating logic.

### 1. Granular feature registration

Each area/feature should expose focused registration methods.

Examples:

- `AddSecurityHeaders(...)`
- `AddCookies(...)` or a better final cookie-specific name
- `AddAntiforgery(...)`
- `AddCorsDefaults(...)` / `AddCorsHelpers(...)`
- `AddRateLimiting(...)`
- `AddObservability(...)`
- `AddUiHelpers(...)`

These are the lowest-level, most explicit registrations.

### 2. Section-level defaults

Each section should expose a curated defaults method.

Examples:

- `AddSecurityDefaults(...)`
- `AddObservabilityDefaults(...)`
- `AddRateLimitingDefaults(...)`
- `AddUiDefaults(...)` if that proves useful

These should be **composed from granular registrations**, not implemented separately.

### 3. Package-level defaults

The package may also expose:

- `AddWebDefaults(...)`

This should compose a safe baseline across the package as a whole.

---

## Critical Design Rule for Defaults

Defaults must be:

- composable
- predictable
- low-risk
- easy to override
- implemented by calling the granular registrations internally

They must **not** create a second hidden configuration system.

### Important principle

`AddSecurityDefaults()` or `AddWebDefaults()` should **not** contain unique logic that bypasses the granular feature methods.

Instead, defaults should simply be curated bundles of existing feature registrations.

That gives:

- one implementation path
- consistent behaviour between granular and bundled usage
- easier maintenance
- fewer bugs
- clearer mental model for consumers

---

## Defaults Must Still Accept Options

All defaults methods should still allow configuration.

For example:

- `AddSecurityDefaults(...)` may internally call:
  - `AddSecurityHeaders(...)`
  - cookie registration
  - maybe rate limiting registration
- while still allowing callers to configure the underlying options for headers, cookies, etc.

### Example philosophy

A defaults bundle should be able to include:

- headers
- cookies
- rate limiting

while intentionally excluding:

- antiforgery
- CORS

if those are considered higher-friction or more app-specific.

That is acceptable **as long as it is documented clearly**.

---

## Recommended Behaviour of Defaults

### `AddSecurityDefaults(...)`

Should likely include:

- Security Headers baseline
- Cookie/security defaults
- possibly rate limiting baseline if considered part of the security baseline

Should likely exclude by default:

- strict CSP enforcement unless explicitly configured
- aggressive cross-origin isolation
- CORS configuration unless intentionally requested
- antiforgery unless clearly appropriate for the target app type

### `AddObservabilityDefaults(...)`

Should likely include:

- request metadata helpers
- IP extraction helpers
- forwarded header aware client context helpers
- user agent parsing/classification helpers

Should exclude:

- storage/analytics/tracking systems

### `AddRateLimitingDefaults(...)`

Should likely include:

- sane default named policies
- per-IP baseline rate limiting if appropriate
- helper registration for endpoint use

Should exclude:

- any “full DDoS protection” framing or feature creep

### `AddWebDefaults(...)`

Should compose only the **safe, broadly useful, low-risk** defaults from each section.

This method should be:

- boring
- safe
- predictable
- easy to override

It should not unexpectedly enable highly strict or break-prone features.

---

## Option Handling Philosophy

All granular and bundled registrations should support explicit configuration.

### Rule

**Explicit caller configuration should win over package defaults.**

Defaults should provide a baseline, then allow overrides.

### Desired characteristics

- users can opt into a section default bundle
- still configure the individual options within the included features
- no feature should become harder to configure just because it is registered through a default bundle

---

## Recommended Naming Direction

### Good names

- `AddSecurityHeaders(...)`
- `AddSecurityDefaults(...)`
- `AddObservabilityDefaults(...)`
- `AddRateLimitingDefaults(...)`
- `AddWebDefaults(...)`

### Cookie naming

`AddCookies(...)` may be acceptable, but may be too vague depending on the eventual scope.

Possible alternatives if needed:

- `AddCookieServices(...)`
- `AddCookieDefaults(...)`
- `AddSecureCookies(...)`

Final naming should reflect the actual cookie feature scope.

---

## Current Security Headers Position

The current Security Headers implementation appears to be a solid v1 foundation and should remain part of the Security section.

### Current strengths

- clean options model
- sensible enum usage for fixed-value headers
- CSP builder with pragmatic validation
- middleware with precomputed values
- HSTS correctly restricted to HTTPS responses
- extension-based setup pattern

### Current caveats

- defaults should remain conservative
- stricter cross-origin headers may be better as opt-in rather than always-on defaults
- tests and documentation are essential before release

---

## Recommended Immediate Roadmap

### Near-term priorities

1. Complete **Cookies** under Security
2. Add **Antiforgery helpers**
3. Consider **CORS wrappers/helpers** only if kept thin and useful
4. Build **RateLimiting** wrappers/middleware
5. Add lightweight **Observability** helpers
6. Expand/update README and docs for section usage and defaults
7. Add strong test coverage

### Suggested order of effort

A sensible sequence would be:

1. Security (Cookies next)
2. Antiforgery / CORS if justified
3. RateLimiting
4. Observability
5. Additional package-level defaults polish

This order keeps the most concrete and high-value areas moving first.

---

## Guardrails for the Whole Package

To keep `JC.Web` healthy long-term, every new feature should satisfy the following:

- clearly belongs to one section
- clearly improves or simplifies normal ASP.NET Core usage
- does not create a second framework over ASP.NET Core
- does not introduce unnecessary abstraction layers
- is broadly reusable rather than app-specific
- can be documented clearly with predictable behaviour

### Anti-goals

Avoid turning `JC.Web` into:

- a dumping ground for unrelated helpers
- wrappers around wrappers around wrappers
- a giant options bag with unclear defaults
- a pseudo-framework that hides standard ASP.NET Core behaviour

---

## Final Summary

The planned structure for `JC.Web` is:

- **Security** for web hardening and request/browser safety
- **Observability** for request/client metadata helpers
- **RateLimiting** for app-level throttling/abuse control
- **UI** for reusable web UX helpers

The extension strategy should be:

- granular registrations for individual features
- section-level curated defaults
- optional package-level defaults

All defaults should:

- internally call granular registrations
- remain configurable
- remain predictable
- stay conservative by default

This gives `JC.Web` a strong structure without repo/project sprawl, while keeping the package modular, maintainable, and practical.
