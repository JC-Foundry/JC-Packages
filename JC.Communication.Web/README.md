# JC.Communication.Web

ASP.NET Core Razor tag helpers rendering [JC.Communication](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.Communication)'s features — notifications, messaging and a contact form. No class name or icon is hardcoded: both come from dictionaries selected by the CSS framework and icon set you register, which are independent choices.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.Communication.Web/JC.Communication.Web.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

## Prerequisites

- .NET 9.0 SDK, and an ASP.NET Core project using Razor Pages or MVC views
- **JC.Communication**, with the features whose tag helpers you use — `AddNotifications` for the notification components, `AddMessaging` for the chat components
- **JC.Web**, which supplies the UI framework services
- A CSS framework matching the one you register: Bootstrap 5, Tailwind v4, or jc-tailwind-ui
- An icon set matching the one you register: Bootstrap Icons or Font Awesome 6

## Quick start

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.AddNotifications<AppDbContext>();
builder.Services.AddMessaging<AppDbContext>();

// Registers the UI framework services and this package's class and icon dictionaries
builder.Services.AddCommunicationWeb();
```

**`AddCommunicationWeb` is required.** Every tag helper here takes constructor dependencies the container cannot otherwise supply, and omitting the call fails when a page renders — not at build or start-up.

### Tag helpers — `_ViewImports.cshtml`

```cshtml
@addTagHelper *, JC.Communication.Web
```

### Configuration — `appsettings.json`

None. Everything is configured in code.

## Feature areas

### Notifications

```cshtml
<notification-dropdown view-all-href="/notifications" />
<notification-badge />
<notification-toast model="@Model.Latest" />
```

The dropdown reads unread notifications from `NotificationCache`, orders them newest first, and caps the count badge at `99+`. Each item's icon and colour derive from the notification's `NotificationType`, with per-notification overrides on `NotificationStyle` taking precedence independently.

### Messaging

```cshtml
<message-thread model="@Model.Chat" current-user-id="@userInfo.UserId" />
<chat-list model="@Model.Chats" href-format="/messages/{0}" />
<chat-input endpoint="/messages/send" thread-id="@Model.Chat.ThreadId" />
<chat-participants model="@Model.Chat" />
```

`<chat-list>` computes unread counts from the current user's read logs in a single query across every thread in the model. Set `show-unread="false"` to skip the database entirely.

### Contact form

```cshtml
<contact-form endpoint="/contact" heading="Get in touch" />
```

Posts in the `ContactInputModel` shape — bind it directly:

```csharp
public async Task<IActionResult> OnPostAsync(ContactInputModel input)
{
    if (!ModelState.IsValid) return Page();
    // input.Email, input.Subject, input.Message
    return RedirectToPage("ThankYou");
}
```

### Choosing a framework

```csharp
builder.Services.AddCommunicationWeb(UIFramework.CustomJCTailwind, IconFramework.FontAwesome);
```

| `UIFramework` | Classes |
|---------------|---------|
| `Bootstrap` | Bootstrap 5 |
| `Tailwind` | Tailwind v4 utilities, reproducing Bootstrap's appearance |
| `CustomJCTailwind` | jc-tailwind-ui, using its tone engine so any colour composes |

| `IconFramework` | Icons |
|-----------------|-------|
| `Bootstrap` | Bootstrap Icons |
| `FontAwesome` | Font Awesome 6 free, solid style |

The CSS framework and the icon set are chosen separately — a Tailwind application may still use Bootstrap Icons.

`AddUI` registers through `TryAdd`, so if the application already called `AddWebDefaults` or `AddUI`, that choice wins and the arguments here are ignored. Pass the framework to whichever call runs first.

### Contextual colours

Colour attributes are nullable and fall back to the dictionary, which is what lets the same markup work under every framework:

```cshtml
<notification-badge />                        <!-- the framework's own danger colour -->
<notification-badge badge-colour="danger" />  <!-- Bootstrap: bg-danger · Tailwind: bg-red-600 · jc-tailwind-ui: tone-danger -->
```

Set one explicitly and you are writing that framework's vocabulary, so the value must match the framework you registered.

### Tailwind safelist

Tailwind generates utilities by scanning source files, and these class names live in a compiled assembly it never reads. The package ships `jc-communication.tailwind.css` declaring them — import it from your Tailwind entry CSS, along with JC.Web's:

```css
@import "../path/to/JC.Web/UI/jc-web.tailwind.css";
@import "../path/to/JC.Communication.Web/jc-communication.tailwind.css";
```

Without it the components render valid class names with no CSS behind them.

## Defaults

| Component | Default behaviour |
|-----------|-------------------|
| `<notification-dropdown>` | Bell from the icon dictionary, `danger` badge, 350px list, 360px menu, aligned `end`, no footer link |
| `<notification-badge>` | Badge omitted when the count is zero, leaving the icon |
| `<notification-toast>` | Top-right, auto-hide after 5000ms, body truncated at 120 characters |
| `<message-thread>` | Sent `primary` on `white`, received `light` on `dark`, 500px max height with auto-scroll |
| `<chat-list>` | Links to `/chat/{0}`, preview truncated at 50 characters, unread badges shown |
| `<chat-input>` | 2 rows, 4096 character limit, `primary` send button, anti-forgery token included |
| `<chat-participants>` | 5 avatars before an overflow count, 32px |
| `<contact-form>` | Heading "Contact Us", `primary` submit, anti-forgery token included |
| Framework / icon set | Bootstrap / Bootstrap Icons |

## Documentation

- [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Communication/Communication.Web-Setup.md) — registration, framework selection, per-framework requirements
- [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Communication/Communication.Web-Guide.md) — every component, nuances, and the JavaScript each expects
- [API Reference](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Communication/Communication.Web-API.md)

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
