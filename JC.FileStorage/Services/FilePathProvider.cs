using JC.FileStorage.Models;
using Microsoft.Extensions.Configuration;

namespace JC.FileStorage.Services;

public class FilePathProvider
{
    private readonly FolderRegistry _folderRegistry;
    private readonly string _basePath;
    private readonly string? _staticPath;
    private readonly string[] _blockedFolderChars = ["..", "/", "\\", "*", "?", "\"", "<", ">", "|"];

    public FilePathProvider(IConfiguration config,
        FolderRegistry folderRegistry)
    {
        _folderRegistry = folderRegistry;
        _basePath = config["FileStorage:BasePath"]
            ?? throw new InvalidOperationException("FileStorage:BasePath is not set in configuration.");
        
        _staticPath = config["FileStorage:StaticPath"];
    }

    /// <summary>
    /// Retrieves the full path for the specified folder and tenant.
    /// </summary>
    /// <param name="folderName">The name of the folder whose path is to be retrieved.</param>
    /// <param name="tenantId">The identifier of the tenant. If null, the default tenant is used.</param>
    /// <returns>The full path to the given folder for the specified tenant.</returns>
    public string GetPath(string folderName, string? tenantId)
        => GetPath(new FolderModel(folderName, tenantId));

    /// <summary>
    /// Retrieves the full path for the specified folder and tenant.
    /// </summary>
    /// <param name="folder">The folder model whose path is to be retrieved.</param>
    /// <returns>The full path to the specified folder for the given tenant.</returns>
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

    /// <summary>
    /// Retrieves the static path combined with the specified subfolders, if any.
    /// </summary>
    /// <param name="subFolders">The subfolders to be appended to the static path. Invalid subfolder names are ignored.</param>
    /// <returns>The full static path with the specified subfolders appended, or the static path if no valid subfolders are provided.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the static path is not set in the configuration.</exception>
    public string GetStaticPath(params IEnumerable<string> subFolders)
    {
        if (string.IsNullOrWhiteSpace(_staticPath))
            throw new InvalidOperationException("Static path is not set in configuration.");
        
        var subFolderList = subFolders
            .Where(f => !string.IsNullOrWhiteSpace(f) 
                        && !f.StartsWith("..") 
                        && !f.StartsWith('/')
                        && !f.StartsWith('\\') 
                        && ValidSubFolder(f))
            .ToList();
        
        var path = subFolderList.Count == 0 
            ? _staticPath 
            : subFolderList.Aggregate(_staticPath, Path.Combine);
        EnsureFolderExists(path);
        return path;
    }
    
    private bool ValidSubFolder(string subFolder)
        => !_blockedFolderChars.Any(subFolder.Contains);


    /// <summary>
    /// Constructs the full file path by combining the provided path, file identifier, and file extension.
    /// </summary>
    /// <param name="path">The base directory path where the file is located or will be stored.</param>
    /// <param name="id">The unique identifier of the file.</param>
    /// <param name="ext">The extension of the file, which should include a preceding '.' character (e.g., ".txt").</param>
    /// <returns>The full file path including the directory, file identifier, and extension.</returns>
    /// <exception cref="ArgumentException">Thrown when any of the parameters (path, id, or ext) are null, empty, or consist solely of whitespace.</exception>
    public string GetFilePath(string path, string id, string ext)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(ext))
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

    /// <summary>
    /// Ensures that the specified folder path exists by creating it if it does not already exist.
    /// </summary>
    /// <param name="path">The full path of the folder to be verified or created.</param>
    public void EnsureFolderExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    /// <summary>
    /// Checks whether a file exists at the specified path.
    /// </summary>
    /// <param name="path">The full path of the file to check for existence.</param>
    /// <returns>True if the file exists at the specified path; otherwise, false.</returns>
    public bool CheckFileExists(string path)
        => File.Exists(path);
}