using JC.FileStorage.Models;
using Microsoft.Extensions.Caching.Memory;

namespace JC.FileStorage.Services;

/// <summary>
/// Holds the content of registered static files in memory. Static files are written at deploy time
/// and are not managed at runtime, so a read only has to reach the disk once per cache window.
/// </summary>
public class StaticFileCache
{
    private const string FileByteKey = "StaticFile:Bytes";
    private const string FileTextKey = "StaticFile:Text";

    private readonly IMemoryCache _cache;
    private readonly StaticFileService _staticFileService;
    private readonly StaticFileRegistry _registry;
    private readonly TimeSpan? _cacheDuration;

    /// <param name="cache">The application's memory cache.</param>
    /// <param name="staticFileService">Reads a file's content when it is not held in the cache.</param>
    /// <param name="registry">Resolves a name to a registered file. An unregistered name is not read.</param>
    /// <param name="cacheDurationMinutes">
    /// How long a file's content is held before it is read again. <c>0</c> disables caching, so every
    /// read goes to disk — useful in development, where a deploy-time file may be edited in place.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">The duration is negative.</exception>
    public StaticFileCache(IMemoryCache cache,
        StaticFileService staticFileService,
        StaticFileRegistry registry,
        int cacheDurationMinutes = 10)
    {
        //Rejected here rather than absolute-valued, so a bad duration fails where the registration is
        //written. IMemoryCache refuses a non-positive expiry, so zero has to mean something explicit
        if(cacheDurationMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(cacheDurationMinutes), cacheDurationMinutes,
                "A cache duration cannot be negative. Use 0 to disable caching.");

        _cache = cache;
        _staticFileService = staticFileService;
        _registry = registry;
        _cacheDuration = cacheDurationMinutes == 0
            ? null
            : TimeSpan.FromMinutes(cacheDurationMinutes);
    }

    /// <summary>
    /// Reads a registered static file's bytes, from the cache when it is held there.
    /// </summary>
    /// <param name="fileName">The file name, including its extension.</param>
    /// <param name="ct">
    /// Cancels the read. Must be passed explicitly, since it precedes a <c>params</c> parameter.
    /// </param>
    /// <param name="subFolders">The subfolders the file sits under, relative to the static path.</param>
    /// <returns>The file's bytes, or a failed response when it is not registered or cannot be read.</returns>
    public async Task<GetStaticFileByteResponse> GetStaticFileBytes(string fileName,
        CancellationToken ct = default, params IEnumerable<string> subFolders)
    {
        var file = GetRegisteredStaticFile(fileName, subFolders);
        if(file == null)
            return new GetStaticFileByteResponse("Static file not found");

        var key = CacheKey(FileByteKey, file);
        if(_cache.TryGetValue(key, out GetStaticFileByteResponse? cached) && cached != null)
            return cached;

        //The registry has already resolved the file, so the service is handed it directly rather
        //than looking the same name up a second time
        var response = await _staticFileService.GetStaticFileBytes(file, ct);
        Cache(key, response);
        return response;
    }

    /// <summary>
    /// Reads a registered static file's text, from the cache when it is held there.
    /// </summary>
    /// <param name="fileName">The file name, including its extension.</param>
    /// <param name="ct">
    /// Cancels the read. Must be passed explicitly, since it precedes a <c>params</c> parameter.
    /// </param>
    /// <param name="subFolders">The subfolders the file sits under, relative to the static path.</param>
    /// <returns>The file's text, or a failed response when it is not registered or cannot be read.</returns>
    public async Task<GetStaticFileTextResponse> GetStaticFileText(string fileName,
        CancellationToken ct = default, params IEnumerable<string> subFolders)
    {
        var file = GetRegisteredStaticFile(fileName, subFolders);
        if(file == null)
            return new GetStaticFileTextResponse("Static file not found");

        //Text and bytes are held separately - the same file under one key would hand a caller back
        //the other form's response
        var key = CacheKey(FileTextKey, file);
        if(_cache.TryGetValue(key, out GetStaticFileTextResponse? cached) && cached != null)
            return cached;

        var response = await _staticFileService.GetStaticFileText(file, ct);
        Cache(key, response);
        return response;
    }

    private StaticFile? GetRegisteredStaticFile(string fileName, IEnumerable<string> subFolders)
    {
        _registry.TryGetStaticFile(fileName, out var file, subFolders);
        return file;
    }

    private static string CacheKey(string prefix, StaticFile file)
        => $"{prefix}:{file.Key}";

    /// <summary>
    /// Holds a response, unless caching is off or the read failed. A failed read is never held — a
    /// transient lock during a deployment would otherwise persist for the whole cache window.
    /// </summary>
    private void Cache<T>(string key, T response)
        where T : ResponseBase
    {
        if(_cacheDuration == null || !response.Result)
            return;

        _cache.Set(key, response, _cacheDuration.Value);
    }
}