using System.Security.Claims;

namespace JC.Core.Models;

public interface IUserInfo
{
    string UserId { get; set; }
    string Username { get; set; }
    
    string Email { get; set; }
    bool EmailConfirmed { get; set; }
    
    string? PhoneNumber { get; set; }
    bool PhoneNumberConfirmed { get; set; }
    
    bool TwoFactorEnabled { get; set; }
    bool LockoutEnabled { get; set; }
    DateTime? LockoutEnd { get; set; }
    int AccessFailedCount { get; set; }
    
    string? TenantId { get; set; }
    string? DisplayName { get; set; }
    DateTime? LastLoginUtc { get; set; }
    bool IsEnabled { get; set; }
    bool RequiresPasswordChange { get; set; }

    bool IsSetup { get; set; }
    bool MultiTenancyEnabled { get; set; }
    
    IReadOnlyList<string> Roles { get; set; }
    IReadOnlyList<Claim> Claims { get; set; }
    
    bool IsInRole(string role);
}