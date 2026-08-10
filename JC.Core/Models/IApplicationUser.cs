namespace JC.Core.Models;

/// <summary>
/// How the suite stores a user. Describes <b>any</b> user record, not only the one currently
/// signed in.
/// </summary>
/// <remarks>
/// Not the same concern as <see cref="IUserInfo"/>, which is the runtime projection of whoever is
/// executing the current operation. Read/write, because storage is not a one-way concern; which
/// store stands behind it — ASP.NET Identity, a CAP-supplied record, something else — is not this
/// contract's business.
/// </remarks>
public interface IApplicationUser
{
    /// <summary>Gets or sets the unique identifier of the user record.</summary>
    string Id { get; set; }

    /// <summary>Gets or sets the username, if one is set.</summary>
    string? UserName { get; set; }

    /// <summary>Gets or sets the email address, if one is set.</summary>
    string? Email { get; set; }

    /// <summary>Gets or sets whether the email address has been confirmed.</summary>
    bool EmailConfirmed { get; set; }

    /// <summary>Gets or sets the phone number, if one is set.</summary>
    string? PhoneNumber { get; set; }

    /// <summary>Gets or sets whether the phone number has been confirmed.</summary>
    bool PhoneNumberConfirmed { get; set; }

    /// <summary>Gets or sets whether two-factor authentication is enabled.</summary>
    bool TwoFactorEnabled { get; set; }

    /// <summary>Gets or sets whether lockout is enabled for this account.</summary>
    bool LockoutEnabled { get; set; }

    /// <summary>Gets or sets the UTC date and time when the lockout ends, if the account is locked out.</summary>
    DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>Gets or sets the number of consecutive failed access attempts.</summary>
    int AccessFailedCount { get; set; }

    /// <summary>Gets or sets the display name, if one is set.</summary>
    string? DisplayName { get; set; }

    /// <summary>Gets or sets whether the account is enabled.</summary>
    bool IsEnabled { get; set; }

    /// <summary>Gets or sets whether the user must change their password before continuing.</summary>
    bool RequirePasswordChange { get; set; }

    /// <summary>Gets or sets the UTC date and time of the user's last login, if they have ever logged in.</summary>
    DateTime? LastLoginUtc { get; set; }
    
    /// <summary>Gets or sets the UTC date and time of the user's registration.</summary>
    DateTime? RegistrationUtc { get; set; }

    /// <summary>
    /// Gets or sets the tenant that owns the authoritative identity record.
    /// </summary>
    /// <remarks>
    /// This is <b>not</b> interchangeable with <see cref="IUserInfo.TenantId"/>, which means the
    /// tenant assigned to the user inside the consuming application. For local ASP.NET Identity the
    /// two commonly hold the same value; for an externally supplied identity they need not.
    /// </remarks>
    string? IdentityTenantId { get; set; }
}
