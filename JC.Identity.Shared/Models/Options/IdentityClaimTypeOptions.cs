using System.Security.Claims;
using JC.Core.Enums;

namespace JC.Identity.Shared.Models.Options;

/// <summary>
/// How <see cref="JC.Identity.Shared.Middleware.UserInfoMiddleware"/> projects the current
/// principal: the claim types it reads the user's identity from, and the authority it stamps on
/// the result.
/// </summary>
/// <remarks>
/// These three values exist because the middleware must not know which authority authenticated the
/// user. ASP.NET Identity keeps the same three on <c>IdentityOptions.ClaimsIdentity</c>, and
/// JC.Identity copies them across at registration so that reconfiguring ASP.NET Identity keeps
/// working. A different authority sets them to whatever its own tokens carry.
/// <para>
/// The remaining claims the middleware reads are JC's own, and are fixed constants on
/// <see cref="JC.Identity.Shared.Authentication.DefaultClaims"/> rather than options — every
/// authority is expected to emit those under the same names.
/// </para>
/// </remarks>
public class IdentityClaimTypeOptions
{
    /// <summary>
    /// Gets or sets the claim type carrying the user identifier. Defaults to
    /// <see cref="ClaimTypes.NameIdentifier"/>, matching ASP.NET Identity.
    /// </summary>
    public string UserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;

    /// <summary>
    /// Gets or sets the claim type carrying the email address. Defaults to
    /// <see cref="ClaimTypes.Email"/>, matching ASP.NET Identity.
    /// </summary>
    public string EmailClaimType { get; set; } = ClaimTypes.Email;

    /// <summary>
    /// Gets or sets the claim type carrying role membership. Defaults to
    /// <see cref="ClaimTypes.Role"/>, matching ASP.NET Identity.
    /// </summary>
    public string RoleClaimType { get; set; } = ClaimTypes.Role;

    /// <summary>
    /// Gets or sets the identity authority type used for authentication.
    /// This property indicates the source or mechanism of authentication,
    /// such as local, CAP SSO, custom, or none.
    /// </summary>
    public IdentityAuthority Authority { get; set; } = IdentityAuthority.Local;
}
