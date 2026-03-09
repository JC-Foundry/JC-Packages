using Hangfire;
using JC.BackgroundJobs.Models;

namespace JC.BackgroundJobs.Services;

/// <inheritdoc />
internal sealed class HangfireScheduler(IBackgroundJobClient jobClient) : IHangfireScheduler
{
    /// <inheritdoc />
    public string Enqueue<TJob>() where TJob : class, IBackgroundJob
        => jobClient.Enqueue<TJob>(job => job.ExecuteAsync(CancellationToken.None));

    /// <inheritdoc />
    public string Schedule<TJob>(TimeSpan delay) where TJob : class, IBackgroundJob
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);

        return jobClient.Schedule<TJob>(job => job.ExecuteAsync(CancellationToken.None), delay);
    }

    /// <inheritdoc />
    public string Schedule<TJob>(DateTimeOffset enqueueAt) where TJob : class, IBackgroundJob
        => jobClient.Schedule<TJob>(job => job.ExecuteAsync(CancellationToken.None), enqueueAt);

    /// <inheritdoc />
    public string ContinueWith<TJob>(string parentJobId) where TJob : class, IBackgroundJob
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentJobId);

        return jobClient.ContinueJobWith<TJob>(parentJobId, job => job.ExecuteAsync(CancellationToken.None));
    }
}
