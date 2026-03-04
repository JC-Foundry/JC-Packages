using System.Security.Claims;
using JC.Core.Models;

namespace JC.Identity.Models;

public class UserInfo : IUserInfo
{
    public const string SYSTEM_USER_ID = "System__ID";
    public const string SYSTEM_USER_NAME = "System";
    public const string SYSTEM_USER_EMAIL = "<SYSTEM@EMAIL>";
    public const string UNKNOWN_USER_ID = "Unknown__ID";
    public const string UNKNOWN_USER_NAME = "Unknown";
    public const string UNKNOWN_USER_EMAIL = "<UNKNOWN@EMAIL>";
    
    public string UserId { get; set; } = UNKNOWN_USER_ID;
    public string Username { get; set; } = UNKNOWN_USER_NAME;
    
    public string Email { get; set; } = UNKNOWN_USER_EMAIL;
    public bool EmailConfirmed { get; set; }
    
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    
    public string? TenantId { get; set; }
    public string? DisplayName { get; set; }
    public DateTime? LastLoginUtc { get; set; }
    public bool IsEnabled { get; set; }
    public bool RequiresPasswordChange { get; set; }

    public bool IsSetup { get; set; }
    public bool MultiTenancyEnabled { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = [];
    public IReadOnlyList<Claim> Claims { get; set; } = [];


    public bool IsInRole(string role)
    {
        if(string.IsNullOrEmpty(role))
            return false;
        
        return Roles.Contains(role) || Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == role);
    }
}