using JC.Content.Moderation.Models.Options;
using JC.Content.Moderation.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JC.Content.Extensions;

public static class ServiceCollectionExtensions
{
    #region Moderation

    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureOptions">
    /// Sets the default level and the context width. The level is a default only — every call can
    /// override it.
    /// </param>
    /// <param name="includeImportedTerms">
    /// Whether to load the bundled term list alongside the curated one. The curated slurs load either
    /// way. Set to <c>false</c> to run on curated terms only, which trades coverage for accuracy.
    /// </param>
    /// <param name="configureTerms">
    /// Runs against the seeded registry, for an application to add its own terms, remove ones it
    /// disagrees with, or allow words that were producing false positives.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddContentModeration(this IServiceCollection services,
        Action<ProfanityModerationOptions>? configureOptions = null,
        bool includeImportedTerms = true,
        Action<ProfanityTermRegistry>? configureTerms = null)
    {
        var options = new ProfanityModerationOptions();
        configureOptions?.Invoke(options);
        options.Validate();

        services.TryAddSingleton(options);

        services.TryAddSingleton<ProfanityTermRegistry>(_ =>
        {
            var registry = new ProfanityTermRegistry();
            registry.Seed(includeImportedTerms);

            //After seeding, so an application's terms take precedence over both bundled sources
            configureTerms?.Invoke(registry);

            return registry;
        });

        //Singleton: it holds an index over the term set, and rebuilding that per request would cost
        //far more than the moderation itself
        services.TryAddSingleton<ProfanityModerator>();
        services.TryAddSingleton<ProfanityMasker>();

        return services;
    }

    #endregion


    #region Comparison

    //TODO: Register Content Comparison

    #endregion
}
