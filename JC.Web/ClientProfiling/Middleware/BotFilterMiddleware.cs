using JC.Web.ClientProfiling.Models;
using JC.Web.ClientProfiling.Models.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace JC.Web.ClientProfiling.Middleware;

/// <summary>
/// Middleware that blocks requests from detected bots based on the <see cref="RequestMetadata"/>
/// stored in <see cref="HttpContext.Items"/> by the <see cref="RequestMetadataMiddleware"/>.
/// Must be registered after <see cref="RequestMetadataMiddleware"/> in the pipeline.
/// Bots listed in <see cref="BotFilterOptions.AllowedBots"/> are permitted through.
/// </summary>
public class BotFilterMiddleware(RequestDelegate next, IOptions<BotFilterOptions> options)
{
    private readonly BotFilterOptions _options = options.Value;

    /// <summary>
    /// Checks whether the current request is from a bot. If the bot is not in the allowed list
    /// and the request path matches the filter, the request is short-circuited with the configured status code.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.IsEnabled)
        {
            await next(context);
            return;
        }
        
        var metadata = context.GetRequestMetadata();

        if (metadata is { UserAgent.IsBot: true })
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Skip filtering if path doesn't match the filter
            if (_options.PathFilter != null && !_options.PathFilter(path))
            {
                await next(context);
                return;
            }

            // Allow bots in the allowed list
            var browser = metadata.UserAgent.Browser;
            if (browser != null && _options.AllowedBots.Contains(browser))
            {
                await next(context);
                return;
            }

            context.Response.StatusCode = (int)_options.StatusCode;
            return;
        }

        await next(context);
    }
}