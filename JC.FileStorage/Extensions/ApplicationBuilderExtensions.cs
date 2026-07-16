using JC.FileStorage.Models;
using JC.FileStorage.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace JC.FileStorage.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder AddFolders(this IApplicationBuilder app,
        bool throwOnFail = true, params IEnumerable<string> folderNames)
    {
        var folders = folderNames.Select(n => new FolderModel(n));
        return app.AddFolders(throwOnFail, folders);
    }

    public static IApplicationBuilder AddFolders(this IApplicationBuilder app,
        bool throwOnFail = true, params IEnumerable<FolderModel> folders)
    {
        var folderRegistry = app.ApplicationServices.GetRequiredService<FolderRegistry>();
        
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
        
        return app;
    }
}