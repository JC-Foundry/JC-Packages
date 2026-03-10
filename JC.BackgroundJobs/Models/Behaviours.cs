namespace JC.BackgroundJobs.Models;

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