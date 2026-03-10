using System.Security.Claims;

namespace JC.Core.Models;

/// <summary>
/// Read-only contract representing the current user's identity, profile, security state, and authorisation details.
/// Populated per-request by middleware in JC.Identity.
/// </summary>
public interface IUserInfo
{
    public const string MissingUserInfoId = "<NONE>";
    
    /// <summary>System user identifier used for unauthenticated requests.</summary>
    public const string SYSTEM_USER_ID = "System__ID";

    /// <summary>System username used for unauthenticated requests.</summary>
    public const string SYSTEM_USER_NAME = "System";

    /// <summary>System email used for unauthenticated requests.</summary>
    public const string SYSTEM_USER_EMAIL = "<SYSTEM@EMAIL>";

    /// <summary>Unknown user identifier used as the default fallback.</summary>
    public const string UNKNOWN_USER_ID = "Unknown__ID";

    /// <summary>Unknown username used as the default fallback.</summary>
    public const string UNKNOWN_USER_NAME = "Unknown";

    /// <summary>Unknown email used as the default fallback.</summary>
    public const string UNKNOWN_USER_EMAIL = "<UNKNOWN@EMAIL>";
    
    
    /// <summary>Gets the unique identifier of the current user.</summary>
    string UserId { get; set; }

    /// <summary>Gets the username of the current user.</summary>
    string Username { get; set; }

    /// <summary>Gets the email address of the current user.</summary>
    string Email { get; set; }

    /// <summary>Gets whether the user's email address has been confirmed.</summary>
    bool EmailConfirmed { get; set; }

    /// <summary>Gets the user's phone number, if set.</summary>
    string? PhoneNumber { get; set; }

    /// <summary>Gets whether the user's phone number has been confirmed.</summary>
    bool PhoneNumberConfirmed { get; set; }

    /// <summary>Gets whether two-factor authentication is enabled for the user.</summary>
    bool TwoFactorEnabled { get; set; }

    /// <summary>Gets whether lockout is enabled for the user.</summary>
    bool LockoutEnabled { get; set; }

    /// <summary>Gets the UTC date and time when the user's lockout ends, if locked out.</summary>
    DateTime? LockoutEnd { get; set; }

    /// <summary>Gets the number of consecutive failed access attempts.</summary>
    int AccessFailedCount { get; set; }

    /// <summary>Gets the tenant identifier the user belongs to, if multi-tenancy is active.</summary>
    string? TenantId { get; set; }

    /// <summary>Gets the user's display name.</summary>
    string? DisplayName { get; set; }

    /// <summary>Gets the UTC date and time of the user's last login.</summary>
    DateTime? LastLoginUtc { get; set; }

    /// <summary>Gets whether the user account is enabled.</summary>
    bool IsEnabled { get; set; }

    /// <summary>Gets whether the user is required to change their password.</summary>
    bool RequiresPasswordChange { get; set; }

    /// <summary>Gets whether the user info has been populated for this request.</summary>
    bool IsSetup { get; set; }

    /// <summary>Gets whether multi-tenancy is enabled for the current user (i.e. they have a tenant).</summary>
    bool MultiTenancyEnabled { get; set; }

    /// <summary>Gets the list of role names assigned to the current user.</summary>
    IReadOnlyList<string> Roles { get; set; }

    /// <summary>Gets the full list of claims associated with the current user.</summary>
    IReadOnlyList<Claim> Claims { get; set; }

    /// <summary>
    /// Determines whether the current user belongs to the specified role.
    /// </summary>
    /// <param name="role">The role name to check.</param>
    /// <returns><c>true</c> if the user is in the role; otherwise <c>false</c>.</returns>
    bool IsInRole(string role);
}