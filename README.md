# JC-Packages

A suite of .NET 9 NuGet packages providing shared infrastructure for .NET applications. Licensed under MIT.

## Packages

| Package | Description | Docs |
|---------|-------------|------|
| **JC.Core** | Repository pattern with multi-DbContext support, automatic audit trail on SaveChanges, soft-delete, pagination, and utility helpers | [Documentation](Documentation/JC.Core/) |
| **JC.Web** | Security headers, cookie management, client profiling, rate limiting, SEO (sitemap, robots, meta, JSON-LD), and a swappable UI framework for tag helpers | [Documentation](Documentation/JC.Web/) |
| **JC.Identity** | ASP.NET Core Identity integration — users, roles, claims, account rules, and role and administrator seeding | [Documentation](Documentation/JC.Identity/) |
| **JC.Identity.Shared** | The identity runtime shared by every authority — the default `IUserInfo` implementation, the claims projection, account rules, two-factor helpers. The contract itself lives in JC.Core. No ASP.NET Core dependency | [Documentation](Documentation/JC.Identity.Shared/) |
| **JC.Identity.Shared.Web** | The ASP.NET Core half of JC.Identity.Shared — the claims and account-rule middleware | [Documentation](Documentation/JC.Identity.Shared/) |
| **JC.CAP** | Single sign-on against CAP, the Central Admin Portal: an OpenIddict client, a token-backed session that refreshes silently, CAP's claims in ASP.NET Identity's vocabulary, role publishing, CAP's API, a member cache and branded links into CAP's account pages. The second identity authority on JC.Identity.Shared | [Documentation](Documentation/JC.CAP/) |
| **JC.Tenancy** | Application tenancy — tenant scope, EF Core query filters, a tenant store with caching, and safe and unsafe cross-tenant access | [Documentation](Documentation/JC.Tenancy/) |
| **JC.MySql** | MySQL database provider extensions using Pomelo.EntityFrameworkCore.MySql | [Database Setup](Documentation/JC.Core/Database-Setup.md) |
| **JC.SqlServer** | SQL Server database provider extensions using Microsoft.EntityFrameworkCore.SqlServer | [Database Setup](Documentation/JC.Core/Database-Setup.md) |
| **JC.Communication** | Email sending with multiple providers, in-app notifications with caching and logging, real-time messaging with threads/participants/read tracking, and database logging | [Documentation](Documentation/JC.Communication/) |
| **JC.Communication.Web** | Razor tag helpers for JC.Communication — notification dropdown/badge/toasts, chat thread/list/input/participants, and contact form | [Documentation](Documentation/JC.Communication/) |
| **JC.Github** | GitHub integration for bug report and issue tracking services | [Documentation](Documentation/JC.Github/) |
| **JC.BackgroundJobs** | Lightweight hosted-service background jobs and Hangfire recurring/ad-hoc job integration | [Documentation](Documentation/JC.BackgroundJobs/) |
| **JC.FileStorage** | Tenant-scoped file storage on disk with database-backed records, audited deletion, per-folder size and type limits, single-level folder separation, and read-only static files served from a deploy-time directory | [Documentation](Documentation/JC.FileStorage/) |
| **JC.FileStorage.Web** | ASP.NET Core integration for JC.FileStorage — `IFormFile` handling, MIME type inference, an upload constraints tag helper, and an `IApplicationBuilder` overload of `AddFolders` | [Documentation](Documentation/JC.FileStorage/) |
| **JC.Content** | Content processing — profanity moderation with evasion-aware matching and confidence scoring, diffing by line/word/character, conversion between plain text, Markdown and HTML, HTML sanitisation, and Unicode normalisation. No database, no ASP.NET Core dependency and no configuration | [Documentation](Documentation/JC.Content/) |
| **JC.SqlServer.Hangfire** | Hangfire SQL Server storage registration for JC-Packages applications | — |

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Installation

These packages are **not published to NuGet.org**. To use them in your projects, clone the repository and either:

1. **Project references** — add direct project references to the relevant `.csproj` files from your consuming solution.
2. **Local NuGet feed** — pack the projects (`dotnet pack`) and push the `.nupkg` files to a local NuGet feed.

```bash
git clone https://github.com/JC-Foundry/JC-Packages.git
```

## Package Dependencies

```
JC.Core (foundation — no JC dependencies)
├── JC.Identity.Shared
│   ├── JC.Identity.Shared.Web
│   ├── JC.Identity (depends on both halves)
│   └── JC.CAP (depends on both halves, and on CAP.SSO from the CAP repository)
├── JC.Tenancy
├── JC.Web
├── JC.Communication
│   └── JC.Communication.Web (depends on JC.Communication + JC.Web)
├── JC.Github
├── JC.BackgroundJobs
├── JC.Content
├── JC.FileStorage
│   └── JC.FileStorage.Web (depends on JC.FileStorage + JC.Web)
├── JC.MySql
└── JC.SqlServer

JC.SqlServer.Hangfire (standalone — no JC dependencies)
```

Every package except JC.SqlServer.Hangfire depends on **JC.Core**. The database providers (JC.MySql / JC.SqlServer) are interchangeable. **JC.Communication.Web** depends on both **JC.Communication** and **JC.Web**, and **JC.FileStorage.Web** depends on both **JC.FileStorage** and **JC.Web**.

**Identity and tenancy are independent.** `JC.Tenancy` references no identity package, and no identity package references `JC.Tenancy` — the consuming application joins them. That is what lets an application take tenancy without users, or identity without tenants. `JC.Identity` and `JC.CAP` each bring `JC.Identity.Shared` and `JC.Identity.Shared.Web` with them; reference the Shared halves directly only when supplying identity from somewhere other than local ASP.NET Identity or CAP.

**JC.Core, JC.Tenancy, JC.Identity.Shared, JC.Communication, JC.BackgroundJobs, JC.Content and JC.FileStorage carry no ASP.NET Core dependency**, so they run unchanged from a worker service or console host.

**JC.Content depends on JC.Core for consistency with the rest of the suite, but uses nothing from it.** It holds no entities, reads no configuration and touches no database, so it needs neither `AddCore` nor a `DbContext` to work.

**JC.FileStorage** depends only on JC.Core, but JC.Tenancy is required for tenant isolation — without it, every stored file belongs to the no-tenant scope. **JC.FileStorage.Web** is optional and needed only by web applications: it adds `IFormFile` handling, a tag helper, and an `IApplicationBuilder` overload of `AddFolders`.

**JC.SqlServer.Hangfire** is standalone — it has no dependency on JC.Core. It depends on Hangfire.SqlServer and Hangfire.AspNetCore.

## Quick Start

### JC.Core

```csharp
builder.Services.AddCore<AppDbContext>();
```

Repositories are accessed through `IRepositoryManager` — inject it and call `GetRepository<T>()` for any entity type. See [JC.Core documentation](Documentation/JC.Core/) for full setup, multi-DbContext support, audit trail configuration, and API reference.

### Database Providers

```csharp
builder.Services.AddCore<AppDbContext>();

// MySQL
builder.Services.AddMySqlDatabase<AppDbContext>(builder.Configuration, migrationsAssembly: "MyApp");

// SQL Server
builder.Services.AddSqlServerDatabase<AppDbContext>(builder.Configuration, migrationsAssembly: "MyApp");
```

See [Database Setup](Documentation/JC.Core/Database-Setup.md) for full configuration.

### JC.Identity

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>();

var app = builder.Build();
app.UseIdentity();

// Optional: seed system roles and a default administrator from configuration
var admin = await app.ConfigureAdminAndRolesAsync<AppUser, AppRole, AppRoles>();
```

See [JC.Identity documentation](Documentation/JC.Identity/) for the account rules, claims, custom `IUserInfo` and role configuration. Tenancy is a separate package — see JC.Tenancy below.

### JC.CAP

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.AddCap(builder.Configuration);

var app = builder.Build();
app.UseCap();
app.MapCap();

// Publish the application's roles to CAP once, failing startup if CAP refuses
await app.SyncCapRolesAsync<AppRoles>();
```

```json
{
  "SSO": {
    "BaseUrl": "https://sso.example.com",
    "ClientId": "evbfqxmh",
    "ClientSecret": "the-secret-CAP-showed-once"
  }
}
```

Users sign in at CAP and arrive back as an ordinary cookie session with `IUserInfo` populated and `Authority` reading `CAP`. See [JC.CAP documentation](Documentation/JC.CAP/) for the session and its refresh, the account rules, roles, CAP's API and extending the principal. JC.Identity and JC.CAP are alternatives: an application takes one.

### JC.Tenancy

```csharp
builder.Services.AddCore<AppDbContext>();

builder.Services.AddTenancy<AppDbContext>(options =>
{
    // Nobody may query across tenants until a role is named
    options.AllowBypassForRole("SystemAdmin");
});
```

Your context implements `ITenantScopedContext` and calls `modelBuilder.ApplyTenantFilters(this)` last in `OnModelCreating`; the one context owning tenant storage also implements `ITenantDbContext` and calls `ApplyTenancyMappings()`.

There is no middleware. Tenant scope is a scoped `ITenantInfo`, derived live from `IUserInfo` where an identity package is registered, and set explicitly for work that has none:

```csharp
await using var scope = await services.CreateAsyncScopeForTenant(tenantId);
```

Entities opt in by implementing `IMultiTenancy`, which lives in JC.Core — marking an entity costs no reference to JC.Tenancy. See [JC.Tenancy documentation](Documentation/JC.Tenancy/) for the store, caching, cross-tenant access and seeding.

### JC.Web

```csharp
builder.Services.AddCore<AppDbContext>();

// Register all services
builder.Services.AddWebDefaults(builder.Configuration);

// Apply middleware
app.UseWebDefaults();

// Optional: rate limiting (opt-in, not included in WebDefaults)
builder.Services.AddRateLimiting();
app.UseRateLimiting();
```

See [JC.Web documentation](Documentation/JC.Web/) for security headers, cookie management, client profiling, rate limiting, bug reporter, and UI helpers.

### JC.Communication

```csharp
builder.Services.AddCore<AppDbContext>();

// Email with database logging (Microsoft provider by default) — optional
builder.Services.AddEmail<AppDbContext>(builder.Configuration);

// In-app notifications with database logging — optional
builder.Services.AddNotifications<AppDbContext>();

// Real-time messaging with threads, participants, and read tracking — optional
builder.Services.AddMessaging<AppDbContext>();
```

Each feature can be registered independently — you don't need all three. See [JC.Communication documentation](Documentation/JC.Communication/) for provider configuration, notification options, messaging setup, and usage guides.

### JC.Github

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.AddGithub<AppDbContext>(builder.Configuration, options =>
{
    options.GithubRepoOwner = "your-username";
    options.GithubRepoName = "your-repo";
});
```

See [JC.Github documentation](Documentation/JC.Github/) for webhook setup and issue tracking.

### JC.BackgroundJobs

```csharp
// Lightweight hosted-service job
builder.Services.AddBackgroundJob<CleanupJob>(options =>
{
    options.Interval = TimeSpan.FromMinutes(5);
});

// Hangfire recurring job (requires storage — see JC.SqlServer.Hangfire)
builder.Services.AddHangfireJob<ReportGenerationJob>(options =>
{
    options.Cron = "0 2 * * *";
});

// Ad-hoc scheduler with job type registration
builder.Services.AddHangfireScheduler(
    AdHocJobRegistration.For<OrderConfirmationJob>(),
    AdHocJobRegistration.For<FollowUpEmailJob>()
);
```

See [JC.BackgroundJobs documentation](Documentation/JC.BackgroundJobs/) for hosted service options, Hangfire configuration, and ad-hoc scheduling.

### JC.FileStorage

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.AddFileStorage();

var app = builder.Build();

// Folders must be registered before any file is saved or read.
// throwOnFail always has to be passed — it precedes a params parameter
app.Services.AddFolders(true, "invoices", "reports");
```

`AddFolders` extends `IServiceProvider`, so JC.FileStorage carries no ASP.NET Core dependency and runs just as well from a worker service or a console host. JC.FileStorage.Web adds an `app.AddFolders(...)` overload on `IApplicationBuilder` for web applications.

Your `DbContext` must implement `IFileStorageDbContext` and call `modelBuilder.ApplyFileStorageMappings()` in `OnModelCreating`. Files are then saved, read, and deleted through `StorageService`.

Folders can cap file size and restrict extensions, falling back to defaults on `FolderRegistry`. `StorageService` enforces them itself, so no caller can bypass them:

```csharp
// 10MB, PDFs only — enforced on every entry point
app.Services.AddFolders(true, new FolderModel("invoices", null, 10 * 1024 * 1024, [".pdf"]));
```

Executable extensions (`.exe`, `.bat`, `.ps1` and around sixty more) can never be stored, and no configuration re-enables them.

Static files are a separate, opt-in feature: read-only documents placed beneath `FileStorage:StaticPath` at deploy time, registered at startup and cached in memory. There is no database record and no way to write them.

```csharp
builder.Services.AddFileStorage(useStaticFiles: true);

// Anywhere with StaticFileCache injected
var policy = await staticFiles.GetStaticFileText("privacy-policy.md", ct);
```

See [JC.FileStorage documentation](Documentation/JC.FileStorage/) for folder registration, limits, tenant scoping, cross-tenant access, static files, and delete behaviour.

### JC.Communication.Web

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.AddNotifications<AppDbContext>();

// Registers the UI framework services and this package's class and icon dictionaries
builder.Services.AddCommunicationWeb();
```

Adds Razor tag helpers for notifications, messaging and a contact form. Add `@addTagHelper *, JC.Communication.Web` to `_ViewImports.cshtml` to use them:

```html
<notification-dropdown view-all-href="/notifications" />
<contact-form endpoint="/contact" />
```

`AddCommunicationWeb` is required — every tag helper in the package takes constructor dependencies the container cannot supply otherwise, and omitting it fails when a page renders. Pass `UIFramework` and `IconFramework` to render for Tailwind or jc-tailwind-ui, and Font Awesome instead of Bootstrap Icons. See [JC.Communication.Web setup](Documentation/JC.Communication/Communication.Web-Setup.md).

### JC.FileStorage.Web

```csharp
builder.Services.AddCore<AppDbContext>();

// Registers WebStorageService and the UI services the tag helper resolves,
// plus everything AddFileStorage registers
builder.Services.AddFileStorageWeb();
```

Adds `IFormFile` uploads, downloads with MIME type inference, and an upload constraints tag helper. Add `@addTagHelper *, JC.FileStorage.Web` to `_ViewImports.cshtml` to use it:

```html
<input type="file" name="Upload" class="form-control" />
<upload-constraints folder="invoices" />
```

The tag helper reads the folder's limits from `FolderRegistry`, so the help text cannot drift from what the server enforces. See the [JC.FileStorage documentation](Documentation/JC.FileStorage/) — JC.FileStorage.Web is documented alongside it.

### JC.Content

```csharp
builder.Services.AddContentManager();
```

Registers moderation, comparison and conversion, and the `ContentManager` that runs them as a pipeline:

```csharp
var response = content.NormaliseAndModerate(comment);
var clean = response.ProfanityModerationMaskResult.UpdatedContent;
```

Each area can be registered on its own with `AddContentModeration`, `AddContentComparison` or `AddContentConversion`. `ContentSanitiser` and `NormalisationHelper` need no registration. See the [JC.Content documentation](Documentation/JC.Content/) for levels, term configuration and sanitiser policies.

### JC.SqlServer.Hangfire

```csharp
builder.Services.AddHangfireSqlServer(builder.Configuration);
```

Registers Hangfire with SQL Server storage. Reads the `HangfireConnection` connection string from configuration by default.

## Configuration

### Connection Strings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-connection-string"
  }
}
```

### Admin Seeding (JC.Identity)

```json
{
  "Admin": {
    "Username": "admin",
    "Email": "admin@example.com",
    "Password": "YourSecurePassword",
    "DisplayName": "System Administrator"
  }
}
```

### Encrypted Cookies (JC.Web)

```json
{
  "Web": {
    "Cookies": {
      "DataProtection_Path": "/path/to/keys"
    }
  }
}
```

Required when using encrypted cookies (enabled by default in `AddWebDefaults` / `AddCookieServices`). Set `useEncryptedCookies: false` to skip.

### JC.Communication

```json
{
  "Communication": {
    "Email": {
      "TenantId": "your-azure-tenant-id",
      "ClientId": "your-azure-client-id",
      "ClientSecret": "your-azure-client-secret",
      "DefaultFromAddress": "noreply@yourdomain.com",
      "DefaultFromDisplayName": "My Application"
    }
  }
}
```

Email configuration is required when using `AddEmail`. The keys shown above are for the Microsoft provider (default). Other providers require different keys — see [Email setup](Documentation/JC.Communication/Email-Setup.md) for full configuration. Notifications are configured entirely in code via `NotificationOptions` — see [Notifications setup](Documentation/JC.Communication/Notifications-Setup.md). Messaging is configured entirely in code via `MessagingOptions` — see [Messaging setup](Documentation/JC.Communication/Messaging-Setup.md).

### GitHub Integration (JC.Github)

```json
{
  "Github": {
    "ApiKey": "ghp_your_personal_access_token",
    "Secret": "your-webhook-secret"
  }
}
```

`ApiKey` is always required. `Secret` is required when webhooks are enabled (the default). All other settings (API URL, repo owner, repo name, etc.) are configured via `GithubOptions` in the `AddGithub` callback.

### File Storage (JC.FileStorage)

```json
{
  "FileStorage": {
    "BasePath": "C:\\app-data\\file-storage",
    "StaticPath": "C:\\app-data\\static-files"
  }
}
```

`BasePath` is required when using `AddFileStorage`. It is the root directory all managed files are written under; the application account needs write access to it. The directory does not need to exist — tenant and folder directories are created on demand.

`StaticPath` is required only when static files are enabled with `AddFileStorage(useStaticFiles: true)`, and holds files placed there at deploy time. Everything else is configured in code.

### Hangfire Storage (JC.SqlServer.Hangfire)

```json
{
  "ConnectionStrings": {
    "HangfireConnection": "Server=.;Database=HangfireDb;Trusted_Connection=true;"
  }
}
```

Required when using `AddHangfireSqlServer`. The connection string name defaults to `"HangfireConnection"` but can be overridden via the `connectionStringName` parameter.

## Documentation

Full documentation for each package is available in the [Documentation](Documentation/) directory:

| Package | Setup | Full Guide | API Reference |
|---------|-------|------------|---------------|
| JC.Core | [Setup](Documentation/JC.Core/Setup.md) | [Guide](Documentation/JC.Core/Guide.md) | [API](Documentation/JC.Core/API.md) |
| JC.Web | [Security Setup](Documentation/JC.Web/Security-Setup.md) · [Client Profiling Setup](Documentation/JC.Web/ClientProfiling-Setup.md) · [SEO Setup](Documentation/JC.Web/SEO-Setup.md) · [UI Setup](Documentation/JC.Web/UI-Setup.md) | [Security Guide](Documentation/JC.Web/Security-Guide.md) · [Client Profiling Guide](Documentation/JC.Web/ClientProfiling-Guide.md) · [SEO Guide](Documentation/JC.Web/SEO-Guide.md) · [UI Guide](Documentation/JC.Web/UI-Guide.md) | [Security API](Documentation/JC.Web/Security-API.md) · [Client Profiling API](Documentation/JC.Web/ClientProfiling-API.md) · [SEO API](Documentation/JC.Web/SEO-API.md) · [UI API](Documentation/JC.Web/UI-API.md) |
| JC.Identity | [Setup](Documentation/JC.Identity/Setup.md) | [Guide](Documentation/JC.Identity/Guide.md) | [API](Documentation/JC.Identity/API.md) |
| JC.Identity.Shared | [Setup](Documentation/JC.Identity.Shared/Setup.md) | [Guide](Documentation/JC.Identity.Shared/Guide.md) | [API](Documentation/JC.Identity.Shared/API.md) |
| JC.Identity.Shared.Web | [Setup](Documentation/JC.Identity.Shared/Setup.md#adding-the-aspnet-core-middleware) | [Guide](Documentation/JC.Identity.Shared/Guide.md) | [API](Documentation/JC.Identity.Shared/API.md) |
| JC.CAP | [Setup](Documentation/JC.CAP/Setup.md) | [Guide](Documentation/JC.CAP/Guide.md) | [API](Documentation/JC.CAP/API.md) |
| JC.Tenancy | [Setup](Documentation/JC.Tenancy/Setup.md) | [Guide](Documentation/JC.Tenancy/Guide.md) | [API](Documentation/JC.Tenancy/API.md) |
| JC.Communication | [Email Setup](Documentation/JC.Communication/Email-Setup.md) · [Notifications Setup](Documentation/JC.Communication/Notifications-Setup.md) · [Messaging Setup](Documentation/JC.Communication/Messaging-Setup.md) | [Email Guide](Documentation/JC.Communication/Email-Guide.md) · [Notifications Guide](Documentation/JC.Communication/Notifications-Guide.md) · [Messaging Guide](Documentation/JC.Communication/Messaging-Guide.md) | [Email API](Documentation/JC.Communication/Email-API.md) · [Notifications API](Documentation/JC.Communication/Notifications-API.md) · [Messaging API](Documentation/JC.Communication/Messaging-API.md) |
| JC.Communication.Web | [Setup](Documentation/JC.Communication/Communication.Web-Setup.md) | [Guide](Documentation/JC.Communication/Communication.Web-Guide.md) | [API](Documentation/JC.Communication/Communication.Web-API.md) |
| JC.Github | [Setup](Documentation/JC.Github/Setup.md) | [Guide](Documentation/JC.Github/Guide.md) | [API](Documentation/JC.Github/API.md) |
| JC.BackgroundJobs | [Setup](Documentation/JC.BackgroundJobs/Setup.md) | [Guide](Documentation/JC.BackgroundJobs/Guide.md) | [API](Documentation/JC.BackgroundJobs/API.md) |
| JC.FileStorage | [Setup](Documentation/JC.FileStorage/Setup.md) | [Guide](Documentation/JC.FileStorage/Guide.md) | [API](Documentation/JC.FileStorage/API.md) |
| JC.FileStorage.Web | [Setup](Documentation/JC.FileStorage/Setup.md) | [Guide](Documentation/JC.FileStorage/Guide.md) | [API](Documentation/JC.FileStorage/API.md) |
| JC.Content | [Setup](Documentation/JC.Content/Setup.md) | [Guide](Documentation/JC.Content/Guide.md) | [API](Documentation/JC.Content/API.md) |
| JC.MySql / JC.SqlServer | [Database Setup](Documentation/JC.Core/Database-Setup.md) | — | — |

## Build from Source

```bash
git clone https://github.com/JC-Foundry/JC-Packages.git
cd JC-Packages
dotnet build
```

No additional configuration or dependencies are required beyond the .NET 9 SDK.

## Versioning Strategy

`JC-Packages` uses a **suite-based versioning model**:

`MAJOR.MINOR.PATCH`

| Part   | Meaning |
|--------|---------|
| Major  | Suite-wide breaking changes |
| Minor  | Suite-wide non-breaking feature changes |
| Patch  | Package-specific fixes and non-breaking improvements |

### Rules

- **Major** and **Minor** are shared across the full package suite
- A **Major** or **Minor** bump in any package updates **all packages**
- **Patch** versions are normally **package-specific**
- **`JC.Core` is the exception**: any patch update to `JC.Core` bumps the patch version of all packages **that depend on JC.Core** — which is every package except the standalone JC.SqlServer.Hangfire, and includes those depending on it transitively (JC.Identity.Shared.Web through JC.Identity.Shared, JC.Communication.Web and JC.FileStorage.Web through their own parents)

### What this means

Packages are expected to stay aligned on the same **Major.Minor** version, while **Patch** may differ between packages.

For example, within the same suite version:

- `JC.Core` = `3.1.0`
- `JC.Web` = `3.1.4`
- `JC.Identity` = `3.1.0`

That is valid.

If `JC.Core` is patched, all packages that depend on it bump their own patch version by 1 (e.g. `JC.Web` `3.1.4` becomes `3.1.5`, `JC.Identity` `3.1.0` becomes `3.1.1`). The standalone package JC.SqlServer.Hangfire is not affected by `JC.Core` patches.

### Why

This approach keeps suite compatibility easy to understand while still allowing individual packages to ship small fixes independently.

In short:

- **Major/Minor = suite compatibility**
- **Patch = package-specific**
- **`JC.Core` patch = patch bump for all JC.Core dependents**

### Release notes

Release notes are published under [Documentation/Release-Notes](Documentation/Release-Notes/) for **major** versions only — they are the only releases that introduce breaking changes and therefore need migration guidance. Minor (feature) and patch (fix) releases are backward-compatible, so they ship no release notes; newly added features are documented in each package's documentation.

## License

[MIT](LICENSE)
