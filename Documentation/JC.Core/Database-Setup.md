# Database providers — Setup

JC.MySql and JC.SqlServer are interchangeable database provider packages. Both register your `DbContext` with EF Core using a connection string from configuration. Pick whichever matches your database — the API is identical.

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- JC.Core already registered
- A running MySQL or SQL Server instance
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project

## 0. Add the package

Add a project reference to **one** of the database providers:

```xml
<!-- MySQL (uses Pomelo.EntityFrameworkCore.MySql) -->
<ProjectReference Include="path/to/JC.MySql/JC.MySql.csproj" />

<!-- SQL Server (uses Microsoft.EntityFrameworkCore.SqlServer) -->
<ProjectReference Include="path/to/JC.SqlServer/JC.SqlServer.csproj" />
```

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

### Configuration — `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-connection-string"
  }
}
```

### Services — `Program.cs`

```csharp
// MySQL
builder.Services.AddMySqlDatabase<AppDbContext>(builder.Configuration, migrationsAssembly: "YourApp");

// SQL Server
builder.Services.AddSqlServerDatabase<AppDbContext>(builder.Configuration, migrationsAssembly: "YourApp");
```

### Defaults

| Default | Value |
|---------|-------|
| Connection string name | `"DefaultConnection"` — reads from `ConnectionStrings:DefaultConnection` |
| Migrations assembly | *required* — the assembly name containing your EF Core migrations |
| Provider-specific options | None applied beyond the migrations assembly |
| Health check | Not registered |
| MySQL server version | Auto-detected from the connection string (MySQL only) |

The method registers your `DbContext` with `AddDbContext<TContext>`, configured with the appropriate provider and migrations assembly. It does not register JC.Core services — call `AddCore<TContext>()` separately.

## 2. Full configuration

### AddMySqlDatabase / AddSqlServerDatabase — generic overload

Both methods have two overloads: a generic version where you specify your `DbContext`, and a non-generic version that defaults to `DataDbContext`.

```csharp
// MySQL — with all options set to defaults
builder.Services.AddMySqlDatabase<AppDbContext>(
    configuration: builder.Configuration,
    migrationsAssembly: "YourApp",
    connectionStringName: "DefaultConnection",
    mySqlOptions: mysql =>
    {
        // Configure Pomelo MySqlDbContextOptionsBuilder here
        // e.g. mysql.EnableRetryOnFailure();
    },
    addHealthCheck: false
);

// SQL Server — with all options set to defaults
builder.Services.AddSqlServerDatabase<AppDbContext>(
    configuration: builder.Configuration,
    migrationsAssembly: "YourApp",
    connectionStringName: "DefaultConnection",
    sqlServerOptions: sql =>
    {
        // Configure SqlServerDbContextOptionsBuilder here
        // e.g. sql.EnableRetryOnFailure();
    },
    addHealthCheck: false
);
```

| Type parameter | Constraint | Description |
|---------------|-----------|-------------|
| `TContext` | `DbContext, IDataDbContext` | Your DbContext. Extend `DataDbContext` (or `IdentityDataDbContext` if using JC.Identity) which implements `IDataDbContext` for you |

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configuration` | `IConfiguration` | *required* | Application configuration — reads the connection string from the `ConnectionStrings` section |
| `migrationsAssembly` | `string` | *required* | The name of the assembly containing your EF Core migrations (typically your application's project name) |
| `connectionStringName` | `string` | `"DefaultConnection"` | The key within `ConnectionStrings` to read. Throws `InvalidOperationException` if not found |
| `mySqlOptions` / `sqlServerOptions` | `Action<MySqlDbContextOptionsBuilder>?` / `Action<SqlServerDbContextOptionsBuilder>?` | `null` | Optional callback to configure provider-specific options (retry policies, command timeouts, etc.) |
| `addHealthCheck` | `bool` | `false` | When `true`, registers an ASP.NET Core health check for the database. Adds a health check named `"mysql"` or `"sqlserver"` using the connection string |

### Non-generic overload

If your DbContext is the base `DataDbContext` from JC.Core, you can omit the type parameter:

```csharp
// MySQL — registers DataDbContext directly
builder.Services.AddMySqlDatabase(builder.Configuration, migrationsAssembly: "YourApp");

// SQL Server — registers DataDbContext directly
builder.Services.AddSqlServerDatabase(builder.Configuration, migrationsAssembly: "YourApp");
```

This delegates to the generic overload with `DataDbContext` as the type parameter. Accepts the same optional parameters.

### Using a named connection string

If your connection string uses a different key:

```json
{
  "ConnectionStrings": {
    "ProductionDb": "your-connection-string"
  }
}
```

```csharp
builder.Services.AddMySqlDatabase<AppDbContext>(
    builder.Configuration,
    migrationsAssembly: "YourApp",
    connectionStringName: "ProductionDb"
);
```

### Health checks

When `addHealthCheck` is `true`, a basic connectivity health check is registered using the same connection string:

```csharp
builder.Services.AddMySqlDatabase<AppDbContext>(
    builder.Configuration,
    migrationsAssembly: "YourApp",
    addHealthCheck: true
);

// Map the health check endpoint (standard ASP.NET Core)
app.MapHealthChecks("/health");
```

The health check name is `"mysql"` for JC.MySql or `"sqlserver"` for JC.SqlServer.

## 3. Apply migrations

After registering the database provider, generate and apply migrations:

```bash
dotnet ef migrations add Initial --project YourApp
dotnet ef database update --project YourApp
```

Alternatively, generate the migration and apply it programmatically at startup:

```bash
dotnet ef migrations add Initial --project YourApp
```

```csharp
await app.Services.MigrateDatabaseAsync<AppDbContext>();
```

## 4. Verify

1. Run the application.
2. Check that the database has been created with the expected tables.
3. If health checks are enabled, hit `/health` and confirm a healthy response.

## Next steps

- [JC.Core Setup](Setup.md) — repository pattern, audit trail, and service registration.
- [JC.Core Guide](Guide.md) — repository usage, soft-delete, pagination, and helpers.
