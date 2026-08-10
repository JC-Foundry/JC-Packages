namespace JC.Tenancy.Data;

/// <summary>
/// Marks a <see cref="Microsoft.EntityFrameworkCore.DbContext"/> that participates in tenant
/// filtering, by exposing the tenant its queries are currently scoped to.
/// </summary>
/// <remarks>
/// Implement this by delegating to the scoped <see cref="Models.ITenantInfo"/>:
/// <code>
/// public string? CurrentTenantId => _tenantInfo.TenantId;
/// </code>
/// <para>
/// The filters bind to this property rather than closing over <c>ITenantInfo</c> directly, and that
/// is not incidental. EF Core caches the compiled model per context type, but makes a specific
/// allowance for a captured <c>DbContext</c> instance in a query filter, re-reading its members
/// against the active context on every query. No such allowance exists for an arbitrary service, so
/// a filter that captured the scoped <c>ITenantInfo</c> would bake whichever tenant happened to
/// warm the model into every later request.
/// </para>
/// </remarks>
public interface ITenantScopedContext
{
    /// <summary>
    /// Gets the tenant the context's queries are currently scoped to, or <c>null</c> for the null
    /// partition.
    /// </summary>
    string? CurrentTenantId { get; }
}
