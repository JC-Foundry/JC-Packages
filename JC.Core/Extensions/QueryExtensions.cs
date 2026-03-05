using JC.Core.Enums;
using JC.Core.Models.Auditing;

namespace JC.Core.Extensions;

/// <summary>
/// Query extension methods for filtering entities by soft-delete status.
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Filters an <see cref="AuditModel"/> queryable by soft-delete status.
    /// </summary>
    /// <typeparam name="T">The entity type, which must extend <see cref="AuditModel"/>.</typeparam>
    /// <param name="query">The source queryable to filter.</param>
    /// <param name="deletedQueryType">The deletion filter to apply.</param>
    /// <returns>The filtered queryable.</returns>
    public static IQueryable<T> FilterDeleted<T>(this IQueryable<T> query, DeletedQueryType deletedQueryType)
        where T : AuditModel
        => query.Where(x => deletedQueryType == DeletedQueryType.All 
                            || x.IsDeleted == (deletedQueryType == DeletedQueryType.OnlyDeleted));
}