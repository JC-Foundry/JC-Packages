using JC.Identity.Models.MultiTenancy;
using Microsoft.AspNetCore.Identity;

namespace JC.Identity.Models;

/// <summary>
/// Base user entity extending ASP.NET Core <see cref="IdentityUser"/> with multi-tenancy,
/// display name, login tracking, and account management properties.
/// </summary>
public class BaseUser : IdentityUser
{
    /// <inheritdoc cref="IMultiTenancy.TenantId" />
    public string? TenantId { get; set; }

    /// <summary>Gets or sets the user's display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the UTC date and time of the user's last login.</summary>
    public DateTime? LastLoginUtc { get; set; }

    /// <summary>Gets or sets whether the user account is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets whether the user must change their password on next login.</summary>
    public bool RequirePasswordChange { get; set; }
}