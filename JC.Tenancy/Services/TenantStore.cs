using JC.Core.Enums;
using JC.Core.Extensions;
using JC.Core.Services.DataRepositories;
using JC.Tenancy.Data;
using JC.Tenancy.Models;
using Microsoft.EntityFrameworkCore;

namespace JC.Tenancy.Services;

/// <summary>
/// Default <see cref="ITenantStore"/>, reading and writing tenants through the context that owns
/// them and invalidating the cache on every write.
/// </summary>
/// <typeparam name="TContext">The context owning tenant storage.</typeparam>
/// <remarks>
/// Generic over the owning context so it can bind the repository manager to it explicitly. A
/// consuming application may have several contexts, and only one of them holds tenants.
/// </remarks>
public class TenantStore<TContext>(IRepositoryManager repos, TenantCache cache) : ITenantStore
    where TContext : DbContext, ITenantDbContext
{
    private IRepositoryContext<Tenant> Repository => repos.For<TContext>().GetRepository<Tenant>();

    #region Queries

    /// <inheritdoc />
    public async Task<Tenant?> GetAsync(string tenantId,
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive,
        CancellationToken cancellationToken = default)
        => await Repository.AsQueryable()
            .AsNoTracking()
            .FilterDeleted(deletedQueryType)
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

    /// <inheritdoc />
    public async Task<Tenant?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default)
        => await Repository.AsQueryable()
            .AsNoTracking()
            .FilterDeleted(DeletedQueryType.OnlyActive)
            .FirstOrDefaultAsync(t => t.Domain == domain, cancellationToken);

    /// <inheritdoc />
    public async Task<List<Tenant>> GetAllAsync(DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive,
        CancellationToken cancellationToken = default)
        => await Repository.AsQueryable()
            .AsNoTracking()
            .FilterDeleted(deletedQueryType)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

    #endregion

    #region Mutations

    /// <inheritdoc />
    public async Task<TenantValidationResponse> TryAddAsync(Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(tenant, cancellationToken);
        if (!validation.IsValid) return validation;

        await Repository.AddAsync(tenant, cancellationToken: cancellationToken);

        // Misses are cached, so a lookup made before this tenant existed would otherwise persist.
        cache.Invalidate(tenant.Id);

        return new TenantValidationResponse(tenant);
    }

    /// <inheritdoc />
    public async Task<TenantValidationResponse> TryUpdateAsync(Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(tenant, cancellationToken);
        if (!validation.IsValid) return validation;

        await Repository.UpdateAsync(tenant, cancellationToken: cancellationToken);

        cache.Invalidate(tenant.Id);

        return new TenantValidationResponse(tenant);
    }

    /// <inheritdoc />
    public async Task<bool> TryDeleteAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await Repository.AsQueryable()
            .FilterDeleted(DeletedQueryType.OnlyActive)
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null) return false;

        await Repository.SoftDeleteAsync(tenant, cancellationToken: cancellationToken);

        cache.Invalidate(tenantId);

        return true;
    }

    /// <inheritdoc />
    public async Task<TenantValidationResponse> TryRestoreAsync(string tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await Repository.AsQueryable()
            .FilterDeleted(DeletedQueryType.OnlyDeleted)
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null) return new TenantValidationResponse("No deleted tenant was found with that identifier.");

        // The name or domain may have been claimed by another tenant since this one was deleted.
        var validation = await ValidateAsync(tenant, cancellationToken);
        if (!validation.IsValid) return validation;

        await Repository.RestoreAsync(tenant, cancellationToken: cancellationToken);

        cache.Invalidate(tenantId);

        return new TenantValidationResponse(tenant);
    }

    /// <inheritdoc />
    public async Task<bool> TrySetSettingAsync(string tenantId, string key, string value, bool isActive = true,
        CancellationToken cancellationToken = default)
    {
        var tenant = await Repository.AsQueryable()
            .FilterDeleted(DeletedQueryType.OnlyActive)
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null) return false;

        tenant.SetSetting(key, value, isActive);
        await Repository.UpdateAsync(tenant, cancellationToken: cancellationToken);

        cache.Invalidate(tenantId);

        return true;
    }

    #endregion

    /// <summary>
    /// Checks that a tenant has a name, and that neither its name nor its domain is already taken
    /// by another tenant.
    /// </summary>
    /// <param name="tenant">The tenant to validate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="TenantValidationResponse"/> carrying the tenant, or the reason it was rejected.</returns>
    /// <remarks>
    /// Checked against active tenants only, so a name freed by a soft-delete can be reused —
    /// at the cost of that tenant failing revalidation if anyone later restores it.
    /// <para>
    /// Case sensitivity follows the database collation rather than being forced here, so that the
    /// check agrees with any unique index an application adds over the same columns.
    /// </para>
    /// </remarks>
    private async Task<TenantValidationResponse> ValidateAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenant.Name))
            return new TenantValidationResponse("A tenant name is required.");

        var others = Repository.AsQueryable()
            .AsNoTracking()
            .FilterDeleted(DeletedQueryType.OnlyActive)
            .Where(t => t.Id != tenant.Id);

        if (await others.AnyAsync(t => t.Name == tenant.Name, cancellationToken))
            return new TenantValidationResponse($"A tenant named '{tenant.Name}' already exists.");

        if (!string.IsNullOrWhiteSpace(tenant.Domain)
            && await others.AnyAsync(t => t.Domain == tenant.Domain, cancellationToken))
            return new TenantValidationResponse($"A tenant using the domain '{tenant.Domain}' already exists.");

        return new TenantValidationResponse(tenant);
    }
}
