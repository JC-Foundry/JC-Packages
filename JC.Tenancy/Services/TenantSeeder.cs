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
    /// Finds or creates a tenant by name and assigns it to a user.
    /// </summary>
    /// <typeparam name="TUser">The user entity type.</typeparam>
    /// <param name="userId">The identifier of the user to assign the tenant to.</param>
    /// <param name="tenantName">The tenant name to find or create.</param>
    /// <param name="description">The description applied on creation. Ignored where the tenant exists.</param>
    /// <param name="userContextType">
    /// The <see cref="DbContext"/> owning the user. Leave <c>null</c> to use the repository
    /// manager's default context.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The assigned tenant; <c>null</c> where the tenant could not be created or no user has that
    /// identifier, in which case the user is left untouched and the reason is logged.
    /// </returns>
    /// <remarks>
    /// Takes an identifier rather than an entity so the user is loaded and tracked by the context
    /// that saves it, and only the tenant column is written.
    /// </remarks>
    public async Task<Tenant?> SeedDefaultTenantAsync<TUser>(
        string userId,
        string tenantName = "Default Tenant",
        string? description = "Default system tenant",
        Type? userContextType = null,
        CancellationToken cancellationToken = default)
        where TUser : class, IApplicationUser
    {
        var tenant = await SeedDefaultTenantAsync(tenantName, description, cancellationToken);
        if (tenant is null) return null;

        var repo = (userContextType is null ? repos : repos.For(userContextType)).GetRepository<TUser>();

        var user = await repo.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            logger.LogError("Cannot assign tenant '{TenantName}': no {UserType} has identifier '{UserId}'.",
                tenantName, typeof(TUser).Name, userId);
            return null;
        }

        // Already assigned — nothing to write, so a repeated startup does not churn the user row.
        if (string.Equals(user.IdentityTenantId, tenant.Id, StringComparison.Ordinal))
            return tenant;

        user.IdentityTenantId = tenant.Id;
        await repo.UpdateAsync(user, cancellationToken: cancellationToken);

        logger.LogInformation("Assigned tenant '{TenantName}' ({TenantId}) to user '{UserName}'.",
            tenant.Name, tenant.Id, user.UserName);

        return tenant;
    }
}
