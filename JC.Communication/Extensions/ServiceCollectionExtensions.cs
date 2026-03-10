using JC.Communication.Email.Models;
using JC.Communication.Email.Models.Options;
using JC.Communication.Email.Services;
using JC.Communication.Logging.Data;
using JC.Communication.Logging.Models.Email;
using JC.Communication.Logging.Services;
using JC.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JC.Communication.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmail<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<EmailOptions>? configure = null)
        where TContext : DbContext, IEmailDbContext
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

        // Logging
        services.TryAddScoped<EmailLogService>();

        // Repository contexts for log entities
        services.RegisterRepositoryContexts(
            typeof(EmailLog),
            typeof(EmailRecipientLog),
            typeof(EmailContentLog),
            typeof(EmailSentLog));

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

                //Registers MicrosoftEmailService
                services.TryAddScoped<IEmailService, MicrosoftEmailService>();
                break;
            
            case EmailProvider.Console:
                //Registers ConsoleEmailService
                services.TryAddScoped<IEmailService, ConsoleEmailService>();
                break;
            
            case EmailProvider.SmtpRelay:
                if (options.UsernameRequired)
                    ValidateConfig(configuration, SmtpRelayOptions.Username, EmailOptions.ConfigFromAddress);
                else
                    ValidateConfig(configuration, EmailOptions.ConfigFromAddress);

                //Registers SmtpRelayEmailService
                services.TryAddScoped<IEmailService, SmtpRelayEmailService>();
                break;

            case EmailProvider.DirectSmtp:
                ValidateConfig(configuration, EmailOptions.ConfigFromAddress);

                //Registers DirectSmtpEmailService
                services.TryAddScoped<IEmailService, DirectSmtpEmailService>();
                break;

            default:
                throw new InvalidOperationException($"Unsupported email provider: {options.Provider}");
        }

        return services;
    }

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
}
