using System.Threading.RateLimiting;
using JC.Web.ClientProfiling.Helpers;
using JC.Web.ClientProfiling.Models.Options;
using JC.Web.ClientProfiling.Services;
using JC.Web.RateLimiting;
using JC.Web.SEO.Helpers;
using JC.Web.SEO.Middleware;
using JC.Web.SEO.Models.Options;
using JC.Web.SEO.Services;
using JC.Web.Security.Helpers;
using JC.Web.Security.Models.Options;
using JC.Web.Security.Services;
using JC.Web.UI.Framework;
using JC.Web.UI.Framework.Icons;
using JC.Web.UI.HTML;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract

namespace JC.Web.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> providing JC.Web service registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all JC.Web services: security headers, cookie services, and client profiling.
    /// This is the recommended single entry point for consuming applications.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration, required when <paramref name="useEncryptedCookies"/> is <c>true</c>.</param>
    /// <param name="useEncryptedCookies">Whether to register the encrypted cookie service. Defaults to <c>true</c>.</param>
    /// <param name="configureHeaderFilter">Optional callback to configure <see cref="SecurityHeaderOptions"/>.</param>
    /// <param name="configureCookieFilter">Optional callback to configure <see cref="CookieDefaultOptions"/>.</param>
    /// <param name="configureBotFilter">Optional callback to configure <see cref="BotFilterOptions"/>.</param>
    /// <param name="configureClientIp">Optional callback to configure <see cref="ClientIpOptions"/>.</param>
    /// <param name="uiFramework">The selected UI framework tag helpers use. Defaults to bootstrap</param>
    /// <param name="iconFramework">The selected icon framework tag helpers use. Chosen independently of <paramref name="uiFramework"/>. Defaults to bootstrap</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWebDefaults(this IServiceCollection services,
        IConfiguration? configuration = null,
        bool useEncryptedCookies = true,
        Action<SecurityHeaderOptions>? configureHeaderFilter = null,
        Action<CookieDefaultOptions>? configureCookieFilter = null,
        Action<BotFilterOptions>? configureBotFilter = null,
        Action<ClientIpOptions>? configureClientIp = null,
        UIFramework uiFramework = UIFramework.Bootstrap,
        IconFramework iconFramework = IconFramework.Bootstrap)
    {
        services.AddSecurityDefaults(configuration, useEncryptedCookies, configureHeaderFilter, configureCookieFilter);
        services.AddClientProfiling(configureBotFilter, configureClientIp);
        services.AddUI(uiFramework, iconFramework);

        return services;
    }

    /// <summary>
    /// Registers all JC.Web services with a custom <see cref="IGeoLocationProvider"/> for
    /// IP-based geographic location resolution in client profiling.
    /// </summary>
    /// <typeparam name="TGeoService">The geo-location provider implementation type.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration, required when <paramref name="useEncryptedCookies"/> is <c>true</c>.</param>
    /// <param name="useEncryptedCookies">Whether to register the encrypted cookie service. Defaults to <c>true</c>.</param>
    /// <param name="configureHeaderFilter">Optional callback to configure <see cref="SecurityHeaderOptions"/>.</param>
    /// <param name="configureCookieFilter">Optional callback to configure <see cref="CookieDefaultOptions"/>.</param>
    /// <param name="configureBotFilter">Optional callback to configure <see cref="BotFilterOptions"/>.</param>
    /// <param name="configureGeoLocation">Optional callback to configure <see cref="GeoLocationOptions"/>.</param>
    /// <param name="configureClientIp">Optional callback to configure <see cref="ClientIpOptions"/>.</param>
    /// <param name="uiFramework">The selected UI framework tag helpers use. Defaults to bootstrap</param>
    /// <param name="iconFramework">The selected icon framework tag helpers use. Chosen independently of <paramref name="uiFramework"/>. Defaults to bootstrap</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWebDefaults<TGeoService>(this IServiceCollection services,
        IConfiguration? configuration = null,
        bool useEncryptedCookies = true,
        Action<SecurityHeaderOptions>? configureHeaderFilter = null,
        Action<CookieDefaultOptions>? configureCookieFilter = null,
        Action<BotFilterOptions>? configureBotFilter = null,
        Action<GeoLocationOptions>? configureGeoLocation = null,
        Action<ClientIpOptions>? configureClientIp = null,
        UIFramework uiFramework = UIFramework.Bootstrap,
        IconFramework iconFramework = IconFramework.Bootstrap)
        where TGeoService : class, IGeoLocationProvider
    {
        services.AddSecurityDefaults(configuration, useEncryptedCookies, configureHeaderFilter, configureCookieFilter);
        services.AddClientProfiling<TGeoService>(configureBotFilter, configureGeoLocation, configureClientIp);
        services.AddUI(uiFramework, iconFramework);

        return services;
    }
    
    
    
    #region Security

    /// <summary>
    /// Registers security headers and cookie services. Combines <see cref="AddSecurityHeaders"/>
    /// and <see cref="AddCookieServices"/> into a single call.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration, required when <paramref name="useEncryptedCookies"/> is <c>true</c>.</param>
    /// <param name="useEncryptedCookies">Whether to register the encrypted cookie service. Defaults to <c>true</c>.</param>
    /// <param name="headerOptions">Optional callback to configure <see cref="SecurityHeaderOptions"/>.</param>
    /// <param name="cookieOptions">Optional callback to configure <see cref="CookieDefaultOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSecurityDefaults(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        bool useEncryptedCookies = true,
        Action<SecurityHeaderOptions>? headerOptions = null,
        Action<CookieDefaultOptions>? cookieOptions = null)
    {
        services.AddSecurityHeaders(headerOptions);
        services.AddCookieServices(configuration, useEncryptedCookies, cookieOptions);
        return services;
    }
    
    
    /// <summary>
    /// Registers security header options. Validates configuration eagerly to fail fast on invalid settings.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Optional callback to configure <see cref="SecurityHeaderOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSecurityHeaders(
        this IServiceCollection services,
        Action<SecurityHeaderOptions>? configure = null)
    {
        // Build options early to validate
        var options = new SecurityHeaderOptions();
        configure?.Invoke(options);

        ValidationHelper.Validate(options);

        services.Configure<SecurityHeaderOptions>(opt =>
        {
            opt.EnableXContentTypeOptions = options.EnableXContentTypeOptions;
            opt.XFrameOptions = options.XFrameOptions;
            opt.ReferrerPolicy = options.ReferrerPolicy;
            opt.PermissionsPolicy = options.PermissionsPolicy;
            opt.CrossOriginOpenerPolicy = options.CrossOriginOpenerPolicy;
            opt.CrossOriginResourcePolicy = options.CrossOriginResourcePolicy;
            opt.CrossOriginEmbedderPolicy = options.CrossOriginEmbedderPolicy;
            opt.EnableHsts = options.EnableHsts;
            opt.HstsMaxAge = options.HstsMaxAge;
            opt.HstsIncludeSubDomains = options.HstsIncludeSubDomains;
            opt.HstsProductionOnly = options.HstsProductionOnly;
            opt.RemoveServerHeader = options.RemoveServerHeader;
            opt.RemoveXPoweredByHeader = options.RemoveXPoweredByHeader;
            opt.ContentSecurityPolicy = options.ContentSecurityPolicy;
        });

        return services;
    }

    /// <summary>
    /// Registers cookie services with configurable encryption support.
    /// <para>
    /// In all modes, an unkeyed <see cref="ICookieService"/> is registered and resolves to <see cref="CookieService"/>
    /// (plain, non-encrypted). This allows simple <c>ICookieService</c> injection to always work.
    /// </para>
    /// <para>
    /// When <paramref name="useEncryptedCookies"/> is <c>false</c>, a keyed registration for
    /// <c>ICookieService.StandardCookieDIKey</c> is also added, delegating to the same unkeyed service.
    /// </para>
    /// <para>
    /// When <paramref name="useEncryptedCookies"/> is <c>true</c> (default), both <see cref="CookieService"/>
    /// and <see cref="EncryptedCookieService"/> are registered as <b>keyed services</b>.
    /// Use <c>[FromKeyedServices(ICookieService.StandardCookieDIKey)]</c> or
    /// <c>[FromKeyedServices(ICookieService.EncryptedCookieDIKey)]</c> to select a specific implementation.
    /// Unkeyed <c>ICookieService</c> injection still resolves to the plain <see cref="CookieService"/> in this mode.
    /// Requires the <c>Web:Cookies:DataProtection_Path</c> configuration key.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration, used to read the Data Protection key path when encryption is enabled.</param>
    /// <param name="useEncryptedCookies">Whether to register the encrypted cookie service and configure Data Protection. Defaults to <c>true</c>.</param>
    /// <param name="configure">Optional callback to configure <see cref="CookieDefaultOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="useEncryptedCookies"/> is <c>true</c> and <c>Web:Cookies:DataProtection_Path</c> is missing from configuration.
    /// </exception>
    public static IServiceCollection AddCookieServices(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        bool useEncryptedCookies = true,
        Action<CookieDefaultOptions>? configure = null)
    {
        // Configure cookie defaults
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<CookieDefaultOptions>(_ => { });

        services.AddHttpContextAccessor();

        //Add the cookie profile dictionary as a singleton
        services.TryAddSingleton<CookieProfileDictionary>();
        
        //ICookieService always resolves standard (unencrypted) cookie service when unkeyed
        services.AddScoped<ICookieService, CookieService>();
        if (!useEncryptedCookies)
        {
            // Unencrypted only — register as a plain service for simple ICookieService injection
            services.AddKeyedScoped<ICookieService>(ICookieService.StandardCookieDIKey,
                (sp, _) => sp.GetRequiredService<ICookieService>());
            return services;
        }

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration),
                "When configuring encrypted cookie services, you must pass IConfiguration as a parameter");
        
        // Both services — register as keyed services requiring [FromKeyedServices] attribute
        var dataProtectionPath = configuration[EncryptedCookieService.DataProtectionConfigKey];
        if (string.IsNullOrEmpty(dataProtectionPath))
            throw new InvalidOperationException(
                $"Encrypted cookies require a Data Protection key storage path. " +
                $"Set the '{EncryptedCookieService.DataProtectionConfigKey}' configuration key " +
                $"(e.g. in appsettings.json: {{ \"Web\": {{ \"Cookies\": {{ \"DataProtection_Path\": \"/path/to/keys\" }} }} }}), " +
                $"or set useEncryptedCookies to false.");

        var directory = new DirectoryInfo(dataProtectionPath);
        if (!directory.Exists)
            directory.Create();

        services.AddDataProtection()
            .PersistKeysToFileSystem(directory);

        services.AddKeyedScoped<ICookieService, CookieService>(ICookieService.StandardCookieDIKey);
        services.AddKeyedScoped<ICookieService, EncryptedCookieService>(ICookieService.EncryptedCookieDIKey);

        return services;
    }

    #endregion


    #region ClientProfiling

    /// <summary>
    /// Registers client profiling services including <see cref="UserAgentService"/> and bot filter options.
    /// Use the generic overload to also register a custom <see cref="IGeoLocationProvider"/>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureBotFilter">Optional callback to configure <see cref="BotFilterOptions"/>.</param>
    /// <param name="configureClientIp">Optional callback to configure <see cref="ClientIpOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddClientProfiling(this IServiceCollection services,
        Action<BotFilterOptions>? configureBotFilter = null,
        Action<ClientIpOptions>? configureClientIp = null)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<UserAgentService>();
        services.TryAddSingleton<IGeoLocationProvider, EmptyGeoLocationProvider>();

        if (configureBotFilter is not null)
            services.Configure(configureBotFilter);
        else
            services.Configure<BotFilterOptions>(_ => { });

        if (configureClientIp is not null)
            services.Configure(configureClientIp);
        else
            services.Configure<ClientIpOptions>(_ => { });

        return services;
    }

    /// <summary>
    /// Registers client profiling services with a custom <see cref="IGeoLocationProvider"/> implementation
    /// for IP-based geographic location resolution.
    /// </summary>
    /// <typeparam name="TGeoService">The geo-location provider implementation type.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureBotFilter">Optional callback to configure <see cref="BotFilterOptions"/>.</param>
    /// <param name="configureGeoLocation">Optional callback to configure <see cref="GeoLocationOptions"/>.</param>
    /// <param name="configureClientIp">Optional callback to configure <see cref="ClientIpOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddClientProfiling<TGeoService>(this IServiceCollection services,
        Action<BotFilterOptions>? configureBotFilter = null,
        Action<GeoLocationOptions>? configureGeoLocation = null,
        Action<ClientIpOptions>? configureClientIp = null)
        where TGeoService : class, IGeoLocationProvider
    {
        services.TryAddScoped<IGeoLocationProvider, TGeoService>();

        if (configureGeoLocation is not null)
            services.Configure(configureGeoLocation);
        else
            services.Configure<GeoLocationOptions>(_ => { });

        services.AddClientProfiling(configureBotFilter, configureClientIp);

        return services;
    }

    #endregion


    #region Rate Limiting

    /// <summary>
    /// The policy name used internally for the JC.Web rate limiter.
    /// </summary>
    internal const string RateLimitPolicyName = "JcWebRateLimit";

    /// <summary>
    /// Registers ASP.NET Core rate limiting with configurable strategy, partitioning, and limits.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Optional callback to configure <see cref="RateLimitingOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services,
        Action<RateLimitingOptions>? configure = null)
    {
        var options = new RateLimitingOptions();
        configure?.Invoke(options);

        if (!options.IsEnabled)
        {
            services.Configure<RateLimitingOptions>(opt => opt.IsEnabled = false);
            return services;
        }

        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<RateLimitingOptions>(_ => { });

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Skip static files — they shouldn't count against the rate limit
                if (options.ExcludeStaticFiles
                    && RateLimitingOptions.IsStaticFile(context.Request.Path.Value ?? string.Empty))
                {
                    return RateLimitPartition.GetNoLimiter(string.Empty);
                }

                var partitionKey = ResolvePartitionKey(context, options);

                return options.Strategy switch
                {
                    RateLimitingStrategy.SlidingWindow => RateLimitPartition.GetSlidingWindowLimiter(partitionKey,
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = options.PermitLimit,
                            Window = options.Window,
                            SegmentsPerWindow = options.SegmentsPerWindow,
                            QueueLimit = options.QueueLimit,
                            QueueProcessingOrder = options.QueueProcessingOrder
                        }),
                    RateLimitingStrategy.TokenBucket => RateLimitPartition.GetTokenBucketLimiter(partitionKey,
                        _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = options.TokenLimit > 0 ? options.TokenLimit : options.PermitLimit,
                            TokensPerPeriod = options.TokensPerPeriod,
                            ReplenishmentPeriod = options.Window,
                            QueueLimit = options.QueueLimit,
                            QueueProcessingOrder = options.QueueProcessingOrder
                        }),
                    RateLimitingStrategy.Concurrency => RateLimitPartition.GetConcurrencyLimiter(partitionKey,
                        _ => new ConcurrencyLimiterOptions
                        {
                            PermitLimit = options.ConcurrencyLimit > 0 ? options.ConcurrencyLimit : options.PermitLimit,
                            QueueLimit = options.QueueLimit,
                            QueueProcessingOrder = options.QueueProcessingOrder
                        }),
                    _ => RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = options.PermitLimit,
                            Window = options.Window,
                            QueueLimit = options.QueueLimit,
                            QueueProcessingOrder = options.QueueProcessingOrder
                        })
                };
            });
        });

        return services;
    }

    private static string ResolvePartitionKey(HttpContext context, RateLimitingOptions options)
    {
        return options.PartitionBy switch
        {
            RateLimitPartitionBy.User =>
                context.User.Identity?.IsAuthenticated == true
                    ? context.User.Identity.Name ?? context.Request.Path.ToString()
                    : context.Request.Path.ToString(),
            RateLimitPartitionBy.Endpoint =>
                context.Request.Path.ToString(),
            RateLimitPartitionBy.ClientIpAndEndpoint =>
                $"{ResolveClientIp(context)}:{context.Request.Path}",
            _ => ResolveClientIp(context)
        };
    }

    /// <summary>
    /// Resolves the client IP for rate limit partitioning, honouring
    /// <see cref="ClientIpOptions.TrustProxyHeaders"/>.
    /// </summary>
    /// <remarks>
    /// Behind a reverse proxy, <c>RemoteIpAddress</c> is the proxy's own address, so partitioning on
    /// it alone puts every visitor into a single bucket and turns a per-client limit into a
    /// site-wide one. Reading the same option the request metadata middleware uses keeps the
    /// partition key and <see cref="ClientProfiling.Models.RequestMetadata.ClientIp"/> consistent
    /// for a given request.
    /// </remarks>
    private static string ResolveClientIp(HttpContext context)
    {
        var ipOptions = context.RequestServices?.GetService<IOptions<ClientIpOptions>>()?.Value
                        ?? new ClientIpOptions();

        return ClientIpResolver.Resolve(context, ipOptions.TrustProxyHeaders);
    }

    #endregion


    #region UI

    /// <summary>
    /// Registers the UI framework services and JC.Web's own class dictionary.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="framework">
    /// The framework tag helpers and builders render for. May combine flags, in which case the most
    /// specific is used. Defaults to <see cref="UIFramework.Bootstrap"/>.
    /// </param>
    /// <param name="iconFramework">
    /// The icon framework tag helpers and builders render for. May combine flags, in which case
    /// the most specific is used. Defaults to <see cref="IconFramework.Bootstrap"/>
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUI(this IServiceCollection services,
        UIFramework framework = UIFramework.Bootstrap,
        IconFramework iconFramework = IconFramework.Bootstrap)
    {
        services.TryAddSingleton<UIFrameworkService>(
            _ => new UIFrameworkService(framework, iconFramework));

        services.AddFrameworkDictionary<IWebFrameworkDictionary>(f => f switch
        {
            UIFramework.Tailwind => new TailwindDictionary(),
            UIFramework.CustomJCTailwind => new CustomJCTailwindDictionary(),
            _ => new BootstrapDictionary()
        });

        // Stateless renderers, so a singleton each. The builders that accumulate state
        // (BreadcrumbBuilder, TableBuilder<T>) are constructed per use and take the dictionary
        // directly instead.
        services.TryAddSingleton<AlertHelper>();
        services.TryAddSingleton<HtmlHelper>();

        return services;
    }

    /// <summary>
    /// Registers a package's CSS class dictionary, selecting the implementation from the framework
    /// resolved by <see cref="UIFrameworkService"/>.
    /// </summary>
    /// <typeparam name="TDictionary">The package's dictionary contract.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="factory">
    /// Returns the implementation for the resolved framework. Called once, on first resolution.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Each package registers its own dictionary through this method, so adding a tag helper to a
    /// package layered above JC.Web never requires changing JC.Web. Every dictionary is driven by
    /// the same single framework choice, so they cannot disagree.
    /// <para>
    /// Requires <see cref="AddUI"/> to have been called, since the factory is handed the framework
    /// held by <see cref="UIFrameworkService"/>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddFrameworkDictionary<TDictionary>(this IServiceCollection services,
        Func<UIFramework, TDictionary> factory)
        where TDictionary : class, IFrameworkDictionary
    {
        services.TryAddSingleton(sp =>
            factory(sp.GetRequiredService<UIFrameworkService>().Framework));

        return services;
    }

    /// <summary>
    /// Registers a package's icon class dictionary, selecting the implementation from the icon set
    /// resolved by <see cref="UIFrameworkService"/>.
    /// </summary>
    /// <typeparam name="TDictionary">The package's icon dictionary contract.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="factory">
    /// Returns the implementation for the resolved icon set. Called once, on first resolution.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// The icon counterpart to <see cref="AddFrameworkDictionary{TDictionary}"/>, reading
    /// <see cref="UIFrameworkService.IconFramework"/> rather than
    /// <see cref="UIFrameworkService.Framework"/>. The two are independent choices, so a package may
    /// register either or both.
    /// <para>
    /// Requires <see cref="AddUI"/> to have been called, since the factory is handed the icon set
    /// held by <see cref="UIFrameworkService"/>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddIconDictionary<TDictionary>(this IServiceCollection services,
        Func<IconFramework, TDictionary> factory)
        where TDictionary : class, IIconDictionary
    {
        services.TryAddSingleton(sp =>
            factory(sp.GetRequiredService<UIFrameworkService>().IconFramework));

        return services;
    }

    #endregion


    #region SEO

    /// <summary>
    /// Registers sitemap generation. Discovered Razor Page routes, registered
    /// <see cref="ISitemapUrlProvider"/> implementations, and explicitly configured URLs are merged
    /// into a single sitemap served by <see cref="ApplicationBuilderExtensions.UseSitemap"/>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Optional callback to configure <see cref="SitemapOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSitemap(this IServiceCollection services,
        Action<SitemapOptions>? configure = null)
        => services.AddSitemap(null, configure);

    /// <summary>
    /// Registers sitemap generation, binding <see cref="SitemapOptions.ConfigSection"/> from
    /// configuration before applying <paramref name="configure"/>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Application configuration, bound from <c>Web:SEO:Sitemap</c>. May be null to skip binding.</param>
    /// <param name="configure">Optional callback applied after binding, so code overrides configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSitemap(this IServiceCollection services,
        IConfiguration? configuration,
        Action<SitemapOptions>? configure = null)
    {
        if (configuration is not null)
            services.Configure<SitemapOptions>(configuration.GetSection(SitemapOptions.ConfigSection));

        if (configure is not null)
            services.Configure(configure);
        else if (configuration is null)
            services.Configure<SitemapOptions>(_ => { });

        // Constructed here rather than resolved lazily, so the timestamp is the application's
        // start time rather than whenever the first crawler happened to arrive.
        services.TryAddSingleton(new SitemapStartTime());
        services.TryAddScoped<SitemapUrlAggregator>();

        // Lets robots.txt tell whether a sitemap is actually being served.
        services.TryAddSingleton<SitemapMarker>();

        return services;
    }

    /// <summary>
    /// Registers an <see cref="ISitemapUrlProvider"/> supplying URLs that route discovery cannot
    /// resolve, such as database-backed content behind a parameterised route.
    /// </summary>
    /// <typeparam name="TProvider">The provider implementation.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Registered scoped, so providers may depend on a DbContext. Call once per provider — multiple
    /// providers are all invoked and their results merged.
    /// </remarks>
    public static IServiceCollection AddSitemapProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, ISitemapUrlProvider
    {
        services.AddScoped<ISitemapUrlProvider, TProvider>();
        return services;
    }

    /// <summary>
    /// Registers robots.txt generation, served by <see cref="ApplicationBuilderExtensions.UseRobots"/>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Optional callback to configure <see cref="RobotsOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRobots(this IServiceCollection services,
        Action<RobotsOptions>? configure = null)
        => services.AddRobots(null, configure);

    /// <summary>
    /// Registers robots.txt generation, binding <see cref="RobotsOptions.ConfigSection"/> from
    /// configuration before applying <paramref name="configure"/>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Application configuration, bound from <c>Web:SEO:Robots</c>. May be null to skip binding.</param>
    /// <param name="configure">Optional callback applied after binding, so code overrides configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Keeping rules in configuration lets a staging environment disallow everything through its own
    /// appsettings file rather than a branch in startup.
    /// </remarks>
    public static IServiceCollection AddRobots(this IServiceCollection services,
        IConfiguration? configuration,
        Action<RobotsOptions>? configure = null)
    {
        if (configuration is not null)
            services.Configure<RobotsOptions>(configuration.GetSection(RobotsOptions.ConfigSection));

        if (configure is not null)
            services.Configure(configure);
        else if (configuration is null)
            services.Configure<RobotsOptions>(_ => { });

        // The robots middleware reads sitemap options to build its Sitemap: directive, so these
        // must resolve even when AddSitemap was never called.
        services.Configure<SitemapOptions>(_ => { });

        return services;
    }

    /// <summary>
    /// Registers site-wide defaults for <see cref="SeoBuilder"/> and the <c>&lt;seo-meta&gt;</c> tag helper.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Optional callback to configure <see cref="SeoMetaOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSeoMeta(this IServiceCollection services,
        Action<SeoMetaOptions>? configure = null)
        => services.AddSeoMeta(null, configure);

    /// <summary>
    /// Registers site-wide SEO meta defaults, binding <see cref="SeoMetaOptions.ConfigSection"/>
    /// from configuration before applying <paramref name="configure"/>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Application configuration, bound from <c>Web:SEO:Meta</c>. May be null to skip binding.</param>
    /// <param name="configure">Optional callback applied after binding, so code overrides configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSeoMeta(this IServiceCollection services,
        IConfiguration? configuration,
        Action<SeoMetaOptions>? configure = null)
    {
        if (configuration is not null)
            services.Configure<SeoMetaOptions>(configuration.GetSection(SeoMetaOptions.ConfigSection));

        if (configure is not null)
            services.Configure(configure);
        else if (configuration is null)
            services.Configure<SeoMetaOptions>(_ => { });

        return services;
    }

    /// <summary>
    /// Convenience registration for the whole SEO area — sitemap, robots.txt and meta defaults.
    /// Equivalent to calling <see cref="AddSitemap(IServiceCollection, IConfiguration?, Action{SitemapOptions}?)"/>,
    /// <see cref="AddRobots(IServiceCollection, IConfiguration?, Action{RobotsOptions}?)"/> and
    /// <see cref="AddSeoMeta(IServiceCollection, IConfiguration?, Action{SeoMetaOptions}?)"/>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Application configuration, bound from <c>Web:SEO</c>. May be null to skip binding.</param>
    /// <param name="sitemap">Optional callback to configure <see cref="SitemapOptions"/>.</param>
    /// <param name="robots">Optional callback to configure <see cref="RobotsOptions"/>.</param>
    /// <param name="meta">Optional callback to configure <see cref="SeoMetaOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// SEO is opt-in and deliberately absent from <c>AddWebDefaults</c>, unlike the UI services.
    /// Internal tools and line-of-business applications have no use for it, and its configuration is
    /// specific enough per application that folding it into the defaults would bloat them.
    /// Register the three parts individually when an application needs only some of them.
    /// </remarks>
    public static IServiceCollection AddSeo(this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<SitemapOptions>? sitemap = null,
        Action<RobotsOptions>? robots = null,
        Action<SeoMetaOptions>? meta = null)
    {
        services.AddSitemap(configuration, sitemap);
        services.AddRobots(configuration, robots);
        services.AddSeoMeta(configuration, meta);

        return services;
    }

    #endregion
}
