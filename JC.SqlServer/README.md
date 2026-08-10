# JC.SqlServer

Registers a [JC.Core](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.Core) `DbContext` against SQL Server using Microsoft.EntityFrameworkCore.SqlServer. Interchangeable with [JC.MySql](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.MySql) — the two present the same shape, so the provider is a one-line choice.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.SqlServer/JC.SqlServer.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

## Prerequisites

- .NET 9.0 SDK
- **JC.Core**, registered with `AddCore<AppDbContext>()`
- A SQL Server instance and connection string

## Quick start

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.AddSqlServerDatabase<AppDbContext>(builder.Configuration, migrationsAssembly: "MyApp");
```

The non-generic overload registers JC.Core's built-in `DataDbContext` instead, for applications that need no context of their own:

```csharp
builder.Services.AddSqlServerDatabase(builder.Configuration, migrationsAssembly: "MyApp");
```

### Configuration — `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MyApp;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

A missing connection string throws at registration, so the failure surfaces at start-up rather than on the first query.

## Feature areas

### Provider options

```csharp
builder.Services.AddSqlServerDatabase<AppDbContext>(
    builder.Configuration,
    migrationsAssembly: "MyApp",
    connectionStringName: "DefaultConnection",
    sqlServerOptions: sql => sql.EnableRetryOnFailure(),
    addHealthCheck: false);
```

The callback receives the provider's own `SqlServerDbContextOptionsBuilder`, so anything it supports is reachable — retry-on-failure, command timeouts and the rest. The migrations assembly is set for you before the callback runs.

### Health checks

Opt in and a SQL Server check is registered under the name `sqlserver`:

```csharp
builder.Services.AddSqlServerDatabase<AppDbContext>(
    builder.Configuration, "MyApp", addHealthCheck: true);
```

Map it with ASP.NET Core's usual `app.MapHealthChecks("/health")`.

### Multiple contexts

Call it once per context, then reach each through `IRepositoryManager.For<T>()`:

```csharp
builder.Services.AddSqlServerDatabase<AppDbContext>(builder.Configuration, "MyApp");
builder.Services.AddSqlServerDatabase<ReportingDbContext>(builder.Configuration, "MyApp", "ReportingConnection");
```

### Hangfire storage

Hangfire's own SQL Server storage is a separate concern and a separate package — see [JC.SqlServer.Hangfire](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.SqlServer.Hangfire), which takes no dependency on JC.Core and is usually pointed at its own database.

## Defaults

| Parameter | Default |
|-----------|---------|
| `connectionStringName` | `DefaultConnection` |
| `sqlServerOptions` | None |
| `addHealthCheck` | `false` |
| `migrationsAssembly` | Required — no default |

## Documentation

- [Database Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Core/Database-Setup.md) — provider registration, connection strings and migrations
- [JC.Core Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Core/Setup.md) — what the registered context is used for

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
