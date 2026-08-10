using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JC.Core.Models;
using JC.Core.Models.MultiTenancy;
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
    /// Reads and writes <see cref="TenantId"/>, so the existing column carries the value and no
    /// migration is needed. Not mapped: it is a second way to reach that column, not a column of
    /// its own.
    /// </remarks>
    [NotMapped]
    public string? IdentityTenantId
    {
        get => TenantId;
        set => TenantId = value;
    }

    /// <inheritdoc/>
    [MaxLength(256)]
    public string? DisplayName { get; set; }

    /// <inheritdoc/>
    public DateTime? LastLoginUtc { get; set; }
    
    /// <inheritdoc/>
    public DateTime? RegistrationUtc { get; set; }


    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public bool RequirePasswordChange { get; set; }
}