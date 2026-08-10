#pragma warning disable CS1574, CS1584, CS1581, CS1580
namespace JC.Core.Models.MultiTenancy;

/// <summary>
/// Contract for entities that belong to a tenant. Entities implementing this interface
/// are automatically scoped by global query filters in <see cref="JC.Identity.Data.IdentityDataDbContext{TUser, TRole}"/>.
/// </summary>
public interface IMultiTenancy
{
    /// <summary>Gets or sets the tenant identifier this entity belongs to.</summary>
    string? TenantId { get; set; }
}