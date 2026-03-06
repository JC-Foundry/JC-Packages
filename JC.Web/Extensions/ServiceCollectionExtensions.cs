using JC.Web.Security.Abstractions;
using JC.Web.Security.Helpers;
using JC.Web.Security.Models.Options;
using JC.Web.Security.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JC.Web.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> providing JC.Web service registration.
/// </summary>
public static class ServiceCollectionExtensions
{
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
    /// When <paramref name="useEncryptedCookies"/> is <c>false</c>, registers <see cref="CookieService"/>
    /// as a plain (non-keyed) <see cref="ICookieService"/> — inject directly via <c>ICookieService</c>.
    /// </para>
    /// <para>
    /// When <paramref name="useEncryptedCookies"/> is <c>true</c> (default), both <see cref="CookieService"/>
    /// and <see cref="EncryptedCookieService"/> are registered as <b>keyed services</b>.
    /// Use <c>[FromKeyedServices(ICookieService.StandardCookieDIKey)]</c> or
    /// <c>[FromKeyedServices(ICookieService.EncryptedCookieDIKey)]</c> to inject the desired implementation.
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

        // Unencrypted only — register as a plain service for simple ICookieService injection
        if (!useEncryptedCookies)
        {
            services.AddScoped<ICookieService, CookieService>();
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
}
