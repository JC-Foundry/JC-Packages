# JC.MySql

Registers a [JC.Core](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.Core) `DbContext` against MySQL using Pomelo.EntityFrameworkCore.MySql. Interchangeable with [JC.SqlServer](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.SqlServer) — the two present the same shape, so the provider is a one-line choice.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.MySql/JC.MySql.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

## Prerequisites

- .NET 9.0 SDK
- **JC.Core**, registered with `AddCore<AppDbContext>()`
- A reachable MySQL server — the server version is detected from the connection string at start-up

## Quick start

### Services — `Program.cs`

```csharp
builder.Services.AddCore<AppDbContext>();
builder.Services.AddMySqlDatabase<AppDbContext>(builder.Configuration, migrationsAssembly: "MyApp");
```

The non-generic overload registers JC.Core's built-in `DataDbContext` instead, for applications that need no context of their own:

```csharp
builder.Services.AddMySqlDatabase(builder.Configuration, migrationsAssembly: "MyApp");
```

### Configuration — `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=myapp;User=app;Password=secret;"
  }
}
```

A missing connection string throws at registration, so the failure surfaces at start-up rather than on the first query.

## Feature areas

### Provider options

```csharp
builder.Services.AddMySqlDatabase<AppDbContext>(
    builder.Configuration,
    migrationsAssembly: "MyApp",
    connectionStringName: "DefaultConnection",
    mySqlOptions: mysql => mysql.EnableRetryOnFailure(),
    addHealthCheck: false);
```

The callback receives Pomelo's own `MySqlDbContextOptionsBuilder`, so anything the provider supports is reachable — retries, command timeouts and the rest. The migrations assembly is set for you before the callback runs.

### Health checks

Opt in and a MySQL check is registered under the name `mysql`:

```csharp
builder.Services.AddMySqlDatabase<AppDbContext>(
    builder.Configuration, "MyApp", addHealthCheck: true);
```

Map it with ASP.NET Core's usual `app.MapHealthChecks("/health")`.

### Server version detection

The MySQL server version is auto-detected from the connection string, which means the database must be reachable when the application starts. That is deliberate — an application that cannot reach its database should not start — but it does make MySQL less forgiving than SQL Server here.

### Multiple contexts

Call it once per context, then reach each through `IRepositoryManager.For<T>()`:

```csharp
builder.Services.AddMySqlDatabase<AppDbContext>(builder.Configuration, "MyApp");
builder.Services.AddMySqlDatabase<ReportingDbContext>(builder.Configuration, "MyApp", "ReportingConnection");
```

## Defaults

| Parameter | Default |
|-----------|---------|
| `connectionStringName` | `DefaultConnection` |
| `mySqlOptions` | None |
| `addHealthCheck` | `false` |
| `migrationsAssembly` | Required — no default |
| Server version | Detected from the connection string |

## Documentation

- [Database Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Core/Database-Setup.md) — provider registration, connection strings and migrations
- [JC.Core Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.Core/Setup.md) — what the registered context is used for

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
