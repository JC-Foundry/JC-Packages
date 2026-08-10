# JC.SqlServer.Hangfire

One call that stands Hangfire up on SQL Server storage and starts a job server. This is the storage half that [JC.BackgroundJobs](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.BackgroundJobs)' Hangfire path expects but deliberately does not provide, so the two version and deploy separately.

Standalone — it takes no dependency on JC.Core or any other JC package.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.SqlServer.Hangfire/JC.SqlServer.Hangfire.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

## Prerequisites

- .NET 9.0 SDK, and an ASP.NET Core project
- A SQL Server instance the application can create tables in — the Hangfire schema is prepared on first run

## Quick start

### Services — `Program.cs`

```csharp
builder.Services.AddHangfireSqlServer(builder.Configuration);
```

That registers Hangfire with SQL Server storage **and** starts a background job server. Nothing else is required to begin scheduling.

### Configuration — `appsettings.json`

```json
{
  "ConnectionStrings": {
    "HangfireConnection": "Server=.;Database=HangfireDb;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

A missing connection string throws at registration. Hangfire is usually pointed at its own database, which is why the default name is `HangfireConnection` rather than `DefaultConnection`.

## Feature areas

### Defining and scheduling jobs

This package provides storage only. Jobs themselves come from [JC.BackgroundJobs](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.BackgroundJobs), which builds on JC.Core's `IBackgroundJob`:

```csharp
builder.Services.AddHangfireSqlServer(builder.Configuration);
builder.Services.AddHangfireJob<ReportGenerationJob>(options => options.Cron = "0 2 * * *");
```

The split means an application can swap storage providers without touching a job, and can take the job abstractions without committing to Hangfire at all.

### Storage, server and global options

Each is exposed as a callback:

```csharp
builder.Services.AddHangfireSqlServer(
    builder.Configuration,
    connectionStringName: "HangfireConnection",
    configureHangfire: config => config.UseFilter(new AutomaticRetryAttribute { Attempts = 3 }),
    configureSqlStorage: storage => storage.QueuePollInterval = TimeSpan.FromSeconds(15),
    configureServer: server =>
    {
        server.WorkerCount = 4;
        server.Queues = ["critical", "default"];
    });
```

`configureSqlStorage` runs before storage is constructed, `configureHangfire` after — so global configuration can see the storage already in place.

### Schema preparation

`PrepareSchemaIfNecessary` is on by default, so Hangfire creates its own tables on first run. Turn it off where migrations are managed separately and the account has no DDL rights:

```csharp
configureSqlStorage: storage => storage.PrepareSchemaIfNecessary = false
```

### The dashboard

Hangfire's dashboard is not registered here. Add it yourself, and gate it — it exposes job data and the ability to trigger work:

```csharp
app.UseHangfireDashboard("/hangfire");
```

For an application with no user accounts of its own, network-level restriction is a legitimate answer; see the [admin considerations](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Admin-Considerations.md) note.

## Defaults

| Parameter | Default |
|-----------|---------|
| `connectionStringName` | `HangfireConnection` |
| `PrepareSchemaIfNecessary` | `true` |
| `configureHangfire` / `configureSqlStorage` / `configureServer` | None |
| Job server | Started, with Hangfire's own default options |
| Dashboard | Not registered |

## Documentation

This package has no documentation folder of its own — one call, covered here. For the jobs that run on it:

- [JC.BackgroundJobs Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.BackgroundJobs/Setup.md)
- [JC.BackgroundJobs Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.BackgroundJobs/Guide.md)

## Versioning

Major and minor versions are shared across the whole suite. This package is standalone, so JC.Core patch releases do not bump it. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
