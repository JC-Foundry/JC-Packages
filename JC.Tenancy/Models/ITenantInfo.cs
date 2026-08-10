using JC.Core.Models.MultiTenancy;

namespace JC.Tenancy.Models;

/// <summary>
/// The tenant the current operation is scoped to. Registered scoped, and resolved per scope from
/// the signed-in user or set explicitly for work that has no user.
/// </summary>
/// <remarks>
/// The counterpart to <see cref="JC.Core.Models.IUserInfo"/>: that answers <i>who</i> is acting,
/// this answers <i>which tenant they are acting within</i>. Most of the surface comes from
/// <see cref="ITenantContext"/> in JC.Core; only the members needing the concrete
/// <see cref="Tenant"/> record are declared here.
/// </remarks>
public interface ITenantInfo : ITenantContext
{
    /// <summary>
    /// Scopes to an already-loaded tenant, skipping the cache lookup entirely.
    /// </summary>
    /// <param name="tenant">The tenant to scope to, or <c>null</c> for the null partition.</param>
    /// <remarks>
    /// For callers holding the record already — seeding, or work immediately after creating a
    /// tenant — so a freshly written tenant is visible without waiting on the cache.
    /// </remarks>
    void SetTenant(Tenant? tenant);

    /// <summary>
    /// Gets the tenant's active settings.
    /// </summary>
    /// <returns>The active settings, or an empty collection in the null partition.</returns>
    IReadOnlyList<TenantSettings> GetSettings();
}
