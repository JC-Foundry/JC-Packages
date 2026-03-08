using JC.Web.ClientProfiling.Models.Options;
using JC.Web.ClientProfiling.Services;
using JC.Web.Security.Helpers;
using JC.Web.Security.Models.Options;
using JC.Web.Security.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWebDefaults(this IServiceCollection services,
        IConfiguration? configuration = null,
        bool useEncryptedCookies = true,
        Action<SecurityHeaderOptions>? configureHeaderFilter = null,
        Action<CookieDefaultOptions>? configureCookieFilter = null,
        Action<BotFilterOptions>? configureBotFilter = null,
        Action<ClientIpOptions>? configureClientIp = null)
    {
        services.AddSecurityDefaults(configuration, useEncryptedCookies, configureHeaderFilter, configureCookieFilter);
        services.AddClientProfiling(configureBotFilter, configureClientIp);

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
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWebDefaults<TGeoService>(this IServiceCollection services,
        IConfiguration? configuration = null,
        bool useEncryptedCookies = true,
        Action<SecurityHeaderOptions>? configureHeaderFilter = null,
        Action<CookieDefaultOptions>? configureCookieFilter = null,
        Action<BotFilterOptions>? configureBotFilter = null,
        Action<GeoLocationOptions>? configureGeoLocation = null,
        Action<ClientIpOptions>? configureClientIp = null)
        where TGeoService : class, IGeoLocationProvider
    {
        services.AddSecurityDefaults(configuration, useEncryptedCookies, configureHeaderFilter, configureCookieFilter);
        services.AddClientProfiling<TGeoService>(configureBotFilter, configureGeoLocation, configureClientIp);

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
    /// Requires the <c>Cookies:DataProtection_Path</c> configuration key.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The application configuration, used to read the Data Protection key path when encryption is enabled.</param>
    /// <param name="useEncryptedCookies">Whether to register the encrypted cookie service and configure Data Protection. Defaults to <c>true</c>.</param>
    /// <param name="configure">Optional callback to configure <see cref="CookieDefaultOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="useEncryptedCookies"/> is <c>true</c> and <c>Cookies:DataProtection_Path</c> is missing from configuration.
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
                $"(e.g. in appsettings.json: {{ \"Cookies\": {{ \"DataProtection_Path\": \"/path/to/keys\" }} }}), " +
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
}
