using JC.FileStorage.Models;
using Microsoft.Extensions.Configuration;

namespace JC.FileStorage.Services;

public class FilePathProvider
{
    private readonly FolderRegistry _folderRegistry;
    private readonly string _basePath;

    public FilePathProvider(IConfiguration config,
        FolderRegistry folderRegistry)
    {
        _folderRegistry = folderRegistry;
        _basePath = config["FileStorage:BasePath"]
            ?? throw new InvalidOperationException("FileStorage:BasePath is not set in configuration.");
    }
    
    public string GetPath(string folderName, string? tenantId)
        => GetPath(new FolderModel(folderName, tenantId));

    public string GetPath(FolderModel folder)
    {
        var result = _folderRegistry.TryGetFolders(folder.Tenant, out var folders);
        if (!result || folders == null)
            throw new ArgumentException($"Tenant '{folder.Tenant}' not found.", nameof(folder.Tenant));
        
        var fm = folders.FirstOrDefault(f => string.Equals(f.Name, folder.Name, StringComparison.OrdinalIgnoreCase));
        if (fm == null)
            throw new ArgumentException($"Folder '{folder.Name}' not found.", nameof(folder.Name));
        
        var path = Path.Combine(_basePath, fm.Tenant, fm.Name);
        EnsureFolderExists(path);
        return path;
    }

    public string GetFilePath(string path, string id, string ext)
    {
        if(string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(ext))
            throw new ArgumentException("Path, id and ext cannot be null or whitespace.");
        
        ext = !ext.StartsWith('.') ? $".{ext}" : ext;
        return Path.Combine(path, $"{id}{ext}");
    }
    
    //GetPath ensures folder exists when called
    public void EnsureFolderExists(string folderName, string? tenantId)
        => GetPath(folderName, tenantId);

    //GetPath ensures folder exists when called   
    public void EnsureFolderExists(FolderModel folder) 
        => GetPath(folder);
    
    public void EnsureFolderExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
    
    public bool CheckFileExists(string path)
        => File.Exists(path);
}