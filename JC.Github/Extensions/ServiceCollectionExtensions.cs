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
    /// <param name="configuration">The application configuration, used to resolve GitHub API settings.</param>
    /// <param name="configure">Optional callback to configure <see cref="GithubOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if required GitHub configuration values are missing.</exception>
    public static IServiceCollection AddGithub<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<GithubOptions>? configure = null)
        where TContext : DbContext, IGithubDbContext
    {
        var gitUrl = configuration["Github:Url"] ?? throw new InvalidOperationException("Configuration value 'Github:Url' not found.");
        var gitApiKey = configuration["Github:ApiKey"] ?? throw new InvalidOperationException("Configuration value 'Github:ApiKey' not found.");

        // Configure options
        var optionsBuilder = services.AddOptions<GithubOptions>();

        optionsBuilder.Configure(options =>
        {
            options.WebhookSecret = configuration["Github:Secret"] ?? throw new InvalidOperationException("Configuration value 'Github:Secret' not found.");
        });

        if (configure is not null)
            optionsBuilder.PostConfigure(configure);

        // Core services
        services.TryAddSingleton(new GitHelper(gitUrl, gitApiKey));
        services.TryAddScoped<BugReportService>();
        services.TryAddScoped<IGithubDbContext>(sp => sp.GetRequiredService<TContext>());

        // Webhook services
        services.TryAddScoped<GithubWebhookService>();

        // Repository contexts
        services.RegisterRepositoryContexts(typeof(ReportedIssue), typeof(IssueComment));

        return services;
    }
}