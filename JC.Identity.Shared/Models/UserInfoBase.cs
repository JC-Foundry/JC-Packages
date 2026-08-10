using System.Security.Claims;
using JC.Core.Enums;
using JC.Core.Models;
using JC.Identity.Shared.Extensions;

namespace JC.Identity.Shared.Models;

/// <summary>
/// Base <see cref="IUserInfo"/> implementation carrying the property surface every identity
/// authority populates, and nothing specific to any one of them.
/// </summary>
/// <remarks>
/// Each authority derives its own type and registers it as the scoped <see cref="IUserInfo"/> —
/// JC.Identity registers one built from its ASP.NET Identity user, a future JC.CAP one built from
/// a CAP-supplied identity. Nothing downstream names the concrete type; consumers inject
/// <see cref="IUserInfo"/>.
/// </remarks>
public class UserInfoBase : IUserInfo
{
    /// <inheritdoc />
    public IdentityAuthority Authority { get; set; } = IdentityAuthority.None;

    /// <inheritdoc />
    public string UserId { get; set; } = IUserInfo.SYSTEM_USER_ID;

    /// <inheritdoc />
    public string Username { get; set; } = IUserInfo.SYSTEM_USER_NAME;

    /// <inheritdoc />
    public string Email { get; set; } = IUserInfo.SYSTEM_USER_EMAIL;

    /// <inheritdoc />
    public bool EmailConfirmed { get; set; }

    /// <inheritdoc />
    public string? PhoneNumber { get; set; }

    /// <inheritdoc />
    public bool PhoneNumberConfirmed { get; set; }

    /// <inheritdoc />
    public bool TwoFactorEnabled { get; set; }

    /// <inheritdoc />
    public bool LockoutEnabled { get; set; }

    /// <inheritdoc />
    public DateTime? LockoutEnd { get; set; }

    /// <inheritdoc />
    public int AccessFailedCount { get; set; }

    /// <inheritdoc />
    public string? TenantId { get; set; }

    /// <inheritdoc />
    public string? DisplayName { get; set; }

    /// <inheritdoc />
    public DateTime? LastLoginUtc { get; set; }

    /// <inheritdoc />
    public DateTime? RegistrationUtc { get; set; }

    /// <inheritdoc />
    public bool IsEnabled { get; set; }

    /// <inheritdoc />
    public bool RequiresPasswordChange { get; set; }

    /// <inheritdoc />
    public bool IsSetup { get; set; }

    /// <inheritdoc />
    public bool MultiTenancyEnabled { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<string> Roles { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<Claim> Claims { get; set; } = [];

    /// <inheritdoc />
    public bool IsInRole(string role)
    {
        if(string.IsNullOrEmpty(role))
            return false;

        return Roles.Contains(role) || Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == role);
    }

    /// <summary>
    /// Initialises an unpopulated instance holding the system-user defaults. This is the
    /// constructor dependency injection activates; the claims middleware fills the instance in
    /// per request.
    /// </summary>
    public UserInfoBase()
    {
    }

    /// <summary>
    /// Initialises an instance projected from an authoritative user record.
    /// </summary>
    /// <param name="user">The user record to project.</param>
    /// <param name="roles">The user's role names. Null and empty entries are discarded.</param>
    /// <remarks>
    /// Delegates to <see cref="Extensions.UserInfoExtensions.PopulateFrom{T}"/>, so constructing an
    /// instance and seeding an existing one share a single projection.
    /// <para>
    /// Deliberately does not set <see cref="TenantId"/>, as
    /// <see cref="JC.Core.Models.IApplicationUser.IdentityTenantId"/> does not inherently mean the user's
    /// application tenant. A derived type that knows the two coincide sets it itself.
    /// </para>
    /// </remarks>
    public UserInfoBase(IApplicationUser user, IEnumerable<string?> roles)
    {
        this.PopulateFrom(user, roles);
    }
}