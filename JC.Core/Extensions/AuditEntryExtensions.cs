using JC.Core.Enums;
using JC.Core.Models.Auditing;
using JC.Core.Services.DataRepositories;
using Microsoft.EntityFrameworkCore;

namespace JC.Core.Extensions;

public static class AuditEntryExtensions
{
    public record AuditEntryTrailSearch(bool KeyIsUserId, string SearchKey);
    
    public static IQueryable<AuditEntry> QueryAuditEntries(this IRepositoryManager repos,
        AuditEntryTrailSearch trailSearch, string? search, AuditAction? action, string? appName)
    {
        var query = repos.GetRepository<AuditEntry>()
            .AsQueryable().AsNoTracking();

        if (trailSearch.KeyIsUserId)
        {
            query = query.Where(a => (a.IsActionIdPreferred && a.ActionUserId == trailSearch.SearchKey) 
                                     || (!a.IsActionIdPreferred && a.UserId == trailSearch.SearchKey));
        }
        else
        {
            query = query.Where(a => a.TableName == trailSearch.SearchKey);
        }

        if (!string.IsNullOrWhiteSpace(appName))
            query = query.Where(a => a.SourceApplication == appName);

        if(action.HasValue)
            query = query.Where(a => a.Action == action.Value);
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = trailSearch.KeyIsUserId
                ? query.Where(a => (!string.IsNullOrEmpty(a.TableName) && a.TableName.ToLower().Contains(search))
                                   || (!string.IsNullOrEmpty(a.EntityKey) && a.EntityKey.ToLower().Contains(search)))
                
                : query.Where(a => (!string.IsNullOrEmpty(a.UserId) && !a.IsActionIdPreferred && a.UserId.ToLower().Contains(search))
                                   || (!string.IsNullOrEmpty(a.ActionUserId) && a.IsActionIdPreferred && a.ActionUserId.ToLower().Contains(search))
                                   || (!string.IsNullOrEmpty(a.UserName) && a.UserName.ToLower().Contains(search))
                                   || (!string.IsNullOrEmpty(a.EntityKey) && a.EntityKey.ToLower().Contains(search)));
        }

        return query;
    }
}