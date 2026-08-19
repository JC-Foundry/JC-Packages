using JC.Content.Comparison.Models.Options;
using JC.Content.Comparison.Services;
using JC.Content.Conversion.Models.Options;
using JC.Content.Conversion.Services;
using JC.Content.Moderation.Models.Options;
using JC.Content.Moderation.Services;
using JC.Content.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JC.Content.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the content management services to the service collection, including moderation, comparison,
    /// and conversion functionalities.
    /// </summary>
    /// <param name="services">The service collection to register the services into.</param>
    /// <param name="configureProfanityOptions">
    /// Configures the profanity moderation options. This includes setting up any default values or
    /// configurations related to profanity moderation.
    /// </param>
    /// <param name="includeImportedProfanityTerms">
    /// Determines whether to include the bundled term list for profanity moderation along with the curated
    /// term list. Set to <c>false</c> to use only the curated list for increased accuracy.
    /// </param>
    /// <param name="configureProfanityTerms">
    /// Configures the profanity term registry, allowing the application to add its own terms, remove
    /// default terms, or adjust term settings for specific needs.
    /// </param>
    /// <param name="configureComparisonOptions">
    /// Configures the content comparison options. This involves setting parameters for comparing content
    /// such as rules or thresholds for similarity detection.
    /// </param>
    /// <param name="configureConversionOptions">
    /// Configures the content conversion options. This may include specifying how content should be
    /// transformed, such as format conversions or rendering adjustments.
    /// </param>
    /// <returns>The service collection, allowing further service registrations to be chained.</returns>
    public static IServiceCollection AddContentManager(this IServiceCollection services,
        Action<ProfanityModerationOptions>? configureProfanityOptions = null,
        bool includeImportedProfanityTerms = true,
        Action<ProfanityTermRegistry>? configureProfanityTerms = null,
        Action<ContentComparisonOptions>? configureComparisonOptions = null,
        Action<ContentConversionOptions>? configureConversionOptions = null)
    {
        services.AddContentModeration(configureProfanityOptions, includeImportedProfanityTerms, configureProfanityTerms);
        services.AddContentComparison(configureComparisonOptions);
        services.AddContentConversion(configureConversionOptions);
        
        services.TryAddSingleton<ContentManager>();
        return services;
    }
    
    
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

    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureOptions">
    /// Sets the default granularity and the content ceiling. The granularity is a default only —
    /// every call can override it.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddContentComparison(this IServiceCollection services,
        Action<ContentComparisonOptions>? configureOptions = null)
    {
        var options = new ContentComparisonOptions();
        configureOptions?.Invoke(options);
        options.Validate();

        services.TryAddSingleton(options);

        //Singleton: it holds nothing per call, and the chunkers behind it are stateless
        services.TryAddSingleton<ContentComparer>();

        return services;
    }

    #endregion


    #region Conversion

    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureOptions">
    /// Sets the Markdown flavour, whether raw HTML survives a Markdown document, and whether link
    /// destinations follow their text in plain-text output.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddContentConversion(this IServiceCollection services,
        Action<ContentConversionOptions>? configureOptions = null)
    {
        var options = new ContentConversionOptions();
        configureOptions?.Invoke(options);

        services.TryAddSingleton(options);

        //Singleton: the Markdown pipeline is built once and the writers hold nothing per call
        services.TryAddSingleton<ContentConverter>();

        return services;
    }

    #endregion
}
