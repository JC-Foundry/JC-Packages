using JC.Tenancy.Services;
using Microsoft.EntityFrameworkCore;

namespace JC.Tenancy.Extensions;

/// <summary>
/// Query extension methods for reading across tenant boundaries.
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Removes tenant filtering from a query, if the current caller is permitted to read across
    /// tenants.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="authoriser">The authoriser deciding whether the bypass is allowed.</param>
    /// <returns>
    /// The query with filters ignored where the bypass is permitted; otherwise the query unchanged.
    /// </returns>
    /// <remarks>
    /// Silently returning the filtered query when permission is refused is deliberate: a caller
    /// without the right to see other tenants still gets a working query over their own data,
    /// rather than an exception in the middle of a page they were entitled to load.
    /// <para>
    /// The authoriser is passed rather than resolved because this is an <see cref="IQueryable{T}"/>
    /// extension with no service provider to hand. Inject <see cref="ITenantBypassAuthoriser"/>
    /// where the query is built.
    /// </para>
    /// </remarks>
    public static IQueryable<T> AllTenants<T>(this IQueryable<T> query, ITenantBypassAuthoriser authoriser)
        where T : class
        => authoriser.CanAccessAllTenants() ? query.IgnoreQueryFilters() : query;

    /// <summary>
    /// Removes tenant filtering from a query without any permission check.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <returns>The query with all filters ignored.</returns>
    /// <remarks>
    /// For trusted callers with no user to authorise: reconciliation jobs, maintenance tooling,
    /// migrations, and infrastructure that legitimately spans every tenant.
    /// <para>
    /// <c>IgnoreQueryFilters</c> is all-or-nothing, so any global filter a consuming application has
    /// added to the entity goes with the tenant one. Soft-delete is unaffected — it is applied by
    /// <c>FilterDeleted</c>, not as a global filter.
    /// </para>
    /// </remarks>
    public static IQueryable<T> AllTenantsUnsafe<T>(this IQueryable<T> query)
        where T : class
        => query.IgnoreQueryFilters();
}
