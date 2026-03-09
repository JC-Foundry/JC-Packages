using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JC.SqlServer.Hangfire;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> providing Hangfire with SQL Server storage.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Hangfire with SQL Server storage using the specified connection string.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration, used to resolve the connection string.</param>
    /// <param name="connectionStringName">The connection string name in configuration. Defaults to <c>"HangfireConnection"</c>.</param>
    /// <param name="configureHangfire">Optional callback to configure additional Hangfire settings.</param>
    /// <param name="configureSqlStorage">Optional callback to configure SQL Server storage options.</param>
    /// <param name="configureServer">Optional callback to configure the Hangfire server (e.g. queues, worker count).</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the connection string is not found.</exception>
    public static IServiceCollection AddHangfireSqlServer(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "HangfireConnection",
        Action<IGlobalConfiguration>? configureHangfire = null,
        Action<SqlServerStorageOptions>? configureSqlStorage = null,
        Action<BackgroundJobServerOptions>? configureServer = null)
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' not found.");

        services.AddHangfire(config =>
        {
            var storageOptions = new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = true
            };
            configureSqlStorage?.Invoke(storageOptions);

            config.UseSqlServerStorage(connectionString, storageOptions);
            configureHangfire?.Invoke(config);
        });

        if (configureServer is not null)
            services.AddHangfireServer(configureServer);
        else
            services.AddHangfireServer();

        return services;
    }
}
