# Documentation Writing Guide
**Note: All documentation is AI generated**

Standards and templates for writing JC-Packages documentation. Each package gets a dedicated folder under `/Documentation/JC.{Package}/` containing a consistent set of documents.

## Updating Package Documentation

You will need to update/verify the following documentation when you make changes to a package (using JC.Core as an example):
- The global [README](../README.md) file.
- The package's [README](../JC.Core/README.md) file.
- The package's [Setup.md](JC.Core/Setup.md) file.
- The package's [Guide.md](JC.Core/Guide.md) file.
- The package's [API reference](JC.Core/API.md) file.

Some packages have split setup-guide-api documentation based on the package's area/feature it implements.

**The codebase is the only source of truth.** Verify every signature, default, namespace and behavioural claim against the source before writing it down. Do not carry a statement across from an existing document, a release note or a design document without re-checking it — those go stale, and a wrong default is worse than a missing one.

**Never describe a previous version.** Documentation states what the package does now. Migration guidance belongs in the release notes for the major version that introduced the change, and nowhere else.

**Document each thing once.** Where two packages share behaviour, document it in the package that owns it and link from the other with a sentence saying where it lives. A reader opening two files is a smaller cost than two copies drifting apart.

## Document Structure

Each package folder contains the following documents, written in the order listed below:

### 1. Setup.md

**Purpose:** Take a consumer from zero to a fully configured integration, covering both the quick path and every available option.

**Audience:** Developers adding the package to an existing ASP.NET Core project.

**Tone:** Direct, informative. Assume the reader knows .NET. Explain what the code does, not how .NET works.

#### Required Sections

```markdown
# {Package} — Setup

## Prerequisites
- .NET 9 SDK, and any package-specific requirements.
- Link back to root README installation instructions.

## 0. Add the package
- Project reference or local NuGet feed reference.
- Show the .csproj snippet.
- Link to the [Versioning Strategy](../../README.md#versioning-strategy) so consumers know which version to pick.

## 1. Quick setup
- The minimal `Program.cs` code to get going using all defaults.
- **Explicitly state what the defaults are** and what behaviour the consumer should expect without any configuration.
- If the package has optional features (e.g. rate limiting in JC.Web), include them here with a comment marking them as opt-in.
- If middleware is needed, include the `app.Use...()` calls with notes on ordering.
- If `appsettings.json` config is required even for the default path, include it inline.

## 2. Full configuration
- Walk through **every** registration method, parameter, and overload.
- For each option/parameter: explain what it does, what the default value is, and show a code example of changing it.
- Group by feature area if the package has multiple (e.g. security headers, cookies, client profiling, rate limiting).
- Optional/opt-in features that appeared briefly in quick setup get their full treatment here with all config options.
- Include `appsettings.json` examples for any configuration-driven options.

## 3. Apply migrations (if applicable)
- If the package introduces DbContext changes, entities, or tables, explain what migrations are needed.
- Show the `dotnet ef` commands.

## 4. Verify
- A quick smoke test the consumer can run to confirm everything is working.
- Keep to 1-3 steps max.

## Next steps
- Link to the full guide and API reference for the package.
```

Omit section 3 where the package introduces no entities or schema, and renumber Verify accordingly rather than leaving a gap.

#### Key Rules

- **Defaults must be documented.** Every registration method's default behaviour must be explicitly stated. The reader should know exactly what happens if they call the method with no arguments.
- **Every option must be documented.** Every parameter, configuration property, and overload must be covered in full configuration. Nothing should be discoverable only through IntelliSense.
- **Opt-in features in quick setup.** If a feature is optional (not included in the defaults convenience method), still show it in quick setup with a clear comment that it's opt-in. Then cover it fully in full configuration.
- **Show complete, copy-pasteable code.** Use `// ...existing code...` to indicate where the snippet fits in a larger file, but never show partial statements.
- **Code samples must compile.** Check constructor signatures, generic arity and constraints against the source — a sample that names a type parameter the method no longer takes is worse than no sample.
- **One code block per concept.** Don't split a single registration across multiple fenced blocks unless showing different files (e.g. `Program.cs` and `appsettings.json`).
- **Configuration examples** use `appsettings.json` with placeholder values. Never include real credentials.
- **Full configuration code examples must show options being set to their default values** so the reader can see what the defaults are directly in the code. If a default is `null` or empty, use a suitable example value instead.

#### Reference example

[Documentation/JC.Identity/Setup.md](JC.Identity/Setup.md) is the reference for this document type. All package Setup.md files should follow that level of detail.

It is linked rather than reproduced here so there is one copy to maintain. Read it alongside these rules before writing a new one, and note in particular how it handles behaviour owned by another package — the shared identity runtime is linked to rather than restated.

---

### 2. Guide.md

**Purpose:** A comprehensive how-to and usage guide. Teaches the consumer how to actually use the features they registered in Setup. Heavy on examples, explains nuances and edge cases.

**Audience:** Developers who have completed setup and want to use the package's features in their application.

**Tone:** Practical, example-driven. Show the code first, then explain the "why" and the gotchas. Assume the reader has completed Setup.md.

#### Required Sections

```markdown
# {Package} — Guide

One or two sentences on what this guide covers. Link back to [Setup](Setup.md) for registration.

## {Feature area}

For each feature area in the package, create a top-level section. Within each section:

### Basic usage
- The simplest, most common way to use the feature.
- A complete, copy-pasteable code example.
- Brief explanation of what happens.

### Advanced usage / scenarios
- Less common patterns, overloads, or combinations.
- Code examples for each scenario.
- Explain **when** you'd use this over the basic approach.

### Nuances and gotchas
- Edge cases, ordering requirements, things that aren't obvious.
- Common mistakes and how to avoid them.
- Behaviour differences between modes/options.
```

#### Key Rules

- **Examples first, explanation second.** Show a working code block, then explain what it does and why. Don't make the reader wade through paragraphs before seeing code.
- **Every public method/service/helper gets at least one example.** If the consumer can call it, show them how.
- **Explain nuances inline.** Don't save gotchas for a footnote — put them right next to the code they affect. Use a short bold note or a sentence after the code block.
- **Group by feature, not by class.** Organise around what the consumer is trying to do (e.g. "Soft-delete and restore", "Cookie management"), not around internal class structure.
- **Show realistic scenarios.** Use examples that look like real application code — controllers, services, Razor pages — not abstract `Foo`/`Bar` samples.
- **Don't repeat Setup.** Don't re-document registration methods or option defaults. Link to Setup.md if the reader needs to change configuration.
- **Cover interactions between features.** If two features work together (e.g. `IUserInfo` with audit trail, `RequestMetadata` with bot filtering), show the combined usage.
- **Code blocks should be self-contained.** Each example should make sense on its own without needing to read three other examples first. Include enough context (constructor injection, class declaration) to be clear.

#### Reference example

[Documentation/JC.Core/Guide.md](JC.Core/Guide.md) is the reference for this document type — repository usage, soft-delete, pagination and helpers, each with a basic case followed by the nuances.

[Documentation/JC.Identity.Shared/Guide.md](JC.Identity.Shared/Guide.md) is a second example worth reading for its "Nuances and gotchas" subsections, which state the failure rather than the principle.

---

### 3. API.md

**Purpose:** A complete reference of every public and protected type, property, and method in the package. Functions as written XML documentation — no code examples, just signatures, parameter descriptions, and behavioural explanations.

**Audience:** Developers who already understand the package (from Setup and Guide) and need a quick reference for exact method signatures, parameter names, defaults, and return types.

**Tone:** Precise, reference-style. Every statement should be factual and verifiable against the source code. Describe what each member does and how it behaves, not how to use it (that's Guide.md's job).

#### Required Sections

```markdown
# {Package} — API reference

One sentence stating what this document covers. Link back to [Setup](Setup.md) and [Guide](Guide.md).

> **Note:** Registration extensions (`IServiceCollection`, `IServiceProvider`, `IApplicationBuilder`) and options classes are documented in [Setup](Setup.md), not here.

## Models

Domain/database models first (entities, base classes), then any other model classes (pagination, DTOs, etc.). Never include options classes here — those belong in Setup.md.

## ViewModels / Input models

If the package defines any view models or input models. Omit this section if none exist.

## Enums

All public enums in the package.

## Services

All public services, including interfaces with in-package implementations (documented together under the implementation name).

## Controllers

If the package defines any controllers. Omit this section if none exist.

## Helpers

All public static helper classes. For web packages: non-UI helpers first, then UI helpers, then tag helpers.

## Extensions

All public extension method classes, excluding registration extensions (those are in Setup.md).

## Data

DbContext interfaces, implementations, and data mappings (e.g. `IEntityTypeConfiguration<T>` classes).
```

Within each section, individual classes follow this structure:

```markdown
## {ClassName}

**Namespace:** `Full.Namespace.Here`

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Name` | `string` | `""` | get; set; | One-sentence description. |
| `IsEnabled` | `bool` | `true` | get; private set; | One-sentence description. |

### Methods

#### MethodName(Type param1, Type param2 = defaultValue)

**Returns:** `ReturnType`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `param1` | `Type` | — | What this parameter controls. |
| `param2` | `Type` | `defaultValue` | What this parameter controls. |

One or two paragraphs describing the method's behaviour — what it does step by step, what side effects it has, what exceptions it may throw, and any conditional logic. This replaces XML summary/remarks comments.
```

#### Key Rules

- **No code examples.** API.md is a reference, not a tutorial. Code belongs in Guide.md.
- **Every public and protected member must be documented.** If a consumer can see it or override it, it belongs here. Internal and private members are excluded.
- **Exclude registration extensions and options classes.** `IServiceCollection`, `IServiceProvider`, and `IApplicationBuilder` extension methods (e.g. `AddCore`, `UseIdentity`) and their associated options classes (e.g. `CoreBackgroundJobOptions`, `NotificationOptions`) are already fully documented in Setup.md — do not repeat them here.
- **Where an `IServiceProvider` extension is a runtime operation rather than registration, document it here and say why.** The exclusion above exists to avoid duplicating Setup, not to hide behaviour that has no other home.
- **Method signatures must be exact.** Show the correct method name, all parameters in order, their types, and default values. If a parameter has no default, use `—` in the default column.
- **Combine interfaces with their implementations.** If `IFoo` is implemented by `Foo` in the same package, document them together under `Foo` (or whichever name is more recognisable). Note which type the consumer injects. Only document standalone interfaces (those with no in-package implementation) separately.
- **Document access modifiers on properties.** If a property has a public get but a private or internal set (or vice versa), show this in the Access column (e.g. `get; internal set;`).
- **Describe method behaviour, not usage.** Explain the flow: what the method checks, what it creates, what it persists, what it returns, what side effects occur. Think of it as the XML `<summary>` and `<remarks>` tags combined into prose.
- **Call out inconsistencies rather than smoothing them over.** A method that omits a `CancellationToken` its siblings take, or takes a parameter they do not, is exactly what a reference is for.
- **State included navigation properties.** If a method eagerly loads EF Core navigation properties (via `.Include()`), list them in the method description. This tells the consumer exactly what's materialised without needing to check the source.
- **Group by category, then by class.** API.md is organised into top-level sections (Models, Enums, Services, Helpers, Extensions, Data, etc.) with individual classes documented under the appropriate section. Within each section, every member of a class appears together under that class heading. Omit any top-level section that has no entries for the package.
- **Always include the namespace.** Every class, interface, enum, and record heading must state its full namespace (e.g. `**Namespace:** \`JC.Core.Models\``). Verify against the source code — never guess.
- **Enums get a simple value table.** List each member with its integer value (if non-default) and a one-sentence description.
- **Extension method classes are documented as their own section.** Group all extension methods under the static class name, with each method as a sub-heading.
- **Inheritance.** If a class extends a base class from the same package, note this but don't re-document inherited members — refer the reader to the base class section.

#### Reference example

[Documentation/JC.Tenancy/API.md](JC.Tenancy/API.md) is the reference for this document type — every section shape in one file, with the behavioural prose that replaces XML comments.

[Documentation/JC.Github/API.md](JC.Github/API.md) is a smaller one to read first if the above is too much at once.

---

## General Writing Rules

1. **British English** spelling where it differs (e.g. "colour" not "color" in prose — code identifiers stay as-is).
2. **No emojis** in documentation.
3. **Headers use sentence case** (e.g. "Register services", not "Register Services") — except for proper nouns and package names.
4. **Code blocks** always specify the language (`csharp`, `json`, `xml`, `bash`).
5. **Links** between documents use relative paths.
6. **Keep files focused.** Each document answers one question. Setup answers "how do I add this and configure it?", Guide answers "how do I use the features?", API answers "what's available?".
7. **Version-agnostic.** Don't reference specific version numbers in docs — the consumer is expected to use the version they've pulled.
