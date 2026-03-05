using JC.Github.Models.Options;
using JC.Github.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JC.Github.Extensions;

/// <summary>
/// Extension methods for <see cref="IEndpointRouteBuilder"/> providing GitHub webhook endpoint registration.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Maps the GitHub webhook endpoint if webhooks are enabled in <see cref="GithubOptions"/>.
    /// </summary>
    /// <param name="app">The endpoint route builder (typically <c>WebApplication</c>).</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder UseGithubWebhooks(this IEndpointRouteBuilder app)
    {
        var options = app.ServiceProvider.GetRequiredService<IOptions<GithubOptions>>().Value;

        if (!options.EnableWebhooks) return app;

        app.MapPost(options.WebhookPath, (Delegate)GithubWebhookEndpoint.HandleAsync)
            .ExcludeFromDescription();

        return app;
    }
}