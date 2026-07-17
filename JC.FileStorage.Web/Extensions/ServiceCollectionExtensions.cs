using JC.FileStorage.Extensions;
using JC.FileStorage.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JC.FileStorage.Web.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="WebStorageService"/>, along with the JC.FileStorage services it wraps.
    /// Calling <c>AddFileStorage</c> separately is not required.
    /// </summary>
    public static IServiceCollection AddFileStorageWeb(this IServiceCollection services)
    {
        services.AddFileStorage();
        services.TryAddScoped<WebStorageService>();

        return services;
    }
}