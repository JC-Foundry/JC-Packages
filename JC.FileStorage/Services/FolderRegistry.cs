using System.Collections.Concurrent;
using JC.FileStorage.Models;

namespace JC.FileStorage.Services;

public class FolderRegistry
{
    private readonly ConcurrentDictionary<string, FolderModel> _folders = new();

    public bool TryAddFolder(FolderModel folder)
        //Cannot add folder with same name (case insensitive)
        => !_folders.Any(f => 
            string.Equals(f.Key, folder.Name, StringComparison.OrdinalIgnoreCase)) 
           && _folders.TryAdd(folder.Name, folder);

    public bool TryGetFolder(string name, string? tenantId, out FolderModel? folder)
    {
        folder = null;
        var result = _folders.TryGetValue(name, out var f);
        if(!result || f == null)
            return false;

        if ((string.IsNullOrEmpty(tenantId)
             && !string.Equals(f.Tenant, FolderModel.NullTenantName, StringComparison.OrdinalIgnoreCase))
            || !string.Equals(f.Tenant, tenantId))
            return false;
        
        folder = f;
        return true;
    }
    
    public IReadOnlyList<FolderModel> GetFolders(string? tenantId = null)
        => _folders.Values.Where(f => string.IsNullOrEmpty(tenantId) 
                ? string.Equals(f.Tenant, FolderModel.NullTenantName, StringComparison.OrdinalIgnoreCase) 
                : string.Equals(f.Tenant, tenantId))
            .ToList();
    
    public IReadOnlyList<string> GetFolderNames(string? tenantId = null)
        => GetFolders(tenantId)
            .Select(f => f.Name)
            .ToList();
}