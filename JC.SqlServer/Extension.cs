using JC.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JC.SqlServer;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> providing SQL Server database registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default <see cref="DataDbContext"/> with the SQL Server provider.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration, used to resolve the connection string.</param>
    /// <param name="migrationsAssembly">The assembly name containing EF Core migrations.</param>
    /// <param name="connectionStringName">The connection string name in configuration. Defaults to <c>"DefaultConnection"</c>.</param>
    /// <param name="sqlServerOptions">Optional callback to configure SQL Server-specific options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the connection string is not found.</exception>
    public static IServiceCollection AddSqlServerDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        string migrationsAssembly,
        string connectionStringName = "DefaultConnection",
        Action<SqlServerDbContextOptionsBuilder>? sqlServerOptions = null)
    {
        services.AddSqlServerDatabase<DataDbContext>(configuration, migrationsAssembly, connectionStringName, sqlServerOptions);
        return services;
    }

    /// <summary>
    /// Registers the specified <typeparamref name="TContext"/> with the SQL Server provider.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type implementing <see cref="IDataDbContext"/>.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration, used to resolve the connection string.</param>
    /// <param name="migrationsAssembly">The assembly name containing EF Core migrations.</param>
    /// <param name="connectionStringName">The connection string name in configuration. Defaults to <c>"DefaultConnection"</c>.</param>
    /// <param name="sqlServerOptions">Optional callback to configure SQL Server-specific options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the connection string is not found.</exception>
    public static IServiceCollection AddSqlServerDatabase<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string migrationsAssembly,
        string connectionStringName = "DefaultConnection",
        Action<SqlServerDbContextOptionsBuilder>? sqlServerOptions = null)
        where TContext : DbContext, IDataDbContext
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' not found.");

        services.AddDbContext<TContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(migrationsAssembly);
                sqlServerOptions?.Invoke(sql);
            }));

        return services;
    }
}