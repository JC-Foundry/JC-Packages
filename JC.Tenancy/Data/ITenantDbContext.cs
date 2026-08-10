using JC.Tenancy.Models;
using Microsoft.EntityFrameworkCore;

namespace JC.Tenancy.Data;

/// <summary>
/// Marks the one <see cref="DbContext"/> that owns authoritative tenant storage.
/// </summary>
/// <remarks>
/// Many contexts may be tenant <i>filtered</i> — that only requires
/// <see cref="ITenantScopedContext"/> — but exactly one owns the table the tenants themselves live
/// in. Which one is a deployment decision: the identity context and the main application context
/// are both reasonable homes, and the tenancy engine does not care as long as there is only ever
/// one for a given tenancy domain.
/// </remarks>
public interface ITenantDbContext
{
    /// <summary>Gets the set of tenants.</summary>
    DbSet<Tenant> Tenants { get; set; }
}
