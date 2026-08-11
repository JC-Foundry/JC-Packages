namespace JC.Core.Models.MultiTenancy;

/// <summary>
/// Contract for entities that belong to a tenant.
/// </summary>
/// <remarks>
/// Marking is free and lives in Core, so any package can declare an entity tenant-scoped without
/// depending on the tenancy engine. Nothing in Core acts on the mark — JC.Tenancy's
/// <c>ApplyTenantFilters</c> installs the filters, and an application without tenancy simply
/// carries an unused column.
/// <para>
/// This marks a partition, not a relationship. No foreign key is configured, because the tenant
/// record may live in another context or another database entirely; an application whose model holds
/// both may configure one itself.
/// </para>
/// </remarks>
public interface IMultiTenancy
{
    /// <summary>Gets or sets the tenant identifier this entity belongs to.</summary>
    string? TenantId { get; set; }
}
