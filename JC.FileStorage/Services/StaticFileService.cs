using JC.FileStorage.Models;
using Microsoft.Extensions.Logging;

namespace JC.FileStorage.Services;

public class StaticFileService
{
    private readonly FilePathProvider _pathProvider;
    private readonly ILogger<StaticFileService> _logger;
    private readonly StaticFileRegistry _registry;

    public StaticFileService(FilePathProvider pathProvider,
        ILogger<StaticFileService> logger,
        StaticFileRegistry registry)
    {
        _pathProvider = pathProvider;
        _logger = logger;
        _registry = registry;
    }
    
    
    public async Task<GetStaticFileByteResponse> GetStaticFileBytes(string fileName, 
        CancellationToken ct = default, params IEnumerable<string> subfolders)
    {
        var result = _registry.TryGetStaticFile(fileName, out var file, subfolders);
        if(!result || file == null)
            return new GetStaticFileByteResponse("Static file not found");
        
        return await GetStaticFileBytes(file, ct);
    }
    
    /// <summary>
    /// Reads an already-resolved file. Internal so <see cref="StaticFileCache"/> can hand over the
    /// file the registry gave it, rather than paying for the same lookup twice.
    /// </summary>
    internal async Task<GetStaticFileByteResponse> GetStaticFileBytes(StaticFile file, CancellationToken ct = default)
    {
        var path = _pathProvider.GetStaticPath(file.SubFolders);
        var filePath = Path.Combine(path, file.FileName);
        
        if(!_pathProvider.CheckFileExists(filePath))
            return new GetStaticFileByteResponse("Static file not found");
        
        try
        {
            var bytes = await File.ReadAllBytesAsync(filePath, ct);
            return new GetStaticFileByteResponse(file, bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading static file {FileName} in path {Path}", file.FileName, path);
            return new GetStaticFileByteResponse("Error reading file.");
        }
    }

    
    public async Task<GetStaticFileTextResponse> GetStaticFileText(string fileName, 
        CancellationToken ct = default, params IEnumerable<string> subfolders)
    {
        var result = _registry.TryGetStaticFile(fileName, out var file, subfolders);
        if(!result || file == null)
            return new GetStaticFileTextResponse("Static file not found");
        
        return await GetStaticFileText(file, ct);
    }
    
    /// <summary>
    /// Reads an already-resolved file. Internal so <see cref="StaticFileCache"/> can hand over the
    /// file the registry gave it, rather than paying for the same lookup twice.
    /// </summary>
    internal async Task<GetStaticFileTextResponse> GetStaticFileText(StaticFile file, CancellationToken ct = default)
    {
        var path = _pathProvider.GetStaticPath(file.SubFolders);
        var filePath = Path.Combine(path, file.FileName);
        
        if(!_pathProvider.CheckFileExists(filePath))
            return new GetStaticFileTextResponse("Static file not found");
        
        try
        {
            var text = await File.ReadAllTextAsync(filePath, ct);
            return new GetStaticFileTextResponse(file, text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading static file {FileName} in path {Path}", file.FileName, path);
            return new GetStaticFileTextResponse("Error reading file.");
        }
    }
}