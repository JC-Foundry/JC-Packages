using System.Globalization;
using JC.Web.SEO.Models.Options;
using JC.Web.SEO.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JC.Web.SEO.Middleware;

/// <summary>
/// Serves the sitemap at the configured path, splitting into numbered files behind an index once
/// the URL count exceeds <see cref="SitemapOptions.MaxUrlsPerFile"/>.
/// </summary>
/// <remarks>
/// Register this <b>before</b> <see cref="ClientProfiling.Middleware.BotFilterMiddleware"/>. The bot
/// filter blocks every detected crawler by default, and a sitemap only has an audience of crawlers —
/// registered the other way round, the search engines this exists for receive a 403.
/// </remarks>
public class SitemapMiddleware(RequestDelegate next, IOptions<SitemapOptions> options)
{
    private readonly SitemapOptions _options = options.Value;

    /// <summary>
    /// Serves the sitemap when the request path matches, otherwise passes the request along.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        if (!HttpMethods.IsGet(context.Request.Method) || string.IsNullOrEmpty(path))
        {
            await next(context);
            return;
        }

        var chunkIndex = ResolveChunkIndex(path);
        var isRoot = string.Equals(path, _options.Path, StringComparison.OrdinalIgnoreCase);

        if (!isRoot && chunkIndex is null)
        {
            await next(context);
            return;
        }

        // Resolved here rather than injected into InvokeAsync so these stay internal — they are
        // implementation detail, not public surface. Only requests that actually hit the sitemap
        // pay for the lookup.
        var aggregator = context.RequestServices.GetRequiredService<SitemapUrlAggregator>();
        var startTime = context.RequestServices.GetRequiredService<SitemapStartTime>();

        var urls = await aggregator.GetAllAsync(context.RequestAborted);
        var baseUrl = ResolveBaseUrl(context);
        var maxPerFile = Math.Max(1, _options.MaxUrlsPerFile);
        var fileCount = (int)Math.Ceiling(urls.Count / (double)maxPerFile);

        string xml;

        if (isRoot)
        {
            // A single file's worth of URLs is served directly; an index pointing at one child
            // would only cost crawlers an extra round trip.
            xml = fileCount <= 1
                ? SitemapXmlWriter.WriteUrlSet(urls, baseUrl)
                : SitemapXmlWriter.WriteIndex(fileCount, baseUrl, _options.Path, startTime.StartedUtc);
        }
        else if (chunkIndex is { } index && index >= 1 && index <= fileCount)
        {
            var chunk = urls.Skip((index - 1) * maxPerFile).Take(maxPerFile);
            xml = SitemapXmlWriter.WriteUrlSet(chunk, baseUrl);
        }
        else
        {
            // A numbered file outside the current range — the sitemap shrank since it was indexed.
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await WriteResponse(context, xml);
    }

    private async Task WriteResponse(HttpContext context, string xml)
    {
        context.Response.ContentType = "application/xml; charset=utf-8";

        if (_options.ClientCacheDuration > TimeSpan.Zero)
            context.Response.Headers.CacheControl =
                $"public, max-age={(int)_options.ClientCacheDuration.TotalSeconds}";

        await context.Response.WriteAsync(xml, context.RequestAborted);
    }

    /// <summary>
    /// Returns the 1-based file number when the path is a numbered sitemap, otherwise null.
    /// </summary>
    private int? ResolveChunkIndex(string path)
    {
        var extension = Path.GetExtension(_options.Path);
        var prefix = string.IsNullOrEmpty(extension)
            ? _options.Path + "-"
            : _options.Path[..^extension.Length] + "-";

        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            return null;

        var number = path[prefix.Length..^extension.Length];

        return int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var index)
            ? index
            : null;
    }

    /// <summary>
    /// Prefers the configured base URL. The request host is a fallback only — behind a reverse
    /// proxy it reports the internal host unless forwarded headers are configured, which would
    /// fill the sitemap with URLs no crawler can reach.
    /// </summary>
    private string ResolveBaseUrl(HttpContext context)
        => !string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? _options.BaseUrl.TrimEnd('/')
            : $"{context.Request.Scheme}://{context.Request.Host}";
}
