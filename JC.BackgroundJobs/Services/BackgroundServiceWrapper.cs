using JC.BackgroundJobs.Models;
using JC.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JC.BackgroundJobs.Services;

/// <summary>
/// Hosted service that wraps an <see cref="IBackgroundJob"/> implementation,
/// executing it on a recurring interval with scoped DI, configurable error
/// handling, and configurable logging.
/// </summary>
internal sealed class BackgroundServiceWrapper<TJob>(
    BackgroundJobOptionsFor<TJob> jobOptions,
    IServiceProvider serviceProvider,
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundServiceWrapper<TJob>> logger)
    : BackgroundService
    where TJob : class, IBackgroundJob
{
    private readonly BackgroundJobOptions _options = jobOptions.Value;
    private readonly string _jobName = typeof(TJob).Name;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (ShouldLogInfo())
            logger.LogInformation("{Job} starting — initial delay: {Delay}, interval: {Interval}",
                _jobName, _options.InitialDelay, _options.Interval);

        await Task.Delay(_options.InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (ShouldLogInfo())
                    logger.LogInformation("{Job} executing", _jobName);

                await RunJobAsync(stoppingToken);

                if (ShouldLogInfo())
                    logger.LogInformation("{Job} completed", _jobName);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                if (ShouldLogInfo())
                    logger.LogInformation("{Job} cancelled — stopping job", _jobName);
                break;
            }
            catch (OperationCanceledException) when (_options.ExecutionTimeout.HasValue)
            {
                if (ShouldLogErrors())
                    logger.LogWarning("{Job} timed out after {Timeout}", _jobName, _options.ExecutionTimeout.Value);
            }
            catch (Exception ex)
            {
                switch (_options.ErrorBehavior)
                {
                    case JobErrorBehavior.Continue:
                        if (ShouldLogErrors())
                            logger.LogError(ex, "{Job} failed — continuing", _jobName);
                        break;

                    case JobErrorBehavior.Stop:
                        if (ShouldLogErrors())
                            logger.LogError(ex, "{Job} failed — stopping job", _jobName);
                        return;

                    case JobErrorBehavior.Throw:
                        logger.LogCritical(ex, "{Job} failed — throwing", _jobName);
                        throw;
                }
            }

            await Task.Delay(_options.Interval, stoppingToken);
        }

        if (ShouldLogInfo())
            logger.LogInformation("{Job} stopped", _jobName);
    }

    /// <summary>
    /// Resolves and executes the job, creating a scope for scoped/transient lifetimes.
    /// When <see cref="BackgroundJobOptions.ExecutionTimeout"/> is configured, a linked
    /// cancellation token is used that triggers when either the host stops or the timeout elapses.
    /// </summary>
    private async Task RunJobAsync(CancellationToken stoppingToken)
    {
        using var timeoutCts = _options.ExecutionTimeout.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(stoppingToken)
            : null;

        if (timeoutCts != null)
            timeoutCts.CancelAfter(_options.ExecutionTimeout!.Value);

        var token = timeoutCts?.Token ?? stoppingToken;

        if (_options.ServiceLifetime == ServiceLifetime.Singleton)
        {
            var job = serviceProvider.GetRequiredService<TJob>();
            await job.ExecuteAsync(token);
        }
        else
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var job = scope.ServiceProvider.GetRequiredService<TJob>();
            await job.ExecuteAsync(token);
        }
    }

    /// <summary>Returns <see langword="true"/> when informational messages should be logged.</summary>
    private bool ShouldLogInfo()
        => _options.LogBehavior is JobLogBehavior.LogAll or JobLogBehavior.LogInfoOnly;

    /// <summary>Returns <see langword="true"/> when error messages should be logged.</summary>
    private bool ShouldLogErrors()
        => _options.LogBehavior is JobLogBehavior.LogAll or JobLogBehavior.LogErrorsOnly;
}
