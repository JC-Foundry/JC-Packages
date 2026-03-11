# JC.BackgroundJobs — Guide

Covers writing job classes, hosted service job behaviour, Hangfire recurring jobs, ad-hoc scheduling, error handling strategies, and DI lifetime considerations. See [Setup](Setup.md) for registration.

## Writing jobs

### Basic job class

Implement `IBackgroundJob` with your work in `ExecuteAsync`. The infrastructure handles looping, timing, error handling, and DI scoping — your job is just the work:

```csharp
public class ExpiredSessionCleanupJob(AppDbContext db, ILogger<ExpiredSessionCleanupJob> logger) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);
        var expired = await db.Sessions
            .Where(s => s.LastActivity < cutoff)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
            return;

        db.Sessions.RemoveRange(expired);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Cleaned up {Count} expired sessions", expired.Count);
    }
}
```

The same job class works with both the hosted service path and the Hangfire path. The only difference is how it's registered and scheduled.

**Nuance:** Do not add `try`/`catch` around your job logic for the sake of error handling — the wrapper (hosted service) or Hangfire (recurring/ad-hoc) handles exceptions based on the configured behaviour. Only catch exceptions if your job needs to handle specific failure scenarios internally (e.g. partial batch processing).

**Nuance:** Do not loop inside `ExecuteAsync`. The hosted service wrapper calls `ExecuteAsync` on each tick at the configured interval. Hangfire calls it once per scheduled execution. Adding your own loop would duplicate the scheduling behaviour.

### Jobs with constructor dependencies

Jobs are resolved from DI, so use standard constructor injection:

```csharp
public class InvoiceReminderJob(
    IRepositoryContext<Invoice> invoices,
    IEmailService emailService,
    ILogger<InvoiceReminderJob> logger) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var overdue = await invoices.GetAllAsync(
            i => i.DueDate < DateTime.UtcNow && !i.Paid && !i.ReminderSent);

        foreach (var invoice in overdue)
        {
            await emailService.SendReminderAsync(invoice, cancellationToken);
            invoice.ReminderSent = true;
            await invoices.UpdateAsync(invoice);
        }

        logger.LogInformation("Sent {Count} invoice reminders", overdue.Count);
    }
}
```

For hosted service jobs, the DI lifetime of the job class is controlled by `BackgroundJobOptions.ServiceLifetime`. Scoped jobs (the default) get a fresh scope per tick — so `DbContext`, repositories, and other scoped services behave exactly as they would in a request.

### Cancellation token

For **hosted service jobs**, the `CancellationToken` passed to `ExecuteAsync` is the host's stopping token — it is signalled during graceful shutdown. Respect it for long-running operations:

```csharp
public class DataExportJob(AppDbContext db) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var batches = await db.Records.CountAsync(cancellationToken) / 1000;

        for (var i = 0; i < batches; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = await db.Records
                .Skip(i * 1000)
                .Take(1000)
                .ToListAsync(cancellationToken);

            await ProcessBatchAsync(batch);
        }
    }

    private Task ProcessBatchAsync(List<Record> batch) => Task.CompletedTask; // ...
}
```

**Nuance:** When the hosted service wrapper catches an `OperationCanceledException` and the stopping token is cancelled, it exits the loop cleanly without triggering the error behaviour. This is normal shutdown — not a job failure.

**Nuance:** When `ExecutionTimeout` is configured (see [Setup](Setup.md)), the token passed to `ExecuteAsync` is a linked token that triggers on either host shutdown or timeout. If the timeout fires, the wrapper logs a warning and continues to the next interval — it does not trigger the `ErrorBehavior`. Check `cancellationToken.IsCancellationRequested` inside long-running loops so your job exits promptly when the timeout fires.

**Nuance:** For **Hangfire jobs**, `ExecuteAsync` receives `CancellationToken.None` unless `ExecutionTimeout` is configured, in which case the job is wrapped with a timeout-linked token. Hangfire manages job cancellation through its own infrastructure (server shutdown, job deletion from dashboard), not via the token parameter. If your job is used exclusively with Hangfire and needs to respond to cancellation, use Hangfire's `IJobCancellationToken` directly. If your job is shared across both paths, the token is useful for hosted service shutdown but will not be signalled by Hangfire.

## Hosted service jobs

### Execution lifecycle

When the application starts, the hosted service wrapper for each registered job:

1. Waits for `InitialDelay` (default: 10 seconds)
2. Calls `ExecuteAsync` on the job
3. Waits for `Interval` (default: 1 minute) after the execution completes
4. Repeats from step 2 until the host shuts down

```
Host starts → [InitialDelay] → Execute → [Interval] → Execute → [Interval] → ...
```

**Nuance:** The interval is measured from when the previous execution finishes, not from when it started. A job that takes 30 seconds to run with a 1-minute interval will execute roughly every 90 seconds, not every 60 seconds.

**Nuance:** If `ExecutionTimeout` is configured, a job that exceeds the timeout is cancelled and the wrapper moves straight to the interval wait. The timed-out execution does not count as an error — it is logged as a warning and does not trigger `ErrorBehavior`.

### Error handling

The `ErrorBehavior` option controls what happens when `ExecuteAsync` throws:

```csharp
// Default — log and continue. Best for most jobs.
builder.Services.AddBackgroundJob<CleanupJob>(options =>
{
    options.ErrorBehavior = JobErrorBehavior.Continue;
});
```

With `Continue`, a single failure does not affect subsequent executions. The job runs again on the next interval as normal. This is appropriate for jobs where occasional failures are expected (e.g. external API calls, transient database errors).

```csharp
// Stop permanently on first error. Use when a failure indicates a broken state.
builder.Services.AddBackgroundJob<MigrationJob>(options =>
{
    options.ErrorBehavior = JobErrorBehavior.Stop;
});
```

With `Stop`, the hosted service exits its loop and will not execute again until the application restarts. Use this for jobs where continuing after a failure could cause data corruption or compound the problem.

```csharp
// Throw the exception — terminates the hosted service and may crash the application.
builder.Services.AddBackgroundJob<CriticalSyncJob>(options =>
{
    options.ErrorBehavior = JobErrorBehavior.Throw;
});
```

With `Throw`, the exception propagates out of the `BackgroundService`. Depending on your host configuration, this may terminate the application. Use this sparingly — only when a job failure is so critical that the application should not continue running.

**Nuance:** When `ErrorBehavior` is `Throw`, the wrapper always logs a critical-level message regardless of the `LogBehavior` setting. The application is about to crash, so the log is essential.

### Logging control

The `LogBehavior` option controls the wrapper's logging — it does not affect logging inside your job class:

```csharp
// Quiet mode — only log when something goes wrong
builder.Services.AddBackgroundJob<HealthCheckJob>(options =>
{
    options.Interval = TimeSpan.FromSeconds(10);
    options.LogBehavior = JobLogBehavior.LogErrorsOnly;
});
```

| Behaviour | Starting | Executing | Completed | Stopped | Errors |
|-----------|----------|-----------|-----------|---------|--------|
| `LogAll` | Yes | Yes | Yes | Yes | Yes |
| `LogErrorsOnly` | No | No | No | No | Yes |
| `LogInfoOnly` | Yes | Yes | Yes | Yes | No |
| `None` | No | No | No | No | No |

All wrapper log messages use the job type name as the `{Job}` parameter, making it easy to filter in structured logging.

### DI lifetime: scoped vs singleton

**Scoped (default)** — a new `IServiceScope` is created for each tick. The job and all its dependencies are resolved from that scope and disposed after `ExecuteAsync` completes:

```csharp
// Safe to inject DbContext, repositories, HttpClient factories, etc.
public class DatabaseReportJob(AppDbContext db, IRepositoryContext<Report> reports) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // db and reports are fresh per execution — no stale tracking context
        var count = await db.Orders.CountAsync(cancellationToken);
        await reports.AddAsync(new Report { OrderCount = count, GeneratedUtc = DateTime.UtcNow });
    }
}
```

**Singleton** — the job is resolved once from the root `IServiceProvider` and reused for every tick:

```csharp
builder.Services.AddBackgroundJob<CacheRefreshJob>(options =>
{
    options.ServiceLifetime = ServiceLifetime.Singleton;
});
```

```csharp
// Must not depend on scoped services (DbContext, repositories, etc.)
public class CacheRefreshJob(IMemoryCache cache, ILogger<CacheRefreshJob> logger) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // IMemoryCache is singleton — safe to use here
        cache.Set("last-refresh", DateTime.UtcNow);
        logger.LogInformation("Cache refreshed");
        await Task.CompletedTask;
    }
}
```

**Nuance:** Registering a singleton job that depends on scoped services (e.g. `DbContext`) will throw an `InvalidOperationException` at runtime — the DI container prevents resolving scoped services from the root provider. If your job needs database access, use the default `Scoped` lifetime.

## Hangfire recurring jobs

### How registration works

When you call `AddHangfireJob<TJob>()`, the job is not registered with Hangfire immediately. Instead, the registration is collected in an internal registry. At application startup, a hosted service iterates the registry and registers each job with Hangfire's `IRecurringJobManager`.

This means the Hangfire storage must be available when the application starts. If the database is unreachable, the registration service will fail.

### Cron expressions

Hangfire uses standard five-field cron syntax:

```csharp
builder.Services.AddHangfireJob<HourlyReportJob>(options =>
{
    options.Cron = "0 * * * *"; // Top of every hour
});

builder.Services.AddHangfireJob<DailyCleanupJob>(options =>
{
    options.Cron = "0 2 * * *"; // Daily at 02:00
});

builder.Services.AddHangfireJob<WeeklyDigestJob>(options =>
{
    options.Cron = "0 9 * * 1"; // Mondays at 09:00
});
```

| Field | Allowed values |
|-------|---------------|
| Minute | 0–59 |
| Hour | 0–23 |
| Day of month | 1–31 |
| Month | 1–12 |
| Day of week | 0–6 (Sunday = 0) |

**Nuance:** Cron evaluation uses the `TimeZone` option (default: UTC). If your users expect jobs to run at local times, set the time zone explicitly:

```csharp
builder.Services.AddHangfireJob<DailyReportJob>(options =>
{
    options.Cron = "0 9 * * *"; // 09:00 in the specified time zone
    options.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
});
```

### Queues

Assign jobs to specific queues to control processing priority or worker allocation:

```csharp
builder.Services.AddHangfireJob<EmailJob>(options =>
{
    options.Queue = "emails";
    options.Cron = "*/5 * * * *";
});

builder.Services.AddHangfireJob<ReportJob>(options =>
{
    options.Queue = "reports";
    options.Cron = "0 * * * *";
});
```

The Hangfire server must be configured to process these queues. When using `JC.SqlServer.Hangfire`:

```csharp
builder.Services.AddHangfireSqlServer(builder.Configuration, configureServer: server =>
{
    server.Queues = ["emails", "reports", "default"];
});
```

**Nuance:** If a job is assigned to a queue that no server processes, it will sit in the queue indefinitely. Always ensure your server configuration includes all queues your jobs use.

### Job identifiers

By default, the job ID is the class name (e.g. `"CleanupJob"`). Override it if you need a more descriptive identifier in the Hangfire dashboard:

```csharp
builder.Services.AddHangfireJob<CleanupJob>(options =>
{
    options.JobId = "expired-session-cleanup";
    options.Cron = "0 */6 * * *";
});
```

**Nuance:** Job IDs must be unique across all recurring jobs. If two jobs share the same ID, the second registration overwrites the first in Hangfire.

### Execution timeout

When `ExecutionTimeout` is configured on a Hangfire job, the job is wrapped with `HangfireTimeoutRunner` which creates a linked cancellation token. If the timeout fires, the runner logs a warning and re-throws the `OperationCanceledException` — this causes Hangfire to treat it as a failed execution, subject to the normal retry policy.

```csharp
builder.Services.AddHangfireJob<LongRunningExportJob>(options =>
{
    options.Cron = "0 2 * * *";
    options.ExecutionTimeout = TimeSpan.FromMinutes(10);
});
```

If the job is cancelled by Hangfire itself (server shutdown, job deletion from the dashboard) rather than by the timeout, the exception propagates without the timeout warning log.

### Retry attempts

JC.BackgroundJobs does not configure retry behaviour — Hangfire's default policy applies (10 automatic retries with exponential backoff). Customise retries using Hangfire's `[AutomaticRetry]` attribute on the job class:

```csharp
[AutomaticRetry(Attempts = 3)]
public class ExternalApiJob(HttpClient httpClient) : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await httpClient.GetAsync("https://api.example.com/sync", cancellationToken);
    }
}
```

The `[AutomaticRetry]` attribute works across all Hangfire job types — recurring, fire-and-forget, delayed, and continuation. To disable retries entirely:

```csharp
[AutomaticRetry(Attempts = 0)]
public class NoRetryJob : IBackgroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Fails permanently on first error
        await Task.CompletedTask;
    }
}
```

### Misfire handling

By default, recurring jobs use `MisfireHandlingMode.Relaxed`. If the Hangfire server was offline when a job was due to run, it executes once when the server comes back — rather than trying to catch up on every missed execution.

Switch to `Strict` if your job must run for every missed schedule (e.g. billing, sequential data processing):

```csharp
builder.Services.AddHangfireJob<BillingJob>(options =>
{
    options.Cron = "0 * * * *";
    options.MisfireHandling = MisfireHandlingMode.Strict;
});
```

| Mode | Behaviour |
|------|-----------|
| `Relaxed` | Execute once on recovery, skip missed intervals |
| `Strict` | Catch up on every missed execution |

## Ad-hoc scheduling

### Fire-and-forget

Inject `IHangfireScheduler` to schedule jobs at runtime:

```csharp
public class OrderController(IHangfireScheduler scheduler) : Controller
{
    [HttpPost]
    public IActionResult PlaceOrder(OrderModel model)
    {
        // ... save order ...

        var jobId = scheduler.Enqueue<OrderConfirmationEmailJob>();

        return Ok(new { OrderId = model.Id, BackgroundJobId = jobId });
    }
}
```

`Enqueue` queues the job for immediate execution and returns the Hangfire job ID. The job runs asynchronously — the controller returns immediately.

### Delayed execution

Schedule a job to run after a delay or at a specific time:

```csharp
public class TrialService(IHangfireScheduler scheduler)
{
    public void StartTrial(string userId)
    {
        // Send a reminder 7 days from now
        scheduler.Schedule<TrialExpiryReminderJob>(TimeSpan.FromDays(7));

        // Or schedule for a specific date/time
        var trialEnd = DateTimeOffset.UtcNow.AddDays(14);
        scheduler.Schedule<TrialExpiredJob>(trialEnd);
    }
}
```

### Continuations

Chain jobs so one runs after another completes successfully:

```csharp
public class ReportService(IHangfireScheduler scheduler)
{
    public void GenerateAndEmailReport()
    {
        var generateId = scheduler.Enqueue<GenerateReportJob>();
        scheduler.ContinueWith<EmailReportJob>(generateId);
    }
}
```

`ContinueWith` creates a continuation that only runs if the parent job succeeds. If the parent fails (after all retry attempts), the continuation remains in the `Awaiting` state.

**Nuance:** Continuation jobs do not receive data from the parent. If your continuation needs the output of the parent job, use a shared data store (database, cache) rather than trying to pass data through the job chain.

### Job registration for ad-hoc jobs

Ad-hoc jobs scheduled via `IHangfireScheduler` still need to be registered in DI. Unlike recurring jobs (which `AddHangfireJob` registers automatically), ad-hoc-only jobs must be registered. Pass them directly to `AddHangfireScheduler`:

```csharp
builder.Services.AddHangfireScheduler(
    AdHocJobRegistration.For<OrderConfirmationEmailJob>(),
    AdHocJobRegistration.For<FollowUpEmailJob>(),
    AdHocJobRegistration.For<CacheRefreshJob>(ServiceLifetime.Singleton)
);
```

Each job is registered with `TryAdd`, so if a job is already in DI (e.g. via `AddHangfireJob`), it won't be overwritten. The `ServiceLifetime` defaults to `Scoped` — override it for jobs that need singleton or transient resolution.

You can also call the no-args overload and register job types yourself if you prefer:

```csharp
builder.Services.AddScoped<OrderConfirmationEmailJob>();
builder.Services.AddHangfireScheduler();
```

## Combining hosted service and Hangfire jobs

### When to use which

| Consideration | Hosted service | Hangfire |
|--------------|---------------|---------|
| Persistence | In-memory — lost on restart | Persisted to database |
| Dashboard | Logs only | Full web dashboard |
| Retries | Manual via `ErrorBehavior` | Automatic with exponential backoff |
| Ad-hoc scheduling | Not supported | Fire-and-forget, delayed, continuations |
| External dependencies | None | Requires storage (e.g. SQL Server) |
| Ideal for | Cache refresh, health checks, lightweight periodic tasks | Email sending, report generation, data sync, anything that must survive restarts |

### Same job class, different backends

The same `IBackgroundJob` implementation can be registered on both paths. This is unusual but can be useful during migration:

```csharp
// Run as a quick hosted service during development
builder.Services.AddBackgroundJob<DataSyncJob>(options =>
{
    options.Interval = TimeSpan.FromMinutes(5);
});

// Run as a persistent Hangfire job in production
builder.Services.AddHangfireJob<DataSyncJob>(options =>
{
    options.Cron = "*/5 * * * *";
});
```

**Nuance:** If you register the same job on both paths simultaneously, it will run twice — once via the hosted service and once via Hangfire. This is almost never what you want. Use conditional registration or environment checks to pick one path per environment.
