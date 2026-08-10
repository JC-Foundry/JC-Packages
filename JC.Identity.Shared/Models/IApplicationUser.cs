namespace JC.Identity.Shared.Models;

/// <summary>
/// Read-only contract for an authoritative user record, describing <b>any</b> user rather than
/// only the one currently signed in.
/// </summary>
/// <remarks>
/// This is not the same concern as <see cref="JC.Core.Models.IUserInfo"/>. <c>IUserInfo</c> is the
/// runtime projection of whoever is executing the current operation;
/// <see cref="IApplicationUser"/> is a stored account, and an administrator loading somebody else
/// in a user-management screen is looking at one of these.
/// <para>
/// The contract is read-only so that it fits an ASP.NET Identity entity, a CAP-supplied DTO and a
/// read-only projection equally well. Implementations are free to expose setters.
/// </para>
/// </remarks>
public interface IApplicationUser
{
    /// <summary>Gets the unique identifier of the user record.</summary>
    string Id { get; }

    /// <summary>Gets the username, if one is set.</summary>
    string? UserName { get; }

    /// <summary>Gets the email address, if one is set.</summary>
    string? Email { get; }

    /// <summary>Gets whether the email address has been confirmed.</summary>
    bool EmailConfirmed { get; }

    /// <summary>Gets the phone number, if one is set.</summary>
    string? PhoneNumber { get; }

    /// <summary>Gets whether the phone number has been confirmed.</summary>
    bool PhoneNumberConfirmed { get; }

    /// <summary>Gets whether two-factor authentication is enabled.</summary>
    bool TwoFactorEnabled { get; }

    /// <summary>Gets whether lockout is enabled for this account.</summary>
    bool LockoutEnabled { get; }

    /// <summary>Gets the UTC date and time when the lockout ends, if the account is locked out.</summary>
    DateTimeOffset? LockoutEnd { get; }

    /// <summary>Gets the number of consecutive failed access attempts.</summary>
    int AccessFailedCount { get; }

    /// <summary>Gets the display name, if one is set.</summary>
    string? DisplayName { get; }

    /// <summary>Gets whether the account is enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>Gets whether the user must change their password before continuing.</summary>
    bool RequirePasswordChange { get; }

    /// <summary>Gets the UTC date and time of the user's last login, if they have ever logged in.</summary>
    DateTime? LastLoginUtc { get; }

    /// <summary>
    /// Gets the tenant that owns the authoritative identity record.
    /// </summary>
    /// <remarks>
    /// This is <b>not</b> interchangeable with <see cref="JC.Core.Models.IUserInfo.TenantId"/>,
    /// which means the tenant assigned to the user inside the consuming application. For local
    /// ASP.NET Identity the two commonly hold the same value; for an externally supplied identity
    /// they need not.
    /// </remarks>
    string? IdentityTenantId { get; }
}
