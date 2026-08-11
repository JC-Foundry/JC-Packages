using JC.Core.Enums;
using JC.Core.Models.Pagination;
using JC.Tenancy.Models;

namespace JC.Tenancy.Services;

/// <summary>
/// The supported boundary for reading and mutating tenants.
/// </summary>
/// <remarks>
/// Writing tenants through EF directly still works, and the tenancy engine makes no attempt to
/// intercept it — but nothing invalidates the cache when you do, so those changes stay invisible
/// until the entry expires. Going through the store is what makes invalidation a guarantee rather
/// than a coincidence.
/// </remarks>
public interface ITenantStore
{
    /// <summary>
    /// Gets a tenant by identifier.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="deletedQueryType">Controls whether soft-deleted tenants are included. Defaults to active only.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tenant, or <c>null</c> if no tenant matches.</returns>
    Task<Tenant?> GetAsync(string tenantId,
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a tenant by its name.
    /// </summary>
    /// <param name="name">The name of the tenant to retrieve.</param>
    /// <param name="deletedQueryType">Specifies whether to include active, deleted, or all tenants. Defaults to active only.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tenant with the specified name, or <c>null</c> if no tenant matches.</returns>
    Task<Tenant?> GetByNameAsync(string name,
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a tenant by its domain.
    /// </summary>
    /// <param name="domain">The domain associated with the tenant to retrieve.</param>
    /// <param name="deletedQueryType">Specifies whether to include only active tenants, only deleted tenants, or all tenants. Defaults to active only.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tenant that matches the specified domain, or <c>null</c> if no matching tenant is found.</returns>
    Task<Tenant?> GetByDomainAsync(string? domain,
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tenants, ordered by name.
    /// </summary>
    /// <param name="deletedQueryType">Controls whether soft-deleted tenants are included. Defaults to active only.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching tenants.</returns>
    Task<List<Tenant>> GetAllAsync(DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of tenants based on the specified page number, page size, and deletion query type.
    /// </summary>
    /// <param name="pageNumber">The number of the page to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="deletedQueryType">Specifies whether to include all tenants, only active tenants, or only deleted tenants. Defaults to only active tenants.</param>
    /// <returns>A paginated list of tenants.</returns>
    Task<IPagination<Tenant>> GetAllAsync(int pageNumber, int pageSize,
        DeletedQueryType deletedQueryType = DeletedQueryType.OnlyActive);

    /// <summary>
    /// Adds a tenant, provided its name and domain are not already taken.
    /// </summary>
    /// <param name="tenant">The tenant to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="TenantValidationResponse"/> carrying the added tenant, or the reason it was rejected.</returns>
    Task<TenantValidationResponse> TryAddAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a tenant and invalidates its cache entry.
    /// </summary>
    /// <param name="tenant">The tenant to update.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="TenantValidationResponse"/> carrying the updated tenant, or the reason it was rejected.</returns>
    Task<TenantValidationResponse> TryUpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a tenant and invalidates its cache entry.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if a tenant was deleted; <c>false</c> if none matched.</returns>
    /// <remarks>
    /// Affects the tenant record only. Rows elsewhere carrying its identifier are left as they are —
    /// there is no cascade, because tenant-scoped data can live in contexts and databases this store
    /// has never heard of.
    /// </remarks>
    Task<bool> TryDeleteAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted tenant and invalidates its cache entry.
    /// </summary>
    /// <param name="tenantId">The identifier of the tenant to restore.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="TenantValidationResponse"/> carrying the restored tenant, or the reason it was rejected.</returns>
    /// <remarks>
    /// A deleted tenant keeps its name and domain, so a restore cannot clash with a tenant created
    /// while it was away.
    /// </remarks>
    Task<TenantValidationResponse> TryRestoreAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a single setting on a tenant and invalidates its cache entry.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <param name="isActive">Whether the setting is active. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the setting was written; <c>false</c> if no active tenant matched.</returns>
    Task<bool> TrySetSettingAsync(string tenantId, string key, string value, bool isActive = true,
        CancellationToken cancellationToken = default);
}
