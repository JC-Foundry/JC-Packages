using Hangfire;
using JC.BackgroundJobs.Models;
using JC.BackgroundJobs.Services;
using JC.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JC.BackgroundJobs.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> providing background job registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    // Single registry instance shared across all AddHangfireJob calls.
    // Created once per service collection and registered as a singleton.
    private static readonly Lock RegistryLock = new();

    // ── Hosted service jobs ─────────────────────────────────────────────

    /// <summary>
    /// Registers a hosted-service background job of type <typeparamref name="TJob"/> with default options.
    /// </summary>
    /// <typeparam name="TJob">The job type implementing <see cref="IBackgroundJob"/>.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBackgroundJob<TJob>(this IServiceCollection services)
        where TJob : class, IBackgroundJob
        => services.AddBackgroundJob<TJob>(_ => { });

    /// <summary>
    /// Registers a hosted-service background job of type <typeparamref name="TJob"/> with the specified options.
    /// </summary>
    /// <typeparam name="TJob">The job type implementing <see cref="IBackgroundJob"/>.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Callback to configure <see cref="BackgroundJobOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBackgroundJob<TJob>(
        this IServiceCollection services,
        Action<BackgroundJobOptions> configure)
        where TJob : class, IBackgroundJob
    {
        var options = new BackgroundJobOptions();
        configure(options);
        ValidateBackgroundJobOptions(options);

        services.AddSingleton(new BackgroundJobOptionsFor<TJob>(options));
        services.Add(new ServiceDescriptor(typeof(TJob), typeof(TJob), options.ServiceLifetime));
        services.AddHostedService<BackgroundServiceWrapper<TJob>>();

        return services;
    }

    // ── Hangfire recurring jobs ─────────────────────────────────────────

    /// <summary>
    /// Registers a recurring Hangfire job of type <typeparamref name="TJob"/> with default options.
    /// The job ID defaults to the type name of <typeparamref name="TJob"/>.
    /// Requires Hangfire storage to be configured separately (e.g. via <c>AddHangfireSqlServer</c>).
    /// </summary>
    /// <typeparam name="TJob">The job type implementing <see cref="IBackgroundJob"/>.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHangfireJob<TJob>(this IServiceCollection services)
        where TJob : class, IBackgroundJob
        => services.AddHangfireJob<TJob>(_ => { });

    /// <summary>
    /// Registers a recurring Hangfire job of type <typeparamref name="TJob"/> with the specified options.
    /// The job ID defaults to the type name of <typeparamref name="TJob"/> unless overridden via
    /// <see cref="HangfireJobOptions.JobId"/>.
    /// Requires Hangfire storage to be configured separately (e.g. via <c>AddHangfireSqlServer</c>).
    /// </summary>
    /// <typeparam name="TJob">The job type implementing <see cref="IBackgroundJob"/>.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Callback to configure <see cref="HangfireJobOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHangfireJob<TJob>(
        this IServiceCollection services,
        Action<HangfireJobOptions> configure)
        where TJob : class, IBackgroundJob
    {
        var options = new HangfireJobOptions();
        configure(options);

        var jobId = options.JobId ?? typeof(TJob).Name;
        ValidateHangfireJobOptions(jobId, options);

        services.AddSingleton(new HangfireJobOptionsFor<TJob>(options));
        services.TryAddScoped<TJob>();

        var registry = EnsureHangfireInfrastructure(services);

        registry.Add((manager, _) =>
        {
            manager.AddOrUpdate<TJob>(
                jobId,
                options.Queue,
                job => job.ExecuteAsync(CancellationToken.None),
                options.Cron,
                new RecurringJobOptions
                {
                    TimeZone = options.TimeZone,
                    MisfireHandling = options.MisfireHandling
                });
        });

        return services;
    }

    // ── Hangfire ad-hoc scheduler ───────────────────────────────────────

    /// <summary>
    /// Registers the <see cref="IHangfireScheduler"/> service for scheduling fire-and-forget,
    /// delayed, and continuation jobs at runtime.
    /// Requires Hangfire storage to be configured separately (e.g. via <c>AddHangfireSqlServer</c>).
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHangfireScheduler(this IServiceCollection services)
    {
        services.TryAddScoped<IHangfireScheduler, HangfireScheduler>();
        return services;
    }

    /// <summary>
    /// Registers the <see cref="IHangfireScheduler"/> service and registers the specified
    /// ad-hoc job types in DI with their configured lifetimes.
    /// Requires Hangfire storage to be configured separately (e.g. via <c>AddHangfireSqlServer</c>).
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="jobs">The ad-hoc job registrations to add to DI.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHangfireScheduler(
        this IServiceCollection services,
        params AdHocJobRegistration[] jobs)
    {
        services.TryAddScoped<IHangfireScheduler, HangfireScheduler>();

        foreach (var job in jobs)
            services.TryAdd(new ServiceDescriptor(job.JobType, job.JobType, job.Lifetime));

        return services;
    }

    // ── Validation ──────────────────────────────────────────────────────

    private static void ValidateBackgroundJobOptions(BackgroundJobOptions options)
    {
        if (options.Interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "Interval must be greater than zero.");

        if (options.InitialDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "InitialDelay must not be negative.");
    }

    private static void ValidateHangfireJobOptions(string jobId, HangfireJobOptions options)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("JobId must not be null, empty, or whitespace.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.Cron))
            throw new ArgumentException("Cron must not be null, empty, or whitespace.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.Queue))
            throw new ArgumentException("Queue must not be null, empty, or whitespace.", nameof(options));
    }

    // ── Internal helpers ────────────────────────────────────────────────

    /// <summary>
    /// Ensures the Hangfire job registry, scheduler, and registration hosted service
    /// are registered exactly once. Returns the shared registry instance.
    /// </summary>
    private static HangfireJobRegistry EnsureHangfireInfrastructure(IServiceCollection services)
    {
        lock (RegistryLock)
        {
            var descriptor = services.FirstOrDefault(s =>
                s.ServiceType == typeof(HangfireJobRegistry));

            if (descriptor?.ImplementationInstance is HangfireJobRegistry existing)
                return existing;

            var registry = new HangfireJobRegistry();
            services.AddSingleton(registry);
            services.TryAddScoped<IHangfireScheduler, HangfireScheduler>();
            services.AddHostedService<HangfireJobRegistrationService>();

            return registry;
        }
    }
}
