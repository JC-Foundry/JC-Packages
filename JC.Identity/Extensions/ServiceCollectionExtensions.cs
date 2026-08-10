using JC.Core.Models;
using JC.Identity.Authentication;
using JC.Identity.Data;
using JC.Identity.Models;
using JC.Identity.Shared.Extensions;
using JC.Identity.Shared.Models.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace JC.Identity.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> providing JC.Identity service registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ASP.NET Core Identity with Entity Framework stores and all JC.Identity services.
    /// This is the recommended entry point — it handles both ASP.NET Identity and JC.Identity registration in one call.
    /// </summary>
    /// <typeparam name="TUser">The user entity type, extending <see cref="BaseUser"/>.</typeparam>
    /// <typeparam name="TRole">The role entity type, extending <see cref="BaseRole"/>.</typeparam>
    /// <typeparam name="TContext">The <see cref="DbContext"/> type used for Identity stores.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureMiddleware">Optional callback to configure <see cref="IdentityMiddlewareOptions"/>.</param>
    /// <param name="configureCookie">
    /// Optional callback to configure the application authentication cookie.
    /// By default, sets login/logout/access-denied paths to <c>/Identity/Account/…</c>.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIdentity<TUser, TRole, TContext>(
        this IServiceCollection services,
        Action<IdentityMiddlewareOptions>? configureMiddleware = null,
        Action<CookieAuthenticationOptions>? configureCookie = null)
        where TUser : BaseUser
        where TRole : BaseRole
        where TContext : IdentityDataDbContext<TUser, TRole>
    {
        services.AddIdentity<TUser, TRole>()
            .AddEntityFrameworkStores<TContext>()
            .AddDefaultTokenProviders();

        ConfigureIdentityCookie(services, configureCookie);
        services.AddIdentityServices<TUser, TRole, UserInfo>(configureMiddleware);

        return services;
    }

    /// <summary>
    /// Registers ASP.NET Core Identity with Entity Framework stores and all JC.Identity services,
    /// using a custom <see cref="IUserInfo"/> implementation.
    /// </summary>
    /// <typeparam name="TUser">The user entity type, extending <see cref="BaseUser"/>.</typeparam>
    /// <typeparam name="TRole">The role entity type, extending <see cref="BaseRole"/>.</typeparam>
    /// <typeparam name="TContext">The <see cref="DbContext"/> type used for Identity stores.</typeparam>
    /// <typeparam name="TUserInfo">The <see cref="IUserInfo"/> implementation type to register.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureMiddleware">Optional callback to configure <see cref="IdentityMiddlewareOptions"/>.</param>
    /// <param name="configureCookie">
    /// Optional callback to configure the application authentication cookie.
    /// By default, sets login/logout/access-denied paths to <c>/Identity/Account/…</c>.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIdentity<TUser, TRole, TContext, TUserInfo>(
        this IServiceCollection services,
        Action<IdentityMiddlewareOptions>? configureMiddleware = null,
        Action<CookieAuthenticationOptions>? configureCookie = null)
        where TUser : BaseUser
        where TRole : BaseRole
        where TContext : IdentityDataDbContext<TUser, TRole>
        where TUserInfo : class, IUserInfo
    {
        services.AddIdentity<TUser, TRole>()
            .AddEntityFrameworkStores<TContext>()
            .AddDefaultTokenProviders();

        ConfigureIdentityCookie(services, configureCookie);
        services.AddIdentityServices<TUser, TRole, TUserInfo>(configureMiddleware);

        return services;
    }

    /// <summary>
    /// Configures the application authentication cookie with sensible defaults for Identity UI paths.
    /// If <paramref name="configure"/> is provided, it is used instead of the defaults.
    /// </summary>
    private static void ConfigureIdentityCookie(IServiceCollection services, Action<CookieAuthenticationOptions>? configure)
    {
        services.ConfigureApplicationCookie(configure ?? (options =>
        {
            options.LoginPath = "/Identity/Account/Login";
            options.LogoutPath = "/Identity/Account/Logout";
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";
        }));
    }

    /// <summary>
    /// Registers only the JC.Identity services — authentication, authorisation, the shared identity
    /// runtime and the claims principal factory — without registering ASP.NET Core Identity.
    /// Use this when ASP.NET Core Identity has already been registered separately.
    /// </summary>
    /// <typeparam name="TUser">The user entity type, extending <see cref="BaseUser"/>.</typeparam>
    /// <typeparam name="TRole">The role entity type, extending <see cref="BaseRole"/>.</typeparam>
    /// <typeparam name="TUserInfo">The <see cref="IUserInfo"/> implementation type to register.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureMiddleware">Optional callback to configure <see cref="IdentityMiddlewareOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Replaces <c>AddIdentityBase</c>, which was renamed in v6: with a JC.Identity.Shared package
    /// in the picture, the old name read as that package's registration call, which it never was.
    /// </remarks>
    public static IServiceCollection AddIdentityServices<TUser, TRole, TUserInfo>(
        this IServiceCollection services,
        Action<IdentityMiddlewareOptions>? configureMiddleware = null)
        where TUser : BaseUser
        where TRole : BaseRole
        where TUserInfo : class, IUserInfo
    {
        services.AddAuthorization();
        services.AddAuthentication();

        services.AddSharedIdentityServices<TUserInfo>(configureMiddleware);
        services.AddIdentityClaimTypes();

        // Replace default claims principal factory with our custom one
        services.AddScoped<IUserClaimsPrincipalFactory<TUser>, DefaultClaimsPrincipalFactory<TUser, TRole>>();

        return services;
    }

    /// <summary>
    /// Registers only the JC.Identity services using the default <see cref="UserInfo"/> implementation,
    /// without registering ASP.NET Core Identity.
    /// Use this when ASP.NET Core Identity has already been registered separately.
    /// </summary>
    /// <typeparam name="TUser">The user entity type, extending <see cref="BaseUser"/>.</typeparam>
    /// <typeparam name="TRole">The role entity type, extending <see cref="BaseRole"/>.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureMiddleware">Optional callback to configure <see cref="IdentityMiddlewareOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIdentityServices<TUser, TRole>(
        this IServiceCollection services,
        Action<IdentityMiddlewareOptions>? configureMiddleware = null)
        where TUser : BaseUser
        where TRole : BaseRole
    {
        services.AddIdentityServices<TUser, TRole, UserInfo>(configureMiddleware);
        return services;
    }

    /// <summary>
    /// Points the shared claim-type options at whatever ASP.NET Identity is actually configured to
    /// use, so that a consumer customising <c>IdentityOptions.ClaimsIdentity</c> keeps working.
    /// </summary>
    /// <remarks>
    /// Registered as <see cref="IConfigureOptions{TOptions}"/> rather than copied inline, because at
    /// registration time <c>IdentityOptions</c> has not been configured yet — the consuming
    /// application's own <c>Configure&lt;IdentityOptions&gt;</c> calls may come afterwards. Copying
    /// eagerly would capture the defaults and silently discard any customisation.
    /// </remarks>
    private static void AddIdentityClaimTypes(this IServiceCollection services)
        => services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<IdentityClaimTypeOptions>,
            ConfigureClaimTypesFromIdentityOptions>());

    private sealed class ConfigureClaimTypesFromIdentityOptions(IOptions<IdentityOptions> identityOptions)
        : IConfigureOptions<IdentityClaimTypeOptions>
    {
        public void Configure(IdentityClaimTypeOptions options)
        {
            var claims = identityOptions.Value.ClaimsIdentity;

            options.UserIdClaimType = claims.UserIdClaimType;
            options.EmailClaimType = claims.EmailClaimType;
            options.RoleClaimType = claims.RoleClaimType;
        }
    }
}
