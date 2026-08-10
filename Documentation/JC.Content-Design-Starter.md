# JC.Content — Initial Design & Scope Exploration

> **Status:** Early design / conversation starter  
> **Intent:** Explore what `JC.Content` could become, what belongs within its boundary, what probably does not, and where future package splits may make sense.  
> **Important:** This document is intentionally broad, speculative, and incomplete. Ideas may be rejected, renamed, radically redesigned, moved into other packages, or never implemented.

---

## 1. Why `JC.Content` Might Exist

`JC.Content` would provide reusable functionality for working with **content itself**.

The package should not care where content came from or where it will eventually be displayed.

Possible sources include:

- user input;
- generated text;
- database content;
- imported files;
- console input;
- desktop applications;
- background jobs;
- API payloads;
- emails or messages;
- AI-generated content;
- configuration or metadata text;
- server-generated descriptions or reports.

Likewise, it should not care whether the result is later:

- displayed on a website;
- shown in a desktop application;
- stored in a database;
- logged;
- exported;
- sent through email;
- passed to another service;
- used by another JC package.

The central idea is:

> **JC.Content works with the content, not the delivery mechanism or presentation layer around it.**

This distinction matters because `JC.Content` should not become a dumping ground for web helpers, UI components, HTTP-specific behaviour, database concerns, or document-management features.

---

## 2. High-Level Responsibility

A deliberately broad possible definition:

> **JC.Content provides reusable tools and abstractions for analysing, moderating, comparing, transforming, normalising, formatting, and converting content.**

Potential capability families include:

- moderation;
- profanity filtering;
- content policy evaluation;
- text normalisation;
- text analysis;
- language detection;
- translation;
- diffing and comparison;
- content formatting;
- content conversion;
- redaction;
- structured content processing;
- content processing pipelines;
- provider abstractions for external content services.

This does **not** mean every one of these should exist. This document is a space for discussing them.

---

# 3. Core Design Principles

## 3.1 Content First

The package should operate on content without needing to understand the application around it.

A profanity filter should not care whether text came from a Razor form, console, WPF textbox, imported CSV, API request, or AI response.

Likewise, a diff engine should simply compare content.

## 3.2 Presentation Agnostic

`JC.Content` should not depend on:

- Bootstrap;
- Tailwind;
- Razor;
- MVC;
- Blazor;
- WPF;
- WinForms;
- JavaScript;
- browsers;
- CSS;
- any other presentation framework.

If presentation-specific functionality becomes useful, it should live elsewhere: `JC.Web`, a future wrapper package, a future document package, or the consuming application itself.

## 3.3 Transport Agnostic

The package should not care whether content arrived through HTTP, email, messaging, file upload, CLI input, database records, or background jobs.

## 3.4 Avoid Becoming a `StringExtensions` Dumping Ground

The existence of a string helper does not automatically justify placing it in `JC.Content`.

For example, wrapping `Trim()`, basic casing, or simple replacement APIs adds little value. Trivial helpers may belong in `JC.Core`, or should simply use the .NET BCL.

`JC.Content` should represent meaningful **content-processing concerns**, not a catalogue of convenience wrappers.

## 3.5 Add Value Over Existing Libraries

A feature can absolutely use another library internally.

However, the JC layer should add something meaningful:

- consistent models;
- provider abstraction;
- policy;
- orchestration;
- composition;
- reusable configuration;
- result types;
- integration between capabilities.

Avoid this unless it genuinely helps:

```text
JC.Content
    ↓
thin wrapper
    ↓
existing library
```

## 3.6 Small Does Not Mean Bad

A compact capability with a strong responsibility is preferable to a large package with unclear boundaries.

The question is:

> Does this solve a reusable content problem cleanly?

Not:

> Is this large enough to look like a package?

---

# 4. Possible Package Shape

This is conceptual, not prescriptive:

```text
JC.Content
│
├── Moderation
│   ├── Profanity
│   ├── Policies
│   ├── Rules
│   └── Results
│
├── Analysis
│   ├── Language
│   ├── Similarity
│   ├── Statistics
│   └── Classification
│
├── Comparison
│   └── Diffing
│
├── Formatting
│   ├── Normalisation
│   ├── Cleanup
│   └── Pipelines
│
├── Conversion
│   ├── PlainText
│   ├── Markdown
│   └── Other Representations
│
├── Translation
│   ├── Contracts
│   ├── Providers
│   └── Results
│
└── Redaction
```

Do not create empty namespaces merely because they appear here. The shape should emerge from real implementation.

---

# 5. Profanity Filtering

This is one of the strongest initial candidates.

The existing implementation that inspired the package is too strict, so a replacement should prioritise **accuracy and configurability**, not simply matching blocked substrings.

Potential responsibilities:

- individual blocked words;
- blocked phrases;
- case-insensitive matching;
- configurable strictness;
- word-boundary awareness;
- punctuation inserted into words;
- repeated characters;
- common substitutions or leetspeak;
- Unicode normalisation;
- language-specific rule sets;
- allowlisted words or phrases;
- detection-only mode;
- replacement/filtering mode;
- returning individual matches;
- severity;
- category;
- match positions;
- original and filtered content.

Conceptual result:

```text
ContentModerationResult
├── IsAllowed
├── Matches
│   ├── Term
│   ├── Category
│   ├── Severity
│   └── Position
├── OriginalContent
└── FilteredContent
```

### Important design goal

Avoid naive substring filtering. False positives are arguably as important to solve as missed profanity.

### Questions to explore

- Is profanity filtering its own service or one moderation rule?
- Should filters be culture/language aware?
- Should replacement characters be configurable?
- Should the library include built-in word lists?
- Should consuming applications supply their own lists?
- Should defaults be overridable?
- Should severity differ between contexts?
- How aggressively should deliberate filter evasion be detected?
- At what point does evasion detection produce too many false positives?

---

# 6. Wider Content Moderation

Profanity does not necessarily need to be the entire moderation model.

Possible future areas:

- abusive language;
- spam-like content;
- prohibited phrases;
- custom organisational terms;
- excessive repetition;
- suspicious links;
- application-specific content rules;
- configurable moderation categories;
- provider-backed moderation.

Possible architecture:

```text
Content
   ↓
Moderation Engine
   ↓
┌───────────────────────┐
│ Profanity Rule        │
│ Application Rule      │
│ External Provider     │
│ Custom Rule           │
└───────────────────────┘
   ↓
Moderation Result
```

The package should not attempt to become a universal trust-and-safety platform. It should stay understandable and useful.

---

# 7. Content Policies

A reusable policy model may be more useful than hard-coded moderation behaviour.

Examples:

```text
Strict Public Content
Internal Business Content
User Messaging
Generated Content
Profile Content
```

Policies could potentially control:

- which rules run;
- severity thresholds;
- whether content is rejected;
- whether content is automatically filtered;
- whether warnings are returned;
- ignored categories;
- provider usage;
- maximum content length;
- required language;
- required confidence.

The consuming application should generally decide **what to do** with a result rather than `JC.Content` making application workflow decisions.

---

# 8. Text Normalisation

Normalisation naturally fits because it operates on content itself.

Potential functionality:

- Unicode normalisation;
- newline normalisation;
- whitespace cleanup;
- removal of excessive blank lines;
- trimming;
- invisible-character handling;
- repeated whitespace reduction;
- quotation mark normalisation;
- dash normalisation;
- line-ending consistency;
- optional casing normalisation;
- removal or replacement of unsupported characters.

This can feed other operations:

```text
Raw Content
    ↓
Normalise
    ↓
Moderate
    ↓
Compare
    ↓
Store
```

The package should avoid silently altering meaningful content unless the caller explicitly requests it.

---

# 9. Content Formatting

"Formatting" needs a clear definition before implementation.

Potentially valid content-level formatting:

- paragraph cleanup;
- whitespace rules;
- line wrapping;
- sentence spacing;
- indentation;
- quote normalisation;
- bullet/list normalisation;
- title/headline casing;
- structured text cleanup;
- plain-text pretty printing;
- configurable formatting pipelines.

Potentially **not** valid:

- CSS classes;
- responsive layout;
- colours;
- font selection;
- browser layout;
- Razor rendering;
- component generation.

A useful distinction:

> **JC.Content can change the structure or representation of content, but should not control how an application visually renders it.**

---

# 10. Processing Pipelines

A pipeline model could eventually become useful:

```text
Input
  ↓
Unicode Normalisation
  ↓
Whitespace Cleanup
  ↓
Profanity Check
  ↓
Custom Moderation
  ↓
Formatting
  ↓
Output
```

Possible benefits:

- reusable configured pipelines;
- predictable operation ordering;
- easy addition/removal of stages;
- consistent result handling;
- shared processing across applications.

Potential concerns:

- unnecessary abstraction;
- difficult debugging;
- excessive generics;
- turning simple operations into a framework;
- configuration becoming harder than direct code.

A pipeline should only appear if several real applications benefit from composing operations repeatedly.

---

# 11. Content Diffing

Diffing is a strong candidate because it is clearly content-focused and widely reusable.

The actual diff algorithm should probably come from an established library unless there is a compelling reason to build one.

Possible modes:

- character-level;
- word-level;
- line-level;
- paragraph-level;
- semantic-ish block comparison;
- case-sensitive / insensitive;
- whitespace-sensitive / insensitive.

Possible result:

```text
ContentDiff
├── Added
├── Removed
├── Unchanged
├── Changed
├── Similarity
└── Segments
```

Potential uses:

- audit history;
- configuration changes;
- content revisions;
- generated-text comparisons;
- administrative portals;
- approval workflows;
- migration tooling;
- change summaries;
- test/report output.

The core result should remain presentation-neutral. Adapters may later produce unified diff text or data structures for side-by-side views.

A coloured HTML diff renderer would **not** belong in the core package.

---

# 12. Similarity / Comparison

Related to diffing but potentially distinct:

- approximate similarity percentage;
- edit distance;
- duplicate detection;
- near-duplicate detection;
- word overlap;
- paragraph similarity;
- token-based comparison.

Possible uses:

- detecting nearly identical generated content;
- avoiding duplicate entries;
- comparing edited descriptions;
- moderation heuristics;
- identifying repeated submissions.

Existing libraries may do the heavy lifting.

---

# 13. Language Detection

Language detection fits the boundary well.

```text
Input Content
    ↓
Language Detector
    ↓
Language Result
```

Possible result:

```text
LanguageDetectionResult
├── Language
├── LanguageCode
├── Confidence
└── Alternatives
```

This could be locally implemented, library-backed, provider-backed, or optional. It could support translation and moderation later.

---

# 14. Translation

Translation is plausible future functionality, likely through abstractions/providers rather than a home-grown translation engine.

Potential shape:

```text
ITranslationProvider
        ↓
TranslateAsync(...)
        ↓
TranslationResult
```

Possible provider implementations:

- Microsoft/Azure;
- Google;
- DeepL;
- local model;
- custom application implementation;
- future AI provider.

Possible features:

- source language;
- target language;
- automatic language detection;
- batch translation;
- provider metadata;
- confidence;
- preserved formatting;
- translation warnings;
- cancellation;
- usage/cost metadata where applicable.

### Boundary

`JC.Content` should own the **translation abstraction and content model**, not hard-code one vendor.

Provider-specific packages might eventually be justified:

```text
JC.Content
JC.Content.Azure
JC.Content.DeepL
```

Only create them when real usage warrants it.

---

# 15. Content Classification

A more speculative area.

Possible operations:

- determine category/topic;
- identify likely content type;
- sentiment;
- toxicity;
- urgency;
- business classification;
- custom labels.

The core package might eventually define neutral interfaces without caring whether an implementation is rule-based, ML-based, LLM-backed, or API-backed.

This should remain speculative until a real use case appears.

---

# 16. Content Statistics

Potential low-cost analysis helpers:

- word count;
- character count;
- sentence count;
- paragraph count;
- estimated reading time;
- unique word count;
- repeated-word analysis;
- basic lexical statistics.

This area risks becoming a random helper collection, so only add things with repeated practical value.

---

# 17. Redaction

Redaction may fit well because it modifies content based on detectable patterns.

Possible examples:

- email address redaction;
- phone number redaction;
- IP address redaction;
- account/reference numbers;
- custom regex patterns;
- application-defined sensitive terms.

Example:

```text
Original:
Contact john@example.com

Redacted:
Contact [EMAIL]
```

Potential uses:

- logging;
- support exports;
- AI prompts;
- telemetry;
- debugging;
- public reports.

Heuristic redaction must not be presented as a guarantee that all sensitive information has been removed.

---

# 18. Content Conversion

This area needs careful boundary discussion.

Possible conversions:

```text
Plain Text ↔ Structured Text
Markdown → Plain Text
Markdown → Other Neutral Representation
Structured Content → Plain Text
```

The key question is whether `JC.Content` should operate directly on formats such as Markdown or HTML, or define an internal neutral content model.

---

# 19. Markdown

Markdown is a strong candidate because it is fundamentally a textual content representation.

Potential functionality:

- Markdown parsing;
- Markdown generation;
- Markdown to plain text;
- normalisation;
- conversion between Markdown dialects;
- extracting headings/links;
- Markdown analysis;
- content diffing on parsed Markdown.

Using an established Markdown library would likely make sense.

The package's value could come from a stable JC abstraction, configured parser defaults, common extension policies, conversion models, and integration with other content operations.

---

# 20. HTML — Deliberate Grey Area

HTML is contentious for `JC.Content`.

The package is deliberately not intended to become web-focused, yet HTML can also be treated as a **serialised content representation** independently of a browser.

That gives two valid directions.

## Direction A — HTML Does Not Belong

```text
JC.Content
    ↓
text/content only

JC.Web
    ↓
HTML/web concerns
```

Advantages:

- extremely clean boundary;
- avoids web concepts leaking into `JC.Content`;
- smaller dependency surface;
- keeps sanitisation and rendering firmly in `JC.Web`.

## Direction B — HTML as Data May Belong

Possible neutral operations:

```text
HTML → Plain Text
Markdown → HTML
HTML → Markdown
```

This would still exclude:

- HTTP;
- Razor;
- DOM scripting;
- CSS frameworks;
- browser rendering.

### Likely rule if HTML conversion is eventually allowed

> **HTML security, browser behaviour, web rendering, sanitisation policy, CSS, Razor, and UI integration remain outside `JC.Content`.**

This should remain an explicit design decision rather than being assumed.

---

# 21. HTML Sanitisation

Current leaning:

> **Do not move HTML sanitisation into `JC.Content`.**

HTML sanitisation exists because HTML will be interpreted by a browser or HTML-aware renderer, making it a web/security concern rather than a generic text-content concern.

`JC.Web` remains the natural home for the existing sanitiser.

A future content-conversion feature could transform HTML without taking ownership of browser-security policy.

---

# 22. Plain Text → Markdown

This is not always a true conversion because plain text often lacks semantic information.

For example:

```text
Important Information
```

could be a heading, title, paragraph, or emphasised text.

Possible approaches:

### Conservative Conversion

Escape/normalise text and preserve paragraphs only.

### Rule-Based Conversion

Allow caller-supplied patterns, for example:

- first line is title;
- lines beginning with `-` become lists;
- uppercase lines become headings.

### AI/Inference-Based Conversion

A possible future provider, but this becomes generated interpretation rather than deterministic conversion.

API naming should make that distinction clear.

---

# 23. PDF — Strong Boundary Question

PDF creation sounds content-related but quickly becomes a document-rendering concern.

Useful PDF generation introduces:

- pages;
- margins;
- fonts;
- pagination;
- images;
- headers;
- footers;
- tables;
- layout;
- print styling;
- binary output.

That starts looking more like a future package:

```text
JC.Documents
```

Possible split:

```text
JC.Content
    ↓
defines/transforms content

JC.Documents
    ↓
renders content into documents such as PDF
```

`JC.Documents` should **not** be created merely because PDF has been mentioned. It should appear only if real applications create a reusable document-generation requirement.

A lightweight provider abstraction inside `JC.Content` could be an alternative initially, with extraction later if the responsibility grows.

---

# 24. A Neutral Content Model

A potentially powerful but potentially over-engineered idea is an internal neutral representation.

```text
ContentDocument
├── Paragraph
├── Heading
├── List
├── Quote
├── Link
├── Code
└── Text
```

Conversions could then operate like:

```text
Markdown
    ↓
ContentDocument
    ↓
Plain Text
```

or:

```text
HTML
    ↓
ContentDocument
    ↓
Markdown
```

Advantages:

- format-independent transformations;
- easier analysis;
- reusable diffing;
- cleaner conversion architecture;
- avoids direct converters between every pair of formats.

Disadvantages:

- significant complexity;
- easy to over-design;
- difficult to preserve format-specific features;
- another custom document model to maintain;
- may be unnecessary for actual project needs.

Only pursue this if direct conversions become difficult to maintain.

---

# 25. External / AI Content Providers

Many future operations could be provider-backed:

```text
Translation
Classification
Moderation
Summarisation
Language Detection
Rewrite
Grammar Correction
```

Rather than tying `JC.Content` to a specific vendor, it could define provider contracts.

```text
JC.Content
    ↓
IContentModerationProvider
ITranslationProvider
IContentClassifier
```

Applications could supply Azure, OpenAI, Google, a local model, or a custom implementation.

Provider-specific integration packages should exist only when enough implementation justifies them.

---

# 26. AI-Assisted Content Operations

Potential future ideas:

- summarisation;
- rewriting;
- tone adjustment;
- grammar correction;
- content expansion;
- content shortening;
- classification;
- translation;
- title generation;
- keyword extraction;
- metadata generation.

These are still operations **on content**, so conceptually they can fit.

However, the package should avoid becoming `JC.AI` disguised as `JC.Content`.

A useful distinction might be:

```text
JC.Content
    ↓
defines the content operation

Provider
    ↓
decides how the operation is performed
```

Whether each abstraction is worthwhile should be driven by real use cases.

---

# 27. Content Validation

Validation may fit if it relates specifically to content characteristics.

Potential rules:

- minimum/maximum words;
- minimum/maximum characters;
- required language;
- prohibited terms;
- required phrases;
- allowed character sets;
- excessive repetition;
- maximum number of links;
- similarity thresholds;
- moderation thresholds.

Generic application model validation should remain elsewhere.

For example:

```text
"Description must contain fewer than 500 words"
```

may fit content validation.

```text
"CustomerId is required"
```

does not.

---

# 28. Content Metadata

Operations may return descriptive metadata:

- language;
- word count;
- character count;
- detected categories;
- moderation score;
- hash;
- similarity fingerprints;
- extracted links;
- extracted keywords;
- detected encoding.

This should remain separate from persistence. `JC.Content` should not assume metadata is stored in a database.

---

# 29. Hashing / Fingerprinting

Potential uses:

- duplicate detection;
- change detection;
- content identity;
- cache keys;
- revision comparison.

Simple cryptographic hashing is already easy in .NET, so a wrapper would not justify itself.

Higher-level fingerprints for fuzzy/near-duplicate content could be more interesting.

---

# 30. Structured Extraction

Possible future operations:

- extract URLs;
- extract email addresses;
- extract hashtags;
- extract mentions;
- extract quoted sections;
- detect code blocks;
- detect headings;
- identify paragraphs;
- tokenise words/sentences.

This could support moderation, analysis and conversions, but should not expand into a full NLP platform without a real requirement.

---

# 31. Search / Tokenisation

Tokenisation may make sense as a lower-level content-analysis capability.

Search itself probably does **not** belong if it means searching application-wide persisted content.

A useful boundary:

```text
Tokenise this content
```

belongs.

```text
Search all documents in the application
```

does not.

The latter introduces persistence, indexes, querying and distributed state.

---

# 32. Spell Checking / Grammar

Potentially valid, likely provider/library-backed.

Possible features:

- spelling suggestions;
- dictionary support;
- custom dictionaries;
- grammar checks;
- style warnings.

This is content-focused but can become a very large area, so established tooling would likely be preferable.

---

# 33. Content Templates

Potentially interesting but unclear.

Example:

```text
Hello {{Name}},
Your reference is {{Reference}}.
```

A template engine is technically content generation, but .NET already has many mature templating systems.

`JC.Content` should not invent its own templating language without a genuine requirement.

Possible narrower scope:

- generic template contracts;
- simple token replacement;
- content-template validation.

Low priority unless repeated project needs emerge.

---

# 34. What `JC.Content` Should Probably NOT Contain

## Web / HTTP Behaviour

Do not include:

- middleware;
- HTTP requests;
- HTTP responses;
- cookies;
- headers;
- routing;
- browser detection;
- CSP;
- Razor;
- MVC helpers;
- Tag Helpers;
- web form handling.

These belong in `JC.Web` or consuming applications.

## UI / Presentation

Do not include:

- CSS classes;
- Bootstrap;
- Tailwind;
- component rendering;
- modal/dialog behaviour;
- HTML widgets;
- WPF controls;
- WinForms controls;
- console colouring.

Content results should be neutral enough for any presentation layer to render.

## Persistence

Do not include generic content repositories, application-specific content tables, database schemas, or search indexes merely because the package processes content.

Some future feature may justify optional persistence, but that should be argued independently.

## File Storage

Do not absorb `JC.FileStorage`.

`JC.Content` may process content read from a file, but should not own:

- file paths;
- folders;
- storage quotas;
- metadata persistence;
- uploads;
- downloads;
- file lifecycle.

## Messaging / Email

Do not absorb `JC.Communication`.

`JC.Content` may process an email body or message text. It should not send email, route messages, deliver notifications, or manage conversations.

## Identity / Permissions

Do not include users, roles, authentication, authorisation, or tenant membership.

A caller may select content policies based on user or tenant context, but that context should be supplied to `JC.Content`, not owned by it.

## Full Document Management

Do not become:

- SharePoint;
- Google Docs;
- a revision repository;
- a collaborative editor;
- a document approval platform;
- a file versioning system.

If substantial document lifecycle behaviour appears, it likely deserves another package.

---

# 35. Tenancy

`JC.Content` should ideally be stateless and tenant-neutral.

Applications may still want tenant-specific policies:

```text
Tenant A
    ProfanityPolicy = Moderate

Tenant B
    ProfanityPolicy = Strict
```

The package can accept configuration or policy supplied by the application without owning tenant persistence.

If persistent content entities ever appear, they should use the existing tenancy contracts rather than inventing a separate model.

---

# 36. Sync vs Async

Local operations should remain synchronous where appropriate:

- profanity matching;
- normalisation;
- local diffing;
- word count.

Provider-backed operations will naturally be asynchronous:

- translation APIs;
- remote moderation;
- AI summarisation;
- cloud classification.

Do not make every operation asynchronous simply because some providers require it.

---

# 37. Result-Oriented APIs

For many capabilities, rich result models are preferable to primitives.

Instead of only:

```text
bool IsProfane
```

consider a moderation result.

Instead of only:

```text
string Translate(...)
```

consider a translation result.

Instead of only:

```text
string Diff(...)
```

consider a structured diff result.

Results can grow to include:

- confidence;
- matches;
- warnings;
- provider;
- language;
- changes;
- metadata;
- transformed output.

This can reduce future API breakage.

---

# 38. Avoid Over-Generalising Too Early

There will be a temptation to create something like:

```text
IContentProcessor<TInput, TOutput>
```

and force every capability through it.

That may look architecturally elegant while making real usage worse.

Profanity filtering, translation, and diffing are fundamentally different operations. Shared abstractions should emerge from actual repeated similarities.

---

# 39. Configuration Philosophy

The package should probably provide sensible defaults while allowing applications to override behaviour.

Possible registration concepts:

```text
AddContent(...)
AddProfanityFilter(...)
AddContentModeration(...)
AddTranslation(...)
```

Exact APIs are intentionally not being designed yet.

Questions:

- one root `AddContent()` registration?
- feature-specific registration only?
- options objects?
- named policies?
- provider registration?
- application-level overrides?
- per-operation options?

Applications should not have to configure capabilities they do not use.

---

# 40. Package Dependencies

A desirable direction is to keep the base package lightweight.

Possible dependency:

```text
JC.Content
    ↓
JC.Core
```

Possibly not even that if nothing is required.

Optional integrations could use separate packages if necessary:

```text
JC.Content
JC.Content.OpenAI
JC.Content.Azure
JC.Content.SomeProvider
```

Do not fragment the package pre-emptively.

---

# 41. Possible Initial Feature Set

A realistic first version may deliberately be small.

### Moderation

- robust profanity detection;
- configurable terms;
- allowlist;
- match result model;
- filtering/replacement.

### Normalisation

- Unicode;
- whitespace;
- line endings;
- configurable cleanup.

### Diffing

- word/line differences;
- neutral diff result model;
- library-backed implementation.

That may already be enough to establish a clear package identity.

---

# 42. Possible Later Features

Potential expansions:

- moderation policy engine;
- language detection;
- translation;
- similarity;
- redaction;
- classification;
- summarisation;
- Markdown utilities;
- content conversions;
- spell checking;
- grammar checking;
- AI-assisted operations;
- processing pipelines;
- neutral content document model.

None are commitments.

---

# 43. Potential Package Splits Later

If `JC.Content` grows successfully, some capabilities may eventually deserve their own package.

Possible examples:

```text
JC.Content
JC.Content.AI
JC.Content.Translation
JC.Documents
JC.Content.Markdown
```

But splitting too early would recreate the problem of making packages for the sake of packages.

Preferred approach:

> **Start cohesive. Split only when responsibilities genuinely become independently useful or dependency-heavy.**

---

# 44. Relationship with Existing JC Packages

## JC.Core

Provides foundational contracts/utilities. `JC.Content` should avoid duplicating generic helpers already appropriate for Core.

## JC.Web

Owns web-specific content concerns such as HTML sanitisation, Razor integration, browser security, and web rendering.

Neutral HTML parsing/conversion remains a design question; web behaviour does not.

## JC.Communication

Owns delivery.

Example:

```text
Message body
    ↓
JC.Content moderation/formatting
    ↓
JC.Communication delivery
```

## JC.FileStorage

Owns storage.

Example:

```text
File
    ↓
JC.FileStorage retrieves
    ↓
JC.Content analyses
```

## JC.CAP

Could become a major consumer rather than defining `JC.Content` itself.

Potential uses:

- diffing for audit/history views;
- moderation;
- support-message processing;
- content comparison;
- translation;
- policy configuration;
- generated summaries.

---

# 45. Example Cross-Package Flows

```text
User Input
    ↓
JC.Content
    ├── Normalise
    ├── Moderate
    └── Analyse
    ↓
Application
    ↓
JC.Communication
    ↓
Email / Message Delivery
```

```text
Stored Version A ─┐
                  ├── JC.Content Diff ──> Diff Result
Stored Version B ─┘
```

```text
Markdown Content
    ↓
JC.Content Conversion
    ↓
Neutral Result
    ↓
JC.Web
    ↓
Web Presentation
```

The package relationship should remain composable rather than tightly coupled.

---

# 46. Questions Worth Answering Before v1

## Scope

- Is `JC.Content` primarily text-focused or should binary/document content eventually count?
- Does Markdown belong?
- Does HTML-as-data belong?
- Does PDF belong or imply `JC.Documents`?
- Is AI-assisted processing within scope?
- Is translation core functionality or an optional provider feature?

## Moderation

- Is profanity filtering standalone or part of a wider moderation engine?
- Are built-in dictionaries supplied?
- How configurable should policies be?
- Should moderation ever automatically mutate content?

## Conversion

- Should formats convert directly to one another?
- Is a neutral document/content model worthwhile?
- How much formatting should survive conversion?

## Providers

- Should provider abstractions exist from the beginning?
- Which features genuinely need them?
- Should provider-specific integrations live in separate NuGet packages?

## Composition

- Is a content-processing pipeline useful?
- Or should applications simply call operations explicitly?

## Package Philosophy

- What is the smallest feature set that makes `JC.Content` genuinely worthwhile?
- Which ideas solve repeated application needs?
- Which are merely technically interesting?

---

# 47. Possible Definition of Done for an Initial Release

An initial release does **not** need to solve every content problem.

A successful first release should establish:

1. a clear package responsibility;
2. stable vocabulary and result models;
3. at least one or two genuinely reusable capabilities;
4. no dependency on web/UI frameworks;
5. no unnecessary persistence assumptions;
6. extension points where real future requirements are already obvious;
7. documentation explaining what belongs in the package and what does not.

---

# 48. Signs an Idea Belongs in `JC.Content`

A feature probably belongs if:

- it operates directly on content;
- it does not care where the content came from;
- it does not care where the result will be shown;
- several application types could use it;
- it provides more than a trivial wrapper around .NET;
- it fits naturally beside other content operations;
- it returns a presentation-neutral result.

Examples:

```text
"Does this text contain prohibited language?"
"How different are these two pieces of content?"
"What language is this content?"
"Translate this content."
"Normalise this content."
"Convert this representation."
"Redact these patterns."
```

---

# 49. Signs an Idea Probably Does NOT Belong

A feature probably does not belong if the question sounds like:

```text
"How should this appear in Bootstrap?"
"How do I render this in Razor?"
"How do I send this email?"
"Where should I save this file?"
"Which user can edit this?"
"How do I search the whole database?"
"How do I version this document?"
"How do I display this PDF?"
```

Those are surrounding application concerns rather than content-processing concerns.

---

# 50. Early Direction

The strongest early identity currently appears to be:

> **A framework-neutral toolkit for processing and understanding textual content.**

Likely early anchors:

- robust profanity filtering;
- wider moderation concepts;
- normalisation;
- diffing/comparison.

Likely areas to investigate afterward:

- translation abstractions;
- language detection;
- redaction;
- formatting;
- Markdown/content conversion.

Areas that should remain deliberately unresolved:

- HTML conversion;
- PDF generation;
- neutral document models;
- AI provider integrations;
- full processing pipelines.

The package should grow from real usage rather than trying to define every future capability before implementation begins.

---

# 51. Guiding Rule

When considering any future feature, ask:

> **Is this operation fundamentally about the content itself, or about the system surrounding that content?**

If it is about the content itself, it is worth considering for `JC.Content`.

If it is about transport, storage, presentation, identity, persistence, or application workflow, it probably belongs elsewhere.

---

# 52. Final Note

This document is intentionally messy in scope.

It is **not** a specification.

It is **not** an implementation plan.

It is **not** a promise that every idea will ship.

Its purpose is to give `JC.Content` enough conceptual space to be discussed properly before implementation decisions narrow the design.

Some sections may later be deleted entirely. Some may become separate packages. Some may turn out to be unnecessary. Others may become core features.

That is expected.

The most important goal at this stage is simply to establish a strong central idea:

> **JC.Content should provide reusable, application-agnostic capabilities for working with content itself.**
