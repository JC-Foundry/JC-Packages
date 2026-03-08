using System.Text.Json;
using JC.Core.Data;
using JC.Core.Enums;
using JC.Core.Models;
using JC.Core.Models.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace JC.Core.Services;

/// <summary>
/// Internal service for creating audit trail entries from tracked entity changes.
/// Constructed directly by DbContext implementations during <c>SaveChangesAsync</c> —
/// not registered in DI.
/// </summary>
internal class AuditService
{
    private readonly IDataDbContext _context;
    private readonly string _userId;
    private readonly string _userName;

    internal AuditService(IDataDbContext context, IUserInfo? userInfo)
    {
        _context = context;
        _userId = userInfo?.UserId ?? IUserInfo.MissingUserInfoId;
        _userName = userInfo?.DisplayName ?? userInfo?.Username ?? IUserInfo.MissingUserInfoId;
    }

    /// <summary>
    /// Inspects the <see cref="ChangeTracker"/> for entity changes and creates
    /// corresponding <see cref="AuditEntry"/> records. Skips <see cref="AuditEntry"/>
    /// entities to prevent recursive auditing.
    /// </summary>
    /// <param name="changeTracker">The change tracker to inspect.</param>
    internal async Task ProcessChangesAsync(ChangeTracker changeTracker)
    {
        var entries = changeTracker.Entries()
            .Where(e => e.Entity is not AuditEntry
                        && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var action = ResolveAction(entry);
            if (action is null)
                continue;

            var tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
            var data = SerializeChanges(entry, action.Value);

            await LogAsync(action.Value, tableName, data);
        }
    }

    private async Task LogAsync(AuditAction action, string tableName, string? data)
    {
        var entry = new AuditEntry
        {
            Action = action,
            TableName = tableName,
            UserId = _userId,
            UserName = _userName,
            ActionData = data,
            AuditDate = DateTime.UtcNow
        };

        await _context.AuditEntries.AddAsync(entry);
    }

    private static AuditAction? ResolveAction(EntityEntry entry)
    {
        return entry.State switch
        {
            EntityState.Added => AuditAction.Create,
            EntityState.Deleted => AuditAction.Delete,
            EntityState.Modified => ResolveModifiedAction(entry),
            _ => null
        };
    }

    private static AuditAction ResolveModifiedAction(EntityEntry entry)
    {
        var isDeletedProp = entry.Properties
            .FirstOrDefault(p => p.Metadata.Name == nameof(AuditModel.IsDeleted));

        if (isDeletedProp is { IsModified: true })
        {
            var wasDeleted = isDeletedProp.OriginalValue is true;
            var isNowDeleted = isDeletedProp.CurrentValue is true;

            if (!wasDeleted && isNowDeleted)
                return AuditAction.SoftDelete;

            if (wasDeleted && !isNowDeleted)
                return AuditAction.Restore;
        }

        return AuditAction.Update;
    }

    private static string? SerializeChanges(EntityEntry entry, AuditAction action)
    {
        try
        {
            if (action == AuditAction.Create)
            {
                var created = entry.Properties
                    .Where(p => p.CurrentValue is not null)
                    .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                return created.Count > 0 ? JsonSerializer.Serialize(created) : null;
            }

            var changes = entry.Properties
                .Where(p => p.IsModified)
                .ToDictionary(p => p.Metadata.Name, p => new
                {
                    From = p.OriginalValue,
                    To = p.CurrentValue
                });
            return changes.Count > 0 ? JsonSerializer.Serialize(changes) : null;
        }
        catch
        {
            return null;
        }
    }
}