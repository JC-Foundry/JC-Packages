using JC.Core.Data;
using JC.Core.Models;
using JC.Core.Models.Auditing;
using JC.Core.Models.Options;
using JC.Core.Services;
using JC.Core.Services.DataRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    /// <typeparam name="TContext">The DbContext type implementing <see cref="IDataDbContext"/>.</typeparam>
    /// <param name="services">The service collection to register services into.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCore<TContext>(this IServiceCollection services)
        where TContext : DbContext, IDataDbContext
    {
        services.TryAddScoped<IDataDbContext>(sp => sp.GetRequiredService<TContext>());
        services.TryAddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());
        services.TryAddScoped<IRepositoryManager, RepositoryManager>();
        
        services.RegisterRepositoryContexts(
            typeof(AuditModel));

        return services;
    }

    /// <summary>
    /// Registers a single <see cref="IRepositoryContext{T}"/> / <see cref="RepositoryContext{T}"/> pair for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type to register a repository context for.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection RegisterRepositoryContext<T>(this IServiceCollection services)
        where T : class
        => services.RegisterRepositoryContexts(typeof(T));

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

    /// <summary>
    /// Registers <see cref="IRepositoryContext{T}"/> / <see cref="RepositoryContext{T}"/> pairs for multiple entity types via reflection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="types">The entity types to register repository contexts for. Each must be a class.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if any of the provided types is not a class.</exception>
    public static IServiceCollection RegisterRepositoryContexts(this IServiceCollection services, params Type[] types)
    {
        foreach (var type in types)
        {
            if (!type.IsClass)
                throw new ArgumentException($"Type {type.Name} must be a class.", nameof(types));

            var interfaceType = typeof(IRepositoryContext<>).MakeGenericType(type);
            var implementationType = typeof(RepositoryContext<>).MakeGenericType(type);

            services.TryAddScoped(interfaceType, implementationType);
        }

        return services;
    }
}