## 5. Two-factor

### 5.1 Enrolment is global, enforcement is per app

2FA is a property of the **authentication event**, carried on the session, not of the app.

- Enrolment is global. One authenticator per person, enrolled once, carried into every app.
- Enforcement is per SSO app, set by its administrators, because the app owner carries the risk of a
  compromised account.
- An app that requires 2FA accepts a session that already satisfies it, or forces a step-up. An app
  that does not accepts either.

So a SystemAdmin already 2FA'd into CAP (which is mandatory there) reaches a relaxed client app
silently, and an SSO-only user on a strict app is challenged. The CAP-mandatory and SSO-optional
policies coexist without contradiction.

Carry the trap from 2026-08-21: refresh the principal after `SetTwoFactorEnabledAsync` before
redirecting to the recovery codes, or the stale claim bounces the user and consumes the one-shot
`TempData`. The SSO enrolment flow has identical ingredients and would lose the codes the same way.

### 5.2 The middleware needs a per-request rule set (JC.Identity change)

`IdentityMiddleware` enforcement is global today: one `EnforceTwoFactor` flag, one `TwoFactorRoute`,
and a loop guard that is a single `StartsWith` on that route. Every authenticated request from a
user without 2FA is pushed to `/identity/manage/security/enable-authenticator`, which an SSO user
must never reach. Dead end at best, redirect loop at worst.

This is not a 2FA problem. All four routes have it:

| Route | Wrong destination for an SSO user |
|---|---|
| `TwoFactorRoute` | CAP's enrolment page instead of the SSO one |
| `ChangePasswordRoute` | CAP's set-password page on a forced reset |
| `AccessDeniedRoute` | CAP's branded refusal shown to a website end user |
| `LogoutRoute` | CAP's dashboard rather than the app |

The package change wanted is a hook on `IdentityMiddlewareOptions` that resolves the **rule set per
request** (routes plus flags), returning null to skip. CAP registers "under `/sso` use the SSO rule
set, otherwise the portal one", and the loop guard becomes a `StartsWith` on whichever route the
resolved set names, which fixes that too.

A path-exclusion list is the cheaper stopgap but is the wrong shape: it disables *all* the rules for
those paths, including the ones SSO does still want. **SSO needs a parallel set of these
behaviours, not an exemption** — a disabled SSO user must still be stopped, a forced reset must
still fire, a strict app must still enrol. Same behaviours, different routes, different source of
truth.