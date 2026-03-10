namespace JC.Core.Models;

/// <summary>
/// Defines a background job that can be executed by the JC.BackgroundJobs hosting infrastructure.
/// This interface lives in JC.Core so that any package can declare background jobs
/// without depending on JC.BackgroundJobs directly — the consuming application wires up
/// execution via JC.BackgroundJobs (hosted services or Hangfire) at registration time.
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
