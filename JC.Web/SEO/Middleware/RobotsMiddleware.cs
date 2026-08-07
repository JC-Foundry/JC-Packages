using JC.Web.SEO.Models.Options;
using JC.Web.SEO.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace JC.Web.SEO.Middleware;

/// <summary>
/// Serves robots.txt at the configured path.
/// </summary>
/// <remarks>
/// Register this <b>before</b> <see cref="ClientProfiling.Middleware.BotFilterMiddleware"/>, for the
/// same reason as the sitemap — a robots.txt that crawlers are blocked from reading tells them
/// nothing, and some treat the failure as permission to crawl everything.
/// </remarks>
public class RobotsMiddleware(
    RequestDelegate next,
    IOptions<RobotsOptions> options,
    IOptions<SitemapOptions> sitemapOptions)
{
    private readonly RobotsOptions _options = options.Value;
    private readonly SitemapOptions _sitemapOptions = sitemapOptions.Value;

    /// <summary>
    /// Serves robots.txt when the request path matches, otherwise passes the request along.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        if (!HttpMethods.IsGet(context.Request.Method)
            || !string.Equals(path, _options.Path, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var content = RobotsTxtWriter.Write(_options, ResolveSitemapUrl(context));

        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync(content, context.RequestAborted);
    }

    /// <summary>
    /// Resolves the sitemap URL to advertise, or null when there is nothing to advertise.
    /// </summary>
    private string? ResolveSitemapUrl(HttpContext context)
    {
        if (!string.IsNullOrWhiteSpace(_options.SitemapUrl))
            return _options.SitemapUrl;

        if (!_options.IncludeSitemapDirective)
            return null;

        // SitemapMarker is only registered by AddSitemap, so its absence means nothing is serving
        // a sitemap and the directive would point at a 404.
        if (context.RequestServices.GetService(typeof(SitemapMarker)) is null)
            return null;

        var baseUrl = !string.IsNullOrWhiteSpace(_sitemapOptions.BaseUrl)
            ? _sitemapOptions.BaseUrl.TrimEnd('/')
            : $"{context.Request.Scheme}://{context.Request.Host}";

        return $"{baseUrl}/{_sitemapOptions.Path.TrimStart('/')}";
    }
}

/// <summary>
/// Marker registered by <c>AddSitemap</c>. Lets robots.txt tell whether a sitemap is actually
/// being served without taking a hard dependency on the sitemap registration.
/// </summary>
internal sealed class SitemapMarker;
