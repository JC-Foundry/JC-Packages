using JC.Core.Extensions;
using JC.Github.Data;
using JC.Github.Helpers;
using JC.Github.Models;
using JC.Github.Models.Options;
using JC.Github.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace JC.Github.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> providing JC.Github service registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all JC.Github services including <see cref="GitHelper"/>, <see cref="BugReportService"/>,
    /// webhook processing, the <see cref="IGithubDbContext"/> data context, and repository contexts
    /// for <see cref="ReportedIssue"/> and <see cref="IssueComment"/>.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type implementing <see cref="IGithubDbContext"/>.</typeparam>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="configuration">The application configuration, used to resolve <c>Github:ApiKey</c> and <c>Github:Secret</c>.</param>
    /// <param name="configure">Optional callback to configure <see cref="GithubOptions"/>. Runs before internal
    /// post-configuration, so values such as <see cref="GithubOptions.EnableWebhooks"/> are finalised before
    /// the webhook secret is validated.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <c>Github:ApiKey</c> is missing, or if <c>Github:Secret</c> is missing when
    /// <see cref="GithubOptions.EnableWebhooks"/> is <c>true</c>.
    /// </exception>
    public static IServiceCollection AddGithub<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<GithubOptions>? configure = null)
        where TContext : DbContext, IGithubDbContext
    {
        var gitApiKey = configuration["Github:ApiKey"] ?? throw new InvalidOperationException("Configuration value 'Github:ApiKey' not found.");

        // Configure options — consumer callback runs first so that
        // EnableWebhooks (and other values) are finalised before we
        // make decisions based on them in PostConfigure.
        var optionsBuilder = services.AddOptions<GithubOptions>();

        if (configure is not null)
            optionsBuilder.Configure(configure);

        var webhookSecret = configuration["Github:Secret"];
        optionsBuilder.PostConfigure(options =>
        {
            options.WebhookSecret = options.EnableWebhooks
                ? string.IsNullOrEmpty(webhookSecret)
                    ? throw new InvalidOperationException("Configuration value 'Github:Secret' not found.")
                    : webhookSecret
                : string.Empty;
        });

        // Core services
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<GithubOptions>>().Value;
            return new GitHelper(options, gitApiKey);
        });
        services.TryAddScoped<BugReportService>();
        services.TryAddScoped<IGithubDbContext>(sp => sp.GetRequiredService<TContext>());

        // Webhook services
        services.TryAddScoped<GithubWebhookService>();

        // Repository contexts
        services.RegisterRepositoryContexts(typeof(ReportedIssue), typeof(IssueComment));

        return services;
    }
}