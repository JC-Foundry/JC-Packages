# JC-Packages

A suite of .NET 9 NuGet packages providing shared infrastructure for .NET applications. Licensed under MIT.

## Packages

| Package | Description | Docs |
|---------|-------------|------|
| **JC.Core** | Repository pattern, automatic audit trail on SaveChanges, soft-delete, pagination, and utility helpers | [Documentation](Documentation/JC.Core/) |
| **JC.Web** | Security headers, cookie management, client profiling, rate limiting, bug reporter tag helper, UI helpers | [Documentation](Documentation/JC.Web/) |
| **JC.Identity** | ASP.NET Core Identity integration, multi-tenancy query filters, middleware, user management | [Documentation](Documentation/JC.Identity/) |
| **JC.MySql** | MySQL database provider extensions using Pomelo.EntityFrameworkCore.MySql | [Database Setup](Documentation/JC.Core/Database-Setup.md) |
| **JC.SqlServer** | SQL Server database provider extensions using Microsoft.EntityFrameworkCore.SqlServer | [Database Setup](Documentation/JC.Core/Database-Setup.md) |
| **JC.Github** | GitHub integration for bug report and issue tracking services | [Documentation](Documentation/JC.Github/) |
| **JC.BackgroundJobs** | Lightweight hosted-service background jobs and Hangfire recurring/ad-hoc job integration | [Documentation](Documentation/JC.BackgroundJobs/) |
| **JC.SqlServer.Hangfire** | Hangfire SQL Server storage registration for JC-Packages applications | — |

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

JC.BackgroundJobs (standalone — no JC dependencies)

JC.SqlServer.Hangfire (standalone — no JC dependencies)
```

JC.Identity, JC.Web, JC.Github, JC.MySql, and JC.SqlServer all depend on **JC.Core**. The database providers (JC.MySql / JC.SqlServer) are interchangeable.

**JC.BackgroundJobs** and **JC.SqlServer.Hangfire** are standalone — they have no dependency on JC.Core or each other. JC.BackgroundJobs depends only on Hangfire.Core and Microsoft.Extensions.Hosting.Abstractions. JC.SqlServer.Hangfire depends on Hangfire.SqlServer and Hangfire.AspNetCore.

## Quick Start

### JC.Core

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.RegisterRepositoryContexts(typeof(Product), typeof(Order));
```

See [JC.Core documentation](Documentation/JC.Core/) for full setup, audit trail configuration, and API reference.

### Database Providers

```csharp
// MySQL
builder.Services.AddMySqlDatabase<AppDbContext>(builder.Configuration, migrationsAssembly: "MyApp");

// SQL Server
builder.Services.AddSqlServerDatabase<AppDbContext>(builder.Configuration, migrationsAssembly: "MyApp");
```

See [Database Setup](Documentation/JC.Core/Database-Setup.md) for full configuration.

### JC.Identity

```csharp
builder.Services.AddIdentity<AppUser, AppRole, AppDbContext>();

var app = builder.Build();
app.UseIdentity();
await app.ConfigureAdminAndRolesAsync<AppUser, AppRoles, AppRole>(setupTenancy: true);
```

See [JC.Identity documentation](Documentation/JC.Identity/) for multi-tenancy, custom IUserInfo, and role configuration.

### JC.Web

```csharp
// Register all services
builder.Services.AddWebDefaults(builder.Configuration);

// Apply middleware
app.UseWebDefaults();

// Optional: rate limiting (opt-in, not included in WebDefaults)
builder.Services.AddRateLimiting();
app.UseRateLimiting();
```

See [JC.Web documentation](Documentation/JC.Web/) for security headers, cookie management, client profiling, rate limiting, bug reporter, and UI helpers.

### JC.Github

```csharp
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
  "Cookies": {
    "DataProtection_Path": "/path/to/keys"
  }
}
```

Required when using encrypted cookies (enabled by default in `AddWebDefaults` / `AddCookieServices`). Set `useEncryptedCookies: false` to skip.

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
| JC.Web | [Setup](Documentation/JC.Web/Setup.md) | [Guide](Documentation/JC.Web/Guide.md) | [API](Documentation/JC.Web/API.md) |
| JC.Identity | [Setup](Documentation/JC.Identity/Setup.md) | [Guide](Documentation/JC.Identity/Guide.md) | [API](Documentation/JC.Identity/API.md) |
| JC.Github | [Setup](Documentation/JC.Github/Setup.md) | [Guide](Documentation/JC.Github/Guide.md) | [API](Documentation/JC.Github/API.md) |
| JC.BackgroundJobs | [Setup](Documentation/JC.BackgroundJobs/Setup.md) | [Guide](Documentation/JC.BackgroundJobs/Guide.md) | [API](Documentation/JC.BackgroundJobs/API.md) |
| JC.MySql / JC.SqlServer | [Database Setup](Documentation/JC.Core/Database-Setup.md) | — | — |

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

- `JC.Core` = `3.1.0`
- `JC.Web` = `3.1.4`
- `JC.Identity` = `3.1.0`

That is valid.

If `JC.Core` is patched, all packages bump their own patch version by 1 (e.g. `JC.Web` `3.1.4` becomes `3.1.5`, `JC.Identity` `3.1.0` becomes `3.1.1`).

### Why

This approach keeps suite compatibility easy to understand while still allowing individual packages to ship small fixes independently.

In short:

- **Major/Minor = suite compatibility**
- **Patch = package-specific**
- **`JC.Core` patch = suite-wide patch**

## License

[MIT](LICENSE)
