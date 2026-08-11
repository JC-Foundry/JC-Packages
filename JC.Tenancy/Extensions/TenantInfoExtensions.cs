using JC.Tenancy.Models;
using JC.Tenancy.Services;
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
    /// Scopes a service scope to the tenant claiming a domain.
    /// </summary>
    /// <param name="scopedServices">The scope's service provider.</param>
    /// <param name="domain">The domain to resolve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tenant now in scope, or <c>null</c> if no active tenant claims that domain.</returns>
    /// <remarks>
    /// A miss leaves the scope untouched rather than pinning the null partition. The resolved record
    /// is handed to the scope, so its metadata costs no further lookup.
    /// </remarks>
    public static async Task<Tenant?> SetTenantInfoForDomainAsync(this IServiceProvider scopedServices,
        string? domain, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain)) return null;

        var store = scopedServices.GetRequiredService<ITenantStore>();

        var tenant = await store.GetByDomainAsync(domain, cancellationToken: cancellationToken);
        if (tenant is null) return null;

        scopedServices.SetTenantInfoForTenant(tenant);

        return tenant;
    }

    /// <summary>
    /// Creates a service scope already scoped to a tenant.
    /// </summary>
    /// <param name="services">The root or parent service provider.</param>
    /// <param name="tenantId">The tenant identifier, or <c>null</c> for the null partition.</param>
    /// <returns>The scope. Dispose it to release the scoped services.</returns>
    public static IServiceScope CreateScopeForTenant(this IServiceProvider services, string? tenantId)
    {
        var scope = services.CreateScope();
        scope.ServiceProvider.SetTenantInfoForTenant(tenantId);

        return scope;
    }

    /// <summary>
    /// Creates a new service scope for a specified tenant.
    /// </summary>
    /// <param name="services">The service provider from which the new scope will be created.</param>
    /// <param name="tenant">The tenant for which the scope is being created, or <c>null</c> if the scope should not target a specific tenant.</param>
    /// <returns>A new <see cref="IServiceScope"/> that is configured for the specified tenant.</returns>
    public static IServiceScope CreateScopeForTenant(this IServiceProvider services, Tenant? tenant)
    {
        var scope = services.CreateScope();
        scope.ServiceProvider.SetTenantInfoForTenant(tenant);

        return scope;
    }
    
    
    /// <summary>
    /// Creates an asynchronously disposable service scope already scoped to a tenant, for work
    /// whose scoped services implement <see cref="IAsyncDisposable"/>.
    /// </summary>
    /// <param name="services">The root or parent service provider.</param>
    /// <param name="tenantId">The tenant identifier, or <c>null</c> for the null partition.</param>
    /// <returns>The scope. Dispose it with <c>await using</c>.</returns>
    public static Task<AsyncServiceScope> CreateAsyncScopeForTenant(this IServiceProvider services, string? tenantId)
    {
        var scope = services.CreateAsyncScope();
        scope.ServiceProvider.SetTenantInfoForTenant(tenantId);

        return Task.FromResult(scope);
    }

    /// <summary>
    /// Creates an asynchronous service scope and scopes it to a specified tenant.
    /// </summary>
    /// <param name="services">The service provider for the application.</param>
    /// <param name="tenant">The tenant to scope the service scope to, or <c>null</c> to use the null partition.</param>
    /// <returns>An asynchronous task containing the scoped <see cref="AsyncServiceScope"/>.</returns>
    public static Task<AsyncServiceScope> CreateAsyncScopeForTenant(this IServiceProvider services, Tenant? tenant)
    {
        var scope = services.CreateAsyncScope();
        scope.ServiceProvider.SetTenantInfoForTenant(tenant);

        return Task.FromResult(scope);
    }

    /// <summary>
    /// Creates an asynchronous service scope and configures it for the tenant based on the specified domain.
    /// </summary>
    /// <param name="services">The service provider used to create the asynchronous service scope.</param>
    /// <param name="domain">The domain associated with the tenant to scope to, or <c>null</c> for no domain-specific scope.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the scoped <see cref="AsyncServiceScope"/>.</returns>
    public static async Task<AsyncServiceScope> CreateAsyncScopeForTenantByDomain(this IServiceProvider services, string? domain)
    {
        var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.SetTenantInfoForDomainAsync(domain);

        return scope;
    }
}
