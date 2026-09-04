using CAP.SSO.Models;
using JC.Core.Enums;
using JC.Identity.Shared.Models;

namespace JC.CAP.Models;

/// <summary>
/// The CAP <see cref="JC.Core.Models.IUserInfo"/>. Populated per request from the session cookie by the
/// shared claims middleware, which stamps <see cref="IdentityAuthority.CAP"/> as the authority.
/// </summary>
/// <remarks>
/// Nothing beyond the shared surface: CAP's identity block maps onto it field for field. Derive from it
/// and use the generic <c>AddCap</c> overload to carry more.
/// </remarks>
public class CapUserInfo : UserInfoBase
{
    /// <summary>Initialises an unpopulated instance for dependency injection. The claims middleware fills it in per request.</summary>
    public CapUserInfo()
    {
    }

    /// <summary>Initialises an instance projected from a CAP user and their role keys, for work outside a request.</summary>
    /// <param name="user">The user record to project.</param>
    /// <param name="roles">The user's role keys. Null and empty entries are discarded.</param>
    /// <remarks>Leaves <see cref="UserInfoBase.TenantId"/> alone: the tenant is JC.CAP.Tenancy's to supply.</remarks>
    public CapUserInfo(CapUser user, IEnumerable<string?> roles)
        : base(user, roles)
    {
        Authority = IdentityAuthority.CAP;
    }

    /// <summary>Initialises an instance from a member as CAP's users API returns them, roles included.</summary>
    /// <param name="user">The member, as returned by the users API.</param>
    public CapUserInfo(ApplicationUserDto user)
        : this(new CapUser(user), user.Roles)
    {
    }
}
