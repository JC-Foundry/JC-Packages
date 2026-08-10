using JC.Core.Models;
using JC.Tenancy.Models;
using JC.Tenancy.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JC.Tenancy.Extensions;

/// <summary>
/// Startup helpers for establishing an application's default tenant.
/// </summary>
/// <remarks>
/// Extends <see cref="IServiceProvider"/> rather than <c>IApplicationBuilder</c>, so JC.Tenancy
/// keeps its independence from ASP.NET Core and a worker service or console host can seed a tenant
/// by the same call a web application uses.
/// </remarks>
public static class SeedingExtensions
{
    /// <summary>
    /// Finds or creates a tenant by name and assigns it to a user, in a scope of its own.
    /// </summary>
    /// <typeparam name="TUser">The user entity type.</typeparam>
    /// <param name="services">The root service provider.</param>
    /// <param name="userId">The identifier of the user to assign the tenant to.</param>
    /// <param name="tenantName">The tenant name to find or create. Defaults to <c>"Default Tenant"</c>.</param>
    /// <param name="description">The description applied when the tenant is created. Ignored where it already exists.</param>
    /// <param name="userContextType">
    /// The <see cref="DbContext"/> type owning the user record. Leave <c>null</c> to use the
    /// repository manager's default context.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tenant now assigned to the user, or <c>null</c> where it could not be created.</returns>
    /// <example>
    /// <code>
    /// var admin = await app.ConfigureAdminAndRolesAsync&lt;AppUser, AppRole, AppRoles&gt;();
    /// if (admin is not null)
    ///     await app.Services.SeedDefaultTenantAsync&lt;AppUser&gt;(admin.Id);
    /// </code>
    /// </example>
    public static async Task<Tenant?> SeedDefaultTenantAsync<TUser>(
        this IServiceProvider services,
        string userId,
        string tenantName = "Default Tenant",
        string? description = "Default system tenant",
        Type? userContextType = null,
        CancellationToken cancellationToken = default)
        where TUser : class, IApplicationUser
    {
        using var scope = services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<TenantSeeder>();

        return await seeder.SeedDefaultTenantAsync<TUser>(
            userId, tenantName, description, userContextType, cancellationToken);
    }
}
