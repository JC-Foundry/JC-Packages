# UI Redesign — Framework Generalisation

**Status:** In progress — JC.Web core complete, downstream packages not yet converted
**Target release:** v6 (major, breaking changes green-lit suite-wide)
**Last updated:** 2026-08-07

---

## Goal

JC.Web's tag helpers and HTML builders emit hardcoded Bootstrap 5 class names. The aim is to move
those strings behind a per-framework dictionary so the same components can render Bootstrap,
Tailwind, or an in-house Tailwind variant.

Bootstrap is the reference implementation and must reproduce the previous output exactly. Success
is measured by how cheap the *second* framework is: adding Tailwind should be one new dictionary
class and one switch arm, with no changes to any tag helper, builder, or consuming package.

---

## Decisions taken

### 1. `UIFramework` stays a flags enum, with corrected values

```csharp
[Flags]
public enum UIFramework
{
    Bootstrap        = 1,
    Tailwind         = 2,
    CustomJCTailwind = 4
}
```

Originally `0, 1, 2`, which is broken for flags — `Bootstrap = 0` made `HasFlag(Bootstrap)` return
`true` for every value, since `x & 0 == 0` always holds. Values are now powers of two.

A consumer may declare several frameworks; `UIFrameworkService` resolves to the most specific,
precedence `CustomJCTailwind > Tailwind > Bootstrap`. **Resolution happens once, in the service
constructor**, so nothing downstream ever handles unresolved flags — anything reading
`UIFrameworkService.Framework` can switch on it directly. `GetUIFramework` is private for that
reason.

### 2. No options class — the framework is passed directly

`UIFrameworkOptions` was removed. `AddUi(UIFramework)` takes the enum and constructs the singleton
with it. This is simpler than an options callback and means the value is known at registration
rather than on first resolution.

### 3. One dictionary per package, not one shared dictionary

Rejected: a single `IFrameworkDictionary` in JC.Web listing every class name for every package.

That inverts the dependency — adding one tag helper to JC.Communication.Web would require editing
the interface in JC.Web, updating all three dictionary implementations, and releasing JC.Web before
the Communication change could ship. The lower-level package ends up knowing about every package
above it and gating unrelated releases.

Instead, `IFrameworkDictionary` is an empty marker. Each package declares its own contract deriving
from it and registers it with `AddFrameworkDictionary<T>`. Every dictionary is selected from the
same `UIFrameworkService.Framework`, so they cannot disagree about which framework is in play.

### 4. Grouped records, not flat members

Class names are grouped per component (`AlertClasses`, `BreadcrumbClasses`, …) and exposed as
properties on the package contract, rather than as flat members like `AlertSuccess`,
`BreadcrumbItem`.

This is what keeps the design from becoming a versioning problem. The records are types we own, not
things implementers write, and every property defaults to `""` — so **adding a class name to an
existing record compiles against every existing dictionary**, rendering without it until filled in.
Only adding a whole new component group is breaking, and that is far rarer. Flat members would have
made every single addition a breaking change.

### 5. Values are complete, not compositional

`ActiveItem` holds `"breadcrumb-item active"`, not just the `"active"` modifier.

Only Bootstrap builds states by appending a modifier to a base class. A framework whose active item
shares nothing with its inactive one can still express that if the finished value is stored. This is
the main place Bootstrap's conventions could have leaked into the abstraction.

Every property holds a **whole class attribute value**, not a single token —
`"alert-dismissible fade show"` is three classes in one string.

### 6. Stateless helpers become singletons; stateful builders take the dictionary

| Type | Treatment | Why |
|---|---|---|
| `AlertHelper` | DI singleton | Stateless |
| `HtmlHelper` | DI singleton | Stateless |
| `BreadcrumbBuilder` | Constructor parameter | Accumulates per-use state |
| `TableBuilder<T>` | Constructor parameter | Per-use state, and generic |

`HtmlTagBuilder` stays framework-agnostic. Its `AddActiveAttribute()` and `AddDisabledClass()` were
removed because they hardcoded `"active"` and `"disabled"`; callers now pass
`dictionary.State.Active` / `.Disabled` through `AddClass`, which already ignores empty values, so
an unset dictionary entry no-ops cleanly. `AddCurrentPageAttribute()` remains — `aria-current` is an
ARIA attribute, identical across frameworks.

### 7. Classes only — behavioural markup is out of scope for now

`data-bs-dismiss`, `data-bs-toggle` and similar are deliberately left hardcoded. See
[Known gaps](#known-gaps).

---

## Architecture

```
AddUi(UIFramework.Tailwind)
        │
        ├── UIFrameworkService (singleton)
        │       └── Framework  ← resolved once, single value
        │
        └── AddFrameworkDictionary<IWebFrameworkDictionary>(f => f switch { ... })
                └── IWebFrameworkDictionary (singleton)
                        ├── Alert       : AlertClasses
                        ├── Breadcrumb  : BreadcrumbClasses
                        ├── Pagination  : PaginationClasses
                        ├── Table       : TableClasses
                        └── State       : StateClasses
                                │
        ┌───────────────────────┴───────────────────────┐
        │                                               │
  Singletons                                   Constructed per use
  AlertHelper, HtmlHelper                      BreadcrumbBuilder, TableBuilder<T>
        │                                               │
  Tag helpers inject them                       Callers inject the dictionary
```

The registration helper:

```csharp
public static IServiceCollection AddFrameworkDictionary<TDictionary>(
    this IServiceCollection services,
    Func<UIFramework, TDictionary> factory)
    where TDictionary : class, IFrameworkDictionary
{
    services.TryAddSingleton(sp =>
        factory(sp.GetRequiredService<UIFrameworkService>().Framework));

    return services;
}
```

---

## What is done

### New files — `JC.Web/UI/Framework/`

| File | Contents |
|---|---|
| `UIFramework.cs` | `UIFramework` enum, `UIFrameworkService` with resolve-once constructor |
| `IFrameworkDictionary.cs` | Empty marker interface |
| `IWebFrameworkDictionary.cs` | JC.Web's contract — five component groups |
| `FrameworkClasses.cs` | `AlertClasses`, `BreadcrumbClasses`, `PaginationClasses`, `TableClasses`, `StateClasses` |
| `BootstrapDictionary.cs` | Bootstrap 5 values, extracted verbatim from the previous hardcoded strings |

### Modified — `JC.Web`

| File | Change |
|---|---|
| `Extensions/ServiceCollectionExtensions.cs` | `AddUi` registers the service, dictionary, `AlertHelper`, `HtmlHelper`; new `AddFrameworkDictionary<T>`; both `AddWebDefaults` overloads take `uiFramework` and call `AddUi` |
| `UI/HTML/AlertHelper.cs` | Static class → injectable singleton taking `IWebFrameworkDictionary` |
| `UI/HTML/HtmlHelper.cs` | Static class → injectable singleton; added `PaginationListClass` / `PaginationNavClass` |
| `UI/HTML/BreadcrumbBuilder.cs` | Constructor takes the dictionary; `Build()` reads from it |
| `UI/HTML/TableBuilder.cs` | Constructor takes the dictionary; `Build()` falls back to `Table.Table` |
| `UI/HTML/HtmlTagBuilder.cs` | Removed `AddActiveAttribute()` and `AddDisabledClass()` |
| `UI/TagHelpers/AlertTagHelper.cs` | Injects `AlertHelper` |
| `UI/TagHelpers/PaginationTagHelper.cs` | Injects `HtmlHelper`; `BuildEllipsis` no longer static |

### Bootstrap values captured

Taken from the code as it was, so output is unchanged:

| Group | Values |
|---|---|
| Alert | `alert`, `alert-dismissible fade show`, `btn-close`, variants `alert-success` / `alert-warning` / `alert-danger` / `alert-info` |
| Breadcrumb | List `breadcrumb`, Item `breadcrumb-item`, ActiveItem `breadcrumb-item active`, Nav empty |
| Pagination | List `pagination`, Item `page-item`, ActiveItem `page-item active`, DisabledItem `page-item disabled`, Link `page-link` |
| Table | Table `table` |
| State | Active `active`, Disabled `disabled` |

---

## Current state — build is red

**1 error in JC.Web:**

```
UI/TagHelpers/BreadcrumbTagHelper.cs(40,27): CS7036
  no argument for required parameter 'dictionary' of BreadcrumbBuilder(IWebFrameworkDictionary)
```

`BreadcrumbTagHelper` needs to inject `IWebFrameworkDictionary` and pass it to
`new BreadcrumbBuilder(dictionary)`. It had been changed and has since reverted to the
parameterless call.

**9 downstream files still calling `HtmlHelper` statically** — these are currently masked because
JC.Web fails first, but each will surface as `CS0120` once it builds:

| File | Call sites |
|---|---|
| `JC.Communication.Web/TagHelpers/NotificationDropdownTagHelper.cs` | 20 |
| `JC.Communication.Web/TagHelpers/MessageThreadTagHelper.cs` | 15 |
| `JC.Communication.Web/TagHelpers/ChatListTagHelper.cs` | 15 |
| `JC.Communication.Web/TagHelpers/ChatInputTagHelper.cs` | 13 |
| `JC.Communication.Web/TagHelpers/ContactFormTagHelper.cs` | 13 |
| `JC.Communication.Web/TagHelpers/NotificationToastTagHelper.cs` | 10 |
| `JC.Communication.Web/TagHelpers/ChatParticipantsTagHelper.cs` | 3 |
| `JC.Communication.Web/TagHelpers/NotificationBadgeTagHelper.cs` | 3 |
| `JC.FileStorage.Web/TagHelpers/UploadConstraintsTagHelper.cs` | 1 |

None of these has an existing constructor, so each takes a primary constructor
`(HtmlHelper html)` and `HtmlHelper.` becomes `html.`.

Two notes for that pass:

- `ChatParticipantsTagHelper.GetInitials` is `private static` — check whether it touches `html`
  before deciding if `static` must go.
- `ContactFormTagHelper.cs` and `UploadConstraintsTagHelper.cs` are missing
  `using JC.Web.UI.HTML;`.

---

## Next steps

1. **Fix `BreadcrumbTagHelper`** — inject `IWebFrameworkDictionary`, pass to `BreadcrumbBuilder`.
   Unblocks the JC.Web build.
2. **Convert the 9 downstream tag helpers** to inject `HtmlHelper`.
3. **Ensure downstream registration pulls in `AddUi`.** `JC.Communication.Web` and
   `JC.FileStorage.Web` tag helpers will now fail to resolve `HtmlHelper` at runtime unless `AddUi`
   has been called. Their own `AddX` methods should call `services.AddUi()` — `TryAdd` semantics
   make it harmless if the consumer already called it. **Without this the failure is runtime, not
   compile-time.**
4. **Verify Bootstrap output is byte-identical** to before the change — render each tag helper and
   builder and diff against the previous markup. This is the safety net for the whole refactor.
5. **Rename `AddUi` → `AddUI`** to match `UIFramework`, `UIFrameworkService`,
   `IWebFrameworkDictionary`, and .NET's two-letter acronym convention. v6 is the moment.
6. **Update the JC.Web UI docs** — see [Documentation impact](#documentation-impact).

---

## Known gaps

### Behavioural markup has no Tailwind equivalent

Bootstrap components carry JS contracts, not just classes:

```html
<button class="btn-close" data-bs-dismiss="alert"></button>
```

Tailwind ships no JavaScript, so a dismissible alert there needs its own handler. A pure *class*
dictionary cannot express this. Currently `data-bs-dismiss` is hardcoded in `AlertHelper`.

Two options when this is picked up: let the records carry a small number of attribute strings
alongside the classes (empty for Tailwind), or accept that interactive components ship
self-contained JS — which is what `BugReporterTagHelper` already does, making it framework-agnostic
by construction and the better precedent.

### `BugReporterTagHelper` is not converted

It uses Bootstrap's contextual-colour composition — `border-{colour}`, `text-{colour}`,
`btn-{colour}` built from a runtime string. Tailwind has no equivalent composition; "danger" becomes
something like `bg-red-600 hover:bg-red-700 text-white`.

This needs a semantic-colour decision (a fixed set of intents mapped per framework) rather than a
straight class lookup, so it was deliberately left out of the first pass.

### Only Bootstrap exists

`AddUi` currently registers `BootstrapDictionary` unconditionally. `TailwindDictionary` and
`CustomJCTailwindDictionary` become additional switch arms:

```csharp
services.AddFrameworkDictionary<IWebFrameworkDictionary>(f => f switch
{
    UIFramework.Tailwind         => new TailwindDictionary(),
    UIFramework.CustomJCTailwind => new CustomJCTailwindDictionary(),
    _                            => new BootstrapDictionary()
});
```

Selecting Tailwind today silently renders Bootstrap.

### No tests

Nothing guards the Bootstrap output. Given the whole point is that markup is unchanged, a snapshot
test per component would be the highest-value thing to add — it turns step 4 above from a manual
check into a permanent guarantee.

---

## Documentation impact

`Documentation/JC.Web/UI-Setup.md`, `UI-Guide.md` and `UI-API.md` were written earlier in this work
and **now describe the pre-refactor API**. They state that the UI area registers nothing in the
container and that helpers are static. Both are now false. They need revisiting once the conversion
settles — not before, or they will need doing twice.

Specifically stale:

- "There is no `AddUI` registration" — there is now, and it registers four things.
- `AlertHelper` and `HtmlHelper` documented as static classes.
- `BreadcrumbBuilder` / `TableBuilder<T>` examples use parameterless constructors.
- `HtmlTagBuilder` documents `AddActiveAttribute` and `AddDisabledClass`, both removed.
- Nothing documents `UIFramework`, `UIFrameworkService`, `IFrameworkDictionary`,
  `IWebFrameworkDictionary`, the class records, or `AddFrameworkDictionary`.

Also outstanding from the wider documentation task, unrelated to this work: the SEO trio
(`SEO-Setup.md`, `SEO-Guide.md`, `SEO-API.md`) has not been written, and the monolithic
`Documentation/JC.Web/Setup.md`, `Guide.md` and `API.md` still need deleting once all four area
trios exist, with inbound links repointed.
