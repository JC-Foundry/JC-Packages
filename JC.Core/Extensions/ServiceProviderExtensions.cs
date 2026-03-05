using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JC.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceProvider"/> providing database migration utilities.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Applies any pending EF Core migrations for the specified <typeparamref name="TContext"/> asynchronously.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type to migrate.</typeparam>
    /// <param name="services">The service provider used to resolve the DbContext.</param>
    /// <returns>A task representing the asynchronous migration operation.</returns>
    public static async Task MigrateDatabaseAsync<TContext>(this IServiceProvider services)
        where TContext : DbContext
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Database.MigrateAsync();
    }
}