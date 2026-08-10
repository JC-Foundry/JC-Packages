using JC.FileStorage.Extensions;
using JC.FileStorage.Web.Framework;
using JC.FileStorage.Web.Services;
using JC.Web.Extensions;
using JC.Web.UI.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JC.FileStorage.Web.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="WebStorageService"/>, along with the JC.FileStorage services it wraps
    /// and the UI services this package's tag helpers resolve.
    /// Calling <c>AddFileStorage</c> separately is not required.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="framework">
    /// The framework tag helpers render for. Ignored when the consuming application has already
    /// called <see cref="JC.Web.Extensions.ServiceCollectionExtensions.AddUI"/> or
    /// <c>AddWebDefaults</c>, since the registration there wins.
    /// </param>
    /// <param name="iconFramework">
    /// The icon set, chosen independently of <paramref name="framework"/>. Ignored under the same
    /// conditions. This package registers no icon dictionary of its own.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// The tag helpers in this package inject <see cref="IFileStorageFrameworkDictionary"/> and
    /// JC.Web's <see cref="JC.Web.UI.HTML.HtmlHelper"/>, neither of which the container can supply
    /// on its own. Without this call the failure is at render time rather than at build or startup,
    /// so call it wherever the package is used.
    /// <para>
    /// <see cref="JC.Web.Extensions.ServiceCollectionExtensions.AddUI"/> registers through
    /// <c>TryAdd</c>, so calling it here is harmless when the application has already chosen a
    /// framework — the first registration stands and both dictionaries resolve from the same
    /// <see cref="UIFrameworkService.Framework"/>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddFileStorageWeb(this IServiceCollection services,
        UIFramework framework = UIFramework.Bootstrap,
        IconFramework iconFramework = IconFramework.Bootstrap)
    {
        services.AddFileStorage();
        services.TryAddScoped<WebStorageService>();

        // No icon dictionary is registered — this package's tag helper renders no glyphs. The icon
        // choice is still passed on, so an application registering only this package still gets a
        // resolved icon set for anything layered above it.
        services.AddUI(framework, iconFramework);

        // Adding a framework is a dictionary class and an arm here — no tag helper changes.
        services.AddFrameworkDictionary<IFileStorageFrameworkDictionary>(f => f switch
        {
            UIFramework.Tailwind => new TailwindFileStorageDictionary(),
            UIFramework.CustomJCTailwind => new CustomJCTailwindFileStorageDictionary(),
            _ => new BootstrapFileStorageDictionary()
        });

        return services;
    }
}