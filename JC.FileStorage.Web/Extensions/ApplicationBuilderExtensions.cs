using JC.FileStorage.Extensions;
using JC.FileStorage.Models;
using JC.FileStorage.Services;
using Microsoft.AspNetCore.Builder;

namespace JC.FileStorage.Web.Extensions;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> registering storage folders during
/// application start-up.
/// </summary>
/// <remarks>
/// Convenience overloads only — each defers to the <see cref="IServiceProvider"/> extension in
/// JC.FileStorage, which holds the behaviour. They live here rather than there so JC.FileStorage
/// itself takes no dependency on ASP.NET Core and stays usable from a console application, a worker
/// service or a test host.
/// </remarks>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Registers storage folders by name, each in the no-tenant scope.
    /// </summary>
    /// <param name="app">The application builder, used to reach <see cref="FolderRegistry"/>.</param>
    /// <param name="throwOnFail">
    /// Whether a folder that cannot be added throws. Must be passed explicitly, since it precedes a
    /// <c>params</c> parameter. When <c>false</c>, a clash is skipped and the rest still register.
    /// </param>
    /// <param name="folderNames">The folder names to register.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// A folder could not be added and <paramref name="throwOnFail"/> is <c>true</c>.
    /// </exception>
    public static IApplicationBuilder AddFolders(this IApplicationBuilder app,
        bool throwOnFail = true, params IEnumerable<string> folderNames)
    {
        app.ApplicationServices.AddFolders(throwOnFail, folderNames);
        return app;
    }

    /// <summary>
    /// Registers storage folders, each carrying its own tenant, size limit and allowed extensions.
    /// </summary>
    /// <param name="app">The application builder, used to reach <see cref="FolderRegistry"/>.</param>
    /// <param name="throwOnFail">
    /// Whether a folder that cannot be added throws. Must be passed explicitly, since it precedes a
    /// <c>params</c> parameter. When <c>false</c>, a clash is skipped and the rest still register.
    /// </param>
    /// <param name="folders">The folders to register.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// A folder could not be added and <paramref name="throwOnFail"/> is <c>true</c>.
    /// </exception>
    public static IApplicationBuilder AddFolders(this IApplicationBuilder app,
        bool throwOnFail = true, params IEnumerable<FolderModel> folders)
    {
        app.ApplicationServices.AddFolders(throwOnFail, folders);
        return app;
    }
}
