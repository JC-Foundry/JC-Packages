using JC.BackgroundJobs.Models;
using JC.Core.Models;

namespace JC.BackgroundJobs.Services;

/// <summary>
/// Provides methods to schedule ad-hoc Hangfire jobs using <see cref="IBackgroundJob"/> implementations.
/// </summary>
public interface IHangfireScheduler
{
    /// <summary>Enqueues a fire-and-forget job for immediate execution.</summary>
    /// <typeparam name="TJob">The job type implementing <see cref="IBackgroundJob"/>.</typeparam>
    /// <returns>The Hangfire job ID.</returns>
    string Enqueue<TJob>() where TJob : class, IBackgroundJob;

    /// <summary>Schedules a job for execution after the specified delay.</summary>
    /// <typeparam name="TJob">The job type implementing <see cref="IBackgroundJob"/>.</typeparam>
    /// <param name="delay">The time to wait before executing the job.</param>
    /// <returns>The Hangfire job ID.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="delay"/> is negative.</exception>
    string Schedule<TJob>(TimeSpan delay) where TJob : class, IBackgroundJob;

    /// <summary>Schedules a job for execution at a specific time.</summary>
    /// <typeparam name="TJob">The job type implementing <see cref="IBackgroundJob"/>.</typeparam>
    /// <param name="enqueueAt">The UTC time at which the job should execute.</param>
    /// <returns>The Hangfire job ID.</returns>
    string Schedule<TJob>(DateTimeOffset enqueueAt) where TJob : class, IBackgroundJob;

    /// <summary>Schedules a job to execute after the specified parent job completes successfully.</summary>
    /// <typeparam name="TJob">The job type implementing <see cref="IBackgroundJob"/>.</typeparam>
    /// <param name="parentJobId">The Hangfire job ID of the parent job.</param>
    /// <returns>The Hangfire job ID of the continuation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="parentJobId"/> is null, empty, or whitespace.</exception>
    string ContinueWith<TJob>(string parentJobId) where TJob : class, IBackgroundJob;
}
