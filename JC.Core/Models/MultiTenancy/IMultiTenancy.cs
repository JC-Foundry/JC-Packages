namespace JC.Core.Models.MultiTenancy;

/// <summary>
/// Contract for entities that belong to a tenant.
/// </summary>
/// <remarks>
/// Marking is free and lives in Core, so any package can declare an entity tenant-scoped without
/// depending on the tenancy engine. Nothing in Core acts on the mark — JC.Tenancy's
/// <c>ApplyTenantFilters</c> installs the filters, and an application without tenancy simply
/// carries an unused column.
/// </remarks>
public interface IMultiTenancy
{
    /// <summary>Gets or sets the tenant identifier this entity belongs to.</summary>
    string? TenantId { get; set; }
}
