using JC.Core.Enums;
using JC.Core.Extensions;
using JC.Tenancy.Data;
using JC.Tenancy.Models;
using JC.Tenancy.Models.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JC.Tenancy.Services;

/// <summary>
/// Resolves tenants from the owning context, keeping them in memory for a short, configurable
/// window so that establishing tenant scope stays cheap.
/// </summary>
/// <remarks>
/// Entries are invalidated by <see cref="ITenantStore"/> whenever it writes. Changes made outside
/// the store are not detected and stay stale until the entry expires — see
/// <see cref="TenantOptions.CacheLifetime"/>.
/// </remarks>
public class TenantCache(IMemoryCache cache, IServiceProvider services, IOptions<TenantOptions> options)
{
    private const string KeyPrefix = "jc-tenancy:tenant:";

    private readonly TenantOptions _options = options.Value;

    /// <summary>
    /// Gets a tenant by identifier, from cache where possible.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The tenant, or <c>null</c> if no tenant has that identifier.</returns>
    /// <remarks>
    /// Synchronous by necessity: it backs <see cref="ITenantInfo"/>'s deferred metadata resolution,
    /// which is reached from property getters. The read is a single indexed lookup and, on all but
    /// the first call in the cache window, does not reach the database at all.
    /// </remarks>
    public Tenant? Get(string? tenantId)
    {
        if (string.IsNullOrEmpty(tenantId)) return null;

        if (!_options.CacheEnabled) return Load(tenantId);

        if (cache.TryGetValue(Key(tenantId), out Tenant? cached)) return cached;

        var tenant = Load(tenantId);

        // A miss is cached too, so an unknown identifier does not hit the database on every read.
        cache.Set(Key(tenantId), tenant, _options.CacheLifetime);

        return tenant;
    }

    /// <summary>
    /// Drops a tenant's cache entry, so the next read resolves it afresh.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    public void Invalidate(string? tenantId)
    {
        if (string.IsNullOrEmpty(tenantId)) return;

        cache.Remove(Key(tenantId));
    }

    private Tenant? Load(string tenantId)
    {
        // Resolved on demand rather than injected: a scope that never reads tenant metadata should
        // not pay for a DbContext it was never going to use.
        var context = services.GetService<ITenantDbContext>();

        return context?.Tenants
            .AsNoTracking()
            .FilterDeleted(DeletedQueryType.OnlyActive)
            .FirstOrDefault(t => t.Id == tenantId);
    }

    private static string Key(string tenantId) => $"{KeyPrefix}{tenantId}";
}
