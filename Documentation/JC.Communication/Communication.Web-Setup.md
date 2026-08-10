# JC.Communication.Web — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project using Razor Pages or MVC views
- JC.Core registered — both JC.Communication and JC.Web depend on it
- The JC.Communication features whose tag helpers you use: `AddNotifications` for the notification components, `AddMessaging` for the chat components. See [Notifications setup](Notifications-Setup.md) and [Messaging setup](Messaging-Setup.md)
- A CSS framework matching the one you register — Bootstrap 5, Tailwind v4, or jc-tailwind-ui — and an icon set matching the one you register, Bootstrap Icons or Font Awesome 6
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

## 0. Add the package

Add a project reference to `JC.Communication.Web`:

```xml
<ProjectReference Include="path/to/JC.Communication.Web/JC.Communication.Web.csproj" />
```

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();

// The JC.Communication features you use — both are optional and independent
builder.Services.AddNotifications<AppDbContext>();
builder.Services.AddMessaging<AppDbContext>();

// Registers the UI framework services and this package's class and icon dictionaries
builder.Services.AddCommunicationWeb();
```

### Tag helpers — `_ViewImports.cshtml`

```cshtml
@addTagHelper *, JC.Communication.Web
```

This enables `<notification-dropdown>`, `<notification-badge>`, `<notification-toast>`, `<message-thread>`, `<chat-list>`, `<chat-input>`, `<chat-participants>` and `<contact-form>`. Without it, Razor treats them as unknown HTML elements and renders them literally into the page rather than raising an error.

**`AddCommunicationWeb` is required.** Every tag helper in this package takes constructor dependencies the container cannot supply on its own — `ICommunicationFrameworkDictionary`, `ICommunicationIconDictionary` and JC.Web's `HtmlHelper`. Omitting the call fails when a page renders, not at build or startup.

### Defaults

`AddCommunicationWeb()` with no arguments registers:

| Registration | Lifetime | Value |
|---|---|---|
| `UIFrameworkService` | Singleton | `Framework` = `Bootstrap`, `IconFramework` = `Bootstrap` |
| `IWebFrameworkDictionary` | Singleton | `BootstrapDictionary` (from JC.Web) |
| `AlertHelper`, `HtmlHelper` | Singleton | from JC.Web |
| `ICommunicationFrameworkDictionary` | Singleton | `BootstrapCommunicationDictionary` |
| `ICommunicationIconDictionary` | Singleton | `BootstrapIconsCommunicationDictionary` |

All registrations use `TryAdd`, so an earlier registration wins. An application that has already called `AddUI` or `AddWebDefaults` keeps the framework it chose there, and the arguments passed here are ignored — see [Framework selection](#framework-selection-when-both-packages-register) below.

Component defaults:

| Tag helper | Default behaviour |
|------------|-------------------|
| `<notification-dropdown>` | Bell from the icon dictionary, `danger` badge, 350px list height, 360px menu width, no "View all" footer, aligned `end` |
| `<notification-badge>` | Bell from the icon dictionary, `danger` badge; the badge is omitted when the count is zero, leaving the icon alone |
| `<notification-toast>` | Top-right, auto-hide after 5000ms, body truncated at 120 characters, container id `notification-toasts` |
| `<message-thread>` | Sent bubbles `primary` on `white`, received `light` on `dark`, 500px max height with auto-scroll, reply preview truncated at 60 characters |
| `<chat-list>` | Links to `/chat/{0}`, preview truncated at 50 characters, unread badges shown in `primary` |
| `<chat-input>` | Placeholder "Type a message...", 2 rows, 4096 character limit, `primary` send button, anti-forgery token included |
| `<chat-participants>` | 5 avatars before an overflow count, 32px avatars |
| `<contact-form>` | Heading "Contact Us", `primary` submit button, 5-row message field, anti-forgery token included |

The colour defaults above are the Bootstrap ones. They live on the class dictionary, not the tag helper, so each framework supplies its own — see [Contextual colours change meaning per framework](#contextual-colours-change-meaning-per-framework).

## 2. Full configuration

### AddCommunicationWeb

```csharp
builder.Services.AddCommunicationWeb(
    framework: UIFramework.Bootstrap,
    iconFramework: IconFramework.Bootstrap);
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `framework` | `UIFramework` | `Bootstrap` | The CSS framework tag helpers render classes for |
| `iconFramework` | `IconFramework` | `Bootstrap` | The icon set tag helpers render glyphs from, chosen independently of `framework` |

Both are passed straight to JC.Web's `AddUI`. Both are `[Flags]` enums, and `UIFrameworkService` resolves a combined value to a single one in its constructor.

#### Class dictionary selected by `framework`

| `UIFramework` | Dictionary |
|--------|---------------------|
| `Bootstrap` | `BootstrapCommunicationDictionary` |
| `Tailwind` | `TailwindCommunicationDictionary` |
| `CustomJCTailwind` | `CustomJCTailwindCommunicationDictionary` |

#### Icon dictionary selected by `iconFramework`

| `IconFramework` | Dictionary |
|--------|---------------------|
| `Bootstrap` | `BootstrapIconsCommunicationDictionary` — Bootstrap Icons |
| `FontAwesome` | `FontAwesomeCommunicationDictionary` — Font Awesome 6 free, solid style |

JC.Communication.Web is the only package in the suite that registers an icon dictionary, because it is the only one whose components render a glyph.

### Framework selection when both packages register

`AddCommunicationWeb` calls `AddUI` itself, which registers through `TryAdd`. When the application also calls `AddWebDefaults` or `AddUI`, whichever runs **first** decides the framework for the whole application, and the arguments to the other call are ignored.

```csharp
// AddWebDefaults wins — Tailwind is used, and the Bootstrap argument below has no effect
builder.Services.AddWebDefaults(builder.Configuration, uiFramework: UIFramework.Tailwind);
builder.Services.AddCommunicationWeb(UIFramework.Bootstrap);
```

This is deliberate: every dictionary in the application resolves from the same `UIFrameworkService`, so they cannot disagree about which framework is in play. Pass the framework to whichever call you make first, or pass the same value to both.

### Framework-specific requirements

Bootstrap needs no setup beyond loading Bootstrap 5 and Bootstrap Icons, because Bootstrap ships finished CSS. Both Tailwind dictionaries need their classes declared, and for the same reason.

**Tailwind generates utilities by scanning source files.** This package's class names live in a compiled assembly it never reads, so `@source` over your own markup does not reach them — the components render with valid class names and no CSS behind them.

Both safelists ship in their `.nupkg`, so they reach you either way you consume the suite:

```css
/* Project reference */
@import "../path/to/JC.Web/UI/jc-web.tailwind.css";
@import "../path/to/JC.Communication.Web/jc-communication.tailwind.css";

/* Package reference — under the global packages folder */
@import "<nuget-root>/jc.web/<version>/contentFiles/any/any/jc-web.tailwind.css";
@import "<nuget-root>/jc.communication.web/<version>/contentFiles/any/any/jc-communication.tailwind.css";
```

`<nuget-root>` is `%USERPROFILE%\.nuget\packages` on Windows and `~/.nuget/packages` elsewhere. On NuGet, prefer copying both files into your own `Styles` folder — the package path carries the version number, so every upgrade breaks the import until you edit it. See [JC.Web UI setup](../JC.Web/UI-Setup.md#framework-specific-requirements).

Each file has one block per dictionary, so delete whichever you do not use.

| Framework | Requirement |
|---|---|
| `Tailwind` | Targets Tailwind v4 and assumes Preflight. Import both safelists above |
| `CustomJCTailwind` | Needs jc-tailwind-ui, and the same safelist imports. `<notification-toast>` resolves to `toast-host` and `toast`, which are in that framework's opt-in interactive layer, so `@import "jc-tailwind-ui/interactive"` is also required for toasts to be styled |

**jc-tailwind-ui is not exempt.** It compiles from source through Tailwind rather than shipping finished CSS, so the split is between *authored CSS rules* and *generated utilities*, not between frameworks. Its own component classes — `dropdown-menu`, `list-group-item`, `avatar`, `form-control`, `tone-*` — are real rules in its bundle and need nothing. Everything else does, including the theme-derived colour utilities `text-fg-muted`, `bg-surface-2` and `border-edge`: the framework's CSS reads those tokens as `var(--color-…)` inside declarations, which does not cause the matching utility to be generated.

**The safelist is best-effort.** It covers each dictionary's values and its default contextual colours. An attribute set to anything else — `badge-colour`, `button-colour`, `sent-colour`, `position`, `align` — cannot be predicted from the dictionary, so add those to your own entry CSS.

### Contextual colours change meaning per framework

Attributes such as `badge-colour`, `button-colour` and `sent-colour` are nullable. Left unset they fall back to the dictionary's default; set, their value is interpreted by the dictionary's format, so the same attribute takes a different kind of value per framework:

| Framework | Example value | Becomes |
|---|---|---|
| `Bootstrap` | `danger` | `bg-danger` |
| `Tailwind` | `red-600` | `bg-red-600` |
| `CustomJCTailwind` | `danger` | `tone-danger` — any tone the application defines, not just the built-in eight |

### Icon attributes take a complete class value

`icon` on `<notification-dropdown>` and `<notification-badge>`, and `NotificationStyle.CustomIconClass` on a stored notification, are all normalised against the configured icon set's base class before rendering.

Under Bootstrap Icons the base class is `bi`, so both `"bi-star"` and `"bi bi-star"` render correctly and values written before the icon dictionary existed keep working. Font Awesome has no equivalent base class, so values are taken exactly as given — `"fa-solid fa-star"`. That means a stored `CustomIconClass` written for Bootstrap Icons renders as-is under Font Awesome and shows nothing; those values need migrating when the icon set changes.

### Registering a different dictionary

To override either dictionary — a fourth framework, a different icon set, or a house style — register your own implementation before calling `AddCommunicationWeb`. `TryAdd` means the first registration stands:

```csharp
builder.Services.AddUI(UIFramework.Bootstrap, IconFramework.Bootstrap);
builder.Services.AddFrameworkDictionary<ICommunicationFrameworkDictionary>(
    _ => new HouseStyleCommunicationDictionary());

builder.Services.AddCommunicationWeb();
```

See [JC.Web UI setup](../JC.Web/UI-Setup.md#addframeworkdictionary--registering-another-packages-dictionary) for `AddFrameworkDictionary` and `AddIconDictionary`.

## 3. Verify

1. Add `<contact-form endpoint="/contact" />` to a view — it should render a styled form, not literal `<contact-form>` text. Literal text means `@addTagHelper` is missing; an `InvalidOperationException` naming `ICommunicationFrameworkDictionary` or `HtmlHelper` means `AddCommunicationWeb` was not called.
2. Add `<notification-badge />` to your layout and send yourself a notification — the count should appear.

## Next steps

- [Guide](Communication.Web-Guide.md) — every tag helper with examples, the framework dictionaries, and the JavaScript each component expects.
- [API Reference](Communication.Web-API.md)
