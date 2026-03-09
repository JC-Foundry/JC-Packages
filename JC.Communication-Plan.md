# JC.Communication

## Purpose

`JC.Communication` should be a dedicated package for outbound application communication.

Its job is to provide a clean, reusable, app-level communication layer that lets projects send messages to users, admins, support teams, or external contacts without forcing each application to repeatedly solve the same problems:

- how do I send email again?
- how do I model recipients and message payloads?
- how do I switch providers later without rewriting app code?
- how do I log sent communications?
- how do I queue or retry failed communications?
- how do I expand from email into SMS or in-app notifications later without stuffing everything into `JC.Core` or `JC.Web`?

This package should **not** try to become a giant enterprise messaging platform. It should exist for the same reason as the rest of the suite:

> package up the repetitive communication plumbing that keeps appearing across projects, so application development can move straight to the real app.

---

## Why this should be its own package

A dedicated `JC.Communication` package is cleaner than putting email into `JC.Core` or `JC.Web`.

### Why not `JC.Core`

`JC.Core` should remain foundational infrastructure:

- common models
- repository patterns
- auditing
- pagination
- shared helpers and extensions

Communication is a real feature area with its own:

- providers
- models
- configuration
- delivery concerns
- logging/history
- retries/background sending
- future channel expansion

That is too opinionated and too domain-specific for `JC.Core`.

### Why not `JC.Web`

`JC.Web` is for web application concerns:

- middleware
- security headers
- client profiling
- rate limiting
- UI/tag helpers
- web defaults

Email is used by web apps, but it is not inherently a web concern. A console app, worker service, API-only service, or background process may also need to send messages.

### Why its own package makes sense

Communication is a cross-cutting application concern.

A dedicated package gives it a clean home and avoids future awkwardness when the feature grows from:

- email only

to:

- email
- SMS
- in-app notifications
- possibly push notifications later
- message logging
- delivery status/history
- templates
- job-based retries and scheduling via `JC.BackgroundJobs`

---

## Package goal

The package should provide a **simple, reliable communication abstraction** that supports:

1. **email first**
2. clean provider configuration
3. strongly typed message models
4. logging and auditability where useful
5. optional future channel expansion
6. easy integration with the rest of the suite

It should let an application say:

- send this email
- log that it was sent or failed
- optionally queue/retry it later
- switch delivery implementation without rewriting business logic

without forcing every app to re-implement SMTP, provider setup, and message models from scratch.

---

# Scope

## In scope for the first version

The first version should be **email-focused**.

That means:

- a clean email abstraction
- one provider path that you already trust in production
- strongly typed message and recipient models
- simple configuration
- optional send result model
- optional persistence/logging model if desired now
- clean extension methods for DI and startup

The first implementation should likely target:

- **Microsoft 365 / Exchange Online SMTP relay**

because:

- you already know it works
- you have production experience with it
- it solves a real need immediately
- it avoids premature provider sprawl

## Out of scope for the first version

Do **not** build all of this on day one:

- multiple providers just because they exist
- full template engines
- inbound email processing
- advanced analytics/tracking
- attachments unless you already need them
- SMS implementation
- push notifications
- notification preferences center
- campaign-style messaging
- giant workflow/orchestration features

The package should be designed to grow, but not implemented as if every future feature must exist now.

---

# Core design principles

## 1) Email first, communication later

The package name can be `JC.Communication`, but the first implementation should be **email-only**.

That gives you:

- the right long-term package name
- room to grow later
- no need to rename when SMS or notifications arrive
- no pressure to implement every channel immediately

## 2) Abstract the message, not just the provider

The package should not merely wrap SMTP calls.

It should model communication in a way that application code can depend on, such as:

- recipient
- subject
- body
- HTML/plain text
- send result
- status
- timestamps

That way, the application depends on communication models and interfaces rather than raw SMTP client logic.

## 3) Provider choice should sit behind DI

Application code should call a communication service, not directly instantiate SMTP senders.

This keeps:

- sending logic replaceable
- configuration centralized
- testing easier
- migration from one provider to another cleaner

## 4) Logging should be optional but planned for

Many apps benefit from being able to answer:

- what was sent?
- when was it sent?
- to whom?
- did it succeed?
- if it failed, why?

That does not mean every app must persist every email forever, but the package should be shaped so this can be added cleanly.

## 5) Queueing and retries belong with background jobs

`JC.Communication` should send messages.

`JC.BackgroundJobs` should handle:

- deferred sending
- retrying failures later
- scheduled sends
- escalation/recovery jobs

That separation keeps the package focused.

---

# Recommended first implementation: Email

## Primary use cases

The first version should cover common application email scenarios:

- account-related messages
- admin alerts
- contact form delivery
- support/issue notifications
- password reset / verification style messages
- workflow/status update emails
- simple app notifications

## Email service boundary

The core service should be something like:

- `IEmailService`

with one clear responsibility:

- send an email message

Potential method shape:

```csharp
Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
```

This is better than a method with many primitive parameters because it keeps the contract expandable.

## Email models

A clean model set would likely include:

### `EmailMessage`
Represents a single outbound email.

Potential fields:

- `From`
- `To`
- `Cc`
- `Bcc`
- `ReplyTo`
- `Subject`
- `HtmlBody`
- `TextBody`
- optional `Headers`
- optional `Attachments` later if actually needed

### `EmailRecipient`
Represents an email address plus optional display name.

Potential fields:

- `Address`
- `DisplayName`

### `EmailSendResult`
Represents the result of an attempted send.

Potential fields:

- `Succeeded`
- `Provider`
- `SentAtUtc`
- `ErrorMessage`
- `MessageId` if available

### Optional `EmailLogEntry`
If email logging is implemented early, this would represent persisted history.

Potential fields:

- `ToJson` or normalized recipients
- `Subject`
- `Status`
- `Provider`
- `SentAtUtc`
- `FailedAtUtc`
- `ErrorMessage`
- correlation/reference fields if needed

---

# Recommended provider strategy

## Start with Microsoft 365 / Exchange Online SMTP relay

This should be the first provider because it is already proven in your real-world usage.

That means the package can deliver value immediately without introducing fake abstraction complexity.

Possible implementation types:

- `MicrosoftSmtpEmailService`
- or more generically `SmtpEmailService` with Microsoft-oriented config first

## Provider configuration

The package should allow provider-specific setup while preserving a clean app-facing abstraction.

Example conceptual setup routes:

- `AddCommunication(...)`
- `AddEmail(...)`
- `AddMicrosoftEmail(...)`
- possibly `AddSmtpEmail(...)`

A good pattern would be:

- one root communication registration
- one email registration path
- provider-specific configuration nested under email

Example conceptual configuration areas:

- host
- port
- username
- password / secret
- sender address
- sender display name
- SSL/TLS options
- timeout
- pickup/delivery mode if expanded later

## Secrets

As with the rest of the suite, secrets should stay in configuration/secrets/env vars rather than in hard-coded options.

That includes:

- SMTP credentials
- tenant relay credentials
- provider keys if other providers are added later

---

# Logging and persistence

## Should email logging exist?

Yes, potentially — but only if it solves a real repeated problem.

A communication log becomes worthwhile if you want to answer operational questions such as:

- was the message sent?
- why did it fail?
- what subject/body was used?
- which provider handled it?
- do we need to retry this?
- did a support or workflow email actually leave the system?

## What should be logged?

A practical minimum would be:

- recipients
- subject
- channel
- provider
- created/sent/failed timestamps
- success/failure state
- failure reason

## What should be avoided?

Be careful about logging:

- sensitive body content
- secrets
- unnecessary personal data
- full HTML bodies if not operationally useful

A sensible compromise is:

- log metadata and status by default
- optionally log body/summary if a project truly needs it

## Where should logs live?

If implemented, logs likely belong in a data model inside `JC.Communication`, not inside `JC.Core`.

That would make the package more like `JC.Github`:

- service abstraction
- provider integration
- optional persistence of communication events

---

# Interaction with JC.BackgroundJobs

This is a very natural integration point.

`JC.Communication` should not become a retry engine.

Instead, it should expose a clean send service, and a background job can be responsible for:

- retrying failed sends
- scheduled delivery
- batched sends
- escalation flows
- reprocessing undelivered items from a communication log

This gives you a strong separation of concerns:

- `JC.Communication` = message construction and delivery
- `JC.BackgroundJobs` = execution timing and retries

A future job could look like:

- scan failed email log entries
- retry those under defined rules
- update send status

That is a much cleaner design than stuffing retries directly into the communication package.

---

# Interaction with JC.Identity and JC.Web

## With `JC.Identity`

Possible useful integrations later:

- user email notifications
- password/account emails
- role/admin alerting
- tenant-aware sender behavior if ever needed

But `JC.Communication` should remain independent. It should not depend on identity to function.

## With `JC.Web`

Possible useful integrations later:

- contact/support forms
- bug report endpoints that email as an alternative to GitHub
- admin UI for communication logs
- tag helpers or helper pages later if genuinely useful

But again, communication itself should remain separate from web concerns.

---

# Suggested package structure

A structure similar in spirit to `JC.Web` would work well, while staying smaller initially.

## Initial structure

```text
/Email
  /Models
  /Options
  /Services
  /Extensions
  /Providers
```

### Example conceptual layout

```text
JC.Communication/
  Email/
    Models/
      EmailMessage.cs
      EmailRecipient.cs
      EmailSendResult.cs
    Options/
      EmailOptions.cs
      SmtpEmailOptions.cs
    Services/
      IEmailService.cs
      EmailService.cs (facade if needed)
    Providers/
      Microsoft/
        MicrosoftSmtpEmailService.cs
    Extensions/
      ServiceCollectionExtensions.cs
```

## Future structure

If the package expands:

```text
JC.Communication/
  Email/
  Sms/
  Notifications/
  Logging/
  Templates/
```

The key is to avoid implementing future folders until there is real demand.

---

# Documentation strategy

This package should follow the same doc model as the rest of the suite:

- `Setup.md`
- `Guide.md`
- `API.md`

## `Setup.md`
Should explain:

- how to register the package
- how to configure Microsoft SMTP relay
- where secrets live
- what defaults exist
- how optional logging is enabled if included

## `Guide.md`
Should explain:

- how to send a basic email
- how to send HTML vs plain text
- how to send to multiple recipients
- how to use communication with background jobs
- how and when to log sent messages
- what the package is intentionally not doing

## `API.md`
Should explain:

- services
- models
- options
- provider-specific registrations
- result/status models
- extension methods

This is especially important because AI tools need a clear capability map when generating code against the package.

---

# Future expansion areas

The package name `JC.Communication` becomes most valuable here.

## 1) SMS / text messaging

A future SMS area could provide:

- `ISmsService`
- `SmsMessage`
- provider options
- send result models
- optional delivery logging

Possible use cases:

- MFA/OTP codes
- urgent alerts
- status updates
- appointment/reminder messaging

This should only be added when you actually need it.

## 2) In-app notifications

A future notifications area could provide:

- notification models
- notification persistence
- read/unread tracking
- targeting users/roles/tenants
- notification delivery helpers

Possible use cases:

- app dashboard alerts
- task/status updates
- warning banners
- internal user messaging

This may eventually overlap with `JC.Identity` user context, but should still remain a communication concern.

## 3) Push notifications

Potential future support for:

- browser push
- mobile push

This is much further down the line and should definitely not be implemented without a real project need.

## 4) Template rendering

Eventually you may want templated communication, such as:

- email templates
- shared layouts
- variable substitution
- tenant branding

But this should come later.

Day one should not include a giant templating subsystem unless you already know you need it.

## 5) Delivery logging and communication history

A mature future version of the package might support:

- per-channel logs
- per-recipient delivery history
- retry state
- status transitions
- correlation IDs

Again, only add this when real apps justify it.

---

# Recommended implementation order

## Phase 1: Foundation

Build the minimum useful email layer:

- package scaffold
- email models
- `IEmailService`
- Microsoft SMTP relay implementation
- DI registration/extensions
- configuration options
- basic docs

## Phase 2: Practical polish

Add the most likely useful improvements:

- HTML + text support
- multiple recipients
- CC/BCC/Reply-To
- send result model
- better validation
- clearer exceptions/logging
- full docs set

## Phase 3: Operational improvements

Only if needed:

- communication logging
- persistence model
- retry integration via `JC.BackgroundJobs`
- scheduled send scenarios

## Phase 4: Future channels

Only when a real app needs them:

- SMS
- in-app notifications
- template rendering
- additional providers

---

# What success looks like

`JC.Communication` is successful if it lets a new app avoid the usual communication setup drag.

That means:

- no re-looking up SMTP setup every time
- no rewriting email models every project
- no re-deciding where recipient/subject/body abstractions live
- no stuffing communication into unrelated packages
- no painful migration later when communication needs grow

A successful first release would make email sending feel like the rest of your suite:

- obvious to register
- obvious to configure
- easy to use
- not overbuilt
- ready to grow later

---

# Recommended final direction

## Package name

Use:

- **`JC.Communication`**

## First implemented channel

Start with:

- **Email only**

## First provider

Start with:

- **Microsoft 365 / Exchange Online SMTP relay**

## Design philosophy

Build it as:

- a communication package with an email-first implementation
- not an all-channel framework on day one
- not a tiny SMTP helper stuffed into another package

## Integration philosophy

Keep:

- delivery in `JC.Communication`
- retries/scheduling in `JC.BackgroundJobs`
- web concerns in `JC.Web`
- user/tenant concerns in `JC.Identity`

That preserves the same strong package-boundary discipline as the rest of the suite.

---

# Final summary

`JC.Communication` should exist because communication is a real, repeated application concern that does not fit cleanly into `JC.Core` or `JC.Web`.

The package should start with a focused email implementation using the provider path you already trust in production. It should model messages and sending cleanly, integrate through DI, and leave room for optional logging and future expansion.

It should not try to become a giant notification framework immediately.

The right approach is:

- name it for the long term
- implement email first
- keep the abstraction clean
- separate retries/background execution into `JC.BackgroundJobs`
- add SMS, notifications, templates, and richer delivery tracking only when real projects demand them

That gives you a package that is both immediately useful and architecturally future-safe.
