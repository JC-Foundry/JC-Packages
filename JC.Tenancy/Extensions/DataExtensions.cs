using System.Linq.Expressions;
using JC.Core.Models.MultiTenancy;
using JC.Tenancy.Data;
using JC.Tenancy.Data.DataMappings;
using JC.Tenancy.Models.Options;
using Microsoft.EntityFrameworkCore;

namespace JC.Tenancy.Extensions;

/// <summary>
/// Extension methods for <see cref="ModelBuilder"/> providing JC.Tenancy entity configuration and
/// automatic tenant filtering.
/// </summary>
public static class DataExtensions
{
    private static readonly System.Reflection.PropertyInfo TenantIdProperty =
        typeof(ITenantScopedContext).GetProperty(nameof(ITenantScopedContext.CurrentTenantId))!;

    /// <summary>
    /// Applies the tenant entity mapping. Call this from <c>OnModelCreating</c> in the one context
    /// that owns tenant storage.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ApplyTenancyMappings(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantMap());

        return modelBuilder;
    }

    /// <summary>
    /// Installs global query filters scoping every <see cref="IMultiTenancy"/> entity in the model
    /// to the context's current tenant. Call this from <c>OnModelCreating</c>, last.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <param name="context">The context being built. Must implement <see cref="ITenantScopedContext"/>.</param>
    /// <param name="options">
    /// Optional configuration. Supply the same instance registered with <c>AddTenancy</c> to honour
    /// its exclusions; omit it to filter every tenant-scoped entity.
    /// </param>
    /// <returns>The model builder for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the model contains <see cref="IMultiTenancy"/> entities but
    /// <paramref name="context"/> does not implement <see cref="ITenantScopedContext"/>. Without a
    /// current tenant there is nothing to filter by, and continuing would return every tenant's rows
    /// from a context that plainly expects to be scoped.
    /// </exception>
    /// <remarks>
    /// A no-op where the model holds no tenant-scoped entities, so it is safe to call from a context
    /// that may or may not end up with any.
    /// <para>
    /// Filters match null to null, so the null tenant partition behaves as a partition rather than
    /// as "no filtering".
    /// </para>
    /// </remarks>
    public static ModelBuilder ApplyTenantFilters(this ModelBuilder modelBuilder, DbContext context,
        TenantOptions? options = null)
    {
        var excluded = options?.ExcludedEntityTypes ?? [];

        var tenantEntities = modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(IMultiTenancy).IsAssignableFrom(e.ClrType))
            .Where(e => !excluded.Contains(e.ClrType))
            .ToList();

        if (tenantEntities.Count == 0) return modelBuilder;

        if (context is not ITenantScopedContext)
            throw new InvalidOperationException(
                $"'{context.GetType().Name}' contains tenant-scoped entities " +
                $"({string.Join(", ", tenantEntities.Select(e => e.ClrType.Name))}) but does not implement " +
                $"'{nameof(ITenantScopedContext)}', so there is no current tenant to filter them by. " +
                $"Implement the interface, or exclude those types through {nameof(TenantOptions)}.");

        foreach (var entityType in tenantEntities)
            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(BuildTenantFilter(entityType.ClrType, context));

        return modelBuilder;
    }

    private static LambdaExpression BuildTenantFilter(Type entityType, DbContext context)
    {
        // Build: e => string.IsNullOrEmpty(context.CurrentTenantId)
        //            ? e.TenantId == null
        //            : e.TenantId == context.CurrentTenantId
        var parameter = Expression.Parameter(entityType, "e");
        var tenantIdProperty = Expression.Property(parameter, nameof(IMultiTenancy.TenantId));

        // The constant stays typed as the concrete DbContext: EF Core re-reads members of a captured
        // DbContext against the active instance on every query, and that is what keeps the filter
        // correct once the model is cached. It is also why the tenant has to arrive through the
        // context rather than through the scoped ITenantInfo directly — no equivalent allowance
        // exists for an arbitrary service, so capturing one would bake the first scope's tenant into
        // every later request.
        //
        // Only the property lookup changes: resolved from the interface rather than by name, so a
        // context that spells it differently fails to compile instead of silently filtering nothing.
        var contextConstant = Expression.Constant(context);
        var currentTenantId = Expression.Property(contextConstant, TenantIdProperty);

        var isNullOrEmpty = Expression.Call(
            typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!,
            currentTenantId);

        var tenantIsNull = Expression.Equal(tenantIdProperty, Expression.Constant(null, typeof(string)));
        var tenantEquals = Expression.Equal(tenantIdProperty, currentTenantId);

        var condition = Expression.Condition(isNullOrEmpty, tenantIsNull, tenantEquals);

        return Expression.Lambda(condition, parameter);
    }
}
