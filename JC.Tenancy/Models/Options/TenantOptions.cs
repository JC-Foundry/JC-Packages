namespace JC.Tenancy.Models.Options;

/// <summary>
/// Configuration for the tenancy engine: how long resolved tenants are cached, and which entity
/// types the automatic query filters leave alone.
/// </summary>
public class TenantOptions
{
    /// <summary>
    /// Gets or sets whether resolved tenants are cached. Defaults to <c>true</c>.
    /// </summary>
    /// <remarks>Turning this off makes every metadata read hit the store, and is intended for diagnosis.</remarks>
    public bool CacheEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets how long a resolved tenant stays cached. Defaults to five minutes.
    /// </summary>
    /// <remarks>
    /// Deliberately short. A tenant carries security and business state — expiry, domain rules,
    /// settings — so a long lifetime means a revoked or reconfigured tenant stays live far longer
    /// than anyone expects. Mutations through <see cref="Services.ITenantStore"/> invalidate the
    /// entry immediately; this window only covers changes made outside it.
    /// </remarks>
    public TimeSpan CacheLifetime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets the entity types excluded from automatic tenant filtering, despite implementing
    /// <see cref="JC.Core.Models.MultiTenancy.IMultiTenancy"/>.
    /// </summary>
    /// <remarks>
    /// Every exclusion is a type whose rows cross tenants on every query. Use it for genuinely
    /// shared reference data, and be aware that nothing filters these afterwards.
    /// </remarks>
    public HashSet<Type> ExcludedEntityTypes { get; } = [];

    /// <summary>
    /// Gets the role names permitted to query across all tenants through the safe cross-tenant API.
    /// </summary>
    /// <remarks>
    /// Empty by default, which denies everyone — an application that has not thought about
    /// cross-tenant access does not accidentally grant it. Applications on JC.Identity will
    /// normally add <c>SystemAdmin</c>; the name is configured rather than assumed because
    /// JC.Tenancy cannot reference the identity packages, and another authority may call the same
    /// idea something else.
    /// </remarks>
    public HashSet<string> BypassRoles { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Permits a role to query across all tenants through the safe cross-tenant API.
    /// </summary>
    /// <param name="role">The role name.</param>
    /// <returns>The options, for chaining.</returns>
    public TenantOptions AllowBypassForRole(string role)
    {
        BypassRoles.Add(role);
        return this;
    }

    /// <summary>
    /// Excludes an entity type from automatic tenant filtering.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to exclude.</typeparam>
    /// <returns>The options, for chaining.</returns>
    public TenantOptions Exclude<TEntity>() where TEntity : class
    {
        ExcludedEntityTypes.Add(typeof(TEntity));
        return this;
    }
}
