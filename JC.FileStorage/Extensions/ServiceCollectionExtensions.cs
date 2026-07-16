using JC.FileStorage.Models;
using JC.FileStorage.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JC.FileStorage.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services)
    {
        return services;
    }
}