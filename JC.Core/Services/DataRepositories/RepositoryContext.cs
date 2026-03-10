using System.Linq.Expressions;
using JC.Core.Models;
using JC.Core.Models.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JC.Core.Services.DataRepositories;

/// <inheritdoc />
/// <summary>
/// Default repository context implementation. Auto-detects if <typeparamref name="T"/> extends
/// <see cref="AuditModel"/> for audit field population, and falls back to reflection-based
/// <c>IsDeleted</c> property detection for non-AuditModel entities.
/// </summary>
public class RepositoryContext<T> : IRepositoryContext<T>
    where T : class
{
    private readonly DbContext _context;
    private readonly IUserInfo? _userInfo;
    private readonly ILogger<RepositoryContext<T>> _logger;
    private readonly bool _isAuditModel;
    private readonly bool _isLogModel;
    private readonly bool _hasIsDeletedProperty;

    public RepositoryContext(DbContext context,
        IServiceProvider serviceProvider,
        ILogger<RepositoryContext<T>> logger)
    {
        _context = context;
        _logger = logger;

        var type = typeof(T);
        _isAuditModel = typeof(AuditModel).IsAssignableFrom(type);
        _isLogModel = typeof(LogModel).IsAssignableFrom(type);
        _hasIsDeletedProperty = _isAuditModel || type.GetProperty("IsDeleted") != null;

        _userInfo = serviceProvider.GetService<IUserInfo>();
        if(_userInfo == null)
            _logger.LogDebug("Unable to resolve service 'IUserInfo'. Using '{MissingId}' from IUserInfo for UserId.", 
                IUserInfo.MissingUserInfoId);
    }

    private string GetUserId(string? userId) => userId ?? _userInfo?.UserId ?? IUserInfo.MissingUserInfoId; 


    public IQueryable<T> AsQueryable()
        => _context.Set<T>().AsQueryable();

    public IQueryable<T> GetAll(Expression<Func<T, bool>> predicate)
        => _context.Set<T>().Where(predicate);

    public IQueryable<T> GetAll(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy)
        => orderBy(GetAll(predicate));

    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await GetAll(predicate).ToListAsync(cancellationToken);

    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy, CancellationToken cancellationToken = default)
        => await GetAll(predicate, orderBy).ToListAsync(cancellationToken);

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Set<T>().FindAsync([id], cancellationToken);

    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => await _context.Set<T>().FindAsync([id], cancellationToken);

    public async Task<T?> GetByIdAsync(params object[] id)
        => await _context.Set<T>().FindAsync(id);



    public async Task<T> AddAsync(T entity, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)
        => (await AddRangeAsync([entity], userId, saveNow, cancellationToken)).First();

    public async Task<List<T>> AddAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)
        => await AddRangeAsync(entities, userId, saveNow, cancellationToken);

    public async Task<List<T>> AddRangeAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)
    {
        var list = entities.ToList();

        try
        {
            if (_isAuditModel || _isLogModel)
            {
                foreach (var model in list.Select(e => e as BaseCreateModel))
                {
                    model?.FillCreated(GetUserId(userId));
                }
            }

            await _context.AddRangeAsync(list, cancellationToken);
            if (saveNow) await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entities to database of type {type}.", typeof(T));
            throw;
        }

        return list;
    }



    public async Task<T> UpdateAsync(T entity, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)
        => (await UpdateRangeAsync([entity], userId, saveNow, cancellationToken)).First();

    public async Task<List<T>> UpdateAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)
        => await UpdateRangeAsync(entities, userId, saveNow, cancellationToken);

    public async Task<List<T>> UpdateRangeAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)
    {
        var list = entities.ToList();

        try
        {
            if (_isAuditModel)
            {
                foreach (var audit in list.Select(e => e as AuditModel))
                {
                    audit?.FillModified(GetUserId(userId));
                }
            }

            _context.UpdateRange(list);
            if (saveNow) await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entities in database of type {type}.", typeof(T));
            throw;
        }

        return list;
    }



    public async Task<T> SoftDeleteAsync(T entity, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)
        => (await SoftDeleteRangeAsync([entity], userId, saveNow, cancellationToken)).First();

    public async Task<List<T>> SoftDeleteAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)
        => await SoftDeleteRangeAsync(entities, userId, saveNow, cancellationToken);

    public async Task<List<T>> SoftDeleteRangeAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)
    {
        var list = entities.ToList();

        try
        {
            if (_isAuditModel)
            {
                foreach (var audit in list.Select(e => e as AuditModel))
                {
                    audit?.FillDeleted(GetUserId(userId));
                }
            }
            else if (_hasIsDeletedProperty)
            {
                foreach (var entity in list)
                {
                    entity.GetType().GetProperty("IsDeleted")?.SetValue(entity, true);
                }
            }

            _context.UpdateRange(list);
            if (saveNow) await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entities in database of type {type} when soft deleting.", typeof(T));
            throw;
        }

        return list;
    }



    public async Task<T> RestoreAsync(T entity, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)
        => (await RestoreRangeAsync([entity], userId, saveNow, cancellationToken)).First();

    public async Task<List<T>> RestoreAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)
        => await RestoreRangeAsync(entities, userId, saveNow, cancellationToken);

    public async Task<List<T>> RestoreRangeAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true, CancellationToken cancellationToken = default)
    {
        var list = entities.ToList();

        try
        {
            if (_isAuditModel)
            {
                foreach (var audit in list.Select(e => e as AuditModel))
                {
                    audit?.FillRestored(GetUserId(userId));
                }
            }
            else if (_hasIsDeletedProperty)
            {
                foreach (var entity in list)
                {
                    entity.GetType().GetProperty("IsDeleted")?.SetValue(entity, false);
                }
            }

            _context.UpdateRange(list);
            if (saveNow) await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entities in database of type {type} when restoring.", typeof(T));
            throw;
        }

        return list;
    }



    public async Task<bool> DeleteAsync(T entity, bool saveNow = true, CancellationToken cancellationToken = default)
        => await DeleteRangeAsync([entity], saveNow, cancellationToken);

    public async Task<bool> DeleteAsync(IEnumerable<T> entities, bool saveNow = true, CancellationToken cancellationToken = default)
        => await DeleteRangeAsync(entities, saveNow, cancellationToken);

    public async Task<bool> DeleteRangeAsync(IEnumerable<T> entities, bool saveNow = true, CancellationToken cancellationToken = default)
    {
        var list = entities.ToList();

        try
        {
            _context.Set<T>().RemoveRange(list);
            if (saveNow) await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entities from database of type {type}.", typeof(T));
            throw;
        }
    }
}
