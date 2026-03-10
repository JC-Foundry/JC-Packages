# Documentation Writing Guide
**Note: All documentation is AI generated**

Standards and templates for writing JC-Packages documentation. Each package gets a dedicated folder under `/Documentation/JC.{Package}/` containing a consistent set of documents.

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

#### Key Rules

- **Defaults must be documented.** Every registration method's default behaviour must be explicitly stated. The reader should know exactly what happens if they call the method with no arguments.
- **Every option must be documented.** Every parameter, configuration property, and overload must be covered in full configuration. Nothing should be discoverable only through IntelliSense.
- **Opt-in features in quick setup.** If a feature is optional (not included in the defaults convenience method), still show it in quick setup with a clear comment that it's opt-in. Then cover it fully in full configuration.
- **Show complete, copy-pasteable code.** Use `// ...existing code...` to indicate where the snippet fits in a larger file, but never show partial statements.
- **One code block per concept.** Don't split a single registration across multiple fenced blocks unless showing different files (e.g. `Program.cs` and `appsettings.json`).
- **Configuration examples** use `appsettings.json` with placeholder values. Never include real credentials.
- **Full configuration code examples must show options being set to their default values** so the reader can see what the defaults are directly in the code. If a default is `null` or empty, use a suitable example value instead.

#### Example — JC.Identity Setup.md

This is the reference example. All package Setup.md files should follow this level of detail.

---

````markdown
# JC.Identity — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project with a `DbContext`
- JC.Core already registered (JC.Identity depends on it)
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

## 0. Add the package

Add a project reference to `JC.Identity`:

```xml
<ProjectReference Include="path/to/JC.Identity/JC.Identity.csproj" />
```

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### Services — `Program.cs`

```csharp
// Register JC.Identity with ASP.NET Core Identity, default middleware options, and default cookie paths
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>();
```

### Middleware — `Program.cs`

```csharp
var app = builder.Build();

// Registers authentication, user info population, authorisation, and identity middleware — in that order
app.UseIdentity();

// Optional: seed system roles and a default admin user from config
await app.ConfigureAdminAndRolesAsync<AppUser, AppRole, AppDbContext, AppRoles>();
```

### Configuration — `appsettings.json`

Admin seeding requires these keys (only needed if calling `ConfigureAdminAndRolesAsync`):

```json
{
  "Admin": {
    "Username": "admin",
    "Email": "admin@example.com",
    "Password": "YourSecurePassword123!",
    "DisplayName": "System Administrator"
  }
}
```

### Defaults

When called with no configuration callbacks, `AddIdentity` sets:

| Default | Value |
|---------|-------|
| Login path | `/Identity/Account/Login` |
| Logout path | `/Identity/Account/Logout` |
| Access denied path | `/Identity/Account/AccessDenied` |
| Password change enforcement | Enabled — users with `RequiresPasswordChange` are redirected |
| Password change route | `/Identity/Account/Manage/SetPassword` |
| Two-factor enforcement | Disabled |
| Two-factor route | `/Identity/Account/Manage/EnableAuthenticator` |
| `IUserInfo` implementation | `UserInfo` (built-in) |
| Claims factory | `DefaultClaimsPrincipalFactory` — adds 12 custom claims from `BaseUser` properties |

`UseIdentity` registers middleware in this order:
1. `UseAuthentication()` — ASP.NET Core authentication
2. `UseUserInfo()` — populates `IUserInfo` from the authenticated user's claims
3. `UseAuthorization()` — ASP.NET Core authorisation
4. `UseIdentityMiddleware()` — enforces disabled account redirects, password change, and 2FA

## 2. Full configuration

### AddIdentity — standard registration

Registers ASP.NET Core Identity with EF Core stores, default token providers, the JC.Identity claims factory, `IUserInfo`, and `IdentityMiddlewareOptions`.

```csharp
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>(
    configureMiddleware: options =>
    {
        options.RequirePasswordChange = true;
        options.ChangePasswordRoute = "/Identity/Account/Manage/SetPassword";
        options.EnforceTwoFactor = false;
        options.TwoFactorRoute = "/Identity/Account/Manage/EnableAuthenticator";
        options.AccessDeniedRoute = "/Identity/Account/AccessDenied";
        options.LogoutRoute = "/Identity/Account/Logout";
        options.ErrorRoute = "/Error";
    },
    configureCookie: cookie =>
    {
        cookie.LoginPath = "/Identity/Account/Login";
        cookie.LogoutPath = "/Identity/Account/Logout";
        cookie.AccessDeniedPath = "/Identity/Account/AccessDenied";
    }
);
```

#### `IdentityMiddlewareOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RequirePasswordChange` | `bool` | `true` | When enabled, users with `RequiresPasswordChange = true` are redirected to the change password route |
| `ChangePasswordRoute` | `string` | `/Identity/Account/Manage/SetPassword` | Route users are redirected to when a password change is required |
| `EnforceTwoFactor` | `bool` | `false` | When enabled, users without 2FA configured are redirected to the 2FA setup route |
| `TwoFactorRoute` | `string` | `/Identity/Account/Manage/EnableAuthenticator` | Route users are redirected to for 2FA setup |
| `AccessDeniedRoute` | `string` | `/Identity/Account/AccessDenied` | Route disabled users are redirected to |
| `LogoutRoute` | `string` | `/Identity/Account/Logout` | Logout route — excluded from middleware enforcement |
| `ErrorRoute` | `string` | `/Error` | Error route — excluded from middleware enforcement |

`ExcludedPaths` is a read-only array automatically built from `AccessDeniedRoute`, `LogoutRoute`, and `ErrorRoute`. Requests to these paths skip all middleware enforcement checks.

#### `CookieAuthenticationOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `LoginPath` | `string` | `/Identity/Account/Login` | Where unauthenticated users are redirected |
| `LogoutPath` | `string` | `/Identity/Account/Logout` | Where the sign-out handler is mapped |
| `AccessDeniedPath` | `string` | `/Identity/Account/AccessDenied` | Where users are redirected on 403 |

These are ASP.NET Core's `CookieAuthenticationOptions` — you can set any property on the options object, not just the three above. JC.Identity sets these three defaults if no `configureCookie` callback is provided.

### AddIdentity with custom IUserInfo

If you need additional properties on `IUserInfo`, create a class implementing `IUserInfo` and use the four-type-parameter overload:

```csharp
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext, CustomUserInfo>(
    configureMiddleware: options =>
    {
        options.RequirePasswordChange = true;
        options.EnforceTwoFactor = false;
    },
    configureCookie: cookie =>
    {
        cookie.LoginPath = "/Identity/Account/Login";
    }
);
```

`CustomUserInfo` is registered as the scoped `IUserInfo` implementation instead of the built-in `UserInfo`.

### AddIdentityBase — when ASP.NET Core Identity is already registered

If your project registers ASP.NET Core Identity separately (e.g. with external auth providers), use `AddIdentityBase` to add only the JC.Identity services without re-registering Identity:

```csharp
// Two type parameters — uses built-in UserInfo
builder.Services.AddIdentityBase<AppUser, AppRole>(
    configureMiddleware: options =>
    {
        options.RequirePasswordChange = true;
        options.EnforceTwoFactor = false;
    }
);
```

```csharp
// Three type parameters — uses custom IUserInfo implementation
builder.Services.AddIdentityBase<AppUser, AppRole, CustomUserInfo>(
    configureMiddleware: options =>
    {
        options.RequirePasswordChange = true;
        options.EnforceTwoFactor = false;
    }
);
```

`AddIdentityBase` does **not** accept a `configureCookie` parameter — cookie configuration is the responsibility of whichever code registered ASP.NET Core Identity.

`AddIdentityBase` registers:
- Authorisation and authentication services
- `IUserInfo` (scoped)
- `DefaultClaimsPrincipalFactory` as `IUserClaimsPrincipalFactory<TUser>`
- `IdentityMiddlewareOptions`
- `Tenant` repository context

### Middleware — individual registration

If you need control over middleware ordering, register each component individually instead of calling `UseIdentity()`:

```csharp
app.UseAuthentication();
app.UseUserInfo();       // Must come after UseAuthentication — reads claims from the authenticated user
app.UseAuthorization();
app.UseIdentityMiddleware(); // Must come after UseUserInfo — depends on populated IUserInfo
```

### Admin and role seeding

#### ConfigureAdminAndRolesAsync — combined seeding

Seeds all system roles and creates a default admin user from configuration. Call after `app.Build()`.

```csharp
await app.ConfigureAdminAndRolesAsync<AppUser, AppRole, AppDbContext, AppRoles>(
    setupTenancy: false,
    usernameConfigKey: "Admin:Username",
    emailConfigKey: "Admin:Email",
    passwordConfigKey: "Admin:Password",
    displayNameConfigKey: "Admin:DisplayName",
    additionalRoles: null
);
```

| Parameter | Type | Default | Description |
|----------|------|---------|-------------|
| `setupTenancy` | `bool` | `false` | When `true`, finds or creates a "Default Tenant" and assigns it to the admin user |
| `usernameConfigKey` | `string` | `"Admin:Username"` | Configuration key for the admin username |
| `emailConfigKey` | `string` | `"Admin:Email"` | Configuration key for the admin email |
| `passwordConfigKey` | `string` | `"Admin:Password"` | Configuration key for the admin password |
| `displayNameConfigKey` | `string` | `"Admin:DisplayName"` | Configuration key for the admin display name (falls back to "System Administrator") |
| `additionalRoles` | `IEnumerable<string>?` | `null` | Extra roles to assign to the admin beyond the system defaults |

Configuration — `appsettings.json`:

```json
{
  "Admin": {
    "Username": "admin",
    "Email": "admin@example.com",
    "Password": "YourSecurePassword123!",
    "DisplayName": "System Administrator"
  }
}
```

The admin user is created with `EmailConfirmed = true` and `IsEnabled = true`. If `setupTenancy` is `false`, the admin receives both `SystemAdmin` and `Admin` roles. If `setupTenancy` is `true`, the admin receives only `SystemAdmin`.

Seeding is idempotent — if a user with the configured email or username already exists, no changes are made.

#### SeedRolesAsync — roles only

Seeds roles without creating an admin user. Uses reflection to discover all `const string` pairs from your `SystemRoles` subclass (role name matched with `{RoleName}Desc` for descriptions).

```csharp
await app.SeedRolesAsync<AppRoles, AppRole>();
```

#### SeedDefaultAdminAsync — admin only

Creates the admin user without seeding roles. Accepts the same parameters as `ConfigureAdminAndRolesAsync`.

```csharp
await app.SeedDefaultAdminAsync<AppUser, AppRole, AppDbContext>(
    setupTenancy: false,
    usernameConfigKey: "Admin:Username",
    emailConfigKey: "Admin:Email",
    passwordConfigKey: "Admin:Password",
    displayNameConfigKey: "Admin:DisplayName",
    additionalRoles: ["Editor", "Reviewer"]
);
```

### Defining roles

Extend `SystemRoles` to define application-specific roles. Each role needs a `const string` for the name and a matching `{Name}Desc` constant for the description:

```csharp
public class AppRoles : SystemRoles
{
    public const string Editor = nameof(Editor);
    public const string EditorDesc = "Can create and edit content.";

    public const string Viewer = nameof(Viewer);
    public const string ViewerDesc = "Read-only access to content.";
}
```

`SystemRoles` provides two built-in roles:
- `SystemAdmin` — "Full system administrator with access to tenant management and assignment."
- `Admin` — "Administrator with access to all features within their tenant."

## 3. Apply migrations

JC.Identity introduces tables for Identity (users, roles, claims, tokens, logins), audit entries, and tenants. Your `DbContext` must extend `IdentityDataDbContext<TUser, TRole>`.

Generate and apply the initial migration:

```bash
dotnet ef migrations add InitialIdentity --project YourApp
dotnet ef database update --project YourApp
```

## 4. Verify

1. Run the application.
2. Navigate to a page protected with `[Authorize]` — you should be redirected to `/Identity/Account/Login`.
3. If admin seeding is configured, log in with the credentials from `appsettings.json`.

## Next steps

- [Guide](Guide.md) — multi-tenancy, custom `IUserInfo`, tenant query filters, and `UserInfoMiddleware` behaviour.
- [API Reference](API.md)
````

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

#### Example — JC.Core Guide.md (partial)

````markdown
# JC.Core — Guide

Covers repository pattern usage, soft-delete and restore, pagination, audit trail behaviour, and utility helpers. See [Setup](Setup.md) for registration.

## Repository pattern

### Basic CRUD

Inject `IRepositoryContext<T>` for typed repository access:

```csharp
public class ProductService(IRepositoryContext<Product> products)
{
    public async Task<Product> CreateAsync(string name, decimal price)
    {
        var product = new Product { Name = name, Price = price };
        return await products.AddAsync(product);
    }

    public async Task<Product?> GetAsync(int id)
    {
        return await products.GetByIdAsync(id);
    }

    public async Task<List<Product>> GetAllActiveAsync()
    {
        return await products.GetAllAsync(p => !p.IsDeleted);
    }

    public async Task UpdateAsync(Product product)
    {
        await products.UpdateAsync(product);
    }
}
```

Every `AddAsync` call automatically populates `CreatedById` and `CreatedUtc` on `AuditModel` entities. Every `UpdateAsync` populates `LastModifiedById` and `LastModifiedUtc`. The user ID comes from `IUserInfo.UserId` — if JC.Identity is registered, this is the authenticated user; otherwise it falls back to `IUserInfo.MissingUserInfoId`.

### Batching operations

By default, every repository method calls `SaveChangesAsync` immediately. Pass `saveNow: false` to batch:

```csharp
await products.AddAsync(product1, saveNow: false);
await products.AddAsync(product2, saveNow: false);
await products.AddAsync(product3, saveNow: false);
await repositoryManager.SaveChangesAsync(); // Single round-trip
```

This is useful when creating related entities that should be saved atomically.

### Overriding the user ID

All write operations accept an optional `userId` parameter for audit purposes:

```csharp
// Attribute the change to a system process instead of the current user
await products.UpdateAsync(product, userId: "data-migration-job");
```

## Soft-delete and restore

### Soft-deleting

```csharp
await products.SoftDeleteAsync(product);
```

Sets `IsDeleted = true`, `DeletedById`, and `DeletedUtc`. The entity remains in the database. Clears any previous restore fields.

### Restoring

```csharp
await products.RestoreAsync(product);
```

Sets `IsDeleted = false`, `RestoredById`, and `RestoredUtc`. Clears the deleted fields.

### Querying by soft-delete status

Use `FilterDeleted` to control which records are returned:

```csharp
// Only active (non-deleted) records — the most common case
var active = products.AsQueryable().FilterDeleted(DeletedQueryType.OnlyActive).ToList();

// Only soft-deleted records (e.g. for an "archive" or "recycle bin" view)
var deleted = products.AsQueryable().FilterDeleted(DeletedQueryType.OnlyDeleted).ToList();

// All records regardless of status
var all = products.AsQueryable().FilterDeleted(DeletedQueryType.All).ToList();
```

**Nuance:** `FilterDeleted` only works on entities extending `AuditModel`. For non-`AuditModel` entities with a `bool IsDeleted` property, the repository's soft-delete operations still work (detected via reflection), but `FilterDeleted` is not available — use a manual `.Where(x => !x.IsDeleted)` instead.

## Pagination

### From a queryable

```csharp
var page = await products.AsQueryable()
    .Where(p => !p.IsDeleted)
    .OrderBy(p => p.Name)
    .ToPagedListAsync(pageNumber: 1, pageSize: 20);

// page.Items        — IReadOnlyList<Product> for this page
// page.TotalCount   — total matching records
// page.TotalPages   — calculated from TotalCount / PageSize
// page.HasNextPage  — true if more pages exist
// page.PageNumber   — current page (1-based)
```

`ToPagedListAsync` executes two queries: one `COUNT(*)` and one `Skip/Take`. Use the sync `ToPagedList` overload if you're working with an in-memory collection.

**Nuance:** If `pageNumber` exceeds `TotalPages`, it auto-adjusts to the last valid page rather than returning an empty result.
````

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
- **Method signatures must be exact.** Show the correct method name, all parameters in order, their types, and default values. If a parameter has no default, use `—` in the default column.
- **Combine interfaces with their implementations.** If `IFoo` is implemented by `Foo` in the same package, document them together under `Foo` (or whichever name is more recognisable). Note which type the consumer injects. Only document standalone interfaces (those with no in-package implementation) separately.
- **Document access modifiers on properties.** If a property has a public get but a private or internal set (or vice versa), show this in the Access column (e.g. `get; internal set;`).
- **Describe method behaviour, not usage.** Explain the flow: what the method checks, what it creates, what it persists, what it returns, what side effects occur. Think of it as the XML `<summary>` and `<remarks>` tags combined into prose.
- **State included navigation properties.** If a method eagerly loads EF Core navigation properties (via `.Include()`), list them in the method description. This tells the consumer exactly what's materialised without needing to check the source.
- **Group by category, then by class.** API.md is organised into top-level sections (Models, Enums, Services, Helpers, Extensions, Data, etc.) with individual classes documented under the appropriate section. Within each section, every member of a class appears together under that class heading. Omit any top-level section that has no entries for the package.
- **Always include the namespace.** Every class, interface, enum, and record heading must state its full namespace (e.g. `**Namespace:** \`JC.Core.Models\``). Verify against the source code — never guess.
- **Enums get a simple value table.** List each member with its integer value (if non-default) and a one-sentence description.
- **Extension method classes are documented as their own section.** Group all extension methods under the static class name, with each method as a sub-heading.
- **Inheritance.** If a class extends a base class from the same package, note this but don't re-document inherited members — refer the reader to the base class section.

#### Example — API.md entry (partial)

````markdown
## BugReportService

**Namespace:** `JC.Github.Services`

Manages local persistence and GitHub synchronisation of issue reports. Inject via `BugReportService`.

### Methods

#### RecordIssue(string description, IssueType issueType, string? creatorId = null, string? creatorName = null)

**Returns:** `Task<ReportedIssue>`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `description` | `string` | — | The issue body text. Used as the GitHub issue body. |
| `issueType` | `IssueType` | — | Whether this is a `Bug` or `Suggestion`. Determines the GitHub issue title. |
| `creatorId` | `string?` | `null` | Local-only user identifier. Stored on the `ReportedIssue` but not sent to GitHub. |
| `creatorName` | `string?` | `null` | Local-only display name. Stored on the `ReportedIssue` but not sent to GitHub. |

Persists a new `ReportedIssue` to the local database, then attempts to create a corresponding GitHub issue via the configured `GitHelper`. The GitHub issue title is set to `"New Bug"` or `"New Suggestion"` based on `issueType`, with `description` as the body.

If the GitHub API call succeeds, `ReportSent` is set to `true` and `ExternalId` is populated with the GitHub issue number. If it fails, the exception is logged but not thrown — `ReportSent` remains `false` and `ExternalId` remains `null`. The local record is always saved regardless of GitHub sync outcome.

## ReportedIssue

**Namespace:** `JC.Github.Models`

Entity representing a locally persisted issue report, optionally synced with GitHub.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Id` | `Guid` | Generated | get; set; | Local unique identifier. |
| `Description` | `string` | — | get; set; | The issue body text. |
| `Type` | `IssueType` | — | get; set; | Whether this is a bug or suggestion. |
| `Created` | `DateTime` | `DateTime.UtcNow` | get; set; | Timestamp of local creation. |
| `ReportSent` | `bool` | `false` | get; set; | Whether the GitHub API call succeeded. |
| `ExternalId` | `int?` | `null` | get; set; | GitHub issue number, or null if not synced. |
| `Closed` | `bool` | `false` | get; set; | Whether the issue has been closed (updated via webhook). |
| `UserId` | `string?` | `null` | get; set; | Local-only creator identifier. |
| `UserDisplay` | `string?` | `null` | get; set; | Local-only creator display name. |
| `Image` | `byte[]?` | `null` | get; set; | Optional screenshot data. Not populated by `RecordIssue`. |

## IssueType

**Namespace:** `JC.Github.Models`

Enum indicating the type of reported issue.

| Member | Value | Description |
|--------|-------|-------------|
| `Bug` | `0` | A bug report. GitHub issue title: "New Bug". |
| `Suggestion` | `1` | A feature suggestion. GitHub issue title: "New Suggestion". |
````

---

## General Writing Rules

1. **British English** spelling where it differs (e.g. "colour" not "color" in prose — code identifiers stay as-is).
2. **No emojis** in documentation.
3. **Headers use sentence case** (e.g. "Register services", not "Register Services") — except for proper nouns and package names.
4. **Code blocks** always specify the language (`csharp`, `json`, `xml`, `bash`).
5. **Links** between documents use relative paths.
6. **Keep files focused.** Each document answers one question. Setup answers "how do I add this and configure it?", Guide answers "how do I use the features?", API answers "what's available?".
7. **Version-agnostic.** Don't reference specific version numbers in docs — the consumer is expected to use the version they've pulled.
