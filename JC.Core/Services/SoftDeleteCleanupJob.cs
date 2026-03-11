using System.Linq.Expressions;
using System.Reflection;
using JC.Core.Models;
using JC.Core.Models.Auditing;
using JC.Core.Models.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Core.Services;

public class SoftDeleteCleanupJob : IBackgroundJob
{
    private readonly DbContext _context;
    private readonly CoreBackgroundJobOptions _options;
    private readonly ILogger<SoftDeleteCleanupJob> _logger;

    private static readonly MethodInfo CleanupMethod =
        typeof(SoftDeleteCleanupJob).GetMethod(nameof(CleanupEntitiesAsync),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    public SoftDeleteCleanupJob(DbContext context,
        IOptions<CoreBackgroundJobOptions> options,
        ILogger<SoftDeleteCleanupJob> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if(!_options.RegisterSoftDeleteCleanupJob)
            return;
        
        var softDeletableTypes = _context.Model.GetEntityTypes()
            .Where(t =>
            {
                var clrType = t.ClrType;

                // AuditModel inheritors have IsDeleted built in
                if (typeof(AuditModel).IsAssignableFrom(clrType))
                    return true;

                // Non-AuditModel entities that have their own IsDeleted property
                var prop = clrType.GetProperty("IsDeleted");
                return prop != null && prop.PropertyType == typeof(bool);
            })
            .ToList();

        foreach (var entityType in softDeletableTypes
                     .Where(entityType => !_options.SoftDeleteRetentionBlacklist.Contains(entityType.ClrType.Name.ToLowerInvariant())))
        {
            try
            {
                var generic = CleanupMethod.MakeGenericMethod(entityType.ClrType);
                var task = (Task)generic.Invoke(this, [cancellationToken])!;
                await task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup soft-deleted entities for {EntityType}.",
                    entityType.ClrType.Name);
            }
        }
    }

    private async Task CleanupEntitiesAsync<T>(CancellationToken cancellationToken) where T : class
    {
        var cutoff = DateTime.UtcNow.AddMonths(-(_options.SoftDeleteRetentionMonths));
        var isAuditModel = typeof(AuditModel).IsAssignableFrom(typeof(T));

        List<T> toDelete;

        if (isAuditModel)
        {
            toDelete = await _context.Set<T>()
                .Cast<AuditModel>()
                .Where(e => e.IsDeleted && e.DeletedUtc.HasValue && e.DeletedUtc.Value < cutoff)
                .Cast<T>()
                .ToListAsync(cancellationToken);
        }
        else
        {
            // Build expression tree so EF can translate IsDeleted filter to SQL
            var parameter = Expression.Parameter(typeof(T), "e");
            var property = Expression.Property(parameter, "IsDeleted");
            var lambda = Expression.Lambda<Func<T, bool>>(property, parameter);

            toDelete = await _context.Set<T>().Where(lambda).ToListAsync(cancellationToken);
        }

        if (toDelete.Count == 0) return;

        _context.Set<T>().RemoveRange(toDelete);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cleaned up {Count} soft-deleted {EntityType} entities.",
            toDelete.Count, typeof(T).Name);
    }
}