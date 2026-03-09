# JC.BackgroundJobs — API reference

Complete reference of all public types, properties, and methods in JC.BackgroundJobs. See [Setup](Setup.md) for registration and [Guide](Guide.md) for usage examples.

## IBackgroundJob

**Namespace:** `JC.BackgroundJobs.Models`

Contract for a background job. Implementations provide the work; the infrastructure handles scheduling, looping, error handling, and DI scoping.

### Methods

#### ExecuteAsync(CancellationToken cancellationToken = default)

**Returns:** `Task`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `cancellationToken` | `CancellationToken` | `default` | Token signalling cancellation of the host or job. |

Called on each tick by the hosted-service wrapper, or once per scheduled execution by Hangfire. Implementations should contain only the actual job logic — looping, error handling, and lifecycle management are handled by the hosting infrastructure.

For hosted service jobs, the token is the host's stopping token and is signalled during graceful shutdown. When the token is cancelled during execution and an `OperationCanceledException` is thrown, the hosted-service wrapper exits its loop cleanly without triggering the configured error behaviour.

For Hangfire jobs, the token is always `CancellationToken.None`. Hangfire manages cancellation through its own infrastructure, not via this parameter.

---

## BackgroundJobOptions

**Namespace:** `JC.BackgroundJobs.Models`

Configuration options for a hosted-service background job registered via `AddBackgroundJob<TJob>`. Each job type receives its own options instance.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Interval` | `TimeSpan` | `TimeSpan.FromMinutes(1)` | get; set; | Time between job executions. Measured from when the previous execution completes, not wall-clock aligned. |
| `InitialDelay` | `TimeSpan` | `TimeSpan.FromSeconds(10)` | get; set; | Delay before the first execution after the host starts. |
| `ErrorBehavior` | `JobErrorBehavior` | `Continue` | get; set; | How the wrapper behaves when `ExecuteAsync` throws an exception. |
| `LogBehavior` | `JobLogBehavior` | `LogAll` | get; set; | Controls which lifecycle messages the wrapper logs. |
| `ServiceLifetime` | `ServiceLifetime` | `Scoped` | get; set; | The DI lifetime used to resolve the job class. `Scoped` and `Transient` create a new `IServiceScope` per tick; `Singleton` resolves from the root `IServiceProvider`. |

Validated at registration time: `Interval` must be greater than `TimeSpan.Zero` (`ArgumentOutOfRangeException`). `InitialDelay` must not be negative (`ArgumentOutOfRangeException`).

---

## BackgroundJobOptionsFor\<TJob\>

**Namespace:** `JC.BackgroundJobs.Models`

Typed wrapper around `BackgroundJobOptions` so that each job type gets its own options instance in DI. Registered as a singleton by `AddBackgroundJob<TJob>`.

**Type constraint:** `TJob : class, IBackgroundJob`

### Constructor

#### BackgroundJobOptionsFor(BackgroundJobOptions options)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `options` | `BackgroundJobOptions` | — | The options instance for this job type. |

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Value` | `BackgroundJobOptions` | — | get; | The underlying options instance. |

---

## HangfireJobOptions

**Namespace:** `JC.BackgroundJobs.Models`

Configuration options for a recurring Hangfire job registered via `AddHangfireJob<TJob>`. Each job type receives its own options instance.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Cron` | `string` | `"* * * * *"` | get; set; | Cron expression for the recurring schedule. Standard five-field syntax. |
| `Queue` | `string` | `"default"` | get; set; | The Hangfire queue this job is assigned to. Passed as an explicit parameter to `IRecurringJobManager.AddOrUpdate`. |
| `JobId` | `string?` | `null` | get; set; | Unique identifier for the recurring job in Hangfire. When `null`, defaults to the job type name (e.g. `"CleanupJob"`). |
| `TimeZone` | `TimeZoneInfo` | `TimeZoneInfo.Utc` | get; set; | Time zone used for cron evaluation. Passed to `RecurringJobOptions.TimeZone`. |
| `MisfireHandling` | `MisfireHandlingMode` | `Relaxed` | get; set; | How missed job executions are handled when the server was offline. Passed to `RecurringJobOptions.MisfireHandling`. `Relaxed` executes once on recovery; `Strict` catches up on every missed execution. |

Validated at registration time: `Cron` must not be null, empty, or whitespace (`ArgumentException`). `Queue` must not be null, empty, or whitespace (`ArgumentException`). `JobId`, when set, must not be empty or whitespace (`ArgumentException`).

---

## HangfireJobOptionsFor\<TJob\>

**Namespace:** `JC.BackgroundJobs.Models`

Typed wrapper around `HangfireJobOptions` so that each job type gets its own options instance in DI. Registered as a singleton by `AddHangfireJob<TJob>`.

**Type constraint:** `TJob : class, IBackgroundJob`

### Constructor

#### HangfireJobOptionsFor(HangfireJobOptions options)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `options` | `HangfireJobOptions` | — | The options instance for this job type. |

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `Value` | `HangfireJobOptions` | — | get; | The underlying options instance. |

---

## IHangfireScheduler

**Namespace:** `JC.BackgroundJobs.Services`

Service for scheduling ad-hoc Hangfire jobs at runtime. The internal implementation delegates to Hangfire's `IBackgroundJobClient`. Inject via `IHangfireScheduler`. Registered as scoped.

### Methods

#### Enqueue\<TJob\>()

**Returns:** `string`

**Type constraint:** `TJob : class, IBackgroundJob`

Enqueues a fire-and-forget job for immediate execution. Calls `IBackgroundJobClient.Enqueue<TJob>(job => job.ExecuteAsync(CancellationToken.None))` and returns the Hangfire job ID.

#### Schedule\<TJob\>(TimeSpan delay)

**Returns:** `string`

**Type constraint:** `TJob : class, IBackgroundJob`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `delay` | `TimeSpan` | — | The time to wait before executing the job. |

Schedules a job for execution after the specified delay. Calls `IBackgroundJobClient.Schedule<TJob>(job => job.ExecuteAsync(CancellationToken.None), delay)` and returns the Hangfire job ID.

#### Schedule\<TJob\>(DateTimeOffset enqueueAt)

**Returns:** `string`

**Type constraint:** `TJob : class, IBackgroundJob`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `enqueueAt` | `DateTimeOffset` | — | The UTC time at which the job should execute. |

Schedules a job for execution at a specific time. Calls `IBackgroundJobClient.Schedule<TJob>(job => job.ExecuteAsync(CancellationToken.None), enqueueAt)` and returns the Hangfire job ID.

#### ContinueWith\<TJob\>(string parentJobId)

**Returns:** `string`

**Type constraint:** `TJob : class, IBackgroundJob`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `parentJobId` | `string` | — | The Hangfire job ID of the parent job. |

Schedules a continuation job that executes after the specified parent job completes successfully. Calls `IBackgroundJobClient.ContinueJobWith<TJob>(parentJobId, job => job.ExecuteAsync(CancellationToken.None))` and returns the Hangfire job ID of the continuation. If the parent job fails after all retry attempts, the continuation remains in the `Awaiting` state.

---

## AdHocJobRegistration

**Namespace:** `JC.BackgroundJobs.Models`

Describes an ad-hoc Hangfire job type and its DI lifetime for registration via `AddHangfireScheduler`.

### Constructor

#### AdHocJobRegistration(Type jobType, ServiceLifetime lifetime = ServiceLifetime.Scoped)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `jobType` | `Type` | — | The job class type. Must implement `IBackgroundJob`. |
| `lifetime` | `ServiceLifetime` | `Scoped` | The DI lifetime for the job class. |

Throws `ArgumentException` if `jobType` does not implement `IBackgroundJob`.

### Properties

| Property | Type | Default | Access | Description |
|----------|------|---------|--------|-------------|
| `JobType` | `Type` | — | get; | The job class type implementing `IBackgroundJob`. |
| `Lifetime` | `ServiceLifetime` | `Scoped` | get; | The DI lifetime for the job class. |

### Methods

#### For\<TJob\>(ServiceLifetime lifetime = ServiceLifetime.Scoped)

**Returns:** `AdHocJobRegistration`

**Type constraint:** `TJob : class, IBackgroundJob`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `lifetime` | `ServiceLifetime` | `Scoped` | The DI lifetime for the job class. |

Static factory method that creates an `AdHocJobRegistration` for the specified job type. Provides a type-safe, concise alternative to calling the constructor directly with `typeof(TJob)`.

---

## JobErrorBehavior

**Namespace:** `JC.BackgroundJobs.Models`

Determines how the hosted-service wrapper behaves when the job's `ExecuteAsync` throws an exception. Only applies to hosted service jobs registered via `AddBackgroundJob<TJob>`.

| Member | Value | Description |
|--------|-------|-------------|
| `Continue` | `0` | Log the error and continue running on the next interval. |
| `Stop` | `1` | Log the error and stop the job permanently — the hosted service exits its loop. |
| `Throw` | `2` | Re-throw the exception, terminating the hosted service. Always logs at critical level regardless of `LogBehavior`. |

---

## JobLogBehavior

**Namespace:** `JC.BackgroundJobs.Models`

Controls the logging verbosity of the hosted-service wrapper. Only applies to hosted service jobs registered via `AddBackgroundJob<TJob>`. Does not affect logging within the job class itself.

| Member | Value | Description |
|--------|-------|-------------|
| `None` | `0` | No lifecycle or error logging from the wrapper. |
| `LogErrorsOnly` | `1` | Log errors only — no informational start, execute, complete, or stop messages. |
| `LogInfoOnly` | `2` | Log informational messages only — errors are silenced. |
| `LogAll` | `3` | Log both informational and error messages. |
