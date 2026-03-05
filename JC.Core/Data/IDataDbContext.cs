using JC.Core.Models;
using JC.Core.Models.Auditing;
using Microsoft.EntityFrameworkCore;

namespace JC.Core.Data;

/// <summary>
/// Contract for the application data context, exposing core entity sets and persistence.
/// </summary>
public interface IDataDbContext
{
    /// <summary>
    /// Gets the set of reported issues (bugs and suggestions).
    /// </summary>
    DbSet<ReportedIssue> ReportedIssues { get; }

    /// <summary>
    /// Gets the set of audit trail entries.
    /// </summary>
    DbSet<AuditEntry> AuditEntries { get; }

    /// <summary>
    /// Persists all pending changes to the database.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}