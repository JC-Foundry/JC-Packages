# JC-Packages

A suite of .NET 9 NuGet packages providing shared infrastructure for .NET applications. Licensed under MIT.

## Packages

| Package | Description |
|---------|-------------|
| **JC.Core** | Repository pattern, automatic audit trail on SaveChanges, soft-delete, pagination, and utility helpers |
| **JC.Web** | ASP.NET Core web hardening and helpers — security headers, cookie management (with optional Data Protection encryption), client profiling (user agent, bot filtering, geolocation), UI tag helpers, dropdown builders, QR code generation |
| **JC.Identity** | ASP.NET Core Identity integration, context-bound multi-tenancy query filters, middleware, and user management helpers |
| **JC.MySql** | MySQL database provider extensions using Pomelo.EntityFrameworkCore.MySql |
| **JC.SqlServer** | SQL Server database provider extensions using Microsoft.EntityFrameworkCore.SqlServer |
| **JC.Github** | GitHub integration for bug report and issue tracking services |

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Installation

These packages are **not published to NuGet.org**. To use them in your projects, clone the repository and either:

1. **Project references** — add direct project references to the relevant `.csproj` files from your consuming solution.
2. **Local NuGet feed** — pack the projects (`dotnet pack`) and push the `.nupkg` files to a local NuGet feed.

```bash
git clone https://github.com/johncraik/JC-Packages.git
```

## Package Dependencies

```
JC.Core (foundation — no JC dependencies)
├── JC.Identity
├── JC.Web
├── JC.Github
├── JC.MySql
└── JC.SqlServer
```

All packages depend on **JC.Core**. The database providers (JC.MySql / JC.SqlServer) are interchangeable. JC.Identity, JC.Web, and JC.Github are independent of each other.

## Quick Start & Usage

### JC.Core

Register core services and your entity repository contexts:

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.RegisterRepositoryContexts(typeof(Product), typeof(Order));
```

Your `DbContext` must extend `DataDbContext`:

```csharp
public class AppDbContext : DataDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
```

Use the repository manager to query and persist data:

```csharp
public class ProductService(IRepositoryManager repositoryManager)
{
    public async Task<IPagination<Product>> GetPagedAsync(int page, int size)
    {
        var repo = repositoryManager.GetRepository<Product>();
        return await repo.AsQueryable().ToPagedListAsync(page, size);
    }
}
```

Extend `AuditModel` for automatic audit fields and soft-delete:

```csharp
public class Product : AuditModel
{
    public string Name { get; set; } = string.Empty;
}
```

Both `DataDbContext` and `IdentityDataDbContext` automatically create `AuditEntry` records on `SaveChangesAsync` for all tracked entity changes (create, update, soft-delete, restore, hard-delete). Audit entries are persisted in the same transaction — no additional wiring required.

### Database Providers (JC.MySql / JC.SqlServer)

Register one database provider — they are interchangeable:

```csharp
// MySQL
builder.Services.AddMySqlDatabase<AppDbContext>(
    builder.Configuration,
    migrationsAssembly: "MyApp",
    addHealthCheck: true
);

// SQL Server
builder.Services.AddSqlServerDatabase<AppDbContext>(
    builder.Configuration,
    migrationsAssembly: "MyApp",
    addHealthCheck: true
);
```

### JC.Identity

Your `DbContext` must extend `IdentityDataDbContext<TUser, TRole>` instead of `DataDbContext`:

```csharp
public class AppDbContext : IdentityDataDbContext<AppUser, AppRole>
{
    public AppDbContext(DbContextOptions<AppDbContext> options, IUserInfo userInfo) : base(options, userInfo) { }
}
```

Extend `BaseUser` and `BaseRole` for your application:

```csharp
public class AppUser : BaseUser { }
public class AppRole : BaseRole { }
```

Register identity services and apply the middleware pipeline:

```csharp
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>();

var app = builder.Build();

app.UseIdentity();

await app.ConfigureAdminAndRolesAsync<AppUser, AppRoles, AppRole>(setupTenancy: true);
```

If you need to register ASP.NET Core Identity separately (e.g. custom configuration), use `AddIdentityBase` instead:

```csharp
builder.Services
    .AddIdentity<AppUser, AppRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddIdentityBase<AppUser, AppRole>();
```

Define application roles by extending `SystemRoles`:

```csharp
public class AppRoles : SystemRoles
{
    public const string Manager = "Manager";
    public const string ManagerDesc = "Can manage resources";
}
```

Access the current user anywhere via `IUserInfo`:

```csharp
public class SomeService(IUserInfo userInfo)
{
    public string CurrentUser => userInfo.DisplayName;
    public bool IsAdmin => userInfo.IsInRole(SystemRoles.Admin);
}
```

### JC.Web

Web hardening, cookie management, client profiling, and UI helpers for ASP.NET Core applications.

Register all services and middleware with a single call, or use individual methods for granular control:

```csharp
// Services — register everything
builder.Services.AddWebDefaults(builder.Configuration);

// Or with a custom geolocation provider
builder.Services.AddWebDefaults<MyGeoProvider>(builder.Configuration);

// Middleware — apply everything
app.UseWebDefaults();
```

Individual registration is also available:

```csharp
builder.Services.AddSecurityHeaders();
builder.Services.AddCookieServices(builder.Configuration);
builder.Services.AddClientProfiling(options =>
{
    options.AllowedBots.Add("Googlebot");
});

app.UseSecurityHeaders();
app.UseClientProfiling();
```

#### Security

**Security Headers** — middleware that applies a configurable set of HTTP security headers to all responses, including X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy, HSTS, and cross-origin headers (COOP/CORP/COEP). Includes a fluent `ContentSecurityPolicyBuilder` with directive validation, nonce/hash helpers, and keyword restrictions. Headers are pre-computed at startup for performance.

**Cookie Management** — a profile-based cookie system with a thread-safe `CookieProfileDictionary` singleton for registering cookie profiles at startup. Supports both standard and encrypted cookies (via ASP.NET Core Data Protection). Global defaults are configured through `CookieDefaultOptions`, with per-cookie overrides via `CookieDefaultOverride` on each `CookieProfile`. Two `ICookieService` implementations (`CookieService` and `EncryptedCookieService`) are available, registered as keyed services for dual-mode injection.

#### Client Profiling

**Request Metadata** — middleware that captures client IP, user agent, geolocation (via optional `IGeoLocationProvider`), and request details early in the pipeline. Access via `HttpContext.GetRequestMetadata()`.

**Bot Filtering** — middleware that blocks detected bots with configurable allowed-bot lists, path filtering, and status codes. Depends on request metadata being populated first.

**User Agent Parsing** — `UserAgentService` parses user agent strings into structured data (browser, OS, device type) with bot/mobile/tablet detection using UAParser.

#### UI

Tag helpers, HTML builders, dropdown utilities, and QR code generation for Razor views.

### JC.Github

Register GitHub services and optionally enable webhook sync:

```csharp
builder.Services.AddGithub<AppDbContext>(builder.Configuration, options =>
{
    options.EnableWebhooks = true;
    options.WebhookPath = "/api/github/webhook";
});

var app = builder.Build();
app.UseGithubWebhooks();
```

Your `DbContext` must extend `DataDbContext` (or `IdentityDataDbContext` if using JC.Identity) and implement `IGithubDbContext`:

```csharp
public class AppDbContext : DataDbContext, IGithubDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ReportedIssue> ReportedIssues => Set<ReportedIssue>();
    public DbSet<IssueComment> IssueComments => Set<IssueComment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyGithubMappings();
    }
}
```

Report issues programmatically:

```csharp
public class FeedbackService(BugReportService bugReportService)
{
    public async Task ReportBugAsync(string description)
    {
        await bugReportService.RecordIssue(description, IssueType.Bug, userId: null, creatorName: "System");
    }
}
```

## Configuration

### Connection Strings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-connection-string"
  }
}
```

The connection string name defaults to `"DefaultConnection"` but can be overridden via the `connectionStringName` parameter on `AddMySqlDatabase` / `AddSqlServerDatabase`.

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

### GitHub Integration (JC.Github)

```json
{
  "Github": {
    "Url": "https://api.github.com",
    "ApiKey": "ghp_your_personal_access_token",
    "Owner": "your-username",
    "Repo": "your-repo",
    "Secret": "your-webhook-secret"
  }
}
```

## Build from Source

```bash
git clone https://github.com/johncraik/JC-Packages.git
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
- **`JC.Core` is the exception**: any patch update to `JC.Core` is treated as a **suite-wide patch update**

### What this means

Packages are expected to stay aligned on the same **Major.Minor** version, while **Patch** may differ between packages.

For example, within the same suite version:

- `JC.Core` = `3.0.0`
- `JC.Web` = `3.0.1`
- `JC.Identity` = `3.0.0`

That is valid.

If `JC.Core` is updated from `3.0.0` to `3.0.1`, all packages receive a patch bump to align with the new core baseline.

### Why

This approach keeps suite compatibility easy to understand while still allowing individual packages to ship small fixes independently.

In short:

- **Major/Minor = suite compatibility**
- **Patch = package-specific**
- **`JC.Core` patch = suite-wide patch**

## License

[MIT](LICENSE)
