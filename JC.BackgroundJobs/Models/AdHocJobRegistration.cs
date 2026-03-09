using Microsoft.Extensions.DependencyInjection;

namespace JC.BackgroundJobs.Models;

/// <summary>
/// Describes an ad-hoc Hangfire job type and its DI lifetime for registration
/// via <c>AddHangfireScheduler</c>.
/// </summary>
public class AdHocJobRegistration
{
    /// <summary>Gets the job class type implementing <see cref="IBackgroundJob"/>.</summary>
    public Type JobType { get; }

    /// <summary>Gets the DI lifetime for the job class. Defaults to <see cref="ServiceLifetime.Scoped"/>.</summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Creates a registration for the specified job type with an optional DI lifetime.
    /// </summary>
    /// <param name="jobType">The job class type. Must implement <see cref="IBackgroundJob"/>.</param>
    /// <param name="lifetime">The DI lifetime for the job class. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="jobType"/> does not implement <see cref="IBackgroundJob"/>.</exception>
    public AdHocJobRegistration(Type jobType, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        if (!typeof(IBackgroundJob).IsAssignableFrom(jobType))
            throw new ArgumentException(
                $"Type '{jobType.FullName}' does not implement {nameof(IBackgroundJob)}.",
                nameof(jobType));

        JobType = jobType;
        Lifetime = lifetime;
    }

    /// <summary>
    /// Creates a registration for the specified job type with an optional DI lifetime.
    /// </summary>
    /// <typeparam name="TJob">The job class type implementing <see cref="IBackgroundJob"/>.</typeparam>
    /// <param name="lifetime">The DI lifetime for the job class. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <returns>A new <see cref="AdHocJobRegistration"/> instance.</returns>
    public static AdHocJobRegistration For<TJob>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TJob : class, IBackgroundJob
        => new(typeof(TJob), lifetime);
}
