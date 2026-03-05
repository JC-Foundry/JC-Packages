using JC.Core.Extensions;
using JC.Core.Models;
using JC.Identity.Authentication;
using JC.Identity.Extensions.Options;
using JC.Identity.Models;
using JC.Identity.Models.MultiTenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JC.Identity.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> providing JC.Identity service registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers JC.Identity services including authentication, authorisation, a custom <see cref="IUserInfo"/> implementation,
    /// the custom claims principal factory, middleware options, and the tenant repository context.
    /// </summary>
    /// <typeparam name="TUser">The user entity type, extending <see cref="BaseUser"/>.</typeparam>
    /// <typeparam name="TRole">The role entity type, extending <see cref="BaseRole"/>.</typeparam>
    /// <typeparam name="TUserInfo">The <see cref="IUserInfo"/> implementation type to register.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureMiddleware">Optional callback to configure <see cref="IdentityMiddlewareOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIdentity<TUser, TRole, TUserInfo>(
        this IServiceCollection services,
        Action<IdentityMiddlewareOptions>? configureMiddleware = null)
        where TUser : BaseUser
        where TRole : BaseRole
        where TUserInfo : class, IUserInfo
    {
        services.AddAuthorization();
        services.AddAuthentication();

        // Register IUserInfo as scoped (per-request)
        services.TryAddScoped<IUserInfo, TUserInfo>();

        // Configure middleware options
        if (configureMiddleware != null)
        {
            services.Configure(configureMiddleware);
        }
        else
        {
            services.Configure<IdentityMiddlewareOptions>(_ => { });
        }

        // Replace default claims principal factory with our custom one
        services.AddScoped<IUserClaimsPrincipalFactory<TUser>, DefaultClaimsPrincipalFactory<TUser, TRole>>();

        // Register Tenant repository
        services.RegisterRepositoryContext<Tenant>();

        return services;
    }

    /// <summary>
    /// Registers JC.Identity services using the default <see cref="UserInfo"/> implementation.
    /// </summary>
    /// <typeparam name="TUser">The user entity type, extending <see cref="BaseUser"/>.</typeparam>
    /// <typeparam name="TRole">The role entity type, extending <see cref="BaseRole"/>.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureMiddleware">Optional callback to configure <see cref="IdentityMiddlewareOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIdentity<TUser, TRole>(
        this IServiceCollection services,
        Action<IdentityMiddlewareOptions>? configureMiddleware = null)
        where TUser : BaseUser
        where TRole : BaseRole
    {
        services.AddIdentity<TUser, TRole, UserInfo>();
        return services;
    }

    /// <summary>
    /// Registers JC.Identity services as an extension on an existing <see cref="IdentityBuilder"/>.
    /// </summary>
    /// <typeparam name="TUser">The user entity type, extending <see cref="BaseUser"/>.</typeparam>
    /// <typeparam name="TRole">The role entity type, extending <see cref="BaseRole"/>.</typeparam>
    /// <param name="builder">The identity builder to extend.</param>
    /// <param name="configureMiddleware">Optional callback to configure <see cref="IdentityMiddlewareOptions"/>.</param>
    /// <returns>The identity builder for chaining.</returns>
    public static IdentityBuilder AddIdentity<TUser, TRole>(
        this IdentityBuilder builder,
        Action<IdentityMiddlewareOptions>? configureMiddleware = null)
        where TUser : BaseUser
        where TRole : BaseRole
    {
        builder.Services.AddIdentity<TUser, TRole>(configureMiddleware);
        return builder;
    }
}
