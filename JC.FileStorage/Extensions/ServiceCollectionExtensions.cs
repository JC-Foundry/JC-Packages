using JC.FileStorage.Models;
using JC.FileStorage.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JC.FileStorage.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services)
    {
        services.TryAddSingleton<FolderRegistry>();
        services.TryAddSingleton<FilePathProvider>();
        services.TryAddScoped<StorageService>();
        
        return services;
    }
}