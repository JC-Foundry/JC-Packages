using JC.Core.Models;
using JC.Identity.Shared.Models.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JC.Identity.Shared.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> registering the shared identity runtime.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the authority-agnostic half of the identity runtime: the scoped
    /// <see cref="IUserInfo"/>, the middleware options and the claim types the projection reads.
    /// </summary>
    /// <typeparam name="TUserInfo">
    /// The <see cref="IUserInfo"/> implementation to register. Each authority supplies its own,
    /// deriving from <see cref="Models.UserInfoBase"/>.
    /// </typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureMiddleware">Optional callback to configure <see cref="IdentityMiddlewareOptions"/>.</param>
    /// <param name="configureProjection">
    /// Optional callback to configure <see cref="IdentityProjectionOptions"/>. Defaults match ASP.NET
    /// Identity, so a local-Identity application normally leaves this alone.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Deliberately does not call <c>AddAuthentication</c> or <c>AddAuthorization</c>. Establishing
    /// that a principal is authenticated belongs to the authority; this package only projects the
    /// result.
    /// </remarks>
    public static IServiceCollection AddSharedIdentityServices<TUserInfo>(
        this IServiceCollection services,
        Action<IdentityMiddlewareOptions>? configureMiddleware = null,
        Action<IdentityProjectionOptions>? configureProjection = null)
        where TUserInfo : class, IUserInfo
    {
        // Scoped: one instance per request, populated once by UserInfoMiddleware.
        services.TryAddScoped<IUserInfo, TUserInfo>();

        if (configureMiddleware != null)
            services.Configure(configureMiddleware);
        else
            services.Configure<IdentityMiddlewareOptions>(_ => { });

        if (configureProjection != null)
            services.Configure(configureProjection);
        else
            services.Configure<IdentityProjectionOptions>(_ => { });

        return services;
    }
}
