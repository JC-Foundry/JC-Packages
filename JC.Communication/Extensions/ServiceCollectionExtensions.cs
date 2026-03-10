using JC.Communication.Email.Models;
using JC.Communication.Email.Models.Options;
using JC.Communication.Email.Services;
using JC.Communication.Logging.Data;
using JC.Communication.Logging.Models.Email;
using JC.Communication.Logging.Models.Notifications;
using JC.Communication.Logging.Services;
using JC.Communication.Notifications.Data;
using JC.Communication.Notifications.Models;
using JC.Communication.Notifications.Models.Options;
using JC.Communication.Notifications.Services;
using JC.Core.Data;
using JC.Core.Extensions;
using JC.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JC.Communication.Extensions;

/// <summary>
/// Extension methods for registering JC.Communication services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    #region Email

    /// <summary>
    /// Registers email services without database logging support.
    /// The <see cref="EmailLoggingMode"/> must be set to <see cref="EmailLoggingMode.None"/>
    /// or an <see cref="InvalidOperationException"/> is thrown.
    /// Use the generic <see cref="AddEmail{TContext}"/> overload to enable logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration, used to validate required provider config keys.</param>
    /// <param name="configure">Optional action to configure <see cref="EmailOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if logging mode is not <see cref="EmailLoggingMode.None"/>.</exception>
    public static IServiceCollection AddEmail(this IServiceCollection services,
        IConfiguration configuration,
        Action<EmailOptions>? configure = null)
    {
        var options = new EmailOptions();
        configure?.Invoke(options);

        services.AddOptions<EmailOptions>()
            .Configure(opts =>
            {
                configure?.Invoke(opts);
            });

        if(options.LoggingMode != EmailLoggingMode.None)
            throw new InvalidOperationException("You must use generic overload to use logging.");

        services.AddEmailBase(configuration, options);
        return services;
    }


    /// <summary>
    /// Registers email services with database logging support.
    /// Configures the <see cref="IEmailDbContext"/>, <see cref="EmailLogService"/>,
    /// and repository contexts for all email log entities.
    /// </summary>
    /// <typeparam name="TContext">The application's DbContext type, which must implement <see cref="IEmailDbContext"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration, used to validate required provider config keys.</param>
    /// <param name="configure">Optional action to configure <see cref="EmailOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEmail<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<EmailOptions>? configure = null)
        where TContext : DbContext, IDataDbContext, IEmailDbContext
    {
        var options = new EmailOptions();
        configure?.Invoke(options);

        services.AddOptions<EmailOptions>()
            .Configure(opts =>
            {
                configure?.Invoke(opts);
            });

        // Db context
        services.TryAddScoped<IEmailDbContext>(sp => sp.GetRequiredService<TContext>());

        // Repository contexts for log entities
        services.RegisterRepositoryContexts(
            typeof(EmailLog),
            typeof(EmailRecipientLog),
            typeof(EmailContentLog),
            typeof(EmailSentLog));

        services.AddEmailBase(configuration, options);
        return services;
    }

    /// <summary>
    /// Shared provider registration logic. Validates required configuration keys
    /// and registers the appropriate <see cref="IEmailService"/> implementation based on the configured provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="options">The resolved email options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if required configuration values are missing or the provider is unsupported.</exception>
    private static IServiceCollection AddEmailBase(this IServiceCollection services,
        IConfiguration configuration,
        EmailOptions options)
    {
        // Logging (always required as it is injected)
        services.TryAddScoped<EmailLogService>();
        
        switch (options.Provider)
        {
            // Provider registration
            case EmailProvider.Microsoft:
                //Validates all config values are set for Microsoft provider
                ValidateConfig(configuration,
                    MicrosoftOptions.TenantId,
                    MicrosoftOptions.ClientId,
                    MicrosoftOptions.ClientSecret,
                    EmailOptions.ConfigFromAddress);

                ValidateSmtpOptions(options);

                //Registers MicrosoftEmailService
                services.TryAddScoped<IEmailService, MicrosoftEmailService>();
                break;

            case EmailProvider.Console:
                ValidateConfig(configuration, EmailOptions.ConfigFromAddress);
                
                //Registers ConsoleEmailService
                services.TryAddScoped<IEmailService, ConsoleEmailService>();
                break;

            case EmailProvider.SmtpRelay:
                ValidateConfig(configuration, EmailOptions.ConfigFromAddress);

                if (options.UsernameRequired)
                    ValidateConfig(configuration, SmtpRelayOptions.Username);

                ValidateAnyConfig(configuration,
                    SmtpRelayOptions.Password,
                    SmtpRelayOptions.ApiKey,
                    SmtpRelayOptions.Secret);

                ValidateSmtpOptions(options);

                //Registers SmtpRelayEmailService
                services.TryAddScoped<IEmailService, SmtpRelayEmailService>();
                break;

            case EmailProvider.DirectSmtp:
                ValidateConfig(configuration, EmailOptions.ConfigFromAddress);
                ValidateSmtpOptions(options);

                //Registers DirectSmtpEmailService
                services.TryAddScoped<IEmailService, DirectSmtpEmailService>();
                break;

            default:
                throw new InvalidOperationException($"Unsupported email provider: {options.Provider}");
        }

        return services;
    }
    
    
    
    /// <summary>
    /// Validates that at least one of the specified configuration keys has a non-empty value.
    /// Used for flexible secret resolution where multiple key names are supported (e.g. Password, ApiKey, Secret).
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="keys">The configuration keys to check. At least one must have a value.</param>
    /// <exception cref="InvalidOperationException">Thrown if none of the configuration keys have a value.</exception>
    private static void ValidateAnyConfig(IConfiguration configuration, params string[] keys)
    {
        if (keys.Any(key => !string.IsNullOrEmpty(configuration[key])))
            return;

        throw new InvalidOperationException(
            $"Email configuration validation failed: at least one of the following must be configured: {string.Join(", ", keys)}");
    }

    /// <summary>
    /// Validates that the SMTP options have a non-empty host and a valid port.
    /// Required for all SMTP-based providers (Microsoft, SmtpRelay, DirectSmtp).
    /// </summary>
    /// <param name="options">The email options to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown if host is empty or port is out of range.</exception>
    private static void ValidateSmtpOptions(EmailOptions options)
    {
        string? errors = null;

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            errors = "EmailOptions.Host is required for SMTP-based providers.";
        }

        if (options.Port is < 1 or > 65535)
        {
            if (!string.IsNullOrEmpty(errors)) errors += ", ";
            errors += $"EmailOptions.Port must be between 1 and 65535 (was {options.Port}).";
        }

        if (!string.IsNullOrEmpty(errors))
            throw new InvalidOperationException($"Email configuration validation failed: {errors}");
    }

    /// <summary>
    /// Validates that all specified configuration keys have non-empty values.
    /// Collects all missing keys and throws a single aggregated error.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="keys">The configuration keys to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown if any configuration values are missing.</exception>
    private static void ValidateConfig(IConfiguration configuration, params string[] keys)
    {
        string? errors = null;
        foreach (var key in keys)
        {
            if (!string.IsNullOrEmpty(configuration[key])) continue;

            if(!string.IsNullOrEmpty(errors)) errors += ", ";
            errors += $"Configuration value '{key}' is required but was not found.";
        }

        if(!string.IsNullOrEmpty(errors))
            throw new InvalidOperationException($"Email configuration validation failed: {errors}");
    }

    #endregion


    #region Notifications

    /// <summary>
    /// Registers notification services without database logging support, using the default <see cref="NotificationManager"/>.
    /// Requires <see cref="IUserInfo"/> to be registered in the service collection (typically via JC.Identity).
    /// Use <see cref="AddNotificationsWithLogging{TContext}"/> to enable database logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure <see cref="NotificationOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="IUserInfo"/> is not registered,
    /// if <see cref="NotificationOptions.CacheDurationHours"/> is outside the valid range (1–72),
    /// or if <see cref="NotificationOptions.LoggingMode"/> is not <see cref="NotificationLoggingMode.None"/>.</exception>
    public static IServiceCollection AddNotifications(this IServiceCollection services,
        Action<NotificationOptions>? configure = null)
        => services.AddNotifications<NotificationManager>(configure);


    /// <summary>
    /// Registers notification services without database logging support, using a custom <see cref="INotificationManager"/> implementation.
    /// Requires <see cref="IUserInfo"/> to be registered in the service collection (typically via JC.Identity).
    /// Use <see cref="AddNotificationsWithLogging{TContext, TNotificationManager}"/> to enable database logging.
    /// </summary>
    /// <typeparam name="TNotificationManager">The <see cref="INotificationManager"/> implementation type to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure <see cref="NotificationOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="IUserInfo"/> is not registered,
    /// if <see cref="NotificationOptions.CacheDurationHours"/> is outside the valid range (1–72),
    /// or if <see cref="NotificationOptions.LoggingMode"/> is not <see cref="NotificationLoggingMode.None"/>.</exception>
    public static IServiceCollection AddNotifications<TNotificationManager>(this IServiceCollection services,
        Action<NotificationOptions>? configure = null)
        where TNotificationManager : class, INotificationManager
    {
        // Options
        var options = new NotificationOptions();
        configure?.Invoke(options);

        if (options.CacheDurationHours is < 1 or > 72)
            throw new InvalidOperationException(
                $"NotificationOptions.CacheDurationHours must be between 1 and 72 hours (was {options.CacheDurationHours}).");

        services.AddOptions<NotificationOptions>()
            .Configure(opts => configure?.Invoke(opts));

        if(options.LoggingMode != NotificationLoggingMode.None)
            throw new InvalidOperationException("You must use generic overload to use logging.");
        
        services.AddNotificationsBase<TNotificationManager>(options);
        return services;
    }
    
    
    
    /// <summary>
    /// Registers notification services with database logging support, using the default <see cref="NotificationManager"/>.
    /// Configures the <see cref="INotificationDbContext"/> and repository contexts for all notification entities.
    /// Requires <see cref="IUserInfo"/> to be registered in the service collection (typically via JC.Identity).
    /// </summary>
    /// <typeparam name="TContext">The application's DbContext type, which must implement
    /// <see cref="IDataDbContext"/> and <see cref="INotificationDbContext"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure <see cref="NotificationOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="IUserInfo"/> is not registered
    /// or if <see cref="NotificationOptions.CacheDurationHours"/> is outside the valid range (1–72).</exception>
    public static IServiceCollection AddNotificationsWithLogging<TContext>(
        this IServiceCollection services,
        Action<NotificationOptions>? configure = null)
        where TContext : DbContext, IDataDbContext, INotificationDbContext
        => services.AddNotificationsWithLogging<TContext, NotificationManager>(configure);

    
    /// <summary>
    /// Registers notification services with database logging support and a custom <see cref="INotificationManager"/> implementation.
    /// Configures the <see cref="INotificationDbContext"/> and repository contexts for all notification entities.
    /// Requires <see cref="IUserInfo"/> to be registered in the service collection (typically via JC.Identity).
    /// </summary>
    /// <typeparam name="TContext">The application's DbContext type, which must implement
    /// <see cref="IDataDbContext"/> and <see cref="INotificationDbContext"/>.</typeparam>
    /// <typeparam name="TNotificationManager">The <see cref="INotificationManager"/> implementation type to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure <see cref="NotificationOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="IUserInfo"/> is not registered
    /// or if <see cref="NotificationOptions.CacheDurationHours"/> is outside the valid range (1–72).</exception>
    public static IServiceCollection AddNotificationsWithLogging<TContext, TNotificationManager>(
        this IServiceCollection services,
        Action<NotificationOptions>? configure = null)
        where TContext : DbContext, IDataDbContext, INotificationDbContext
        where TNotificationManager : class, INotificationManager
    {
        // Options
        var options = new NotificationOptions();
        configure?.Invoke(options);

        if (options.CacheDurationHours is < 1 or > 72)
            throw new InvalidOperationException(
                $"NotificationOptions.CacheDurationHours must be between 1 and 72 hours (was {options.CacheDurationHours}).");

        services.AddOptions<NotificationOptions>()
            .Configure(opts => configure?.Invoke(opts));

        // Db context
        services.TryAddScoped<INotificationDbContext>(sp => sp.GetRequiredService<TContext>());

        // Repository contexts for notification entities
        services.RegisterRepositoryContexts(
            typeof(Notification),
            typeof(NotificationStyle),
            typeof(NotificationLog));

        services.AddNotificationsBase<TNotificationManager>(options);
        return services;
    }


    /// <summary>
    /// Shared registration logic for notification services. Validates that <see cref="IUserInfo"/>
    /// is registered, then registers the core notification services and the specified
    /// <see cref="INotificationManager"/> implementation.
    /// </summary>
    /// <typeparam name="TNotificationManager">The <see cref="INotificationManager"/> implementation type to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The resolved notification options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="IUserInfo"/> is not registered.</exception>
    private static IServiceCollection AddNotificationsBase<TNotificationManager>(
        this IServiceCollection services,
        NotificationOptions options)
        where TNotificationManager : class, INotificationManager
    {
        // Guard: IUserInfo must be registered (typically by JC.Identity)
        if (services.All(s => s.ServiceType != typeof(IUserInfo)))
            throw new InvalidOperationException(
                $"{nameof(IUserInfo)} is not registered. " +
                "Ensure JC.Identity services are registered before calling AddNotifications.");
        
        // Services
        services.TryAddScoped<NotificationService>();
        services.TryAddScoped<NotificationLogService>();
        services.TryAddScoped<NotificationCache>();
        services.TryAddScoped<NotificationSender>();
        services.TryAddScoped<INotificationManager, TNotificationManager>();

        return services;
    }

    #endregion
}
