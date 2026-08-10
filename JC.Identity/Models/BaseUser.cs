using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Core.Models.MultiTenancy;
using JC.Identity.Shared.Models;
using Microsoft.AspNetCore.Identity;

namespace JC.Identity.Models;

/// <summary>
/// Base user entity extending ASP.NET Core <see cref="IdentityUser"/> with multi-tenancy,
/// display name, login tracking, and account management properties. Implements
/// <see cref="IApplicationUser"/> so the record can be consumed without a reference to ASP.NET
/// Identity.
/// </summary>
public class BaseUser : IdentityUser, IApplicationUser
{
    /// <inheritdoc cref="IMultiTenancy.TenantId" />
    [MaxLength(36)]
    public string? TenantId { get; set; }
    
    /// <inheritdoc cref="IApplicationUser.IdentityTenantId" />
    /// <remarks>
    /// A projection of <see cref="TenantId"/>, so the existing column is reused and no migration is
    /// needed. Not mapped: it has no setter and is not a column of its own.
    /// </remarks>
    [NotMapped]
    public string? IdentityTenantId => TenantId;

    /// <summary>Gets or sets the user's display name.</summary>
    [MaxLength(256)]
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the UTC date and time of the user's last login.</summary>
    public DateTime? LastLoginUtc { get; set; }


    /// <summary>Gets or sets whether the user account is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets whether the user must change their password on next login.</summary>
    public bool RequirePasswordChange { get; set; }
}