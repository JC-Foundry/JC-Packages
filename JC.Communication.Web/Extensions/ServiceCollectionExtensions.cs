using JC.Communication.Web.Framework;
using JC.Web.Extensions;
using JC.Web.UI.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace JC.Communication.Web.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> providing JC.Communication.Web service
/// registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services JC.Communication.Web's tag helpers resolve — the UI framework
    /// services from JC.Web and this package's own class dictionary.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="framework">
    /// The framework tag helpers render for. Ignored when the consuming application has already
    /// called <see cref="JC.Web.Extensions.ServiceCollectionExtensions.AddUI"/> or
    /// <c>AddWebDefaults</c>, since the registration there wins.
    /// </param>
    /// <param name="iconFramework">
    /// The icon set tag helpers render glyphs from, chosen independently of
    /// <paramref name="framework"/>. Ignored under the same conditions.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Every tag helper in this package injects <see cref="ICommunicationFrameworkDictionary"/> and
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
    public static IServiceCollection AddCommunicationWeb(this IServiceCollection services,
        UIFramework framework = UIFramework.Bootstrap,
        IconFramework iconFramework = IconFramework.Bootstrap)
    {
        services.AddUI(framework, iconFramework);

        // Bootstrap is the only dictionary implemented so far. Tailwind and CustomJCTailwind become
        // additional switch arms here once theirs exist; no tag helper changes.
        services.AddFrameworkDictionary<ICommunicationFrameworkDictionary>(
            _ => new BootstrapCommunicationDictionary());

        // Likewise for icons — FontAwesome becomes a second arm here, selected by its own choice
        // rather than by the CSS framework.
        services.AddIconDictionary<ICommunicationIconDictionary>(
            _ => new BootstrapIconsCommunicationDictionary());

        return services;
    }
}
