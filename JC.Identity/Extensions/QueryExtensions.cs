using System.Linq.Expressions;
using JC.Core.Models;
using JC.Identity.Authentication;
using Microsoft.EntityFrameworkCore;
using JC.Identity.Models.MultiTenancy;

namespace JC.Identity.Extensions;

/// <summary>
/// Query extension methods for multi-tenancy filtering.
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Bypasses tenant query filters for <see cref="SystemRoles.SystemAdmin"/> users,
    /// allowing them to query across all tenants. Non-admin users receive the unmodified query.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="userInfo">The current user information, used to check the SystemAdmin role.</param>
    /// <returns>The queryable, optionally with query filters ignored.</returns>
    public static IQueryable<T> AllTenants<T>(this IQueryable<T> query, IUserInfo userInfo)
        where T : class
        => userInfo.IsInRole(SystemRoles.SystemAdmin) ? query.IgnoreQueryFilters() : query; 
    
    
    /// <summary>
    /// Applies global tenant query filters to all entities implementing <see cref="IMultiTenancy"/>.
    /// The filter references <c>CurrentTenantId</c> on the <paramref name="context"/> instance.
    /// EF Core re-evaluates DbContext member access per query, ensuring the filter always uses the
    /// current request's tenant rather than a value cached at model creation time.
    /// </summary>
    /// <param name="modelBuilder">The model builder to apply filters to.</param>
    /// <param name="context">The DbContext instance whose <c>CurrentTenantId</c> property will be referenced in the filter expression.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ApplyTenantQueryFilters(this ModelBuilder modelBuilder, DbContext context)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IMultiTenancy).IsAssignableFrom(entityType.ClrType))
                continue;

            var filter = BuildTenantFilter(entityType.ClrType, context);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }

        return modelBuilder;
    }

    private static LambdaExpression BuildTenantFilter(Type entityType, DbContext context)
    {
        // Build: e => string.IsNullOrEmpty(context.CurrentTenantId)
        //            ? e.TenantId == null
        //            : e.TenantId == context.CurrentTenantId
        var parameter = Expression.Parameter(entityType, "e");
        var tenantIdProperty = Expression.Property(parameter, nameof(IMultiTenancy.TenantId));

        // Access context.CurrentTenantId — EF Core re-evaluates this per query
        var contextConstant = Expression.Constant(context);
        var currentTenantId = Expression.Property(contextConstant, "CurrentTenantId");

        // string.IsNullOrEmpty(context.CurrentTenantId)
        var isNullOrEmpty = Expression.Call(
            typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!,
            currentTenantId);

        // e.TenantId == null
        var tenantIsNull = Expression.Equal(tenantIdProperty, Expression.Constant(null, typeof(string)));

        // e.TenantId == context.CurrentTenantId
        var tenantEquals = Expression.Equal(tenantIdProperty, currentTenantId);

        // Conditional: IsNullOrEmpty ? TenantIsNull : TenantEquals
        var condition = Expression.Condition(isNullOrEmpty, tenantIsNull, tenantEquals);

        return Expression.Lambda(condition, parameter);
    }
}