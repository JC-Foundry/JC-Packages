using JC.Web.Security.Helpers;
using JC.Web.Security.Models.Options;
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
}
