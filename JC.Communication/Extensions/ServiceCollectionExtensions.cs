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

        // Provider registration
        if (options.Provider == EmailProvider.Microsoft)
        {
            ValidateConfig(configuration,
                MicrosoftOptions.TenantId,
                MicrosoftOptions.ClientId,
                MicrosoftOptions.ClientSecret,
                EmailOptions.ConfigFromAddress);

            services.TryAddScoped<IEmailService, MicrosoftEmailService>();
        }
        else if (options.Provider == EmailProvider.Console)
        {
            // TODO: ConsoleEmailService
            throw new NotImplementedException("Console email provider is not yet implemented.");
        }
        else
        {
            throw new InvalidOperationException($"Unsupported email provider: {options.Provider}");
        }

        return services;
    }

    private static void ValidateConfig(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (string.IsNullOrEmpty(configuration[key]))
                throw new InvalidOperationException($"Configuration value '{key}' is required but was not found.");
        }
    }
}
