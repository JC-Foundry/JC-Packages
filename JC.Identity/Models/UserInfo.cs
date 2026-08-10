using JC.Core.Enums;
using JC.Identity.Shared.Models;

namespace JC.Identity.Models;


/// <summary>
/// The local ASP.NET Identity <see cref="JC.Core.Models.IUserInfo"/>, projecting a
/// <see cref="BaseUser"/> onto the shared user-info surface.
/// </summary>
public class UserInfo : UserInfoBase
{
    /// <summary>
    /// Initialises an unpopulated instance for dependency injection to activate. The claims
    /// middleware fills it in per request.
    /// </summary>
    public UserInfo()
    {
        Authority = IdentityAuthority.Local;
    }
    
    /// <summary>
    /// Initialises an instance projected from a user entity and their role names.
    /// </summary>
    /// <param name="user">The user entity to project.</param>
    /// <param name="roles">The user's role names. Null and empty entries are discarded.</param>
    /// <remarks>
    /// Adds the two things the base constructor deliberately leaves alone:
    /// <see cref="UserInfoBase.TenantId"/>, because for local Identity the tenant owning the record
    /// and the user's application tenant are the same value; and
    /// <see cref="UserInfoBase.Authority"/>, which this package can state outright.
    /// </remarks>
    public UserInfo(BaseUser user, IEnumerable<string?> roles)
        : base(user, roles)
    {
        //Sets tenant ID as this is on the BaseUser
        TenantId = user.TenantId;
        Authority = IdentityAuthority.Local;
    }

    /// <summary>
    /// Initialises an instance projected from a user entity and their role entities.
    /// </summary>
    /// <param name="user">The user entity to project.</param>
    /// <param name="roles">The user's roles.</param>
    public UserInfo(BaseUser user, IEnumerable<BaseRole> roles)
        : this(user, roles.Select(r => r.Name))
    {
    }
}