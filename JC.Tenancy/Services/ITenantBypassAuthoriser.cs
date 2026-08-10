using JC.Core.Models;
using JC.Tenancy.Models.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JC.Tenancy.Services;

/// <summary>
/// Decides whether the current caller may read across tenant boundaries.
/// </summary>
/// <remarks>
/// Consulted by the safe cross-tenant query extension. The unsafe variant bypasses this entirely,
/// which is the whole reason it carries that word in its name.
/// </remarks>
public interface ITenantBypassAuthoriser
{
    /// <summary>
    /// Gets whether the current caller may query across all tenants.
    /// </summary>
    /// <returns><c>true</c> to allow the bypass; otherwise <c>false</c>.</returns>
    bool CanAccessAllTenants();
}

/// <summary>
/// Grants cross-tenant access to callers holding one of the roles named in
/// <see cref="TenantOptions.BypassRoles"/>.
/// </summary>
/// <remarks>
/// Role <i>names</i> rather than a constant, deliberately. The obvious answer is JC.Identity's
/// <c>SystemAdmin</c>, but JC.Tenancy and the identity packages are siblings and neither may
/// reference the other — and an application on a different identity authority will have its own
/// name for the same idea. Configuring the name keeps the decision with the application that owns
/// the role.
/// <para>
/// Denies when no roles are configured, and denies when no user is resolvable, so an unconfigured
/// application is closed rather than open.
/// </para>
/// </remarks>
public class RoleTenantBypassAuthoriser(IOptions<TenantOptions> options, IServiceProvider services)
    : ITenantBypassAuthoriser
{
    private readonly TenantOptions _options = options.Value;

    /// <inheritdoc />
    public bool CanAccessAllTenants()
    {
        if (_options.BypassRoles.Count == 0) return false;

        // Resolved rather than injected: tenancy works without an identity package registered,
        // and no user means no bypass.
        var userInfo = services.GetService<IUserInfo>();

        return userInfo is not null && _options.BypassRoles.Any(userInfo.IsInRole);
    }
}
