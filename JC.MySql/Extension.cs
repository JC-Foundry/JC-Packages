using JC.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JC.MySql;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> providing MySQL database registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default <see cref="DataDbContext"/> with the MySQL provider.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration, used to resolve the connection string.</param>
    /// <param name="migrationsAssembly">The assembly name containing EF Core migrations.</param>
    /// <param name="connectionStringName">The connection string name in configuration. Defaults to <c>"DefaultConnection"</c>.</param>
    /// <param name="mySqlOptions">Optional callback to configure MySQL-specific options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the connection string is not found.</exception>
    public static IServiceCollection AddMySqlDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        string migrationsAssembly,
        string connectionStringName = "DefaultConnection",
        Action<MySqlDbContextOptionsBuilder>? mySqlOptions = null)
    {
        services.AddMySqlDatabase<DataDbContext>(configuration, migrationsAssembly, connectionStringName, mySqlOptions);
        return services;
    }

    /// <summary>
    /// Registers the specified <typeparamref name="TContext"/> with the MySQL provider using Pomelo.
    /// Auto-detects the MySQL server version from the connection string.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type implementing <see cref="IDataDbContext"/>.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration, used to resolve the connection string.</param>
    /// <param name="migrationsAssembly">The assembly name containing EF Core migrations.</param>
    /// <param name="connectionStringName">The connection string name in configuration. Defaults to <c>"DefaultConnection"</c>.</param>
    /// <param name="mySqlOptions">Optional callback to configure MySQL-specific options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the connection string is not found.</exception>
    public static IServiceCollection AddMySqlDatabase<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string migrationsAssembly,
        string connectionStringName = "DefaultConnection",
        Action<MySqlDbContextOptionsBuilder>? mySqlOptions = null)
        where TContext : DbContext, IDataDbContext
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' not found.");

        services.AddDbContext<TContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mysql =>
            {
                mysql.MigrationsAssembly(migrationsAssembly);
                mySqlOptions?.Invoke(mysql);
            }));

        return services;
    }
}