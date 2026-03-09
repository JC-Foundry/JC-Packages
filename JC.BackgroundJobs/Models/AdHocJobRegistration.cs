using Microsoft.Extensions.DependencyInjection;

namespace JC.BackgroundJobs.Models;

/// <summary>
/// Describes an ad-hoc Hangfire job type and its DI lifetime for registration
/// via <c>AddHangfireScheduler</c>.
/// </summary>
/// <param name="JobType">The job class type implementing <see cref="IBackgroundJob"/>.</param>
/// <param name="Lifetime">The DI lifetime for the job class. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
public record AdHocJobRegistration(Type JobType, ServiceLifetime Lifetime = ServiceLifetime.Scoped)
{
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
