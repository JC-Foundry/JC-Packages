using JC.Core.Data;
using JC.Core.Models.Options;
using JC.Core.Services;
using JC.Core.Services.DataRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract

namespace JC.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> providing JC.Core service registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all JC.Core services including <see cref="AuditService"/>,
    /// the data context, repository manager, and default repository contexts.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type implementing <see cref="IDataDbContext"/>. This is your application's default database context</typeparam>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="applicationName">
    /// Optional application name stamped onto <c>AuditEntry.SourceApplication</c> so audit records can be
    /// attributed to the application that wrote them (useful when several applications share a database).
    /// Leave <c>null</c> if attribution is not required.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCore<TContext>(this IServiceCollection services,
        string? applicationName = null)
        where TContext : DbContext, IDataDbContext
    {
        services.TryAddScoped<IDataDbContext>(sp => sp.GetRequiredService<TContext>());
        services.TryAddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());
        services.TryAddScoped<IRepositoryManager, RepositoryManager>();

        services.AddOptions<CoreAuditOptions>()
            .Configure(opts => opts.ApplicationName = applicationName);

        return services;
    }
    
    /// <summary>
    /// Configures <see cref="CoreBackgroundJobOptions"/> for core background jobs
    /// such as <see cref="AuditCleanupJob"/> and <see cref="SoftDeleteCleanupJob"/>.
    /// Only needs to be called if overriding the default options — jobs will use
    /// defaults automatically if this is not called.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Action to configure <see cref="CoreBackgroundJobOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureCoreBackgroundJobs(this IServiceCollection services,
        Action<CoreBackgroundJobOptions> configure)
    {
        services.AddOptions<CoreBackgroundJobOptions>()
            .Configure(opts => configure?.Invoke(opts));

        return services;
    }
}