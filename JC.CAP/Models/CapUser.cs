using CAP.SSO.Models;
using JC.Core.Models;

namespace JC.CAP.Models;

public class CapUser : IApplicationUser
{
    public string Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    public string? DisplayName { get; set; }
    public bool IsEnabled { get; set; }
    public bool RequirePasswordChange { get; set; }
    public DateTime? LastLoginUtc { get; set; }
    public DateTime? RegistrationUtc { get; set; }
    public string? IdentityTenantId { get; set; }

    public CapUser(ApplicationUserDto dto)
    {
        Id = dto.UserId;
        UserName = dto.Username;
        Email = dto.Email;
        EmailConfirmed = dto.EmailConfirmed;
        DisplayName = dto.DisplayName;
        PhoneNumber = dto.PhoneNumber;
        PhoneNumberConfirmed = dto.PhoneNumberConfirmed;
        IsEnabled = dto.IsEnabled;
        TwoFactorEnabled = dto.TwoFactorEnabled;
        LastLoginUtc = dto.LastLoginUtc;
        RegistrationUtc = dto.RegistrationUtc;
    }
}