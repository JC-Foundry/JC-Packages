using System.Security.Claims;
using JC.Core.Models;

namespace JC.Identity.Models;

/// <summary>
/// Default <see cref="IUserInfo"/> implementation populated per-request by <see cref="JC.Identity.Middleware.UserInfoMiddleware"/>.
/// Provides system and unknown user constants for unauthenticated/fallback scenarios.
/// </summary>
public class UserInfo : IUserInfo
{
    /// <inheritdoc />
    public string UserId { get; set; } = IUserInfo.UNKNOWN_USER_ID;

    /// <inheritdoc />
    public string Username { get; set; } = IUserInfo.UNKNOWN_USER_NAME;

    /// <inheritdoc />
    public string Email { get; set; } = IUserInfo.UNKNOWN_USER_EMAIL;

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

    public UserInfo()
    {
    }

    public UserInfo(BaseUser user, IEnumerable<string?> roles)
    {
        UserId = user.Id;
        Username = user.UserName ?? IUserInfo.UNKNOWN_USER_NAME;
        Email = user.Email ?? IUserInfo.UNKNOWN_USER_EMAIL;
        EmailConfirmed = user.EmailConfirmed;
        PhoneNumber = user.PhoneNumber;
        PhoneNumberConfirmed = user.PhoneNumberConfirmed;
        
        TwoFactorEnabled = user.TwoFactorEnabled;
        LockoutEnabled = user.LockoutEnabled;
        LockoutEnd = user.LockoutEnd?.DateTime;
        AccessFailedCount = user.AccessFailedCount;
        
        TenantId = user.TenantId;
        DisplayName = user.DisplayName;
        LastLoginUtc = user.LastLoginUtc;
        IsEnabled = user.IsEnabled;
        
        RequiresPasswordChange = user.RequirePasswordChange;

        Roles = roles.Where(r => !string.IsNullOrEmpty(r)).ToList()!;
        IsSetup = true;
    }

    public UserInfo(BaseUser user, IEnumerable<BaseRole> roles)
        : this(user, roles.Select(r => r.Name))
    {
    }
}