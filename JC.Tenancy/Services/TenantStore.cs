using JC.Core.Enums;
using JC.Core.Extensions;
using JC.Core.Models.Pagination;
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
    public async Task<Tenant?> GetByNameAsync(string name, 
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive,
        CancellationToken cancellationToken = default)
        => await Repository.AsQueryable()
            .AsNoTracking()
            .FilterDeleted(deletedQueryType)
            .FirstOrDefaultAsync(t => t.Name.ToUpper() == name.ToUpper(), cancellationToken);

    /// <inheritdoc />
    public async Task<Tenant?> GetByDomainAsync(string? domain, 
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive,
        CancellationToken cancellationToken = default)
        => await Repository.AsQueryable()
            .AsNoTracking()
            .FilterDeleted(deletedQueryType)
            .FirstOrDefaultAsync(t => !string.IsNullOrEmpty(t.Domain) 
                                      && !string.IsNullOrEmpty(domain) 
                                      && t.Domain.ToLower() == domain.ToLower(), 
                cancellationToken);

    /// <inheritdoc />
    public async Task<List<Tenant>> GetAllAsync(DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive,
        CancellationToken cancellationToken = default)
        => await Repository.AsQueryable()
            .AsNoTracking()
            .FilterDeleted(deletedQueryType)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    
    /// <inheritdoc />
    public async Task<IPagination<Tenant>> GetAllAsync(int pageNumber, int pageSize,
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive)
        => await Repository.AsQueryable()
            .AsNoTracking()
            .FilterDeleted(deletedQueryType)
            .OrderBy(t => t.Name)
            .ToPagedListAsync(pageNumber, pageSize);

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
    /// Checked against soft-deleted tenants too, because the unique index on <c>Name</c> still holds
    /// their rows — a deleted name is not free to reuse, so validating active-only would pass here
    /// and then fail on the constraint.
    /// <para>
    /// Case sensitivity follows the database collation, so the check agrees with the index.
    /// </para>
    /// </remarks>
    private async Task<TenantValidationResponse> ValidateAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenant.Name))
            return new TenantValidationResponse("A tenant name is required.");

        var others = Repository.AsQueryable()
            .AsNoTracking()
            .FilterDeleted(DeletedQueryType.All)
            .Where(t => t.Id != tenant.Id);

        var nameClash = await others.FirstOrDefaultAsync(t => t.Name == tenant.Name, cancellationToken);
        if (nameClash is not null)
            return new TenantValidationResponse(Taken($"named '{tenant.Name}'", nameClash.IsDeleted));

        if (!string.IsNullOrWhiteSpace(tenant.Domain))
        {
            var domainClash = await others.FirstOrDefaultAsync(t => t.Domain == tenant.Domain, cancellationToken);
            if (domainClash is not null)
                return new TenantValidationResponse(Taken($"using the domain '{tenant.Domain}'", domainClash.IsDeleted));
        }

        return new TenantValidationResponse(tenant);

        //A deleted tenant keeps its name and domain, so say so - otherwise the caller is told
        //something already exists that they cannot find anywhere
        static string Taken(string what, bool isDeleted)
            => isDeleted
                ? $"A deleted tenant {what} still holds it. Restore that tenant, or rename it to free the value."
                : $"A tenant {what} already exists.";
    }
}
