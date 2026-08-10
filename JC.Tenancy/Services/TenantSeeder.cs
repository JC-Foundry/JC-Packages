using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using JC.Tenancy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JC.Tenancy.Services;

/// <summary>
/// Creates an application's default tenant, optionally assigning it to a user.
/// </summary>
/// <remarks>
/// Both overloads are idempotent: an existing tenant of the same name is reused, and a user already
/// holding that tenant is left alone. Safe to call on every startup.
/// </remarks>
public class TenantSeeder(IRepositoryManager repos, ITenantStore store, ILogger<TenantSeeder> logger)
{
    /// <summary>
    /// Finds or creates a tenant by name.
    /// </summary>
    /// <param name="tenantName">The tenant name to find or create.</param>
    /// <param name="description">The description applied on creation. Ignored where the tenant exists.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tenant, or <c>null</c> where it could not be created — the reason is logged.</returns>
    public async Task<Tenant?> SeedDefaultTenantAsync(
        string tenantName = "Default Tenant",
        string? description = "Default system tenant",
        CancellationToken cancellationToken = default)
    {
        var tenant = (await store.GetAllAsync(cancellationToken: cancellationToken))
            .FirstOrDefault(t => string.Equals(t.Name, tenantName, StringComparison.OrdinalIgnoreCase));

        if (tenant is null)
        {
            var response = await store.TryAddAsync(
                new Tenant { Name = tenantName, Description = description },
                cancellationToken);

            if (!response.IsValid)
            {
                logger.LogError("Failed to create default tenant '{TenantName}': {Error}",
                    tenantName, response.ErrorMessage);
                return null;
            }

            tenant = response.ValidatedTenant;
        }

        return tenant;
    }

    /// <summary>
    /// Finds or creates a tenant by name, assigns it to <paramref name="user"/>, and saves the user.
    /// </summary>
    /// <typeparam name="T">The user record type.</typeparam>
    /// <param name="user">The user to assign the tenant to.</param>
    /// <param name="tenantName">The tenant name to find or create.</param>
    /// <param name="description">The description applied on creation. Ignored where the tenant exists.</param>
    /// <param name="userContextType">
    /// The <see cref="DbContext"/> owning the user. Leave <c>null</c> to use the repository
    /// manager's default context.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The assigned tenant, or <c>null</c> where it could not be created — the user is then left untouched.</returns>
    public async Task<Tenant?> SeedDefaultTenantAsync<T>(
        T user,
        string tenantName = "Default Tenant",
        string? description = "Default system tenant",
        Type? userContextType = null,
        CancellationToken cancellationToken = default)
        where T : class, IApplicationUser
    {
        var tenant = await SeedDefaultTenantAsync(tenantName, description, cancellationToken);
        if (tenant is null) return null;

        // Already assigned — nothing to write, so a repeated startup does not churn the user row.
        if (string.Equals(user.IdentityTenantId, tenant.Id, StringComparison.Ordinal))
            return tenant;

        user.IdentityTenantId = tenant.Id;

        var manager = userContextType is null ? repos : repos.For(userContextType);
        await manager.GetRepository<T>().UpdateAsync(user, cancellationToken: cancellationToken);

        logger.LogInformation("Assigned tenant '{TenantName}' ({TenantId}) to user '{UserName}'.",
            tenant.Name, tenant.Id, user.UserName);

        return tenant;
    }
}
