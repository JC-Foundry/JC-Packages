using System.Text.Json;
using JC.Core.Data;
using JC.Core.Models;
using JC.Core.Models.Auditing;

namespace JC.Core.Services;

/// <summary>
/// Service for recording audit trail entries against the current user.
/// </summary>
public class AuditService
{
    private readonly IDataDbContext _context;
    private readonly IUserInfo _userInfo;

    /// <summary>
    /// Initialises a new instance of <see cref="AuditService"/>.
    /// </summary>
    /// <param name="context">The data context for persisting audit entries.</param>
    /// <param name="userInfo">The current user information.</param>
    public AuditService(IDataDbContext context, IUserInfo userInfo)
    {
        _context = context;
        _userInfo = userInfo;
    }

    /// <summary>
    /// Creates and persists an audit entry for the specified action.
    /// </summary>
    /// <param name="action">The type of action being audited.</param>
    /// <param name="tableName">The name of the affected database table.</param>
    /// <param name="data">Optional entity data to serialise as JSON in the audit record.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LogAsync(AuditAction action, string tableName, object? data = null)
    {
        var entry = new AuditEntry
        {
            Action = action,
            TableName = tableName,
            UserId = _userInfo.UserId,
            UserName = _userInfo.DisplayName ?? _userInfo.Username,
            ActionData = data != null ? JsonSerializer.Serialize(data) : null,
            AuditDate = DateTime.UtcNow
        };

        await _context.AuditEntries.AddAsync(entry);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Logs a <see cref="AuditAction.Create"/> audit entry.
    /// </summary>
    /// <param name="tableName">The name of the affected database table.</param>
    /// <param name="data">Optional entity data to serialise as JSON.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LogCreateAsync(string tableName, object? data = null)
        => await LogAsync(AuditAction.Create, tableName, data);

    /// <summary>
    /// Logs a <see cref="AuditAction.Update"/> audit entry.
    /// </summary>
    /// <param name="tableName">The name of the affected database table.</param>
    /// <param name="data">Optional entity data to serialise as JSON.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LogUpdateAsync(string tableName, object? data = null)
        => await LogAsync(AuditAction.Update, tableName, data);

    /// <summary>
    /// Logs a <see cref="AuditAction.Delete"/> audit entry.
    /// </summary>
    /// <param name="tableName">The name of the affected database table.</param>
    /// <param name="data">Optional entity data to serialise as JSON.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LogDeleteAsync(string tableName, object? data = null)
        => await LogAsync(AuditAction.Delete, tableName, data);

    /// <summary>
    /// Logs a <see cref="AuditAction.Restore"/> audit entry.
    /// </summary>
    /// <param name="tableName">The name of the affected database table.</param>
    /// <param name="data">Optional entity data to serialise as JSON.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LogRestoreAsync(string tableName, object? data = null)
        => await LogAsync(AuditAction.Restore, tableName, data);
}