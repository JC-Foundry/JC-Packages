# JC.BackgroundJobs

Background job hosting for .NET applications, built on the single `IBackgroundJob` contract defined in JC.Core. Two independent paths share it — lightweight in-process recurring jobs, and Hangfire-backed jobs with cron scheduling and ad-hoc dispatch. Your job class holds only the work; looping, error handling and lifecycle belong to the infrastructure.

Part of [JC-Packages](https://github.com/JC-Foundry/JC-Packages), a suite of .NET 9 packages providing shared infrastructure for .NET applications.

## Install

These packages are not published to NuGet.org. Reference the project directly:

```xml
<ProjectReference Include="path/to/JC.BackgroundJobs/JC.BackgroundJobs.csproj" />
```

Or pack them to a local feed and reference by package ID. See [Installation](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#installation).

## Prerequisites

- .NET 9.0 SDK
- **JC.Core** — `IBackgroundJob` is defined there
- For the Hangfire path only: a configured Hangfire storage provider, such as [JC.SqlServer.Hangfire](https://github.com/JC-Foundry/JC-Packages/tree/master/JC.SqlServer.Hangfire)

## Quick start

### Define a job

```csharp
public class CleanupJob : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Only the work — no loop, no try/catch
        await Task.CompletedTask;
    }
}
```

### Services — `Program.cs`

```csharp
// Hosted service path — no external dependencies
builder.Services.AddBackgroundJob<CleanupJob>();

// Hangfire path — requires storage, registered separately
builder.Services.AddHangfireSqlServer(builder.Configuration);
builder.Services.AddHangfireJob<ReportGenerationJob>(options => options.Cron = "0 2 * * *");
```

Nothing is added to the pipeline — hosted services start with the host, and Hangfire recurring jobs are registered at start-up by an internal hosted service.

## Feature areas

### Hosted service jobs

A recurring job on .NET's `BackgroundService`, with no external dependencies:

```csharp
builder.Services.AddBackgroundJob<CleanupJob>(options =>
{
    options.Interval = TimeSpan.FromMinutes(15);
    options.InitialDelay = TimeSpan.FromSeconds(30);
    options.ErrorBehavior = JobErrorBehavior.Continue;
    options.LogBehavior = JobLogBehavior.LogAll;
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.ExecutionTimeout = TimeSpan.FromMinutes(5);
});
```

`ErrorBehavior` decides what a thrown exception costs you: `Continue` logs and waits for the next interval, `Stop` ends that job permanently, `Throw` re-throws and may take the host down with it.

### Hangfire recurring jobs

Persistent, cron-scheduled, and visible in the Hangfire dashboard:

```csharp
builder.Services.AddHangfireJob<ReportGenerationJob>(options =>
{
    options.Cron = "0 2 * * *";
    options.Queue = "reports";
    options.JobId = "nightly-reports";
    options.TimeZone = TimeZoneInfo.Utc;
    options.ExecutionTimeout = TimeSpan.FromMinutes(30);
});
```

Storage is deliberately not registered here, so the storage provider and this package version and deploy independently.

### Ad-hoc scheduling

Fire-and-forget, delayed and continuation jobs dispatched at runtime:

```csharp
builder.Services.AddHangfireScheduler(
    AdHocJobRegistration.For<OrderConfirmationJob>(),
    AdHocJobRegistration.For<FollowUpEmailJob>());
```

```csharp
public class OrderService(IHangfireScheduler scheduler)
{
    public void Placed()
    {
        var id = scheduler.Enqueue<OrderConfirmationJob>();
        scheduler.Schedule<FollowUpEmailJob>(TimeSpan.FromDays(3));
        scheduler.ContinueWith<AuditJob>(id);
    }
}
```

### Execution timeouts

Both paths accept `ExecutionTimeout`. When it elapses, the token passed to `ExecuteAsync` is cancelled — so a long-running loop must check `cancellationToken.IsCancellationRequested` for the timeout to take effect. On the hosted path a timeout is logged as a warning and the job continues on its next interval; it does not trigger `ErrorBehavior`.

### Per-context jobs

Closed generic job types are named distinctly, so registering the same job against several DbContexts does not collide on one Hangfire job ID:

```csharp
builder.Services.AddHangfireJob<AuditCleanupJob<PortfolioDbContext>>(o => o.JobId = "portfolio:audit-cleanup");
builder.Services.AddHangfireJob<AuditCleanupJob<ShopDbContext>>(o => o.JobId = "shop:audit-cleanup");
```

## Defaults

| Option | Default |
|--------|---------|
| `BackgroundJobOptions.Interval` | 1 minute |
| `BackgroundJobOptions.InitialDelay` | 10 seconds |
| `BackgroundJobOptions.ErrorBehavior` | `Continue` |
| `BackgroundJobOptions.LogBehavior` | `LogAll` |
| `BackgroundJobOptions.ServiceLifetime` | `Scoped` |
| `HangfireJobOptions.Cron` | `* * * * *` (every minute) |
| `HangfireJobOptions.Queue` | `default` |
| `HangfireJobOptions.JobId` | The job type's name |
| `HangfireJobOptions.TimeZone` | UTC |
| `HangfireJobOptions.MisfireHandling` | `Relaxed` |
| `ExecutionTimeout` (both) | None |

## Documentation

- [Setup](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.BackgroundJobs/Setup.md) — registration, every option and its default
- [Guide](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.BackgroundJobs/Guide.md) — job patterns, error handling strategies, scheduler usage
- [API Reference](https://github.com/JC-Foundry/JC-Packages/blob/master/Documentation/JC.BackgroundJobs/API.md)

## Versioning

Major and minor versions are shared across the whole suite; patch versions are package-specific. See [Versioning Strategy](https://github.com/JC-Foundry/JC-Packages/blob/master/README.md#versioning-strategy).

## Licence

[MIT](https://github.com/JC-Foundry/JC-Packages/blob/master/LICENSE)
