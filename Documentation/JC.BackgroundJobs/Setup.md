# JC.BackgroundJobs — Setup

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An existing ASP.NET Core project
- See [Installation](../../README.md#installation) for how to add JC-Packages to your project
- For Hangfire jobs: a configured Hangfire storage provider (e.g. `JC.SqlServer.Hangfire`)

## 0. Add the package

Add a project reference to `JC.BackgroundJobs`:

```xml
<ProjectReference Include="path/to/JC.BackgroundJobs/JC.BackgroundJobs.csproj" />
```

See [Versioning Strategy](../../README.md#versioning-strategy) to understand which version to use.

## 1. Quick setup

JC.BackgroundJobs provides two independent paths for running background work:

1. **Hosted service jobs** — lightweight, in-process recurring jobs using .NET's `BackgroundService`. No external dependencies.
2. **Hangfire jobs** — persistent, feature-rich jobs backed by Hangfire. Requires a storage provider.

Both paths use the same `IBackgroundJob` interface. Your job class contains only the work — looping, error handling, and lifecycle management are handled by the infrastructure.

### Define a job

```csharp
public class CleanupJob : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Your job logic here — no try/catch or looping needed
        await Task.CompletedTask;
    }
}
```

### Hosted service path — `Program.cs`

```csharp
// Register a recurring hosted-service job with default options
builder.Services.AddBackgroundJob<CleanupJob>();
```

### Hangfire path — `Program.cs`

```csharp
// Register Hangfire storage (e.g. via JC.SqlServer.Hangfire)
builder.Services.AddHangfireSqlServer(builder.Configuration);

// Register a recurring Hangfire job with default options
builder.Services.AddHangfireJob<CleanupJob>();

// Optional: register the ad-hoc scheduler and its job types for fire-and-forget, delayed, and continuation jobs
builder.Services.AddHangfireScheduler(
    AdHocJobRegistration.For<OrderConfirmationJob>(),
    AdHocJobRegistration.For<FollowUpEmailJob>()
);
```

### Configuration — `appsettings.json` (Hangfire only)

Hangfire requires a connection string for its storage. When using `JC.SqlServer.Hangfire`, add:

```json
{
  "ConnectionStrings": {
    "HangfireConnection": "Server=.;Database=HangfireDb;Trusted_Connection=true;"
  }
}
```

### Defaults

#### Hosted service jobs — `AddBackgroundJob<TJob>()`

| Option | Default | Description |
|--------|---------|-------------|
| `Interval` | 1 minute | Time between job executions |
| `InitialDelay` | 10 seconds | Delay before the first execution after the host starts |
| `ErrorBehavior` | `Continue` | Log the error and continue running on the next interval |
| `LogBehavior` | `LogAll` | Log both informational and error messages |
| `ServiceLifetime` | `Scoped` | DI lifetime used to resolve the job — a fresh scope is created per tick |

The job class is registered in DI with the specified lifetime. A `BackgroundService` wrapper manages the execution loop, creating a new DI scope for each tick when the lifetime is `Scoped` or `Transient`. `Singleton` jobs are resolved from the root provider.

#### Hangfire jobs — `AddHangfireJob<TJob>()`

| Option | Default | Description |
|--------|---------|-------------|
| `Cron` | `"* * * * *"` (every minute) | Cron expression for the recurring schedule |
| `Queue` | `"default"` | Hangfire queue name |
| `JobId` | Job type name | Unique identifier for the recurring job |
| `TimeZone` | `TimeZoneInfo.Utc` | Time zone for cron evaluation |
| `MisfireHandling` | `Relaxed` | How missed executions are handled when the server was offline |

The job class is registered as scoped in DI. At startup, a hosted service registers all collected recurring jobs with Hangfire's `IRecurringJobManager`.

#### Hangfire scheduler — `AddHangfireScheduler()`

Registers `IHangfireScheduler` as a scoped service. Optionally registers ad-hoc job types in DI with their configured lifetimes. Provides methods for scheduling ad-hoc jobs at runtime:

| Method | Description |
|--------|-------------|
| `Enqueue<TJob>()` | Fire-and-forget — executes immediately |
| `Schedule<TJob>(TimeSpan delay)` | Executes after the specified delay |
| `Schedule<TJob>(DateTimeOffset enqueueAt)` | Executes at the specified UTC time |
| `ContinueWith<TJob>(string parentJobId)` | Executes after the parent job completes successfully |

All methods return the Hangfire job ID as a `string`.

## 2. Full configuration

### AddBackgroundJob — hosted service registration

```csharp
builder.Services.AddBackgroundJob<CleanupJob>(options =>
{
    options.Interval = TimeSpan.FromMinutes(1);
    options.InitialDelay = TimeSpan.FromSeconds(10);
    options.ErrorBehavior = JobErrorBehavior.Continue;
    options.LogBehavior = JobLogBehavior.LogAll;
    options.ServiceLifetime = ServiceLifetime.Scoped;
});
```

| Type parameter | Constraint | Description |
|---------------|-----------|-------------|
| `TJob` | `class, IBackgroundJob` | Your job class implementing `IBackgroundJob` |

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configure` | `Action<BackgroundJobOptions>` | `_ => { }` | Callback to configure the job's options |

#### BackgroundJobOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Interval` | `TimeSpan` | `TimeSpan.FromMinutes(1)` | Time between executions. The timer starts after the previous execution completes — not wall-clock aligned |
| `InitialDelay` | `TimeSpan` | `TimeSpan.FromSeconds(10)` | Delay before the first execution. Useful to avoid work during application startup |
| `ErrorBehavior` | `JobErrorBehavior` | `Continue` | How the wrapper handles exceptions thrown by `ExecuteAsync` |
| `LogBehavior` | `JobLogBehavior` | `LogAll` | Controls which lifecycle messages the wrapper logs |
| `ServiceLifetime` | `ServiceLifetime` | `Scoped` | The DI lifetime for the job class. `Scoped` and `Transient` create a new scope per tick; `Singleton` resolves from the root provider |

#### JobErrorBehavior

| Value | Description |
|-------|-------------|
| `Continue` | Log the error and continue — the job runs again on the next interval |
| `Stop` | Log the error and stop the job permanently — the hosted service exits its loop |
| `Throw` | Re-throw the exception, terminating the hosted service. This may crash the application depending on host configuration |

#### JobLogBehavior

| Value | Description |
|-------|-------------|
| `None` | No lifecycle or error logging from the wrapper |
| `LogErrorsOnly` | Log errors only — no informational start/complete/stop messages |
| `LogInfoOnly` | Log informational messages only — errors are silenced |
| `LogAll` | Log both informational and error messages |

**Note:** When `ErrorBehavior` is `Throw`, the critical-level log is always written regardless of `LogBehavior` — the application is about to crash, so the log is essential.

#### Registering multiple jobs

Each `AddBackgroundJob` call registers an independent hosted service with its own options:

```csharp
builder.Services.AddBackgroundJob<CleanupJob>(options =>
{
    options.Interval = TimeSpan.FromHours(1);
    options.ErrorBehavior = JobErrorBehavior.Stop;
});

builder.Services.AddBackgroundJob<HealthCheckJob>(options =>
{
    options.Interval = TimeSpan.FromSeconds(30);
    options.LogBehavior = JobLogBehavior.LogErrorsOnly;
});
```

#### DI lifetime behaviour

Jobs that depend on scoped services (e.g. `DbContext`, repositories) should use the default `Scoped` lifetime. The wrapper creates a fresh `IServiceScope` for each tick and resolves the job from that scope.

Jobs that hold long-lived state or expensive resources can use `Singleton`. Singleton jobs are resolved from the root `IServiceProvider` — they must not depend on scoped services (this will throw at runtime).

```csharp
// Scoped job (default) — safe to inject DbContext, repositories, etc.
builder.Services.AddBackgroundJob<DatabaseCleanupJob>();

// Singleton job — resolved once from the root provider
builder.Services.AddBackgroundJob<CacheRefreshJob>(options =>
{
    options.ServiceLifetime = ServiceLifetime.Singleton;
});
```

### AddHangfireJob — recurring Hangfire job registration

```csharp
builder.Services.AddHangfireJob<ReportGenerationJob>(options =>
{
    options.Cron = "* * * * *";
    options.Queue = "default";
    options.JobId = "ReportGenerationJob";
    options.TimeZone = TimeZoneInfo.Utc;
    options.MisfireHandling = MisfireHandlingMode.Relaxed;
});
```

| Type parameter | Constraint | Description |
|---------------|-----------|-------------|
| `TJob` | `class, IBackgroundJob` | Your job class implementing `IBackgroundJob` |

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `configure` | `Action<HangfireJobOptions>` | `_ => { }` | Callback to configure the job's Hangfire options |

#### HangfireJobOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Cron` | `string` | `"* * * * *"` | Cron expression for the recurring schedule. Standard five-field cron syntax |
| `Queue` | `string` | `"default"` | The Hangfire queue this job is assigned to. The Hangfire server must be configured to process this queue |
| `JobId` | `string?` | `null` (falls back to type name) | Unique identifier for the recurring job in Hangfire. Defaults to the job class name (e.g. `"ReportGenerationJob"`) |
| `TimeZone` | `TimeZoneInfo` | `TimeZoneInfo.Utc` | Time zone used for cron evaluation |
| `MisfireHandling` | `MisfireHandlingMode` | `Relaxed` | How missed executions are handled. `Relaxed` executes once when the server comes back; `Strict` attempts to catch up on every missed execution |

Hangfire jobs are registered as scoped in DI. Hangfire creates its own scope when executing a job, so scoped dependencies (DbContext, repositories) work correctly.

#### Retry attempts

JC.BackgroundJobs does not configure retry behaviour — Hangfire's default retry policy applies (10 automatic retries with exponential backoff). To customise retries, use Hangfire's `[AutomaticRetry]` attribute on your job class:

```csharp
[AutomaticRetry(Attempts = 3)]
public class FragileJob : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Job work
    }
}
```

#### Registering multiple Hangfire jobs

```csharp
builder.Services.AddHangfireJob<EmailDigestJob>(options =>
{
    options.Cron = "0 8 * * *"; // Daily at 08:00 UTC
    options.Queue = "emails";
});

builder.Services.AddHangfireJob<DataSyncJob>(options =>
{
    options.Cron = "*/15 * * * *"; // Every 15 minutes
    options.Queue = "sync";
});
```

All `AddHangfireJob` calls share a single internal registry. At startup, a hosted service iterates the registry and registers each job with Hangfire's `IRecurringJobManager`.

### AddHangfireScheduler — ad-hoc job scheduling

Register the scheduler without any job types (you must register them in DI yourself):

```csharp
builder.Services.AddHangfireScheduler();
```

Or register the scheduler and its ad-hoc job types in one call using `AdHocJobRegistration`:

```csharp
builder.Services.AddHangfireScheduler(
    AdHocJobRegistration.For<OrderConfirmationJob>(),
    AdHocJobRegistration.For<FollowUpEmailJob>(),
    AdHocJobRegistration.For<OrderCleanupJob>(ServiceLifetime.Singleton)
);
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `jobs` | `params AdHocJobRegistration[]` | — | Ad-hoc job types to register in DI with their configured lifetimes |

#### AdHocJobRegistration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `JobType` | `Type` | — | The job class type implementing `IBackgroundJob` |
| `Lifetime` | `ServiceLifetime` | `Scoped` | The DI lifetime for the job class |

Use the generic factory method `AdHocJobRegistration.For<TJob>()` for a concise, type-safe call site. Pass an optional `ServiceLifetime` to override the default scoped lifetime.

Inject `IHangfireScheduler` into controllers, services, or Razor pages to schedule jobs at runtime:

```csharp
public class OrderService(IHangfireScheduler scheduler)
{
    public void ProcessOrder(int orderId)
    {
        // Fire-and-forget
        scheduler.Enqueue<OrderConfirmationJob>();

        // Delayed — send follow-up email in 24 hours
        scheduler.Schedule<FollowUpEmailJob>(TimeSpan.FromHours(24));

        // Continuation — run cleanup after confirmation completes
        var confirmationId = scheduler.Enqueue<OrderConfirmationJob>();
        scheduler.ContinueWith<OrderCleanupJob>(confirmationId);
    }
}
```

`AddHangfireScheduler` uses `TryAddScoped` for the scheduler and `TryAdd` for job types, so calling it multiple times is safe. It is also registered automatically when `AddHangfireJob` is called — you only need to call `AddHangfireScheduler` explicitly if you use the ad-hoc scheduler without any recurring jobs.

### Hangfire dashboard

The Hangfire dashboard is provided by Hangfire itself — JC.BackgroundJobs does not wrap it. Add it directly in your middleware pipeline:

```csharp
var app = builder.Build();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "My App — Background Jobs",
    Authorization = [new MyDashboardAuthFilter()]
});
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| Path (first argument) | `string` | `"/hangfire"` | The URL path where the dashboard is served |
| `DashboardTitle` | `string` | `"Hangfire Dashboard"` | Title displayed in the dashboard header |
| `Authorization` | `IDashboardAuthorizationFilter[]` | Allows local requests only | Authorization filters — in production, always provide a filter that checks authentication |
| `IsReadOnlyFunc` | `Func<DashboardContext, bool>` | `_ => false` | When `true`, hides action buttons (retry, delete, trigger) |
| `StatsPollingInterval` | `int` | `2000` | Polling interval in milliseconds for real-time statistics |

**Important:** By default, the dashboard only allows local requests. In production, you must provide an `IDashboardAuthorizationFilter` implementation that verifies the user is authorised to view the dashboard:

```csharp
public class MyDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Admin");
    }
}
```

The dashboard requires `Hangfire.AspNetCore` — if you're using `JC.SqlServer.Hangfire`, this dependency is already included.

### Combining both paths

The hosted service and Hangfire paths are independent. You can use both in the same application:

```csharp
// Lightweight in-process job — no external dependencies
builder.Services.AddBackgroundJob<CacheWarmupJob>(options =>
{
    options.Interval = TimeSpan.FromMinutes(5);
    options.ServiceLifetime = ServiceLifetime.Singleton;
});

// Persistent Hangfire job — survives restarts, visible in dashboard
builder.Services.AddHangfireSqlServer(builder.Configuration);
builder.Services.AddHangfireJob<ReportGenerationJob>(options =>
{
    options.Cron = "0 2 * * *"; // Daily at 02:00 UTC
});
```

The same job class can even be registered on both paths (though this is unusual). Each path manages its own scheduling independently.

## 3. Verify

1. **Hosted service jobs:** Run the application and check the logs — you should see `"{JobName} starting"`, `"{JobName} executing"`, and `"{JobName} completed"` messages at the configured interval.
2. **Hangfire jobs:** Run the application and check the Hangfire dashboard (if configured) or the Hangfire storage tables — your recurring job should appear with its cron schedule.

## Next steps

- [Guide](Guide.md) — job patterns, error handling strategies, and scheduler usage.
- [API Reference](API.md)
