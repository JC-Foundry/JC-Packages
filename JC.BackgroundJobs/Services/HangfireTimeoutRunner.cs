using JC.Core.Models;
using Microsoft.Extensions.Logging;

namespace JC.BackgroundJobs.Services;

/// <summary>
/// Wraps a <typeparamref name="TJob"/> execution with a configurable timeout.
/// Used by Hangfire recurring job registration when <see cref="Models.HangfireJobOptions.ExecutionTimeout"/> is set.
/// Creates a <see cref="CancellationTokenSource"/> with the specified timeout and passes
/// the token to <see cref="IBackgroundJob.ExecuteAsync"/>.
/// </summary>
/// <typeparam name="TJob">The job type implementing <see cref="IBackgroundJob"/>.</typeparam>
internal sealed class HangfireTimeoutRunner<TJob>(
    TJob job,
    ILogger<HangfireTimeoutRunner<TJob>> logger)
    where TJob : class, IBackgroundJob
{
    /// <summary>
    /// Executes the job with a timeout-linked cancellation token.
    /// </summary>
    /// <param name="timeout">The maximum execution duration before cancellation is triggered.</param>
    /// <param name="cancellationToken">The Hangfire-provided cancellation token.</param>
    public async Task RunWithTimeoutAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            await job.ExecuteAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("{Job} timed out after {Timeout}", typeof(TJob).Name, timeout);
            throw;
        }
    }
}