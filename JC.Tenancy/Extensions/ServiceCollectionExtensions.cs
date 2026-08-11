using JC.Core.Models;
using JC.Core.Models.MultiTenancy;
using JC.Tenancy.Data;
using JC.Tenancy.Models;
using JC.Tenancy.Models.Options;
using JC.Tenancy.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JC.Tenancy.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> providing JC.Tenancy registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the tenancy engine against the context that owns tenant storage.
    /// </summary>
    /// <typeparam name="TContext">The context owning the tenant table.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Optional callback to configure <see cref="TenantOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when tenancy has already been registered against a different context. One tenancy
    /// domain has exactly one authoritative tenant table, and two would mean two disagreeing
    /// answers to which tenants exist.
    /// </exception>
    /// <remarks>
    /// Registering the engine does not by itself filter anything. Each context that should be
    /// tenant-scoped implements <see cref="ITenantScopedContext"/> and calls
    /// <see cref="DataExtensions.ApplyTenantFilters"/> from its <c>OnModelCreating</c> — which is
    /// what allows many contexts to be filtered while only this one stores tenants.
    /// <para>
    /// <see cref="ITenantInfo"/> is registered as a scoped factory rather than populated by
    /// middleware, so tenant scope is established identically in a request, a background job and a
    /// console application, and JC.Tenancy needs no dependency on ASP.NET Core.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddTenancy<TContext>(this IServiceCollection services,
        Action<TenantOptions>? configure = null)
        where TContext : DbContext, ITenantDbContext
    {
        var owner = services
            .FirstOrDefault(s => s.ServiceType == typeof(TenantStoreOwner))?
            .ImplementationInstance as TenantStoreOwner;

        if (owner is not null)
            throw new InvalidOperationException(
                $"Tenancy has already been registered against '{owner.ContextType.Name}'. " +
                $"Exactly one {nameof(DbContext)} may own tenant storage. Other contexts participate in " +
                $"filtering by implementing '{nameof(ITenantScopedContext)}' — they do not need to be registered here.");

        services.AddSingleton(new TenantStoreOwner(typeof(TContext)));

        if (configure != null)
            services.Configure(configure);
        else
            services.Configure<TenantOptions>(_ => { });

        services.AddMemoryCache();

        services.AddScoped<ITenantDbContext>(sp => sp.GetRequiredService<TContext>());

        services.AddScoped<TenantCache>();
        services.TryAddScoped<ITenantStore, TenantStore<TContext>>();

        // TryAdd, so an application can register its own authorisation rule instead.
        services.TryAddScoped<ITenantBypassAuthoriser, RoleTenantBypassAuthoriser>();

        services.TryAddScoped<TenantSeeder>();

        // IUserInfo is handed over, not read here: it is populated in place later in the request, so
        // reading it at construction would pin the scope to whatever it held at the time - usually
        // nothing. Optional, so tenancy works with no identity package and starts in the null partition.
        services.AddScoped<ITenantInfo>(sp =>
            new TenantInfo(sp.GetRequiredService<TenantCache>(), sp.GetService<IUserInfo>()));

        // Forwarded to the same scoped instance, never registered separately — two instances in one
        // scope could hold different tenants, which is worse than not resolving at all. This is what
        // lets a package read the operational tenant with only a JC.Core reference.
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<ITenantInfo>());

        return services;
    }
}
