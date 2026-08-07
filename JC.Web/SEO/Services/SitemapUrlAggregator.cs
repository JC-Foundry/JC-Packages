using JC.Web.SEO.Models;
using JC.Web.SEO.Models.Options;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace JC.Web.SEO.Services;

/// <summary>
/// Collects sitemap URLs from all three sources — discovered routes, registered providers, and
/// explicitly configured entries — then de-duplicates and orders them.
/// </summary>
/// <remarks>
/// <para>
/// Every path into the sitemap runs through <see cref="GetAllAsync"/>, so this is the single place
/// a cache would be introduced if provider cost ever becomes a problem.
/// </para>
/// <para>
/// Ordering is deliberately deterministic. When the sitemap is split into numbered files, the
/// contents of <c>/sitemap-2.xml</c> are decided by position in this list — if the order varied
/// between requests, a crawler fetching the index and then each file would see URLs shift
/// between them and miss some entirely.
/// </para>
/// </remarks>
internal sealed class SitemapUrlAggregator(
    EndpointDataSource endpointDataSource,
    IEnumerable<ISitemapUrlProvider> providers,
    SitemapStartTime startTime,
    IOptions<SitemapOptions> options)
{
    private readonly SitemapOptions _options = options.Value;

    /// <summary>
    /// Builds the complete, ordered, de-duplicated URL list for the sitemap.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>All sitemap URLs, ordered deterministically.</returns>
    public async Task<IReadOnlyList<SitemapUrl>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var collected = new List<SitemapUrl>();

        if (_options.DiscoverRoutes)
            collected.AddRange(DiscoverRoutes());

        collected.AddRange(_options.Urls);

        foreach (var provider in providers)
        {
            var urls = await provider.GetUrlsAsync(cancellationToken);
            collected.AddRange(urls);
        }

        // Later entries win, so a provider or explicit entry can supply a real lastmod for a
        // path discovery already found with only the start-up timestamp.
        var deduplicated = new Dictionary<string, SitemapUrl>(StringComparer.OrdinalIgnoreCase);
        foreach (var url in collected.Where(u => !string.IsNullOrWhiteSpace(u.Location)))
            deduplicated[url.Location] = url;

        return deduplicated.Values
            .OrderBy(u => u.Location, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Finds indexable routes from the application's endpoints, skipping anything parameterised,
    /// excluded by prefix, marked with <see cref="SitemapIgnoreAttribute"/>, or not reachable by GET.
    /// </summary>
    private IEnumerable<SitemapUrl> DiscoverRoutes()
        => endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Where(IsDiscoverableEndpoint)
            .Where(e => e.Metadata.GetMetadata<SitemapIgnoreAttribute>() is null)
            .Where(IsGettable)
            .Select(e => e.RoutePattern.RawText)
            .Where(IsIndexablePath)
            .Select(path => NormalisePath(path!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => new SitemapUrl(path) { LastModified = startTime.StartedUtc });

    private bool IsDiscoverableEndpoint(RouteEndpoint endpoint)
    {
        if (endpoint.Metadata.GetMetadata<PageActionDescriptor>() is not null)
            return true;

        return _options.DiscoverControllers
               && endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is not null;
    }

    /// <summary>
    /// An endpoint with no declared methods is reachable by any verb, so absence of the metadata
    /// counts as GET.
    /// </summary>
    private static bool IsGettable(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        return methods is null || methods.HttpMethods.Contains("GET", StringComparer.OrdinalIgnoreCase);
    }

    private bool IsIndexablePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // A parameterised route cannot be resolved to a real URL here — those belong to a provider.
        if (path.Contains('{'))
            return false;

        var rooted = path.StartsWith('/') ? path : "/" + path;
        return !_options.ExcludePrefixes.Any(prefix =>
            rooted.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Collapses the Razor Pages convention where <c>/Index</c> and its parent directory address the
    /// same page. Emitting both would hand crawlers two URLs for identical content.
    /// </summary>
    internal static string NormalisePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        if (!path.StartsWith('/'))
            path = "/" + path;

        if (path.Equals("/Index", StringComparison.OrdinalIgnoreCase))
            return "/";

        if (path.EndsWith("/Index", StringComparison.OrdinalIgnoreCase))
            return path[..^"/Index".Length];

        return path;
    }
}
