# UI Redesign — Framework Generalisation

**Status:** JC.Web complete across all three frameworks. Downstream packages are Bootstrap-only.
**Target release:** v6 (major, breaking changes green-lit suite-wide)
**Last updated:** 2026-08-07

Session handover note — what is built, what was decided, what is left.

---

## Goal

Move hardcoded framework class names behind a per-package dictionary so the same components render
Bootstrap, Tailwind, or jc-tailwind-ui. Success is measured by how cheap the *second* framework is:
adding one should be a new dictionary class and a switch arm, with no tag helper, builder or
consuming-package changes. That has held — Tailwind and CustomJCTailwind for JC.Web each cost exactly
one file plus one switch arm.

---

## Architecture

```
AddUI(UIFramework, IconFramework)
        │
        ├── UIFrameworkService (singleton)
        │       ├── Framework      ← resolved once
        │       └── IconFramework  ← resolved once, independent choice
        │
        ├── AddFrameworkDictionary<T>(f => …)   selects on Framework
        └── AddIconDictionary<T>(f => …)        selects on IconFramework
```

`IFrameworkDictionary` and `IIconDictionary` are empty markers. Each package declares its own
contract, so adding a component never requires a JC.Web change or release.

---

## Decisions

1. **Values are complete, not compositional.** `ActiveItem` holds `"page-item active"`, not `"active"`.
   Every property is a whole class attribute value and defaults to `""`, so adding a property to a
   record is non-breaking — only adding a whole new group to a contract breaks.

2. **`{0}` means whatever that framework's format says it means.** Bootstrap writes `border-{0}` and is
   given `danger`; Tailwind writes `border-{0}` and is given `red-600`; jc-tailwind-ui writes
   `tone-{0}`. No shared colour-mapping machinery — this was tried and reverted as over-engineering.

3. **Icons are a separate choice from CSS.** A Tailwind app may still use Bootstrap Icons. Icon values
   are complete too (`"bi bi-bell"`, not `"bi-bell"`) since Font Awesome shares no base class.
   `IconClass.WithBase` normalises caller-supplied values so legacy `"bi-star"` still works.

4. **Contextual colour defaults live on the dictionary**, not the tag helper — `"danger"` is a
   Bootstrap name, not a universal one. Tag helper colour properties are nullable and fall back.

5. **jc-tailwind-ui uses the tone engine, not its Bootstrap shorthands.** Shorthands cover only its 8
   built-in types; a tone works for any colour the app defines. Framework's own stated preference.

6. **`data-bs-*` stays, as a documented contract.** Under Bootstrap these are consumed by Bootstrap's
   own JS, so renaming them would force us to ship JS for Bootstrap users too. Non-Bootstrap consumers
   implement matching behaviour themselves. `NotificationToastTagHelper` is the exception: it injects
   `UIFrameworkService` and omits its `bootstrap.Toast` script entirely when the framework is not
   Bootstrap, since that one is a hard global dependency, not an attribute.

---

## What is done

| Package | Class dictionaries | Icon dictionary |
|---|---|---|
| JC.Web | Bootstrap, Tailwind, CustomJCTailwind | none — no component renders a glyph |
| JC.Communication.Web | Bootstrap only | Bootstrap Icons only |
| JC.FileStorage.Web | Bootstrap only | none |

- All tag helpers in all three packages read from dictionaries. No class literals remain outside them
  except `data-bs-*` (decision 6).
- `AddCommunicationWeb` and `AddFileStorageWeb` both call `AddUI` and register their dictionaries.
- `NotificationUIHelper` (JC.Communication) **deleted** — it lived in a package that cannot reference
  JC.Web. Its icons moved to `CommunicationIcons`, its colours to `NotificationTypeClasses`.
- JC.Web docs (`UI-Setup.md`, `UI-Guide.md`, `UI-API.md`) rewritten against the code.
- Whole solution builds clean.

---

## Outstanding

1. **Tailwind + CustomJCTailwind dictionaries for JC.Communication.Web** — 8 component groups, plus
   `NotificationTypeClasses`. Selecting Tailwind today silently renders Bootstrap classes.
2. **Tailwind + CustomJCTailwind dictionaries for JC.FileStorage.Web** — one group, `UploadConstraints`.
   Trivial.
3. **FontAwesome icon dictionary for JC.Communication.Web** — the only package with icons.
4. **Document the `data-bs-*` contract** — agreed but not written. Two categories: declarative
   attributes (`data-bs-dismiss`, `data-bs-toggle`, `data-bs-autohide`, `data-bs-delay`) which a
   consumer can shadow with their own JS, and the toast, which needs replacement rather than shadowing.
5. **Docs for JC.Communication.Web and JC.FileStorage.Web** not yet updated for any of this.
6. **No tests.** Nothing pins "Bootstrap output is unchanged" beyond a literal-by-literal diff done by
   hand. A snapshot test per component is still the highest-value addition.
7. **Nothing has been rendered in a browser.** The Bootstrap path is unchanged and safe; the two
   Tailwind paths are reasoned from source, not observed.

---

## Known gaps

**Tailwind purging.** `TailwindDictionary`'s classes live in a compiled assembly that Tailwind's
scanner never reads, so `@source` over app markup does not reach them — the app needs a file repeating
those values, or the stylesheet comes out empty. `CustomJCTailwindDictionary` barely suffers this
because jc-tailwind-ui ships its own CSS; only a handful of stock utilities need declaring. Worth
considering whether the package should ship a companion CSS/safelist file.

**Breaking changes for consumers.** The `icon=` attributes and `NotificationStyle.CustomIconClass` now
take complete class values, though `IconClass.WithBase` keeps bare `bi-*` working under Bootstrap
Icons. `AddUi` was renamed `AddUI`.

**Also outstanding, unrelated:** the monolithic `Documentation/JC.Web/Setup.md`, `Guide.md` and
`API.md` still carry pre-refactor claims and need deleting once the SEO trio exists; the SEO trio
(`SEO-Setup.md`, `SEO-Guide.md`, `SEO-API.md`) has not been written.
