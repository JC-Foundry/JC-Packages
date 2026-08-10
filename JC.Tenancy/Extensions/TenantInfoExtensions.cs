using JC.Tenancy.Models;
using Microsoft.Extensions.DependencyInjection;

namespace JC.Tenancy.Extensions;

/// <summary>
/// Establishes tenant scope explicitly, for work that does not derive it from a signed-in user.
/// </summary>
/// <remarks>
/// <see cref="ITenantInfo"/> is registered scoped and populated in place, so constructing one and
/// handing it around does not change what anything else sees. Everything here resolves the scope's
/// own instance and sets it.
/// <para>
/// The counterpart to JC.Identity.Shared's user-scoping extensions, and usable alongside them: a
/// job that needs both an actor and a tenant establishes the user first, then the tenant.
/// </para>
/// </remarks>
public static class TenantInfoExtensions
{
    /// <summary>
    /// Scopes a service scope to a tenant by identifier.
    /// </summary>
    /// <param name="scopedServices">The scope's service provider.</param>
    /// <param name="tenantId">The tenant identifier, or <c>null</c> for the null partition.</param>
    /// <returns>The scoped <see cref="ITenantInfo"/>.</returns>
    /// <remarks>
    /// Tenant metadata is resolved lazily, so this costs nothing until something reads it. Calling
    /// it inside a live request scope re-scopes the rest of that request, which is a deliberate
    /// cross-tenant act rather than a convenience.
    /// <para>
    /// Passing <c>null</c> pins the null partition, overriding the current user's tenant rather than
    /// falling back to it.
    /// </para>
    /// </remarks>
    public static ITenantInfo SetTenantInfoForTenant(this IServiceProvider scopedServices, string? tenantId)
    {
        var tenantInfo = scopedServices.GetRequiredService<ITenantInfo>();

        tenantInfo.TenantId = tenantId;

        return tenantInfo;
    }

    /// <summary>
    /// Scopes a service scope to an already-loaded tenant, skipping the cache lookup.
    /// </summary>
    /// <param name="scopedServices">The scope's service provider.</param>
    /// <param name="tenant">The tenant to scope to, or <c>null</c> for the null partition.</param>
    /// <returns>The scoped <see cref="ITenantInfo"/>.</returns>
    public static ITenantInfo SetTenantInfoForTenant(this IServiceProvider scopedServices, Tenant? tenant)
    {
        var tenantInfo = scopedServices.GetRequiredService<ITenantInfo>();

        tenantInfo.SetTenant(tenant);

        return tenantInfo;
    }

    /// <summary>
    /// Creates a service scope already scoped to a tenant.
    /// </summary>
    /// <param name="services">The root or parent service provider.</param>
    /// <param name="tenantId">The tenant identifier, or <c>null</c> for the null partition.</param>
    /// <returns>The scope. Dispose it to release the scoped services.</returns>
    /// <example>
    /// <code>
    /// using var scope = _services.CreateScopeForTenant(tenantId);
    /// var repo = scope.ServiceProvider.GetRequiredService&lt;IOrderRepository&gt;();
    /// var orders = await repo.GetAllAsync();   // filtered to that tenant
    /// </code>
    /// </example>
    public static IServiceScope CreateScopeForTenant(this IServiceProvider services, string? tenantId)
    {
        var scope = services.CreateScope();
        scope.ServiceProvider.SetTenantInfoForTenant(tenantId);

        return scope;
    }

    /// <summary>
    /// Creates an asynchronously disposable service scope already scoped to a tenant, for work
    /// whose scoped services implement <see cref="IAsyncDisposable"/>.
    /// </summary>
    /// <param name="services">The root or parent service provider.</param>
    /// <param name="tenantId">The tenant identifier, or <c>null</c> for the null partition.</param>
    /// <returns>The scope. Dispose it with <c>await using</c>.</returns>
    public static AsyncServiceScope CreateAsyncScopeForTenant(this IServiceProvider services, string? tenantId)
    {
        var scope = services.CreateAsyncScope();
        scope.ServiceProvider.SetTenantInfoForTenant(tenantId);

        return scope;
    }
}
