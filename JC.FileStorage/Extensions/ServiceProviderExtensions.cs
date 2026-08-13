using JC.FileStorage.Models;
using JC.FileStorage.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JC.FileStorage.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceProvider"/> registering storage folders after the
/// application's services have been built.
/// </summary>
/// <remarks>
/// Folders must be registered before any file is saved or read, since <see cref="FolderRegistry"/>
/// is what resolves a folder's path and its size and extension limits.
/// <para>
/// These take <see cref="IServiceProvider"/> rather than a host-specific builder so the package
/// stays usable from a console application, a worker service or a test host. ASP.NET Core
/// applications can call this through <c>app.Services</c>, or take the <c>IApplicationBuilder</c>
/// overload from JC.FileStorage.Web.
/// </para>
/// </remarks>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Registers storage folders by name, each in the no-tenant scope.
    /// </summary>
    /// <param name="services">The built service provider, used to resolve <see cref="FolderRegistry"/>.</param>
    /// <param name="throwOnFail">
    /// Whether a folder that cannot be added throws. Must be passed explicitly, since it precedes a
    /// <c>params</c> parameter. When <c>false</c>, a clash is skipped and the rest still register.
    /// </param>
    /// <param name="folderNames">The folder names to register.</param>
    /// <returns>The service provider for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// A folder could not be added and <paramref name="throwOnFail"/> is <c>true</c>. The usual cause
    /// is a name already registered for that tenant — folder names are compared case-insensitively.
    /// </exception>
    public static IServiceProvider AddFolders(this IServiceProvider services,
        bool throwOnFail = true, params IEnumerable<string> folderNames)
    {
        var folders = folderNames.Select(n => new FolderModel(n));
        return services.AddFolders(throwOnFail, folders);
    }

    /// <summary>
    /// Registers storage folders, each carrying its own tenant, size limit and allowed extensions.
    /// </summary>
    /// <param name="services">The built service provider, used to resolve <see cref="FolderRegistry"/>.</param>
    /// <param name="throwOnFail">
    /// Whether a folder that cannot be added throws. Must be passed explicitly, since it precedes a
    /// <c>params</c> parameter. When <c>false</c>, a clash is skipped and the rest still register.
    /// </param>
    /// <param name="folders">The folders to register.</param>
    /// <returns>The service provider for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// A folder could not be added and <paramref name="throwOnFail"/> is <c>true</c>. The usual cause
    /// is a name already registered for that tenant — folder names are compared case-insensitively.
    /// </exception>
    public static IServiceProvider AddFolders(this IServiceProvider services,
        bool throwOnFail = true, params IEnumerable<FolderModel> folders)
    {
        var folderRegistry = services.GetRequiredService<FolderRegistry>();

        foreach (var folder in folders)
        {
            var result = folderRegistry.TryAddFolder(folder);
            switch (result)
            {
                case false when !throwOnFail:
                    continue;
                case false:
                    throw new InvalidOperationException($"Unable to add folder '{folder.Name}'");
            }
        }

        return services;
    }


    /// <summary>
    /// Registers static files by name and other properties in the application's storage system.
    /// </summary>
    /// <param name="services">The built service provider, used to resolve <see cref="StaticFileRegistry"/>.</param>
    /// <param name="throwOnFail">
    /// Indicates whether a static file that cannot be added should throw an exception.
    /// When <c>false</c>, any file that fails to register is skipped, and the rest are processed.
    /// </param>
    /// <param name="files">The collection of static files to be registered.</param>
    /// <returns>The service provider for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a static file cannot be added and <paramref name="throwOnFail"/> is <c>true</c>.
    /// The usual cause is a conflict preventing the file from being registered.
    /// </exception>
    public static IServiceProvider AddStaticFiles(this IServiceProvider services,
        bool throwOnFail = true, params IEnumerable<StaticFile> files)
    {
        var registry = services.GetRequiredService<StaticFileRegistry>();

        foreach (var file in files)
        {
            var result = registry.TryAddStaticFile(file);
            switch (result)
            {
                case false when !throwOnFail:
                    continue;
                case false:
                    throw new InvalidOperationException($"Unable to add static file '{file.Name}'");
            }
        }
        
        return services;
    }
}
