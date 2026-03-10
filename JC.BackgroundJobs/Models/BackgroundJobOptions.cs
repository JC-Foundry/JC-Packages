using JC.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace JC.BackgroundJobs.Models;

/// <summary>
/// Configuration options for a background job registered via
/// <c>AddBackgroundJob&lt;TJob&gt;</c>.
/// </summary>
public class BackgroundJobOptions
{
    /// <summary>Gets or sets the interval between job executions. Defaults to 1 minute.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Gets or sets the delay before the first execution after the host starts. Defaults to 10 seconds.</summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Gets or sets how the wrapper behaves when the job throws an exception. Defaults to <see cref="JobErrorBehavior.Continue"/>.</summary>
    public JobErrorBehavior ErrorBehavior { get; set; } = JobErrorBehavior.Continue;

    /// <summary>Gets or sets the logging verbosity for the wrapper. Defaults to <see cref="JobLogBehavior.LogAll"/>.</summary>
    public JobLogBehavior LogBehavior { get; set; } = JobLogBehavior.LogAll;

    /// <summary>Gets or sets the DI lifetime used to resolve the job. Defaults to <see cref="ServiceLifetime.Scoped"/>.</summary>
    public ServiceLifetime ServiceLifetime { get; set; } = ServiceLifetime.Scoped;
}

/// <summary>
/// Typed wrapper around <see cref="BackgroundJobOptions"/> so that each job type
/// gets its own options instance in DI.
/// </summary>
/// <typeparam name="TJob">The job type these options belong to.</typeparam>
public class BackgroundJobOptionsFor<TJob>(BackgroundJobOptions options)
    where TJob : class, IBackgroundJob
{
    /// <summary>Gets the underlying <see cref="BackgroundJobOptions"/> instance.</summary>
    public BackgroundJobOptions Value { get; } = options;
}
