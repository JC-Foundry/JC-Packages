using JC.FileStorage.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace JC.FileStorage.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds file storage services to the service collection, optionally enabling static file handling.
    /// </summary>
    /// <param name="services">The service collection to which the file storage services will be added.</param>
    /// <param name="useStaticFiles">Specifies whether static file handling should be enabled. Defaults to false.</param>
    /// <param name="autoDiscoverStaticFiles">Determines if static files should be automatically discovered. Defaults to true.</param>
    /// <param name="staticFileCacheDurationMinutes">The duration, in minutes, for caching static files. Defaults to 10 minutes.</param>
    /// <returns>The updated service collection, including file storage services.</returns>
    public static IServiceCollection AddFileStorage(this IServiceCollection services,
        bool useStaticFiles = false,
        bool autoDiscoverStaticFiles = true,
        int staticFileCacheDurationMinutes = 10)
    {
        services.TryAddSingleton<FolderRegistry>();
        services.TryAddSingleton<FilePathProvider>();
        services.TryAddScoped<StorageService>();
        
        if(!useStaticFiles)
            return services;
        
        services.TryAddSingleton<StaticFileService>();
        services.TryAddSingleton<StaticFileRegistry>(sp =>
        {
            var pathProvider = sp.GetRequiredService<FilePathProvider>();
            var logger = sp.GetRequiredService<ILogger<StaticFileRegistry>>();
            var registry = new StaticFileRegistry(pathProvider, logger);
            
            if(autoDiscoverStaticFiles)
                registry.AutoDiscoverStaticFiles();
            
            return registry;
        });

        services.AddMemoryCache();
        services.TryAddSingleton<StaticFileCache>(sp =>
        {
            var cache = sp.GetRequiredService<IMemoryCache>();
            var staticFileService = sp.GetRequiredService<StaticFileService>();
            var registry = sp.GetRequiredService<StaticFileRegistry>();
            return new StaticFileCache(cache, staticFileService, registry, staticFileCacheDurationMinutes);
        });
        
        return services;
    }
}