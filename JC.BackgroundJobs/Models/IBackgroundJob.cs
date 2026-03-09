namespace JC.BackgroundJobs.Models;

/// <summary>
/// Defines a recurring background job. Configuration is provided via
/// <see cref="BackgroundJobOptions"/> at registration time.
/// </summary>
public interface IBackgroundJob
{
    /// <summary>
    /// Executes the job's work. Called on each tick (hosted service) or by Hangfire.
    /// Implementations should contain only the actual job logic — looping, error handling,
    /// and lifecycle management are handled by the hosting infrastructure.
    /// </summary>
    /// <param name="cancellationToken">Token signalling cancellation of the host or job.</param>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Determines how the hosted-service wrapper behaves when a job throws an exception.
/// </summary>
public enum JobErrorBehavior
{
    /// <summary>Log the error and continue running on the next interval.</summary>
    Continue,

    /// <summary>Log the error and stop the job permanently.</summary>
    Stop,

    /// <summary>Re-throw the exception, which will terminate the hosted service and may crash the application.</summary>
    Throw
}

/// <summary>
/// Controls the logging verbosity of the hosted-service wrapper.
/// </summary>
public enum JobLogBehavior
{
    /// <summary>No lifecycle or error logging.</summary>
    None,

    /// <summary>Log errors only — no informational messages.</summary>
    LogErrorsOnly,

    /// <summary>Log informational messages only — errors are silenced.</summary>
    LogInfoOnly,

    /// <summary>Log both informational and error messages.</summary>
    LogAll
}
