using JC.CAP.Authentication;
using JC.CAP.Models;
using JC.CAP.Models.Options;
using JC.Core.Enums;
using JC.Core.Models;
using JC.Identity.Shared.Extensions;
using JC.Identity.Shared.Models.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenIddict.Client;
using OpenIddict.Client.AspNetCore;

namespace JC.CAP.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> registering JC.CAP: the session cookie, the
/// OpenIddict client registration for CAP, and the shared identity runtime behind <see cref="IUserInfo"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers JC.CAP from code, with the default <see cref="CapUserInfo"/>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Configures <see cref="CapOptions"/>. The issuer, client id and secret are required.</param>
    /// <param name="configureMiddleware">Optional callback to configure <see cref="IdentityMiddlewareOptions"/>.</param>
    /// <param name="configureCookie">Optional callback to configure the session cookie, applied after JC.CAP's own defaults.</param>
    /// <param name="configureClient">Optional callback handed the raw OpenIddict builder, applied after JC.CAP's own configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCap(
        this IServiceCollection services,
        Action<CapOptions> configure,
        Action<IdentityMiddlewareOptions>? configureMiddleware = null,
        Action<CookieAuthenticationOptions>? configureCookie = null,
        Action<OpenIddictClientBuilder>? configureClient = null)
        => services.AddCap<CapUserInfo>(configure, configureMiddleware, configureCookie, configureClient);

    /// <summary>
    /// Registers JC.CAP from code, with a derived <see cref="CapUserInfo"/> carrying more than the shared surface.
    /// </summary>
    /// <typeparam name="TUserInfo">The <see cref="IUserInfo"/> implementation to register.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Configures <see cref="CapOptions"/>. The issuer, client id and secret are required.</param>
    /// <param name="configureMiddleware">Optional callback to configure <see cref="IdentityMiddlewareOptions"/>.</param>
    /// <param name="configureCookie">Optional callback to configure the session cookie, applied after JC.CAP's own defaults.</param>
    /// <param name="configureClient">Optional callback handed the raw OpenIddict builder, applied after JC.CAP's own configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCap<TUserInfo>(
        this IServiceCollection services,
        Action<CapOptions> configure,
        Action<IdentityMiddlewareOptions>? configureMiddleware = null,
        Action<CookieAuthenticationOptions>? configureCookie = null,
        Action<OpenIddictClientBuilder>? configureClient = null)
        where TUserInfo : class, IUserInfo
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<CapOptions>()
                .Configure(configure)
                .ValidateOnStart();

        return services.AddCapServices<TUserInfo>(configureMiddleware, configureCookie, configureClient);
    }

    /// <summary>
    /// Registers JC.CAP from the <c>CAP</c> configuration section, with the default <see cref="CapUserInfo"/>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The configuration to bind <see cref="CapOptions.ConfigSection"/> from.</param>
    /// <param name="configure">Optional code configuration, applied after binding so it wins.</param>
    /// <param name="configureMiddleware">Optional callback to configure <see cref="IdentityMiddlewareOptions"/>.</param>
    /// <param name="configureCookie">Optional callback to configure the session cookie, applied after JC.CAP's own defaults.</param>
    /// <param name="configureClient">Optional callback handed the raw OpenIddict builder, applied after JC.CAP's own configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCap(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CapOptions>? configure = null,
        Action<IdentityMiddlewareOptions>? configureMiddleware = null,
        Action<CookieAuthenticationOptions>? configureCookie = null,
        Action<OpenIddictClientBuilder>? configureClient = null)
        => services.AddCap<CapUserInfo>(configuration, configure, configureMiddleware, configureCookie, configureClient);

    /// <summary>
    /// Registers JC.CAP from the <c>CAP</c> configuration section, with a derived <see cref="CapUserInfo"/>.
    /// </summary>
    /// <typeparam name="TUserInfo">The <see cref="IUserInfo"/> implementation to register.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The configuration to bind <see cref="CapOptions.ConfigSection"/> from.</param>
    /// <param name="configure">Optional code configuration, applied after binding so it wins.</param>
    /// <param name="configureMiddleware">Optional callback to configure <see cref="IdentityMiddlewareOptions"/>.</param>
    /// <param name="configureCookie">Optional callback to configure the session cookie, applied after JC.CAP's own defaults.</param>
    /// <param name="configureClient">Optional callback handed the raw OpenIddict builder, applied after JC.CAP's own configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <see cref="CapOptions.Scopes"/> is a getter-only collection, so the binder adds to the defaults
    /// rather than replacing them. A <c>CAP:Scopes</c> array in configuration therefore reads as
    /// "these as well", never "these instead".
    /// </remarks>
    public static IServiceCollection AddCap<TUserInfo>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CapOptions>? configure = null,
        Action<IdentityMiddlewareOptions>? configureMiddleware = null,
        Action<CookieAuthenticationOptions>? configureCookie = null,
        Action<OpenIddictClientBuilder>? configureClient = null)
        where TUserInfo : class, IUserInfo
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var builder = services.AddOptions<CapOptions>()
                              .Bind(configuration.GetSection(CapOptions.ConfigSection));

        if (configure is not null)
            builder.Configure(configure);

        builder.ValidateOnStart();

        return services.AddCapServices<TUserInfo>(configureMiddleware, configureCookie, configureClient);
    }

    /// <summary>
    /// The registration every <c>AddCap</c> overload shares, once <see cref="CapOptions"/> is arranged.
    /// </summary>
    /// <remarks>
    /// Nothing here reads <see cref="CapOptions"/> eagerly. At this point the application's own
    /// <c>Configure&lt;CapOptions&gt;</c> calls have not run and configuration binding has not been
    /// resolved, so a snapshot taken now would capture the defaults and silently discard both. The
    /// cookie, the OpenIddict registration and the transport-security switch are all reached through
    /// <see cref="IConfigureOptions{TOptions}"/> instead, which resolves at first use.
    /// </remarks>
    private static IServiceCollection AddCapServices<TUserInfo>(
        this IServiceCollection services,
        Action<IdentityMiddlewareOptions>? configureMiddleware,
        Action<CookieAuthenticationOptions>? configureCookie,
        Action<OpenIddictClientBuilder>? configureClient)
        where TUserInfo : class, IUserInfo
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<CapOptions>, CapOptionsValidator>());

        // So the refresh timing is testable.
        services.TryAddSingleton(TimeProvider.System);

        services.AddAuthorization();

        // The cookie carries ASP.NET Identity's claim types, so the projection's own defaults are
        // already right. Only the authority has to be stated.
        services.AddSharedIdentityServices<TUserInfo>(
            configureMiddleware,
            projection => projection.Authority = IdentityAuthority.CAP);

        AddCapCookie(services, configureCookie);
        AddCapClient(services, configureClient);

        return services;
    }

    /// <summary>
    /// Registers the session cookie as the application's default scheme, so <c>[Authorize]</c>
    /// challenges it and it redirects to the local sign-in trigger.
    /// </summary>
    private static void AddCapCookie(IServiceCollection services, Action<CookieAuthenticationOptions>? configureCookie)
    {
        services.AddAuthentication(CapDefaults.AuthenticationScheme)
                .AddCookie(CapDefaults.AuthenticationScheme);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IConfigureOptions<CookieAuthenticationOptions>, ConfigureCapCookie>());

        // Registered after JC.CAP's own, so the application's settings win.
        if (configureCookie is not null)
            services.Configure(CapDefaults.AuthenticationScheme, configureCookie);
    }

    /// <summary>
    /// Registers the OpenIddict client: the flows CAP allows, the keys it insists on, and the
    /// ASP.NET Core passthroughs that let JC.CAP's own endpoints finish both callbacks.
    /// </summary>
    private static void AddCapClient(IServiceCollection services, Action<OpenIddictClientBuilder>? configureClient)
    {
        services.AddOpenIddict()
                .AddClient(client =>
                {
                    client.AllowAuthorizationCodeFlow()
                          .AllowRefreshTokenFlow()
                          .AllowClientCredentialsFlow();

                    // The client refuses to start without at least one of each once a redirection
                    // endpoint is configured. Ephemeral rather than a certificate every consumer
                    // must provision: state tokens are formatted through Data Protection below, so
                    // they survive a restart and work across a farm on the application's key ring.
                    client.AddEphemeralEncryptionKey()
                          .AddEphemeralSigningKey();

                    // The tokens JC.CAP needs live in the cookie and CAP holds the authoritative
                    // copies, so no consumer needs AddCore() or OpenIddict's tables. An application
                    // with an OpenIddict database can turn storage back on through configureClient.
                    client.DisableTokenStorage();

                    // CapClaimsPrincipalFactory is the only thing that writes ClaimTypes.* onto the
                    // cookie. Left on, OpenIddict would map some of them too and the translation
                    // would stop being the single source of the identity's vocabulary.
                    client.DisableWebServicesFederationClaimMapping();

                    client.UseDataProtection();

                    client.UseAspNetCore()
                          .EnableRedirectionEndpointPassthrough()
                          .EnablePostLogoutRedirectionEndpointPassthrough();

                    client.UseSystemNetHttp()
                          .SetProductInformation(typeof(CapDefaults).Assembly);

                    // Last, so an application can override anything above: production certificates,
                    // a resilience pipeline, re-enabling token storage.
                    configureClient?.Invoke(client);
                });

        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IConfigureOptions<OpenIddictClientOptions>, ConfigureCapRegistration>());

        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IConfigureOptions<OpenIddictClientAspNetCoreOptions>, ConfigureCapTransportSecurity>());
    }

    /// <summary>JC.CAP's defaults for the session cookie, built from <see cref="CapOptions"/>.</summary>
    private sealed class ConfigureCapCookie(IOptions<CapOptions> cap) : IConfigureNamedOptions<CookieAuthenticationOptions>
    {
        public void Configure(string? name, CookieAuthenticationOptions options)
        {
            if (!string.Equals(name, CapDefaults.AuthenticationScheme, StringComparison.Ordinal))
                return;

            var capOptions = cap.Value;

            options.Cookie.Name = CapDefaults.CookieName;
            options.Cookie.HttpOnly = true;

            // Lax, not Strict: CAP returns the browser to the callback by a top-level redirect from
            // another origin, and Strict would withhold the cookie on that first request back.
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = capOptions.AllowInsecureHttp
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;

            options.ExpireTimeSpan = capOptions.Session.Lifetime;
            options.SlidingExpiration = true;

            options.LoginPath = capOptions.SignInPath;
            options.LogoutPath = capOptions.SignOutPath;
            options.AccessDeniedPath = capOptions.DeniedPath;
            options.ReturnUrlParameter = CapEndpoints.ReturnUrlParameter;
        }

        public void Configure(CookieAuthenticationOptions options) { }
    }

    /// <summary>
    /// The single OpenIddict client registration for CAP, and the two local paths CAP returns to.
    /// </summary>
    /// <remarks>
    /// Adds to the endpoint collections rather than calling the builder's
    /// <c>SetRedirectionEndpointUris</c>, which clears them first and would discard anything an
    /// application had already configured through <c>configureClient</c>.
    /// <para>
    /// Grant types, response types and the code challenge method are deliberately left unset, so
    /// they are negotiated from CAP's discovery document. CAP advertises S256 only, which is what
    /// the client then uses.
    /// </para>
    /// </remarks>
    private sealed class ConfigureCapRegistration(IOptions<CapOptions> cap) : IConfigureOptions<OpenIddictClientOptions>
    {
        public void Configure(OpenIddictClientOptions options)
        {
            var capOptions = cap.Value;

            var callback = new Uri(capOptions.CallbackPath, UriKind.Relative);
            var postLogoutCallback = new Uri(capOptions.PostLogoutCallbackPath, UriKind.Relative);

            options.RedirectionEndpointUris.Add(callback);
            options.PostLogoutRedirectionEndpointUris.Add(postLogoutCallback);

            var registration = new OpenIddictClientRegistration
            {
                Issuer = new Uri(capOptions.Issuer, UriKind.Absolute),
                ClientId = capOptions.ClientId,
                ClientSecret = capOptions.ClientSecret,
                RegistrationId = CapDefaults.RegistrationId,
                ProviderName = CapDefaults.ProviderName,
                RedirectUri = callback,
                PostLogoutRedirectUri = postLogoutCallback
            };

            foreach (var scope in capOptions.Scopes)
                registration.Scopes.Add(scope);

            options.Registrations.Add(registration);
        }
    }

    /// <summary>
    /// Lets the callbacks answer over plain http in development, when
    /// <see cref="CapOptions.AllowInsecureHttp"/> says so.
    /// </summary>
    private sealed class ConfigureCapTransportSecurity(IOptions<CapOptions> cap)
        : IConfigureOptions<OpenIddictClientAspNetCoreOptions>
    {
        public void Configure(OpenIddictClientAspNetCoreOptions options)
        {
            if (cap.Value.AllowInsecureHttp)
                options.DisableTransportSecurityRequirement = true;
        }
    }
}
