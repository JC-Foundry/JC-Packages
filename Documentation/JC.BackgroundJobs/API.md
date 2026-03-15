# JC.BackgroundJobs — API reference

Complete reference of all public types, properties, and methods in JC.BackgroundJobs. See [Setup](Setup.md) for registration and [Guide](Guide.md) for usage examples.

> **Note:** Registration extensions (`IServiceCollection`, `IServiceProvider`, `IApplicationBuilder`) and options classes are documented in [Setup](Setup.md), not here.

---

# Models

## IBackgroundJob

**Namespace:** `JC.Core.Models`

Defined in JC.Core, not JC.BackgroundJobs. This interface is the contract that all background jobs implement — it lives in JC.Core so any package can declare jobs without depending on JC.BackgroundJobs. See [JC.Core API — IBackgroundJob](../JC.Core/API.md#ibackgroundjob) for the full reference.

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

## BackgroundJobOptionsFor\<TJob\>

**Namespace:** `JC.BackgroundJobs.Models`

Typed wrapper around `BackgroundJobOptions` so that each job type gets its own options instance in DI. Registered as a singleton when `AddBackgroundJob<TJob>()` is called.

```csharp
public class BackgroundJobOptionsFor<TJob>(BackgroundJobOptions options)
    where TJob : class, IBackgroundJob
```

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Value` | `BackgroundJobOptions` | get; | The underlying `BackgroundJobOptions` instance. |

---

## HangfireJobOptionsFor\<TJob\>

**Namespace:** `JC.BackgroundJobs.Models`

Typed wrapper around `HangfireJobOptions` so that each job type gets its own options instance in DI. Registered as a singleton when `AddHangfireJob<TJob>()` is called.

```csharp
public class HangfireJobOptionsFor<TJob>(HangfireJobOptions options)
    where TJob : class, IBackgroundJob
```

### Properties

| Property | Type | Access | Description |
|----------|------|--------|-------------|
| `Value` | `HangfireJobOptions` | get; | The underlying `HangfireJobOptions` instance. |

---

# Enums

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

---

# Services

## IHangfireScheduler

**Namespace:** `JC.BackgroundJobs.Services`

Service for scheduling ad-hoc Hangfire jobs at runtime. The internal implementation delegates to Hangfire's `IBackgroundJobClient`. Inject via `IHangfireScheduler`. Registered as scoped.

### Methods

#### Enqueue\<TJob\>()

**Returns:** `string`

**Type constraint:** `TJob : class, IBackgroundJob`

Enqueues a fire-and-forget job for immediate execution. Calls `IBackgroundJobClient.Enqueue<TJob>(job => job.ExecuteAsync(CancellationToken.None))` and returns the Hangfire job ID.

---

#### Schedule\<TJob\>(TimeSpan delay)

**Returns:** `string`

**Type constraint:** `TJob : class, IBackgroundJob`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `delay` | `TimeSpan` | — | The time to wait before executing the job. Must not be negative. |

Schedules a job for execution after the specified delay. Calls `IBackgroundJobClient.Schedule<TJob>(job => job.ExecuteAsync(CancellationToken.None), delay)` and returns the Hangfire job ID. Throws `ArgumentOutOfRangeException` if `delay` is negative.

---

#### Schedule\<TJob\>(DateTimeOffset enqueueAt)

**Returns:** `string`

**Type constraint:** `TJob : class, IBackgroundJob`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `enqueueAt` | `DateTimeOffset` | — | The UTC time at which the job should execute. |

Schedules a job for execution at a specific time. Calls `IBackgroundJobClient.Schedule<TJob>(job => job.ExecuteAsync(CancellationToken.None), enqueueAt)` and returns the Hangfire job ID.

---

#### ContinueWith\<TJob\>(string parentJobId)

**Returns:** `string`

**Type constraint:** `TJob : class, IBackgroundJob`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `parentJobId` | `string` | — | The Hangfire job ID of the parent job. Must not be null, empty, or whitespace. |

Schedules a continuation job that executes after the specified parent job completes successfully. Calls `IBackgroundJobClient.ContinueJobWith<TJob>(parentJobId, job => job.ExecuteAsync(CancellationToken.None))` and returns the Hangfire job ID of the continuation. Throws `ArgumentException` if `parentJobId` is null, empty, or whitespace. If the parent job fails after all retry attempts, the continuation remains in the `Awaiting` state.
