using JC.FileStorage.Helpers;
using JC.FileStorage.Models;
using Microsoft.Extensions.Logging;

namespace JC.FileStorage.Services;

public class StaticFileRegistry
{
    private readonly FilePathProvider _pathProvider;
    private readonly ILogger<StaticFileRegistry> _logger;
    private readonly Lock _lock = new();
    private readonly Dictionary<string, StaticFile> _staticFiles = new();

    public StaticFileRegistry(FilePathProvider pathProvider,
        ILogger<StaticFileRegistry> logger)
    {
        _pathProvider = pathProvider;
        _logger = logger;
    }

    internal void AutoDiscoverStaticFiles()
    {
        //Base path
        var basePath = _pathProvider.GetStaticPath();
        AutoDiscoverStaticFiles(basePath, []);
    }

    private void AutoDiscoverStaticFiles(string path, List<string> subFolders)
    {
        var files = Directory.GetFiles(path);
        foreach (var file in files)
            RegisterFile(file, subFolders);
        
        var dirs = Directory.GetDirectories(path);
        foreach (var dir in dirs)
        {
            subFolders.Add(Path.GetFileName(dir));
            AutoDiscoverStaticFiles(dir, subFolders);
            subFolders.RemoveAt(subFolders.Count - 1);
        }
    }

    private void RegisterFile(string filePath, List<string> subFolders)
    {
        if(!_pathProvider.CheckFileExists(filePath))
            return;
        
        //Guard in-case name is null - explicitly allows empty names (e.g. .gitignore)
        var name = Path.GetFileNameWithoutExtension(filePath);
        if(name == null!)
            return;
			
        //Guard in-case extension is null or empty
        var ext = Path.GetExtension(filePath);
        if(string.IsNullOrWhiteSpace(ext))
            return;
        
        var file = new StaticFile(NormalisationHelper.GetFileName(name, ext), subFolders);
        var result = TryAddStaticFile(file);
        if(result) return;

        throw new InvalidOperationException(
            $"Static file '{filePath}' clashes with a file already registered under key '{file.Key}'. " +
            "Static file names are compared case-insensitively, including their subfolders.");
    }
    
    
    public bool TryAddStaticFile(string fileName, params IEnumerable<string> subFolders)
        => TryAddStaticFile(new StaticFile(fileName, subFolders));
    
    public bool TryAddStaticFile(StaticFile file)
    {
        try
        {
            lock (_lock)
            {
                var result = _staticFiles.TryGetValue(file.Key, out var cachedFile);
                if (!result || cachedFile == null)
                    return _staticFiles.TryAdd(file.Key, file);
            
                //Returns false if the file already exists
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to add static file to registry, using key {Key} on file {FileName}", file.Key, file.FileName);
            return false;
        }
    }
    
    public bool TryGetStaticFile(string fileName, out StaticFile? file, params IEnumerable<string> subFolders)
    {
        file = null;
        try
        {
            var mockFile = new StaticFile(fileName, subFolders);
        
            lock (_lock)
            {
                var result = _staticFiles.TryGetValue(mockFile.Key, out var f);
                if(!result || f == null)
                    return false;

                file = f;
                return true;
            }
        }
        catch (ArgumentException ex)
        {
            //A name with no extension, or one the model will not accept. A lookup that cannot match
            //is an ordinary miss, not a fault - the caller gets false either way
            _logger.LogDebug(ex, "Static file lookup could not be resolved to a file name, for {FileName}", fileName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to get static file from registry, for file {FileName}", fileName);
            return false;
        }
    }
}