using System.Linq.Expressions;
using JC.Core.Enums;
using JC.Core.Models;
using JC.Identity.Authentication;
using JC.Identity.Models;
using JC.Identity.Models.MultiTenancy;
using Microsoft.EntityFrameworkCore;

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
    /// Applies global tenant query filters to all entities implementing IMultiTenancy.
    /// If tenantId is null/empty, filters to entities where TenantId is null.
    /// Otherwise, filters to entities where TenantId matches the provided value.
    /// </summary>
    public static ModelBuilder ApplyTenantQueryFilters(this ModelBuilder modelBuilder, IUserInfo userInfo)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IMultiTenancy).IsAssignableFrom(entityType.ClrType))
                continue;

            var filter = BuildTenantFilter(entityType.ClrType, userInfo);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }

        return modelBuilder;
    }

    private static LambdaExpression BuildTenantFilter(Type entityType, IUserInfo userInfo)
    {
        // Build: e => string.IsNullOrEmpty(userInfo.TenantId) ? e.TenantId == null : e.TenantId == userInfo.TenantId
        var parameter = Expression.Parameter(entityType, "e");
        var tenantIdProperty = Expression.Property(parameter, nameof(IMultiTenancy.TenantId));

        // Access userInfo.TenantId
        var userInfoConstant = Expression.Constant(userInfo);
        var currentTenantId = Expression.Property(userInfoConstant, nameof(IUserInfo.TenantId));

        // string.IsNullOrEmpty(userInfo.TenantId)
        var isNullOrEmpty = Expression.Call(
            typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!,
            currentTenantId);

        // e.TenantId == null
        var tenantIsNull = Expression.Equal(tenantIdProperty, Expression.Constant(null, typeof(string)));

        // e.TenantId == userInfo.TenantId
        var tenantEquals = Expression.Equal(tenantIdProperty, currentTenantId);

        // Conditional: IsNullOrEmpty ? TenantIsNull : TenantEquals
        var condition = Expression.Condition(isNullOrEmpty, tenantIsNull, tenantEquals);

        return Expression.Lambda(condition, parameter);
    }
}