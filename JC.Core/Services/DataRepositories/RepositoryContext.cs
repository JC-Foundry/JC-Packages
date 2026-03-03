using System.Linq.Expressions;
using JC.Core.Models;
using JC.Core.Models.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JC.Core.Services.DataRepositories;

public class RepositoryContext<T> : IRepositoryContext<T>
    where T : class
{
    private readonly DbContext _context;
    private readonly IUserInfo _userInfo;
    private readonly ILogger<RepositoryContext<T>> _logger;
    private readonly bool _isAuditModel;
    private readonly bool _hasIsDeletedProperty;

    public RepositoryContext(DbContext context, 
        IUserInfo userInfo,
        ILogger<RepositoryContext<T>> logger)
    {
        _context = context;
        _userInfo = userInfo;
        _logger = logger;

        var type = typeof(T);
        _isAuditModel = typeof(AuditModel).IsAssignableFrom(type);
        _hasIsDeletedProperty = _isAuditModel || type.GetProperty("IsDeleted") != null;
    }

    private string GetUserId(string? userId) => userId ?? _userInfo.UserId;
    
    
    public IQueryable<T> AsQueryable()
        => _context.Set<T>().AsQueryable();

    public IQueryable<T> GetAll(Expression<Func<T, bool>> predicate)
        => _context.Set<T>().Where(predicate);

    public IQueryable<T> GetAll(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy)
        => orderBy(GetAll(predicate));

    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
        => await GetAll(predicate).ToListAsync();

    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy)
        => await GetAll(predicate, orderBy).ToListAsync();

    public async Task<T?> GetByIdAsync(int id)
        => await _context.Set<T>().FindAsync(id);

    public async Task<T?> GetByIdAsync(string id)
        => await _context.Set<T>().FindAsync(id);

    public async Task<T?> GetByIdAsync(params object[] id)
        => await _context.Set<T>().FindAsync(id);



    public async Task<T> AddAsync(T entity, string? userId = null, bool saveNow = true)
        => (await AddRangeAsync([entity], userId, saveNow)).First();

    public async Task<List<T>> AddAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true)
        => await AddRangeAsync(entities, userId, saveNow);

    public async Task<List<T>> AddRangeAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true)
    {
        var list = entities.ToList();

        try
        {
            if (_isAuditModel)
            {
                foreach (var audit in list.Select(e => e as AuditModel))
                {
                    audit?.FillCreated(GetUserId(userId));
                }
            }
            
            await _context.AddRangeAsync(list);
            if (saveNow) await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entities to database of type {type}.", typeof(T));
            throw;
        }
        
        return list;
    }



    public async Task<T> UpdateAsync(T entity, string? userId = null, bool saveNow = true)
        => (await UpdateRangeAsync([entity], userId, saveNow)).First();

    public async Task<List<T>> UpdateAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true)
        => await UpdateRangeAsync(entities, userId, saveNow);

    public async Task<List<T>> UpdateRangeAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true)
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
            if (saveNow) await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entities in database of type {type}.", typeof(T));
            throw;
        }
        
        return list;
    }



    public async Task<T> SoftDeleteAsync(T entity, string? userId = null, bool saveNow = true)
        => (await SoftDeleteRangeAsync([entity], userId, saveNow)).First();

    public async Task<List<T>> SoftDeleteAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true)
        => await SoftDeleteRangeAsync(entities, userId, saveNow);
    
    public async Task<List<T>> SoftDeleteRangeAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true)
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
            if (saveNow) await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entities in database of type {type} when soft deleting.", typeof(T));
            throw;
        }
        
        return list;
    }

    
    
    public async Task<T> RestoreAsync(T entity, string? userId = null, bool saveNow = true)
        => (await RestoreRangeAsync([entity], userId, saveNow)).First();

    public async Task<List<T>> RestoreAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true)
        => await RestoreRangeAsync(entities, userId, saveNow);

    public async Task<List<T>> RestoreRangeAsync(IEnumerable<T> entities, string? userId = null, bool saveNow = true)
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
            if (saveNow) await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entities in database of type {type} when restoring.", typeof(T));
            throw;
        }
        
        return list;
    }

    
    
    public async Task<bool> DeleteAsync(T entity, bool saveNow = true)
        => await DeleteRangeAsync([entity], saveNow);

    public async Task<bool> DeleteAsync(IEnumerable<T> entities, bool saveNow = true)
        => await DeleteRangeAsync(entities, saveNow);

    public async Task<bool> DeleteRangeAsync(IEnumerable<T> entities, bool saveNow = true)
    {
        var list = entities.ToList();
        
        try
        {
            _context.Set<T>().RemoveRange(list);
            if (saveNow) await _context.SaveChangesAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entities from database of type {type}.", typeof(T));
            throw;
        }
    }
}