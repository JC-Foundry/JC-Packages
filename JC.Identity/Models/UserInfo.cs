using System.Security.Claims;
using JC.Core.Models;

namespace JC.Identity.Models;

/// <summary>
/// Default <see cref="IUserInfo"/> implementation populated per-request by <see cref="JC.Identity.Middleware.UserInfoMiddleware"/>.
/// Provides system and unknown user constants for unauthenticated/fallback scenarios.
/// </summary>
public class UserInfo : IUserInfo
{
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

    /// <inheritdoc />
    public string UserId { get; set; } = UNKNOWN_USER_ID;

    /// <inheritdoc />
    public string Username { get; set; } = UNKNOWN_USER_NAME;

    /// <inheritdoc />
    public string Email { get; set; } = UNKNOWN_USER_EMAIL;

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
}