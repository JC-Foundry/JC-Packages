using JC.Identity.Shared.Models;

namespace JC.CAP.Models;

/// <summary>
/// The CAP <see cref="JC.Core.Models.IUserInfo"/>. Populated per request from the session cookie by the
/// shared claims middleware, which stamps <see cref="JC.Core.Enums.IdentityAuthority.CAP"/> as the authority.
/// </summary>
/// <remarks>
/// Nothing beyond the shared surface: CAP's identity block maps onto it field for field. Derive from it
/// and use the generic <c>AddCap</c> overload to carry more.
/// </remarks>
public class CapUserInfo : UserInfoBase;
