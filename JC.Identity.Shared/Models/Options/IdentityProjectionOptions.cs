using System.Security.Claims;
using JC.Core.Enums;

namespace JC.Identity.Shared.Models.Options;

/// <summary>
/// How <see cref="JC.Identity.Shared.Middleware.UserInfoMiddleware"/> projects the current
/// principal: the claim types it reads the user's identity from, and the authority it stamps on
/// the result.
/// </summary>
/// <remarks>
/// These exist so the middleware need not know which authority authenticated the user. JC.Identity
/// copies the claim types from <c>IdentityOptions.ClaimsIdentity</c> at registration; another
/// authority sets whatever its own tokens carry. The remaining claims are fixed constants on
/// <see cref="JC.Identity.Shared.Authentication.DefaultClaims"/>.
/// </remarks>
public class IdentityProjectionOptions
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
    /// Gets or sets the authority that supplied the current identity.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="IdentityAuthority.None"/>, so an authority that never states its own
    /// value cannot silently pass as local. JC.Identity sets <see cref="IdentityAuthority.Local"/>
    /// at registration.
    /// </remarks>
    public IdentityAuthority Authority { get; set; } = IdentityAuthority.None;
}
